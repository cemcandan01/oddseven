using System;
using System.Collections.Generic;
using NUnit.Framework;
using WeirdLand.Core.Ai;
using WeirdLand.Core.Match;

namespace WeirdLand.Core.Tests
{
    public class GridFixWaveTests
    {
        private static bool IsGridEvent(GameEvent e)
            => e is GridKickoff || e is PlayerMoved || e is BallPassed || e is PassIntercepted
               || e is DuelTriggered || e is PlayerStunned || e is PossessionChanged || e is ShotChance
               || e is MatchMoveAdvanced || e is MatchEnded;

        private static void CheckDuelContract(IReadOnlyList<GameEvent> evs, string ctx)
        {
            int idx = -1, count = 0;
            for (int i = 0; i < evs.Count; i++)
                if (evs[i] is DuelTriggered) { count++; if (idx < 0) idx = i; }
            if (count == 0) return;
            Assert.That(count, Is.EqualTo(1), ctx + ": more than one DuelTriggered");
            for (int j = idx + 1; j < evs.Count; j++)
                Assert.That(IsGridEvent(evs[j]), Is.False, ctx + ": grid event " + evs[j] + " after DuelTriggered");
        }

        [Test] public void DuelOpening_SingleTrigger_OnlyDuelEventsAfter_Seeds1to50()
        {
            int lists = 0;
            for (uint s = 1; s <= 50; s++)
            {
                var m = TestKit.NewGridMatch(s);
                var policy = new GreedyGridPolicy();
                var duelPolicy = new GoodPolicy();
                var rng = new Rng(s + 1000);
                m.Kickoff();
                int guard = 0;
                while (m.Phase != GridPhase.Ended)
                {
                    Assert.That(++guard, Is.LessThan(GridRunner.MaxActions));
                    if (m.Phase == GridPhase.InDuel)
                    {
                        var d = m.CurrentDuel;
                        if (d.Outcome.HasValue) { CheckDuelContract(m.CompleteDuel(), "seed " + s + " CompleteDuel"); lists++; continue; }
                        var a = duelPolicy.Decide(d, null, rng);
                        if (a is PlayCardAction pc) d.PlayCard(pc.HandIndex); else d.EndTurn();
                    }
                    else
                    {
                        IReadOnlyList<GameEvent> evs;
                        if (!m.UserHasBall) evs = m.ResolveOpponentMove();
                        else
                        {
                            var act = policy.Decide(m, rng);
                            evs = act is GridAdvance ad ? m.Advance(ad.To) : m.Pass(((GridPassTo)act).TargetId);
                        }
                        CheckDuelContract(evs, "seed " + s + " move " + m.Move);
                        lists++;
                    }
                }
            }
            Assert.That(lists, Is.GreaterThan(0));
        }

        [Test] public void Kickoff_Twice_Throws()
        {
            var m = TestKit.NewGridMatch(1);
            m.Kickoff();
            Assert.Throws<InvalidOperationException>(() => m.Kickoff());
        }

        [Test] public void Restart_AfterGoal_StillWorks_WithKickoffGuard()
        {
            // A full match scores goals (Restart via EndOfMove) without tripping the Kickoff guard.
            int goals = 0;
            for (uint s = 1; s <= 10; s++)
            {
                var r = GridRunner.Play(TestKit.NewGridMatch(s), new GreedyGridPolicy(), new GoodPolicy(), new Rng(s));
                int kickoffs = 0;
                foreach (var e in r.Events) if (e is GridKickoff) kickoffs++;
                goals += r.UserScore + r.OppScore;
                Assert.That(kickoffs, Is.EqualTo(1 + r.UserScore + r.OppScore), "seed " + s);
            }
            Assert.That(goals, Is.GreaterThan(0));
        }

        [Test] public void GreedyDecision_DoesNotTouchRng()
        {
            for (uint s = 1; s <= 20; s++)
            {
                var m = TestKit.NewGridMatch(s);
                m.Kickoff();
                var given = new Rng(s);
                uint matchBefore = m.RngState, givenBefore = given.State;
                new GreedyGridPolicy().Decide(m, given);
                Assert.That(m.RngState, Is.EqualTo(matchBefore), "seed " + s);
                Assert.That(given.State, Is.EqualTo(givenBefore), "seed " + s);
            }
        }
    }
}
