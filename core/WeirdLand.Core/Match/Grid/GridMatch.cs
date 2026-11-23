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
        private readonly List<GridPlayer> _players = new List<GridPlayer>();

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
            foreach (var p in _players) p.Pos = GridGeometry.Home(p.Def.Slot, p.IsUser);
            UserHasBall = _rng.Chance(0.5);
            Carrier = _players.Find(p => p.IsUser == UserHasBall && p.Def.Slot == Slot.CM);
            Phase = GridPhase.AwaitingAction;
            return new List<GameEvent> { new GridKickoff(UserHasBall, Carrier.Def.Id) };
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

        // Move queue shared by Advance and (later) Pass. Later steps: teammate/opponent movement,
        // duel check, shot zone and move clock.
        private void RunMoveTail(List<GameEvent> ev)
        {
        }

        internal void SetPos(string playerId, Cell c) => _players.Find(p => p.Def.Id == playerId).Pos = c;

        internal void SetCarrier(string playerId)
        {
            Carrier = _players.Find(p => p.Def.Id == playerId);
            UserHasBall = Carrier.IsUser;
        }

        internal void SetPhase(GridPhase phase) => Phase = phase;
    }
}
