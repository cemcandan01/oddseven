using WeirdLand.Core.Data;
using WeirdLand.Core.Duels;
using WeirdLand.Core.Match;
using static WeirdLand.Core.Tests.TestKit;

namespace WeirdLand.Core.Tests
{
    public class MatchEngineTests
    {
        private static TeamDef Team(string prefix, int tier = 1)
        {
            var t = new TeamDef { Id = prefix, Name = prefix, Tier = tier, Style = "x" };
            foreach (var slot in System.Enum.GetValues<Slot>())
                t.Players.Add(Player(prefix + slot, slot, null, "c"));
            return t;
        }

        private static GameData Data()
        {
            var cards = new Dictionary<string, CardDef> { ["c"] = Card("c", 1, CardRole.Both, Dmg(1)) };
            return new GameData(cards, new Dictionary<string, TeamDef>());
        }

        private static MatchEngine Engine(uint seed = 1, bool userAttacks = true, Lane lane = Lane.Center, int tier = 1)
        {
            var e = new MatchEngine(Data(), Team("u"), Team("o", tier), seed);
            e.ForcedUserAttacks = userAttacks;
            e.ForcedLane = lane;
            e.ForcedUstunluk = 0;
            return e;
        }

        private static IReadOnlyList<GameEvent> Step(MatchEngine e, DuelOutcome o, PlayerDef? target = null)
        {
            e.StartNextDuel();
            e.CurrentDuel.ForceOutcome(o, target);
            return e.CompleteDuel();
        }

        [Test]
        public void MomentsTotal_Between8And12()
        {
            for (uint s = 1; s <= 200; s++)
            {
                var e = new MatchEngine(Data(), Team("u"), Team("o"), s);
                Assert.That(e.MomentsTotal, Is.InRange(8, 12));
            }
        }

        [Test]
        public void Ustunluk_StaysInRange()
        {
            for (uint s = 1; s <= 30; s++)
            {
                var e = new MatchEngine(Data(), Team("u"), Team("o"), s);
                int i = 0;
                while (e.Phase != MatchPhase.Ended)
                {
                    e.StartNextDuel();
                    e.CurrentDuel.ForceOutcome(i++ % 2 == 0 ? DuelOutcome.AttackerWon : DuelOutcome.DefenderWon);
                    e.CompleteDuel();
                    Assert.That(e.Ustunluk, Is.InRange(-100, 100));
                }
            }
        }

        [Test]
        public void HighUstunluk_UserAttacksMoreOften()
        {
            int user = 0;
            for (uint s = 1; s <= 1000; s++)
            {
                var e = new MatchEngine(Data(), Team("u"), Team("o"), s);
                e.ForcedUstunluk = 100;
                e.StartNextDuel();
                if (e.CurrentDuel.UserIsAttacker) user++;
            }
            Assert.That(user, Is.GreaterThan(800));
        }

        [Test]
        public void FirstDuel_CarrierAndDefender_ByLane()
        {
            var e = Engine(lane: Lane.Left);
            e.StartNextDuel();
            Assert.That(e.CurrentDuel.User.Player.Slot, Is.EqualTo(Slot.LW));
            Assert.That(e.CurrentDuel.Enemy.Player.Slot, Is.EqualTo(Slot.RW));
            Assert.That(e.CurrentDuel.Kind, Is.EqualTo(DuelKind.Field));
            Assert.That(e.Phase, Is.EqualTo(MatchPhase.InDuel));
            Assert.That(e.CurrentDuel.UserPassTargets.Select(p => p.Slot), Does.Not.Contain(Slot.GK).And.Not.Contain(Slot.LW));
            Assert.That(e.CurrentDuel.UserPassTargets.Count, Is.EqualTo(5));
        }

        [Test]
        public void Win_AdvancesStage_ThenShot_ThenGoal()
        {
            var e = Engine();
            Step(e, DuelOutcome.AttackerWon);
            Assert.That(e.Stage, Is.EqualTo(ChainStage.FinalThird));
            Assert.That(e.Phase, Is.EqualTo(MatchPhase.AwaitingDuel));
            e.StartNextDuel();
            Assert.That(e.CurrentDuel.Kind, Is.EqualTo(DuelKind.Field));
            e.CurrentDuel.ForceOutcome(DuelOutcome.AttackerWon);
            e.CompleteDuel();
            Assert.That(e.Stage, Is.EqualTo(ChainStage.Shot));
            e.StartNextDuel();
            Assert.That(e.CurrentDuel.Kind, Is.EqualTo(DuelKind.Shot));
            Assert.That(e.CurrentDuel.Enemy.Player.Slot, Is.EqualTo(Slot.GK));
            e.CurrentDuel.ForceOutcome(DuelOutcome.AttackerWon);
            var ev = e.CompleteDuel();
            Assert.That(e.User.Score, Is.EqualTo(1));
            Assert.That(ev.OfType<GoalScored>().Single().ByUser, Is.True);
            e.StartNextDuel();
            Assert.That(e.Stage, Is.EqualTo(ChainStage.Midfield));
            Assert.That(e.MomentsPlayed, Is.EqualTo(2));
        }

        [Test]
        public void PassSucceeded_TargetBecomesCarrier()
        {
            var e = Engine();
            e.StartNextDuel();
            var st = e.CurrentDuel.UserPassTargets.Single(p => p.Slot == Slot.ST);
            e.CurrentDuel.ForceOutcome(DuelOutcome.PassSucceeded, st);
            e.CompleteDuel();
            Assert.That(e.Stage, Is.EqualTo(ChainStage.FinalThird));
            Assert.That(e.Ustunluk, Is.EqualTo(10));
            e.StartNextDuel();
            Assert.That(e.CurrentDuel.User.Player, Is.SameAs(st));
            Assert.That(e.User.ById(st.Id)!.Hype, Is.EqualTo(0));
        }

        [Test]
        public void Loss_EndsChain_AttackingTacticGivesCounterAtFinalThird()
        {
            var e = Engine();
            e.SetTactic(true, Tactic.Attacking);
            Step(e, DuelOutcome.DefenderWon);
            Assert.That(e.MomentsPlayed, Is.EqualTo(2));
            e.StartNextDuel();
            Assert.That(e.CurrentDuel.UserIsAttacker, Is.False);
            Assert.That(e.Stage, Is.EqualTo(ChainStage.FinalThird));
            Assert.That(e.CurrentDuel.Enemy.Player.Slot, Is.EqualTo(Slot.CM));
            Assert.That(e.CurrentDuel.Kind, Is.EqualTo(DuelKind.Field));
            Assert.That(e.CurrentDuel.UserPassTargets, Is.Empty);
            Assert.That(e.CurrentDuel.EnemyPassTargets.Count, Is.EqualTo(5));
        }

        [Test]
        public void Counter_EmitsMomentStarted()
        {
            var e = Engine();
            e.SetTactic(true, Tactic.Attacking);
            e.StartNextDuel();
            e.CurrentDuel.ForceOutcome(DuelOutcome.DefenderWon);
            var ev = e.CompleteDuel();
            Assert.That(ev.OfType<MomentStarted>().Single().UserAttacks, Is.False);
        }

        [Test]
        public void Loss_WithoutAttackingTactic_StartsNewMoment()
        {
            var e = Engine();
            Step(e, DuelOutcome.DefenderWon);
            Assert.That(e.MomentsPlayed, Is.EqualTo(1));
            e.StartNextDuel();
            Assert.That(e.MomentsPlayed, Is.EqualTo(2));
            Assert.That(e.Stage, Is.EqualTo(ChainStage.Midfield));
        }

        [Test]
        public void Kondisyon_DropsTen_PenaltyBelow30()
        {
            var e = Engine();
            e.StartNextDuel();
            Assert.That(e.CurrentDuel.User.Energy, Is.EqualTo(3));
            e.CurrentDuel.User.KondisyonSpent = 5;
            e.CurrentDuel.ForceOutcome(DuelOutcome.DefenderWon);
            var evs = e.CompleteDuel();
            Assert.That(e.User.BySlot(Slot.CM)!.Kondisyon, Is.EqualTo(85));
            Assert.That(e.Opponent.BySlot(Slot.CM)!.Kondisyon, Is.EqualTo(90));
            Assert.That(evs.OfType<KondisyonChanged>().Count(), Is.EqualTo(2));

            var low = Engine();
            low.User.BySlot(Slot.CM)!.Kondisyon = 25;
            low.StartNextDuel();
            Assert.That(low.CurrentDuel.User.Energy, Is.EqualTo(2));
            low.CurrentDuel.User.KondisyonSpent = 50;
            low.CurrentDuel.ForceOutcome(DuelOutcome.DefenderWon);
            low.CompleteDuel();
            Assert.That(low.User.BySlot(Slot.CM)!.Kondisyon, Is.EqualTo(0));
        }

        [Test]
        public void Hype_WinGivesHype_ThreeGivesAlev_LosingAlevTransfersIt()
        {
            var e = Engine();
            var carrier = e.User.BySlot(Slot.CM)!;
            Step(e, DuelOutcome.AttackerWon);
            Assert.That(carrier.Hype, Is.EqualTo(1));
            e.StartNextDuel();
            e.CurrentDuel.User.Hp = 1; // low hp: +2
            e.CurrentDuel.ForceOutcome(DuelOutcome.AttackerWon);
            e.CompleteDuel();
            Assert.That(carrier.Hype, Is.EqualTo(3));
            Assert.That(carrier.Alev, Is.True);

            // Alev carrier loses to a defender: transfer
            var e2 = Engine();
            var c2 = e2.User.BySlot(Slot.CM)!;
            c2.Alev = true; c2.Hype = 3;
            var d2 = e2.Opponent.BySlot(Slot.CM)!;
            var ev = Step(e2, DuelOutcome.DefenderWon);
            Assert.That(c2.Alev, Is.False);
            Assert.That(c2.Hype, Is.EqualTo(0));
            Assert.That(d2.Alev, Is.True);
            Assert.That(d2.Hype, Is.EqualTo(3));
            Assert.That(ev.OfType<HypeChanged>().Count(), Is.EqualTo(2));
        }

        [Test]
        public void Alev_GivesUserEnergyBonus_EnemyAlevGivesMomentum()
        {
            var e = Engine();
            e.User.BySlot(Slot.CM)!.Alev = true;
            e.Opponent.BySlot(Slot.CM)!.Alev = true;
            e.StartNextDuel();
            Assert.That(e.CurrentDuel.User.Energy, Is.EqualTo(4));
            Assert.That(e.CurrentDuel.Enemy.StatusOf(StatusId.Momentum), Is.EqualTo(2));
        }

        [Test]
        public void Goal_GivesWholeTeamHype()
        {
            var e = Engine();
            Step(e, DuelOutcome.AttackerWon);
            Step(e, DuelOutcome.AttackerWon);
            Step(e, DuelOutcome.AttackerWon);
            foreach (var p in e.User.Players)
                Assert.That(p.Hype, Is.GreaterThanOrEqualTo(1), p.Def.Id);
            Assert.That(e.User.BySlot(Slot.CM)!.Alev, Is.True);
        }

        [Test]
        public void HalfTime_ReachedOnce_TacticChangeAllowedOnlyThen()
        {
            var e = Engine();
            int half = 0;
            Assert.DoesNotThrow(() => e.SetTactic(true, Tactic.Defensive));
            while (e.Phase != MatchPhase.Ended)
            {
                var ev = Step(e, DuelOutcome.DefenderWon);
                half += ev.OfType<HalfTime>().Count();
                if (e.Phase == MatchPhase.HalfTime)
                {
                    Assert.That(e.MomentsPlayed, Is.EqualTo(e.MomentsTotal / 2));
                    Assert.DoesNotThrow(() => e.SetTactic(true, Tactic.Attacking));
                }
                else if (e.Phase != MatchPhase.Ended)
                {
                    Assert.Throws<InvalidOperationException>(() => e.SetTactic(true, Tactic.Balanced));
                }
            }
            Assert.That(half, Is.EqualTo(1));
        }

        [Test]
        public void Match_Ends_WithMatchEndedEvent()
        {
            var e = Engine();
            IReadOnlyList<GameEvent> last = new List<GameEvent>();
            while (e.Phase != MatchPhase.Ended) last = Step(e, DuelOutcome.DefenderWon);
            Assert.That(last.OfType<MatchEnded>().Count(), Is.EqualTo(1));
            Assert.That(e.MomentsPlayed, Is.EqualTo(e.MomentsTotal));
            Assert.That(e.Minute, Is.EqualTo(90));
            Assert.Throws<InvalidOperationException>(() => e.StartNextDuel());
        }

        [Test]
        public void CompleteDuel_WithoutOutcome_Throws()
        {
            var e = Engine();
            Assert.Throws<InvalidOperationException>(() => e.CompleteDuel());
            e.StartNextDuel();
            Assert.Throws<InvalidOperationException>(() => e.CompleteDuel());
        }

        [Test]
        public void EnemyPowerScale_FollowsTier_DefensiveTacticAddsHp()
        {
            var e = Engine(tier: 3);
            e.StartNextDuel();
            // EnemyPowerBasePercent 260 + TierPowerPercent 0 * (tier - 1): tier difficulty comes from team data.
            Assert.That(e.CurrentDuel.Enemy.PowerScalePercent, Is.EqualTo(260));
            Assert.That(MatchRules.PowerScalePercent(1), Is.EqualTo(260));

            var d = Engine();
            d.SetTactic(false, Tactic.Defensive);
            d.StartNextDuel();
            var plain = Engine();
            plain.StartNextDuel();
            Assert.That(d.CurrentDuel.Enemy.MaxHp, Is.EqualTo(plain.CurrentDuel.Enemy.MaxHp + 3));
        }

        [Test]
        public void Momentum_CarriesToNextChainDuel()
        {
            var e = Engine();
            e.StartNextDuel();
            e.CurrentDuel.User.Statuses[StatusId.Momentum] = 2;
            e.CurrentDuel.ForceOutcome(DuelOutcome.AttackerWon);
            e.CompleteDuel();
            e.StartNextDuel();
            Assert.That(e.CurrentDuel.User.StatusOf(StatusId.Momentum), Is.EqualTo(2));
        }
    }
}
