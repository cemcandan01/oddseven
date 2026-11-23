using System;
using System.Collections.Generic;
using System.Linq;
using WeirdLand.Core.Data;
using WeirdLand.Core.Duels;

namespace WeirdLand.Core.Match
{
    public enum GridPhase { AwaitingAction, InDuel, Ended }

    public sealed class GridMatch
    {
        private readonly GameData _data;
        private readonly Rng _rng;
        private readonly TeamDef _opponentDef;
        private readonly List<GridPlayer> _players = new List<GridPlayer>();
        private readonly List<string> _stunnedThisMove = new List<string>();

        public IReadOnlyList<GridPlayer> Players => _players;
        public GridPlayer Carrier { get; private set; }
        public bool UserHasBall { get; private set; }
        public GridPhase Phase { get; private set; } = GridPhase.AwaitingAction;
        public int Move { get; private set; }
        public int UserScore { get; private set; }
        public int OppScore { get; private set; }
        public Duel CurrentDuel { get; private set; }

        public GridMatch(GameData data, TeamDef user, TeamDef opponent, uint seed)
        {
            _data = data;
            _opponentDef = opponent;
            _rng = new Rng(seed);
            AddTeam(user, true);
            AddTeam(opponent, false);
        }

        private void AddTeam(TeamDef team, bool isUser)
        {
            foreach (Slot s in Enum.GetValues(typeof(Slot)))
            {
                var def = team.BySlot(s);
                if (def == null) continue;
                _players.Add(new GridPlayer(def, isUser, GridGeometry.Home(s, isUser),
                    DuelSetupHelpers.BuildDeck(_data, def)));
            }
        }

        public IReadOnlyList<GameEvent> Kickoff()
        {
            var ev = new List<GameEvent>();
            Restart(_rng.Chance(0.5), ev);
            return ev;
        }

        // Everyone home, stuns cleared, CM of the team with the ball carries.
        private void Restart(bool userHasBall, List<GameEvent> ev)
        {
            foreach (var p in _players)
            {
                p.Pos = GridGeometry.Home(p.Def.Slot, p.IsUser);
                p.Stun = 0;
            }
            _stunnedThisMove.Clear();
            UserHasBall = userHasBall;
            Carrier = _players.Find(p => p.IsUser == UserHasBall && p.Def.Slot == Slot.CM);
            Phase = GridPhase.AwaitingAction;
            ev.Add(new GridKickoff(UserHasBall, Carrier.Def.Id));
        }

        public GridPlayer PlayerAt(Cell c)
        {
            foreach (var p in _players) if (p.Pos.Equals(c)) return p;
            return null;
        }

        public IReadOnlyList<Cell> ReachableCells()
        {
            var result = new List<Cell>();
            if (Carrier == null) return result;
            var from = Carrier.Pos;
            bool fast = Carrier.Def.Stats.Speed >= MatchRules.FastSpeed;
            int reach = fast ? 2 : 1;
            for (int x = from.X - reach; x <= from.X + reach; x++)
                for (int y = from.Y - reach; y <= from.Y + reach; y++)
                {
                    var c = new Cell(x, y);
                    if (!GridGeometry.InBounds(c) || PlayerAt(c) != null) continue;
                    int d = GridGeometry.Distance(from, c);
                    if (d == 1 || (d == 2 && HasEmptyIntermediate(from, c))) result.Add(c);
                }
            return result; // loops iterate x then y, so already sorted
        }

        private bool HasEmptyIntermediate(Cell from, Cell to)
        {
            for (int x = from.X - 1; x <= from.X + 1; x++)
                for (int y = from.Y - 1; y <= from.Y + 1; y++)
                {
                    var m = new Cell(x, y);
                    if (GridGeometry.InBounds(m) && GridGeometry.Adjacent(from, m)
                        && GridGeometry.Adjacent(m, to) && PlayerAt(m) == null) return true;
                }
            return false;
        }

        public IReadOnlyList<GameEvent> Advance(Cell to)
        {
            if (Phase != GridPhase.AwaitingAction)
                throw new InvalidOperationException("Advance requires Phase == AwaitingAction, was " + Phase);
            if (!ReachableCells().Contains(to))
                throw new ArgumentException("Cell " + to + " is not reachable", nameof(to));
            var ev = new List<GameEvent>();
            var from = Carrier.Pos;
            Carrier.Pos = to;
            ev.Add(new PlayerMoved(Carrier.Def.Id, from.X, from.Y, to.X, to.Y));
            RunMoveTail(ev);
            return ev;
        }

        internal Func<double, bool> PassRollOverride; // test hook, null in production

        public IReadOnlyList<GridPlayer> PassTargets()
        {
            var r = new List<GridPlayer>();
            if (Carrier == null) return r;
            foreach (var p in _players)
                if (p.IsUser == Carrier.IsUser && p != Carrier && p.Def.Slot != Slot.GK) r.Add(p);
            return r;
        }

        // Visual hint only; the chance itself is never exposed.
        public bool PassLineContested(string targetId)
            => GridPass.Interceptors(this, Carrier, FindPassTarget(targetId)).Count > 0;

        private GridPlayer FindPassTarget(string targetId)
        {
            foreach (var p in PassTargets()) if (p.Def.Id == targetId) return p;
            throw new ArgumentException("Invalid pass target " + targetId, nameof(targetId));
        }

        public IReadOnlyList<GameEvent> Pass(string targetId)
        {
            if (Phase != GridPhase.AwaitingAction)
                throw new InvalidOperationException("Pass requires Phase == AwaitingAction, was " + Phase);
            var to = FindPassTarget(targetId);
            var from = Carrier;
            double chance = GridPass.Chance(this, from, to);
            bool ok = PassRollOverride != null ? PassRollOverride(chance) : _rng.Chance(chance);
            var ev = new List<GameEvent>();
            if (ok)
            {
                ev.Add(new BallPassed(from.Def.Id, to.Def.Id));
                Carrier = to;
            }
            else
            {
                var thief = GridPass.PickInterceptor(this, from, to);
                ev.Add(new PassIntercepted(from.Def.Id, to.Def.Id, thief.Def.Id));
                Carrier = thief;
                UserHasBall = thief.IsUser;
                ev.Add(new PossessionChanged(UserHasBall, thief.Def.Id));
            }
            RunMoveTail(ev);
            return ev;
        }

        // Move queue shared by Advance and (later) Pass: movement, duel check, then EndOfMove.
        private void RunMoveTail(List<GameEvent> ev)
        {
            GridMovement.Step(this, ev);
            if (TryOpenDuel(ev)) return;
            EndOfMove(ev);
        }

        // End of a move: shot zone check, stun tick, move clock, match end.
        private void EndOfMove(List<GameEvent> ev)
        {
            if (GridGeometry.InShotZone(Carrier.Pos, UserHasBall))
            {
                ev.Add(new ShotChance(UserHasBall, Carrier.Def.Id));
                if (UserHasBall) UserScore++; else OppScore++;
                Restart(!UserHasBall, ev);
            }
            foreach (var p in _players)
                if (p.Stun > 0 && !_stunnedThisMove.Contains(p.Def.Id)) p.Stun--;
            _stunnedThisMove.Clear();
            Move++;
            ev.Add(new MatchMoveAdvanced(Move));
            if (Move >= MatchRules.GridMatchMoves)
            {
                ev.Add(new MatchEnded(UserScore, OppScore));
                Phase = GridPhase.Ended;
            }
            else Phase = GridPhase.AwaitingAction;
        }

        private bool TryOpenDuel(List<GameEvent> ev)
        {
            GridPlayer defender = null;
            int best = int.MaxValue;
            foreach (var p in _players) // Slot order within each team; strict < keeps first on ties
            {
                if (p.IsUser == Carrier.IsUser || p.Def.Slot == Slot.GK || p.Stun > 0) continue;
                if (!GridGeometry.Adjacent(p.Pos, Carrier.Pos)) continue;
                int d = GridGeometry.Distance(p.Pos, Carrier.Pos);
                if (d < best) { best = d; defender = p; }
            }
            if (defender == null) return false;

            ev.Add(new DuelTriggered(Carrier.Def.Id, defender.Def.Id));
            var userP = Carrier.IsUser ? Carrier : defender;
            var oppP = Carrier.IsUser ? defender : Carrier;
            var setup = new DuelSetup
            {
                Kind = DuelKind.Field,
                UserIsAttacker = UserHasBall,
                EnemyPowerScalePercent = MatchRules.PowerScalePercent(_opponentDef.Tier),
                UserPlayer = userP.Def,
                EnemyPlayer = oppP.Def,
                UserDeck = userP.Deck,
                EnemyDeck = oppP.Deck,
            };
            CurrentDuel = new Duel(setup, _rng.Fork());
            Phase = GridPhase.InDuel;
            ev.AddRange(CurrentDuel.Begin());
            return true;
        }

        public IReadOnlyList<GameEvent> CompleteDuel()
        {
            var d = CurrentDuel;
            if (Phase != GridPhase.InDuel || d == null || !d.Outcome.HasValue)
                throw new InvalidOperationException("No finished duel to complete.");
            var outcome = d.Outcome.Value;
            if (outcome != DuelOutcome.AttackerWon && outcome != DuelOutcome.DefenderWon)
                throw new InvalidOperationException("Unexpected grid duel outcome " + outcome);
            var ev = new List<GameEvent>();
            var oldCarrier = Carrier;
            var defender = _players.Find(p => p.Def.Id == (UserHasBall ? d.Enemy.Player.Id : d.User.Player.Id));
            if (outcome == DuelOutcome.AttackerWon)
            {
                Stun(defender, ev);
            }
            else
            {
                Stun(oldCarrier, ev);
                Carrier = defender;
                UserHasBall = defender.IsUser;
                ev.Add(new PossessionChanged(UserHasBall, defender.Def.Id));
                if (TryOpenDuel(ev)) return ev;
            }
            EndOfMove(ev);
            return ev;
        }

        private void Stun(GridPlayer p, List<GameEvent> ev)
        {
            p.Stun = MatchRules.StunMoves;
            _stunnedThisMove.Add(p.Def.Id);
            ev.Add(new PlayerStunned(p.Def.Id, MatchRules.StunMoves));
        }

        internal void SetPos(string playerId, Cell c) => _players.Find(p => p.Def.Id == playerId).Pos = c;

        internal void SetCarrier(string playerId)
        {
            Carrier = _players.Find(p => p.Def.Id == playerId);
            UserHasBall = Carrier.IsUser;
        }

        internal void SetPhase(GridPhase phase) => Phase = phase;

        internal void SetMove(int move) => Move = move;
    }
}
