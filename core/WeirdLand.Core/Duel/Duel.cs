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
        public PlayerDef PassTarget => null;
        public IReadOnlyList<PlayerDef> UserPassTargets => _setup.UserPassTargets;
        public bool CanPass => false;

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

        public double PassChance() => 0;

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

        public IReadOnlyList<GameEvent> Pass(string targetId) => throw new NotImplementedException();

        public IReadOnlyList<GameEvent> EndTurn()
        {
            Guard();
            _ev = new List<GameEvent>();
            User.Discard.AddRange(User.Hand);
            User.Hand.Clear();
            Enemy.Block = 0;

            var card = Intent == null ? null : Intent.Card;
            if (card != null)
            {
                _ev.Add(new CardPlayed(false, card.Def.Id));
                foreach (var e in card.Effects)
                {
                    _ev.AddRange(EffectResolver.Apply(e, Enemy, User, Kind, DrawEvent));
                    if (CheckOutcome()) break;
                }
                Enemy.Discard.Add(card);
                Enemy.Discard.AddRange(Enemy.Hand);
                Enemy.Hand.Clear();
            }
            Intent = null;
            if (Outcome.HasValue) return _ev;

            Tick(User);
            Tick(Enemy);

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
            User.Block = 0;
            int energy = DuelRules.EnergyPerTurn + (Turn == 1 ? _setup.UserEnergyBonusTurn1 : 0) - _setup.UserEnergyPenalty;
            User.Energy = Math.Max(0, energy);
            _ev.Add(new EnergyChanged(true, User.Energy));
            DrawEvent(User, DuelRules.HandSize);
            Intent = EnemyAi.ChooseIntent(this, _rng);
            _ev.Add(new IntentRevealed(Intent.Card?.Def.Id, Intent.PassTarget?.Id));
        }

        private static void Tick(Combatant c)
        {
            foreach (var s in Ticking)
            {
                int n = c.StatusOf(s);
                if (n <= 0) continue;
                if (n == 1) c.Statuses.Remove(s); else c.Statuses[s] = n - 1;
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
