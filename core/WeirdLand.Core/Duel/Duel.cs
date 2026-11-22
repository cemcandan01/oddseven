using System;
using System.Collections.Generic;
using WeirdLand.Core.Data;

namespace WeirdLand.Core.Duels
{
    public class Duel
    {
        private static readonly StatusId[] Ticking =
            { StatusId.Unbalanced, StatusId.Tired, StatusId.CoverNear, StatusId.CoverFar };

        private readonly DuelSetup _setup;
        private readonly Rng _rng;
        private List<GameEvent> _ev;
        private bool _begun;

        public DuelKind Kind { get; }
        public bool UserIsAttacker { get; }
        public Combatant User { get; }
        public Combatant Enemy { get; }
        public int Turn { get; private set; }
        public int MaxTurns { get; }
        public EnemyIntent Intent { get; private set; }
        public DuelOutcome? Outcome { get; private set; }
        public PlayerDef PassTarget { get; private set; }
        public IReadOnlyList<PlayerDef> EnemyPassTargets => _setup.EnemyPassTargets;
        public IReadOnlyList<PlayerDef> UserPassTargets => _setup.UserPassTargets;
        public bool CanPass => Kind == DuelKind.Field && UserIsAttacker && _begun && !Outcome.HasValue
            && User.Energy >= DuelRules.PassCost && UserPassTargets.Count > 0;

        public Duel(DuelSetup setup, Rng rng)
        {
            _setup = setup;
            _rng = rng;
            Kind = setup.Kind;
            UserIsAttacker = setup.UserIsAttacker;
            MaxTurns = DuelRules.MaxTurns(Kind);
            User = new Combatant { Player = setup.UserPlayer, IsUser = true };
            Enemy = new Combatant { Player = setup.EnemyPlayer, IsUser = false, PowerScalePercent = setup.EnemyPowerScalePercent };
        }

        public double PassChance() => DuelRules.PassChance(User, Enemy);

        public int IncomingDamage()
        {
            if (Intent == null || Intent.Card == null) return 0;
            return EnemyAi.Damage(Intent.Card, Enemy, User, Kind);
        }

        public IReadOnlyList<GameEvent> Begin()
        {
            if (_begun) throw new InvalidOperationException("Duel already begun.");
            _begun = true;
            _ev = new List<GameEvent>();

            Fill(User, _setup.UserDeck, UserIsAttacker);
            Fill(Enemy, _setup.EnemyDeck, !UserIsAttacker);

            bool field = Kind == DuelKind.Field;
            var attacker = UserIsAttacker ? User : Enemy;
            var defender = UserIsAttacker ? Enemy : User;
            attacker.MaxHp = field ? DuelRules.KontrolMax(attacker.Player.Stats) : DuelRules.ShooterKontrolMax(attacker.Player.Stats);
            defender.MaxHp = (field ? DuelRules.DengeMax(defender.Player.Stats) : DuelRules.KaleMax(defender.Player.Stats)) + _setup.DefenderMaxHpBonus;
            User.Hp = User.MaxHp;
            Enemy.Hp = Enemy.MaxHp;

            if (_setup.UserMomentum > 0) User.Statuses[StatusId.Momentum] = _setup.UserMomentum;
            if (_setup.EnemyMomentum > 0) Enemy.Statuses[StatusId.Momentum] = _setup.EnemyMomentum;

            Turn = 1;
            StartUserTurn();
            return _ev;
        }

        public IReadOnlyList<GameEvent> PlayCard(int handIndex)
        {
            Guard();
            if (handIndex < 0 || handIndex >= User.Hand.Count) throw new ArgumentOutOfRangeException(nameof(handIndex));
            var card = User.Hand[handIndex];
            if (card.Cost > User.Energy) throw new InvalidOperationException("Not enough energy.");
            _ev = new List<GameEvent>();
            User.Hand.RemoveAt(handIndex);
            User.Discard.Add(card);
            User.Energy -= card.Cost;
            _ev.Add(new EnergyChanged(true, User.Energy));
            _ev.Add(new CardPlayed(true, card.Def.Id));
            foreach (var e in card.Effects)
            {
                _ev.AddRange(EffectResolver.Apply(e, User, Enemy, Kind, DrawEvent));
                if (CheckOutcome()) break;
            }
            return _ev;
        }

        public IReadOnlyList<GameEvent> Pass(string targetId)
        {
            Guard();
            if (!CanPass) throw new InvalidOperationException("Pass not allowed.");
            PlayerDef target = null;
            foreach (var p in UserPassTargets) if (p.Id == targetId) { target = p; break; }
            if (target == null) throw new InvalidOperationException("Unknown pass target.");
            _ev = new List<GameEvent>();
            User.Energy -= DuelRules.PassCost;
            _ev.Add(new EnergyChanged(true, User.Energy));
            ResolvePass(true, target, PassChance());
            return _ev;
        }

        private void ResolvePass(bool byUser, PlayerDef target, double chance)
        {
            bool ok = _rng.Chance(chance);
            _ev.Add(new PassAttempted(byUser, target.Id, chance, ok));
            if (ok) PassTarget = target;
            Finish(ok ? DuelOutcome.PassSucceeded : DuelOutcome.PassFailed);
        }

        public IReadOnlyList<GameEvent> EndTurn()
        {
            Guard();
            _ev = new List<GameEvent>();
            User.Discard.AddRange(User.Hand);
            User.Hand.Clear();
            ResetBlock(Enemy);

            if (Intent != null && Intent.PassTarget != null)
            {
                var t = Intent.PassTarget;
                double chance = Intent.PassChance;
                Intent = null;
                ResolvePass(false, t, chance);
                return _ev;
            }

            // Only stacks that exist before the enemy acts tick this round; stacks gained
            // during the enemy's action start ticking next round (ruling R14 a).
            var userBefore = TimedSnapshot(User);
            var enemyBefore = TimedSnapshot(Enemy);

            var card = Intent == null ? null : Intent.Card;
            if (card != null)
            {
                _ev.Add(new CardPlayed(false, card.Def.Id));
                foreach (var e in card.Effects)
                {
                    if (AppliesOnReveal(e)) continue; // already applied when the intent was revealed
                    _ev.AddRange(EffectResolver.Apply(e, Enemy, User, Kind, DrawEvent));
                    if (CheckOutcome()) break;
                }
                Enemy.Discard.Add(card);
                Enemy.Discard.AddRange(Enemy.Hand);
                Enemy.Hand.Clear();
            }
            Intent = null;
            if (Outcome.HasValue) return _ev;

            Tick(User, userBefore);
            Tick(Enemy, enemyBefore);

            if (Turn >= MaxTurns)
            {
                Finish(DuelOutcome.DefenderWon);
                return _ev;
            }
            Turn++;
            StartUserTurn();
            return _ev;
        }

        internal static int DrawCards(Combatant c, int n, Rng rng)
        {
            int drawn = 0;
            while (drawn < n)
            {
                if (c.DrawPile.Count == 0)
                {
                    if (c.Discard.Count == 0) break;
                    c.DrawPile.AddRange(c.Discard);
                    c.Discard.Clear();
                    rng.Shuffle(c.DrawPile);
                }
                c.Hand.Add(c.DrawPile[0]);
                c.DrawPile.RemoveAt(0);
                drawn++;
            }
            return drawn;
        }

        private void DrawEvent(Combatant c, int n)
        {
            int got = DrawCards(c, n, _rng);
            if (got > 0) _ev.Add(new CardsDrawn(c.IsUser, got));
        }

        private void Fill(Combatant c, List<CardInstance> deck, bool attacking)
        {
            foreach (var card in deck)
                if (card.Def.UsableWhen(attacking)) c.DrawPile.Add(card);
            _rng.Shuffle(c.DrawPile);
        }

        private void StartUserTurn()
        {
            _ev.Add(new TurnStarted(Turn));
            ResetBlock(User);
            int energy = DuelRules.EnergyPerTurn + (Turn == 1 ? _setup.UserEnergyBonusTurn1 : 0) - _setup.UserEnergyPenalty;
            User.Energy = Math.Max(0, energy);
            _ev.Add(new EnergyChanged(true, User.Energy));
            DrawEvent(User, DuelRules.HandSize);
            Intent = EnemyAi.ChooseIntent(this, _rng);
            _ev.Add(new IntentRevealed(Intent.Card?.Def.Id, Intent.PassTarget?.Id));

            // The keeper's shown lean is the lean in effect while the user shoots (ruling R14 b).
            if (Intent.Card != null)
                foreach (var e in Intent.Card.Effects)
                    if (AppliesOnReveal(e)) _ev.AddRange(EffectResolver.Apply(e, Enemy, User, Kind, DrawEvent));
        }

        /// <summary>Enemy intent effects that take effect on reveal instead of at EndTurn: self Cover statuses.</summary>
        private static bool AppliesOnReveal(EffectDef e)
            => e.Type == EffectType.Status && e.Target == EffectTarget.Self && e.Status.HasValue
               && (e.Status.Value == StatusId.CoverNear || e.Status.Value == StatusId.CoverFar);

        private void ResetBlock(Combatant c)
        {
            if (c.Block <= 0) return;
            c.Block = 0;
            _ev.Add(new BlockReset(c.IsUser));
        }

        private static int[] TimedSnapshot(Combatant c)
        {
            var n = new int[Ticking.Length];
            for (int i = 0; i < Ticking.Length; i++) n[i] = c.StatusOf(Ticking[i]);
            return n;
        }

        private void Tick(Combatant c, int[] before)
        {
            for (int i = 0; i < Ticking.Length; i++)
            {
                var s = Ticking[i];
                int n = c.StatusOf(s);
                if (before[i] <= 0 || n <= 0) continue;
                if (n == 1) c.Statuses.Remove(s); else c.Statuses[s] = n - 1;
                _ev.Add(new StatusChanged(c.IsUser, s, n - 1));
            }
        }

        private bool CheckOutcome()
        {
            if (Outcome.HasValue) return true;
            var attacker = UserIsAttacker ? User : Enemy;
            var defender = UserIsAttacker ? Enemy : User;
            if (attacker.Hp <= 0) Finish(DuelOutcome.DefenderWon);
            else if (defender.Hp <= 0) Finish(DuelOutcome.AttackerWon);
            return Outcome.HasValue;
        }

        /// <summary>Test hook: end the duel immediately with the given outcome.</summary>
        internal void ForceOutcome(DuelOutcome o, PlayerDef passTarget = null)
        {
            Guard();
            _ev = new List<GameEvent>();
            PassTarget = passTarget;
            Finish(o);
        }

        private void Finish(DuelOutcome o)
        {
            Outcome = o;
            _ev.Add(new DuelEnded(o));
        }

        private void Guard()
        {
            if (!_begun) throw new InvalidOperationException("Duel not begun.");
            if (Outcome.HasValue) throw new InvalidOperationException("Duel already ended.");
        }
    }
}
