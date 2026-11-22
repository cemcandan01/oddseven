using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using WeirdLand.Core.Data;
using WeirdLand.Core.Duels;

[assembly: InternalsVisibleTo("WeirdLand.Core.Tests")]

namespace WeirdLand.Core.Match
{
    public class MatchEngine
    {
        private readonly GameData _data;
        private readonly Rng _rng;
        private readonly TeamDef _opponentDef;

        // chain state
        private bool _chainActive;
        private bool _userAttacks;
        private PlayerState _carrier;
        private int _carriedMomentum;
        private bool _halfTimeDone;

        public TeamState User { get; }
        public TeamState Opponent { get; }
        public MatchPhase Phase { get; private set; } = MatchPhase.BeforeKickoff;
        public int Ustunluk { get; internal set; }
        public int MomentsTotal { get; }
        public int MomentsPlayed { get; private set; }
        public int Minute { get; private set; }
        public ChainStage Stage { get; private set; }
        public Duel CurrentDuel { get; private set; }

        // Test hooks.
        internal int? ForcedUstunluk;
        internal bool? ForcedUserAttacks;
        internal Lane? ForcedLane;

        public MatchEngine(GameData data, TeamDef user, TeamDef opponent, uint seed)
        {
            _data = data;
            _opponentDef = opponent;
            _rng = new Rng(seed);
            MomentsTotal = MatchRules.MomentsMin + _rng.Next(MatchRules.MomentsRange);
            User = Build(user);
            Opponent = Build(opponent);
        }

        private TeamState Build(TeamDef def)
        {
            var t = new TeamState { Def = def };
            foreach (var p in def.Players)
            {
                var ps = new PlayerState { Def = p };
                ps.Deck.AddRange(DuelSetupHelpers.BuildDeck(_data, p));
                t.Players.Add(ps);
            }
            return t;
        }

        public void SetTactic(bool user, Tactic t)
        {
            if (Phase != MatchPhase.BeforeKickoff && Phase != MatchPhase.HalfTime)
                throw new InvalidOperationException("Tactic can only change before kickoff or at half time.");
            (user ? User : Opponent).Tactic = t;
        }

        public IReadOnlyList<GameEvent> StartNextDuel()
        {
            if (Phase != MatchPhase.BeforeKickoff && Phase != MatchPhase.AwaitingDuel && Phase != MatchPhase.HalfTime)
                throw new InvalidOperationException("Cannot start a duel in phase " + Phase);
            var ev = new List<GameEvent>();
            if (!_chainActive) StartMoment(ev);
            StartStep(ev);
            return ev;
        }

        private void StartMoment(List<GameEvent> ev)
        {
            Ustunluk = ForcedUstunluk ?? MatchRules.NextUstunluk(Ustunluk,
                MatchRules.Mid(User.BySlot(Slot.CM).Def), MatchRules.Mid(Opponent.BySlot(Slot.CM).Def),
                User.HypeSum, Opponent.HypeSum, User.Tactic, Opponent.Tactic, _rng.NextDouble());
            _userAttacks = ForcedUserAttacks ?? _rng.Chance(MatchRules.UserAttackChance(Ustunluk));
            var lane = ForcedLane ?? (Lane)_rng.Next(3);
            var atk = _userAttacks ? User : Opponent;
            _carrier = atk.BySlot(lane == Lane.Left ? Slot.LW : lane == Lane.Right ? Slot.RW : Slot.CM);
            Stage = ChainStage.Midfield;
            _carriedMomentum = 0;
            _chainActive = true;
            MomentsPlayed++;
            ev.Add(new MomentStarted(MomentsPlayed, _userAttacks, lane));
        }

        private PlayerState DefenderFor(TeamState def, Lane lane)
        {
            switch (Stage)
            {
                case ChainStage.Shot: return def.BySlot(Slot.GK);
                case ChainStage.Midfield:
                    return def.BySlot(lane == Lane.Center ? Slot.CM : lane == Lane.Left ? Slot.RW : Slot.LW);
                default:
                    if (lane == Lane.Left) return def.BySlot(Slot.RB);
                    if (lane == Lane.Right) return def.BySlot(Slot.LB);
                    var lb = def.BySlot(Slot.LB);
                    var rb = def.BySlot(Slot.RB);
                    return rb.Def.Stats.Defending > lb.Def.Stats.Defending ? rb : lb;
            }
        }

        private void StartStep(List<GameEvent> ev)
        {
            var atk = _userAttacks ? User : Opponent;
            var def = _userAttacks ? Opponent : User;
            var lane = SlotInfo.LaneOf(_carrier.Def.Slot);
            var defender = DefenderFor(def, lane);

            var setup = new DuelSetup
            {
                Kind = Stage == ChainStage.Shot ? DuelKind.Shot : DuelKind.Field,
                UserIsAttacker = _userAttacks,
                EnemyPowerScalePercent = MatchRules.PowerScalePercent(_opponentDef.Tier),
            };
            PlayerState userP = _userAttacks ? _carrier : defender;
            PlayerState oppP = _userAttacks ? defender : _carrier;
            setup.UserPlayer = userP.Def;
            setup.EnemyPlayer = oppP.Def;
            setup.UserDeck = userP.Deck;
            setup.EnemyDeck = oppP.Deck;
            if (userP.Kondisyon < MatchRules.LowKondisyon) setup.UserEnergyPenalty = 1;
            if (userP.Alev) setup.UserEnergyBonusTurn1 = 1;
            if (oppP.Alev) setup.EnemyMomentum += MatchRules.EnemyAlevMomentum;
            if (_userAttacks) setup.UserMomentum += _carriedMomentum; else setup.EnemyMomentum += _carriedMomentum;
            if (def.Tactic == Tactic.Defensive) setup.DefenderMaxHpBonus = MatchRules.DefensiveHpBonus;
            var targets = _userAttacks ? setup.UserPassTargets : setup.EnemyPassTargets;
            DuelSetupHelpers.AddPassTargets(targets, atk.Players.Select(p => p.Def), _carrier.Def);

            CurrentDuel = new Duel(setup, _rng.Fork());
            Phase = MatchPhase.InDuel;
            ev.AddRange(CurrentDuel.Begin());
        }

        public IReadOnlyList<GameEvent> CompleteDuel()
        {
            var d = CurrentDuel;
            if (Phase != MatchPhase.InDuel || d == null || !d.Outcome.HasValue)
                throw new InvalidOperationException("No finished duel to complete.");
            var ev = new List<GameEvent>();
            var outcome = d.Outcome.Value;
            var userP = User.ById(d.User.Player.Id);
            var oppP = Opponent.ById(d.Enemy.Player.Id);

            // Kondisyon
            DrainKondisyon(userP, d.User, ev);
            DrainKondisyon(oppP, d.Enemy, ev);

            // Winner, hype, Ustunluk
            bool attackerWon = outcome == DuelOutcome.AttackerWon || outcome == DuelOutcome.PassSucceeded;
            bool winnerIsUser = attackerWon == _userAttacks;
            if (outcome != DuelOutcome.PassSucceeded)
            {
                var wp = winnerIsUser ? userP : oppP;
                var lp = winnerIsUser ? oppP : userP;
                var wc = winnerIsUser ? d.User : d.Enemy;
                GiveWinHype(wp, lp, wc, ev);
            }
            Ustunluk = MatchRules.Clamp(Ustunluk + (winnerIsUser ? MatchRules.UstunlukStep : -MatchRules.UstunlukStep));

            var atk = _userAttacks ? User : Opponent;
            bool chainEnds = true;

            if (attackerWon)
            {
                if (Stage == ChainStage.Shot)
                {
                    atk.Score++;
                    ev.Add(new GoalScored(_userAttacks, _carrier.Def.Id));
                    foreach (var p in atk.Players) AddHype(p, 1, ev);
                }
                else
                {
                    var carrierCombatant = _userAttacks ? d.User : d.Enemy;
                    _carriedMomentum = carrierCombatant.StatusOf(StatusId.Momentum);
                    if (outcome == DuelOutcome.PassSucceeded && d.PassTarget != null)
                        _carrier = atk.ById(d.PassTarget.Id);
                    Stage = Stage + 1;
                    chainEnds = false;
                }
            }
            else if (atk.Tactic == Tactic.Attacking && Stage != ChainStage.Shot && MomentsPlayed < MomentsTotal)
            {
                // Counter: the winning defender carries from the final third for the other team.
                var winner = _userAttacks ? oppP : userP;
                _userAttacks = !_userAttacks;
                _carrier = winner;
                Stage = ChainStage.FinalThird;
                _carriedMomentum = 0;
                MomentsPlayed++;
                ev.Add(new MomentStarted(MomentsPlayed, _userAttacks, SlotInfo.LaneOf(winner.Def.Slot)));
                chainEnds = false;
            }

            Minute = MatchRules.MatchMinutes * MomentsPlayed / MomentsTotal;
            if (chainEnds)
            {
                _chainActive = false;
                _carriedMomentum = 0;
                if (MomentsPlayed >= MomentsTotal)
                {
                    Phase = MatchPhase.Ended;
                    ev.Add(new MatchEnded(User.Score, Opponent.Score));
                }
                else if (!_halfTimeDone && MomentsPlayed >= MomentsTotal / 2)
                {
                    _halfTimeDone = true;
                    Phase = MatchPhase.HalfTime;
                    ev.Add(new HalfTime());
                }
                else Phase = MatchPhase.AwaitingDuel;
            }
            else Phase = MatchPhase.AwaitingDuel;
            return ev;
        }

        private static void DrainKondisyon(PlayerState p, Combatant c, List<GameEvent> ev)
        {
            p.Kondisyon = Math.Max(0, p.Kondisyon - MatchRules.KondisyonPerDuel - c.KondisyonSpent);
            ev.Add(new KondisyonChanged(p.Def.Id, p.Kondisyon));
        }

        private static void GiveWinHype(PlayerState w, PlayerState l, Combatant wc, List<GameEvent> ev)
        {
            if (l.Alev)
            {
                l.Alev = false;
                l.Hype = 0;
                ev.Add(new HypeChanged(l.Def.Id, l.Hype, l.Alev));
                w.Alev = true;
                w.Hype = MatchRules.HypeMax;
                ev.Add(new HypeChanged(w.Def.Id, w.Hype, w.Alev));
                return;
            }
            int gain = 1;
            if (wc.Hp * 100 <= MatchRules.LowHpPercent * wc.MaxHp) gain++;
            AddHype(w, gain, ev);
        }

        private static void AddHype(PlayerState p, int n, List<GameEvent> ev)
        {
            int before = p.Hype;
            bool alevBefore = p.Alev;
            p.Hype = Math.Min(MatchRules.HypeMax, p.Hype + n);
            if (p.Hype >= MatchRules.HypeMax) p.Alev = true;
            if (p.Hype != before || p.Alev != alevBefore) ev.Add(new HypeChanged(p.Def.Id, p.Hype, p.Alev));
        }
    }
}
