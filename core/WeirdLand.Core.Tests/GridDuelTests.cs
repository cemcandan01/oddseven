using System;
using System.Linq;
using NUnit.Framework;
using WeirdLand.Core.Data;
using WeirdLand.Core.Match;

namespace WeirdLand.Core.Tests
{
    public class GridDuelTests
    {
        // Everyone parked far away; carrier placed at (5,3). mavi = user, kirmizi = opponent.
        private static GridMatch Setup(string carrierId, uint seed = 1)
        {
            var m = TestKit.NewGridMatch(seed); m.Kickoff();
            int i = 0;
            foreach (var p in m.Players) m.SetPos(p.Def.Id, new Cell(9 + (i / 8), i++ % 8));
            m.SetCarrier(carrierId);
            m.SetPos(carrierId, new Cell(5, 3));
            return m;
        }

        private static GridMatch DuelOpen()
        {
            var m = Setup("mavi_cm");
            m.SetPos("kirmizi_cm", new Cell(6, 4));
            m.Advance(new Cell(5, 4));
            return m;
        }

        [Test] public void AdvanceNextToDefender_OpensDuel_WithBothOwnDecks()
        {
            var m = Setup("mavi_cm");
            m.SetPos("kirmizi_cm", new Cell(6, 4));
            var ev = m.Advance(new Cell(5, 4));
            Assert.That(m.Phase, Is.EqualTo(GridPhase.InDuel));
            Assert.That(m.CurrentDuel, Is.Not.Null);
            Assert.That(m.CurrentDuel.User.Player.Id, Is.EqualTo("mavi_cm"));
            Assert.That(m.CurrentDuel.Enemy.Player.Id, Is.EqualTo("kirmizi_cm"));
            Assert.That(m.CurrentDuel.UserIsAttacker, Is.True);
            var trig = ev.OfType<DuelTriggered>().Single();
            Assert.That(trig.CarrierId, Is.EqualTo("mavi_cm"));
            Assert.That(trig.DefenderId, Is.EqualTo("kirmizi_cm"));
            Assert.That(ev.Skip(ev.ToList().IndexOf(trig) + 1).Any(), Is.True, "Begin events follow DuelTriggered");
        }

        [Test] public void Duel_HasNoPassTargets()
        {
            var m = DuelOpen();
            Assert.That(m.CurrentDuel.CanPass, Is.False);
            Assert.That(m.CurrentDuel.UserPassTargets, Is.Empty);
        }

        [Test] public void Keeper_NeverTriggersDuel()
        {
            var m = Setup("mavi_cm");
            m.SetPos("kirmizi_gk", new Cell(6, 4));
            m.Advance(new Cell(5, 4));
            Assert.That(m.Phase, Is.EqualTo(GridPhase.AwaitingAction));
            Assert.That(m.CurrentDuel, Is.Null);
        }

        [Test] public void StunnedDefender_NeverTriggersDuel()
        {
            var m = Setup("mavi_cm");
            m.SetPos("kirmizi_cm", new Cell(6, 4));
            m.PlayerAt(new Cell(6, 4)).Stun = 2;
            m.Advance(new Cell(5, 4));
            Assert.That(m.Phase, Is.EqualTo(GridPhase.AwaitingAction));
        }

        [Test] public void AttackerWins_DefenderStunned2_CarrierKeepsBall()
        {
            var m = DuelOpen();
            m.CurrentDuel.ForceOutcome(DuelOutcome.AttackerWon);
            var ev = m.CompleteDuel();
            Assert.That(m.Carrier.Def.Id, Is.EqualTo("mavi_cm"));
            Assert.That(m.UserHasBall, Is.True);
            Assert.That(m.Players.Single(p => p.Def.Id == "kirmizi_cm").Stun, Is.EqualTo(MatchRules.StunMoves));
            Assert.That(ev.OfType<PlayerStunned>().Single().PlayerId, Is.EqualTo("kirmizi_cm"));
            Assert.That(m.Phase, Is.EqualTo(GridPhase.AwaitingAction));
        }

        [Test] public void DefenderWins_PossessionFlips_OldCarrierStunned()
        {
            var m = DuelOpen();
            m.CurrentDuel.ForceOutcome(DuelOutcome.DefenderWon);
            var ev = m.CompleteDuel();
            Assert.That(m.Carrier.Def.Id, Is.EqualTo("kirmizi_cm"));
            Assert.That(m.UserHasBall, Is.False);
            Assert.That(m.Players.Single(p => p.Def.Id == "mavi_cm").Stun, Is.EqualTo(MatchRules.StunMoves));
            var pc = ev.OfType<PossessionChanged>().Single();
            Assert.That(pc.UserHasBall, Is.False);
            Assert.That(pc.CarrierId, Is.EqualTo("kirmizi_cm"));
            Assert.That(m.Phase, Is.EqualTo(GridPhase.AwaitingAction));
        }

        [Test] public void DefenderWins_NewCarrierNextToFreshOpponent_DuelOpensImmediately()
        {
            var m = Setup("mavi_cm");
            m.SetPos("kirmizi_cm", new Cell(6, 4));
            m.SetPos("mavi_lw", new Cell(7, 5)); // adjacent to the future carrier at (6,4)
            m.Advance(new Cell(5, 4));
            m.CurrentDuel.ForceOutcome(DuelOutcome.DefenderWon);
            var ev = m.CompleteDuel();
            Assert.That(m.Phase, Is.EqualTo(GridPhase.InDuel));
            var trig = ev.OfType<DuelTriggered>().Single();
            Assert.That(trig.CarrierId, Is.EqualTo("kirmizi_cm"));
            Assert.That(trig.DefenderId, Is.EqualTo("mavi_lw"));
            Assert.That(m.CurrentDuel.UserIsAttacker, Is.False);
            Assert.That(m.CurrentDuel.User.Player.Id, Is.EqualTo("mavi_lw"));
        }

        [Test] public void Stun_TicksDownOncePerMove_AndBlocksForTwoMoves()
        {
            var m = Setup("mavi_cm");
            m.SetPos("kirmizi_cm", new Cell(6, 4));
            m.Advance(new Cell(5, 4));
            m.CurrentDuel.ForceOutcome(DuelOutcome.AttackerWon);
            m.CompleteDuel(); // move k: stun = 2, not ticked in the same move
            var cm = m.Players.Single(p => p.Def.Id == "kirmizi_cm");
            Assert.That(cm.Stun, Is.EqualTo(2));
            // k+1: still adjacent to the stunned defender: no duel, it does not move
            m.Advance(new Cell(5, 3));
            Assert.That(m.Phase, Is.EqualTo(GridPhase.AwaitingAction));
            Assert.That(cm.Stun, Is.EqualTo(1));
            Assert.That(cm.Pos, Is.EqualTo(new Cell(6, 4)));
            // k+2
            m.Advance(new Cell(5, 4));
            Assert.That(m.Phase, Is.EqualTo(GridPhase.AwaitingAction));
            Assert.That(cm.Stun, Is.EqualTo(0));
            // k+3: defender active again
            m.Advance(new Cell(5, 3));
            Assert.That(m.Phase, Is.EqualTo(GridPhase.InDuel));
        }

        [Test] public void UserDefending_ThreeTurnRule_AttackerLosesBall()
        {
            var m = Setup("kirmizi_cm", 1);
            m.SetPos("mavi_cm", new Cell(6, 4));
            m.Advance(new Cell(5, 4));
            Assert.That(m.Phase, Is.EqualTo(GridPhase.InDuel));
            Assert.That(m.CurrentDuel.UserIsAttacker, Is.False);
            m.CurrentDuel.User.MaxHp = m.CurrentDuel.User.Hp = 1000; // isolate the turn limit from enemy damage
            int guard = 0;
            while (!m.CurrentDuel.Outcome.HasValue && guard++ < 10) m.CurrentDuel.EndTurn();
            Assert.That(m.CurrentDuel.Outcome, Is.EqualTo(DuelOutcome.DefenderWon));
            m.CompleteDuel();
            Assert.That(m.UserHasBall, Is.True);
            Assert.That(m.Carrier.Def.Id, Is.EqualTo("mavi_cm"));
        }

        [Test] public void CompleteDuel_WithoutFinishedDuel_Throws()
        {
            var m = DuelOpen();
            Assert.Throws<InvalidOperationException>(() => m.CompleteDuel());
            var m2 = Setup("mavi_cm");
            Assert.Throws<InvalidOperationException>(() => m2.CompleteDuel());
        }
    }
}
