using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using WeirdLand.Core.Match;

namespace WeirdLand.Unity.Tests
{
    public class GridMatchTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            TestRig.Teardown();
            yield return null;
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator GridMatch_AdvanceAndPass_UpdateBoard()
        {
            var g = TestRig.CreateGrid(1);
            yield return TestRig.UserTurn(g);
            Assert.That(g.Match.UserHasBall && g.Match.Phase == GridPhase.AwaitingAction, Is.True, "user never got the ball");
            AssertBoardMatchesState(g);

            var cells = g.Match.ReachableCells();
            g.ClickAdvance();
            Assert.That(g.Board.HighlightCount, Is.EqualTo(cells.Count), "reachable cells highlighted");
            int move = g.Match.Move;
            g.ClickCell(cells[0]);
            yield return TestRig.Settle(g);
            Assert.That(g.Match.Move, Is.GreaterThan(move), "advance spent a move");
            AssertBoardMatchesState(g);
            Assert.That(g.MoveText, Is.EqualTo(TestRig.Data.Text.Format(GridUiKeys.Move, g.Match.Move, MatchRules.GridMatchMoves)));

            yield return TestRig.UserTurn(g);
            Assert.That(g.Match.UserHasBall && g.Match.Phase == GridPhase.AwaitingAction, Is.True, "user never got the ball back");
            var targets = g.Match.PassTargets();
            int contested = targets.Count(t => g.Match.PassLineContested(t.Def.Id));
            g.ClickPassMode();
            Assert.That(g.Board.PassLineCount, Is.EqualTo(targets.Count), "one line per target");
            Assert.That(g.Board.DashedLineCount, Is.EqualTo(contested), "contested lines are dashed");
            move = g.Match.Move;
            g.ClickPlayer(targets[0].Def.Id);
            yield return TestRig.Settle(g);
            Assert.That(g.Match.Move, Is.GreaterThan(move), "pass spent a move");
            Assert.That(g.Board.PassLineCount, Is.EqualTo(0), "lines cleared after the pass");
            AssertBoardMatchesState(g);
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator GridMatch_DuelOpensDuelScreen_AndReturns()
        {
            var g = TestRig.CreateGrid(1);
            for (int i = 0; i < 80 && !g.DuelOpen && g.Match.Phase != GridPhase.Ended; i++)
            {
                yield return TestRig.UserTurn(g);
                if (g.DuelOpen || g.Match.Phase != GridPhase.AwaitingAction) break;
                var cells = g.Match.ReachableCells();
                var best = cells.OrderByDescending(c => c.X).First();
                g.ClickAdvance();
                g.ClickCell(best);
                for (int f = 0; f < TestRig.MaxIdleFrames && g.IsBusy && !g.DuelOpen; f++) yield return null;
            }
            Assert.That(g.DuelOpen, Is.True, "no duel opened");
            Assert.That(g.Match.Phase, Is.EqualTo(GridPhase.InDuel));
            Assert.That(g.DuelView.gameObject.activeInHierarchy, Is.True);
            yield return TestRig.Idle(g.DuelView);
            Assert.That(g.DuelView.PassButtonVisible, Is.False, "grid duels have no pass");
            Assert.That(g.LastDuelOpening, Is.Not.Empty, "duel opening slice");
            Assert.That(g.LastDuelOpening[0], Is.InstanceOf<WeirdLand.Core.TurnStarted>(), "slice starts with the duel's opening");
            Assert.That(g.LastDuelOpening.OfType<WeirdLand.Core.DuelTriggered>(), Is.Empty, "no grid trigger inside the slice");
            Assert.That(g.DuelView.Embedded, Is.True, "opened through the already-begun path");
            Assert.That(g.DuelView.HandCardCount, Is.EqualTo(g.Match.CurrentDuel.User.Hand.Count), "opening events played (hand dealt)");
            Assert.That(g.DuelView.HandCardCount, Is.GreaterThan(0));

            var duel = g.Match.CurrentDuel;
            yield return TestRig.Autoplay(g.DuelView, duel, 1);
            Assert.That(duel.Outcome.HasValue, Is.True);
            for (int f = 0; f < TestRig.MaxIdleFrames && g.DuelOpen && g.Match.CurrentDuel == duel; f++) yield return null;
            if (g.DuelOpen) Assert.That(g.Match.CurrentDuel, Is.Not.SameAs(duel), "a chained duel is the only reason to stay open");
            else
            {
                yield return TestRig.Settle(g);
                Assert.That(g.Match.Phase, Is.Not.EqualTo(GridPhase.InDuel));
                Assert.That(g.DuelView.gameObject.activeInHierarchy, Is.False, "duel screen closed");
                AssertBoardMatchesState(g);
            }
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator GridMatch_FieldButton_HidesPanel_NoMoveSpent()
        {
            var g = TestRig.CreateGrid(1);
            yield return TestRig.UserTurn(g);
            int move = g.Match.Move;
            Assert.That(g.PanelVisible, Is.True);
            g.ClickField();
            Assert.That(g.PanelVisible, Is.False);
            yield return null;
            Assert.That(g.IsBusy, Is.False);
            g.ClickField();
            Assert.That(g.PanelVisible, Is.True);
            Assert.That(g.Match.Move, Is.EqualTo(move), "Saha costs no move");
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator GridMatch_DuelFailure_ShowsExitRoute_AndClosesDuel()
        {
            var g = TestRig.CreateGrid(1);
            g.DuelStepHook = () => throw new System.InvalidOperationException("test failure in duel step");
            LogAssert.ignoreFailingMessages = true;
            for (int i = 0; i < 80 && !g.Failed && g.Match.Phase != GridPhase.Ended; i++)
            {
                yield return TestRig.UserTurn(g);
                if (g.Failed || g.Match.Phase != GridPhase.AwaitingAction) break;
                var cells = g.Match.ReachableCells();
                g.ClickAdvance();
                g.ClickCell(cells.OrderByDescending(c => c.X).First());
                for (int f = 0; f < TestRig.MaxIdleFrames && g.IsBusy && !g.Failed; f++) yield return null;
            }
            LogAssert.ignoreFailingMessages = false;
            Assert.That(g.Failed, Is.True, "no duel ran, so the failure never fired");
            Assert.That(g.Match.Phase, Is.EqualTo(GridPhase.InDuel), "failure left the match mid-duel");
            Assert.That(g.ResultShown, Is.True, "exit route (result overlay with back button) visible");
            Assert.That(g.DuelOpen, Is.False, "duel view closed");
            Assert.That(g.IsBusy, Is.False);
            bool back = false;
            g.BackRequested += () => back = true;
            var button = g.GetComponentsInChildren<UnityEngine.UI.Button>()
                .First(b => b.name == "Button_" + WeirdLand.Core.Data.UiKeys.ResultBack);
            Assert.That(button.gameObject.activeInHierarchy, Is.True, "back button shown");
            button.onClick.Invoke();
            Assert.That(back, Is.True, "back routes to the menu");
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator GridMatch_StunLook_RefreshedAfterEveryMove()
        {
            var g = TestRig.CreateGrid(1);
            int checks = 0, recovered = 0;
            var wasStunned = new System.Collections.Generic.HashSet<string>();
            g.MoveShown += _ =>
            {
                checks++;
                foreach (var p in g.Match.Players)
                {
                    Assert.That(g.Board.IsShownStunned(p.Def.Id), Is.EqualTo(p.Stun > 0), p.Def.Id + " stun look after move");
                    if (p.Stun > 0) wasStunned.Add(p.Def.Id);
                    else if (wasStunned.Remove(p.Def.Id)) recovered++;
                }
            };
            for (int i = 0; i < 80 && g.Match.Phase != GridPhase.Ended && (checks < 6 || recovered == 0); i++)
            {
                yield return TestRig.UserTurn(g);
                if (g.DuelOpen) { yield return TestRig.Autoplay(g.DuelView, g.Match.CurrentDuel, (uint)i); continue; }
                if (g.Match.Phase != GridPhase.AwaitingAction || !g.Match.UserHasBall) continue;
                var cells = g.Match.ReachableCells();
                g.ClickAdvance();
                g.ClickCell(cells.OrderByDescending(c => c.X).First());
                for (int f = 0; f < TestRig.MaxIdleFrames && g.IsBusy && !g.DuelOpen; f++) yield return null;
            }
            Assert.That(checks, Is.GreaterThan(0), "no move was shown");
            Assert.That(recovered, Is.GreaterThan(0), "no stunned player recovered during the run");
        }

        private static void AssertBoardMatchesState(GridMatchController g)
        {
            foreach (var p in g.Match.Players)
            {
                var shown = g.Board.TokenPosition(p.Def.Id);
                var want = g.Board.CellPosition(p.Pos);
                Assert.That(Vector2.Distance(shown, want), Is.LessThan(0.5f), p.Def.Id + " token at its cell");
                Assert.That(g.Board.IsShownStunned(p.Def.Id), Is.EqualTo(p.Stun > 0), p.Def.Id + " stun look");
            }
            Assert.That(g.Board.BallHolder, Is.EqualTo(g.Match.Carrier.Def.Id), "ball on the carrier");
        }
    }
}
