using System.Linq;
using NUnit.Framework;
using WeirdLand.Core.Data;
using WeirdLand.Core.Duels;
using WeirdLand.Core.Match;
using static WeirdLand.Core.Tests.TestKit;

namespace WeirdLand.Core.Tests
{
    public class MatchRulesTests
    {
        // v = 0.5u + 8(userMid - oppMid) + 4(userHype - oppHype)/3 + tac(user) - tac(opp) + (40 noise - 20),
        // rounded half away from zero, clamped to [-100, 100]. tac: Attacking +10, Defensive -10.
        [TestCase(0, 4.0, 4.0, 0, 0, Tactic.Balanced, Tactic.Balanced, 0.5, 0)]
        [TestCase(20, 5.0, 4.0, 3, 0, Tactic.Attacking, Tactic.Balanced, 0.75, 42)]      // 10 + 8 + 4 + 10 + 10
        [TestCase(-10, 3.0, 4.5, 0, 2, Tactic.Balanced, Tactic.Attacking, 0.5, -30)]     // -5 - 12 - 2.67 - 10 = -29.67
        [TestCase(0, 4.0, 4.0, 1, 0, Tactic.Defensive, Tactic.Balanced, 0.5, -9)]        // 1.33 - 10 = -8.67
        [TestCase(0, 4.0, 4.0, 0, 0, Tactic.Balanced, Tactic.Defensive, 0.5, 10)]        // opponent defensive: +10
        [TestCase(0, 4.0, 4.0, 0, 0, Tactic.Attacking, Tactic.Attacking, 0.5, 0)]        // tactics cancel
        [TestCase(1, 4.0, 4.0, 0, 0, Tactic.Balanced, Tactic.Balanced, 0.5, 1)]          // 0.5 -> 1
        [TestCase(-1, 4.0, 4.0, 0, 0, Tactic.Balanced, Tactic.Balanced, 0.5, -1)]        // -0.5 -> -1
        [TestCase(0, 4.0, 4.0, 0, 0, Tactic.Balanced, Tactic.Balanced, 0.0, -20)]        // noise low end
        [TestCase(0, 4.0, 4.0, 0, 0, Tactic.Balanced, Tactic.Balanced, 1.0, 20)]         // noise high end
        [TestCase(100, 8.0, 2.0, 0, 0, Tactic.Balanced, Tactic.Balanced, 1.0, 100)]      // 118 clamped
        [TestCase(-100, 2.0, 8.0, 0, 0, Tactic.Balanced, Tactic.Balanced, 0.0, -100)]    // -118 clamped
        public void NextUstunluk_Table(int u, double userMid, double oppMid, int userHype, int oppHype,
            Tactic userTac, Tactic oppTac, double noise, int expected)
        {
            Assert.That(MatchRules.NextUstunluk(u, userMid, oppMid, userHype, oppHype, userTac, oppTac, noise), Is.EqualTo(expected));
        }

        [Test]
        public void Mid_IsMeanOfPassingAndPower()
        {
            Assert.That(MatchRules.Mid(Player("cm", Slot.CM, new Stats { Passing = 5, Power = 4 })), Is.EqualTo(4.5));
        }

        private static TeamDef Team(string prefix, int lbDefending = 3, int rbDefending = 3)
        {
            var t = new TeamDef { Id = prefix, Name = prefix, Tier = 1, Style = "x" };
            foreach (var slot in System.Enum.GetValues<Slot>())
                t.Players.Add(Player(prefix + slot, slot, null, "c"));
            t.BySlot(Slot.LB)!.Stats.Defending = lbDefending;
            t.BySlot(Slot.RB)!.Stats.Defending = rbDefending;
            return t;
        }

        private static MatchEngine Engine(bool userAttacks, Lane lane, int lbDef = 3, int rbDef = 3)
        {
            var cards = new System.Collections.Generic.Dictionary<string, CardDef> { ["c"] = Card("c", 1, CardRole.Both, Dmg(1)) };
            var data = new GameData(cards, new System.Collections.Generic.Dictionary<string, TeamDef>());
            // The defending side gets the LB/RB Defending values under test.
            var user = userAttacks ? Team("u") : Team("u", lbDef, rbDef);
            var opp = userAttacks ? Team("o", lbDef, rbDef) : Team("o");
            var e = new MatchEngine(data, user, opp, 1);
            e.ForcedUserAttacks = userAttacks;
            e.ForcedLane = lane;
            e.ForcedUstunluk = 0;
            return e;
        }

        private static (Slot carrier, Slot defender, DuelKind kind) Current(MatchEngine e)
        {
            var d = e.CurrentDuel;
            var att = d.UserIsAttacker ? d.User : d.Enemy;
            var def = d.UserIsAttacker ? d.Enemy : d.User;
            return (att.Player.Slot, def.Player.Slot, d.Kind);
        }

        private static void Advance(MatchEngine e)
        {
            e.CurrentDuel.ForceOutcome(DuelOutcome.AttackerWon);
            e.CompleteDuel();
            e.StartNextDuel();
        }

        [TestCase(true, Lane.Left, Slot.LW, Slot.RW, Slot.RB)]
        [TestCase(true, Lane.Center, Slot.CM, Slot.CM, Slot.LB)]
        [TestCase(true, Lane.Right, Slot.RW, Slot.LW, Slot.LB)]
        [TestCase(false, Lane.Left, Slot.LW, Slot.RW, Slot.RB)]
        [TestCase(false, Lane.Center, Slot.CM, Slot.CM, Slot.LB)]
        [TestCase(false, Lane.Right, Slot.RW, Slot.LW, Slot.LB)]
        public void CarrierAndDefender_ByLaneAndStage(bool userAttacks, Lane lane, Slot carrier, Slot midDefender, Slot finalDefender)
        {
            var e = Engine(userAttacks, lane);
            e.StartNextDuel();
            Assert.That(e.Stage, Is.EqualTo(ChainStage.Midfield));
            Assert.That(Current(e), Is.EqualTo((carrier, midDefender, DuelKind.Field)));
            Assert.That(e.CurrentDuel.UserIsAttacker, Is.EqualTo(userAttacks));

            Advance(e);
            Assert.That(e.Stage, Is.EqualTo(ChainStage.FinalThird));
            Assert.That(Current(e), Is.EqualTo((carrier, finalDefender, DuelKind.Field)));

            Advance(e);
            Assert.That(e.Stage, Is.EqualTo(ChainStage.Shot));
            Assert.That(Current(e), Is.EqualTo((carrier, Slot.GK, DuelKind.Shot)));
        }

        [TestCase(true, 3, 3, Slot.LB)]  // tie -> LB
        [TestCase(true, 5, 2, Slot.LB)]
        [TestCase(true, 2, 5, Slot.RB)]
        [TestCase(false, 3, 3, Slot.LB)]
        [TestCase(false, 5, 2, Slot.LB)]
        [TestCase(false, 2, 5, Slot.RB)]
        public void FinalThird_Center_HigherDefendingFullback(bool userAttacks, int lbDef, int rbDef, Slot expected)
        {
            var e = Engine(userAttacks, Lane.Center, lbDef, rbDef);
            e.StartNextDuel();
            Advance(e);
            Assert.That(Current(e).defender, Is.EqualTo(expected));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PassFailed_DefenderGetsHype_UstunlukTowardDefenders_NoCounterWhenBalanced(bool userAttacks)
        {
            var e = Engine(userAttacks, Lane.Center);
            e.StartNextDuel();
            e.CurrentDuel.ForceOutcome(DuelOutcome.PassFailed);
            var ev = e.CompleteDuel();

            var atk = userAttacks ? e.User : e.Opponent;
            var def = userAttacks ? e.Opponent : e.User;
            Assert.That(def.BySlot(Slot.CM)!.Hype, Is.EqualTo(1));
            Assert.That(atk.BySlot(Slot.CM)!.Hype, Is.EqualTo(0));
            Assert.That(ev, Does.Contain(new HypeChanged(def.BySlot(Slot.CM)!.Def.Id, 1, false)));
            Assert.That(e.Ustunluk, Is.EqualTo(userAttacks ? -10 : 10));
            Assert.That(ev.OfType<MomentStarted>(), Is.Empty);
            Assert.That(e.MomentsPlayed, Is.EqualTo(1));

            e.StartNextDuel();
            Assert.That(e.MomentsPlayed, Is.EqualTo(2));
            Assert.That(e.Stage, Is.EqualTo(ChainStage.Midfield));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PassFailed_AttackingTactic_DefenderCounters(bool userAttacks)
        {
            var e = Engine(userAttacks, Lane.Center);
            e.SetTactic(userAttacks, Tactic.Attacking); // the attacking side plays Attacking
            e.StartNextDuel();
            e.CurrentDuel.ForceOutcome(DuelOutcome.PassFailed);
            var ev = e.CompleteDuel();

            Assert.That(ev.OfType<MomentStarted>().Single(), Is.EqualTo(new MomentStarted(2, !userAttacks, Lane.Center)));
            Assert.That(e.Stage, Is.EqualTo(ChainStage.FinalThird));
            e.StartNextDuel();
            Assert.That(e.CurrentDuel.UserIsAttacker, Is.EqualTo(!userAttacks));
            Assert.That(Current(e).carrier, Is.EqualTo(Slot.CM));
            var counterTeam = userAttacks ? e.Opponent : e.User;
            var carrier = e.CurrentDuel.UserIsAttacker ? e.CurrentDuel.User : e.CurrentDuel.Enemy;
            Assert.That(carrier.Player, Is.SameAs(counterTeam.BySlot(Slot.CM)!.Def));
        }
    }
}
