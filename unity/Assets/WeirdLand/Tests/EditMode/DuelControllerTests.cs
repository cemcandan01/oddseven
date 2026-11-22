using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using WeirdLand.Core;
using WeirdLand.Core.Duels;

namespace WeirdLand.Unity.Tests
{
    public class DuelControllerTests
    {
        private sealed class FakeView : IDuelView
        {
            public readonly List<string> Calls = new List<string>();
            public UniTaskCompletionSource Gate;
            public bool Throw;

            public async UniTask Animate(GameEvent e, Duel duel, CancellationToken ct)
            {
                Calls.Add("Animate:" + e.GetType().Name);
                if (Throw) throw new System.InvalidOperationException("view boom");
                if (Gate != null) await Gate.Task.AttachExternalCancellation(ct);
            }

            public void Sync(Duel duel) => Calls.Add("Sync");
        }

        private static async UniTask<DuelController> Started(Duel duel, FakeView view)
        {
            var c = new DuelController(duel, view);
            await c.Begin(default);
            view.Calls.Clear();
            return c;
        }

        private static async UniTask Finish(DuelController c)
        {
            for (int i = 0; i < 20 && !c.Duel.Outcome.HasValue; i++) await c.EndTurn(default);
        }

        [UnityTest] public IEnumerator Begin_AnimatesEveryEventInOrder_ThenSyncs() => UniTask.ToCoroutine(async () =>
        {
            var twin = TestDecks.FieldDuel(seed: 3);
            var expected = twin.Begin().Select(e => "Animate:" + e.GetType().Name).ToList();
            var view = new FakeView();
            var c = new DuelController(TestDecks.FieldDuel(seed: 3), view);
            Assert.That(await c.Begin(default), Is.True);
            Assert.That(view.Calls, Is.EqualTo(expected.Append("Sync").ToList()));
        });

        [UnityTest] public IEnumerator PlayCard_WhileBusy_ReturnsFalse_DuelUntouched() => UniTask.ToCoroutine(async () =>
        {
            var view = new FakeView();
            var c = await Started(TestDecks.FieldDuel(), view);
            view.Gate = new UniTaskCompletionSource();
            var first = c.PlayCard(0, default);
            Assert.That(c.IsBusy, Is.True);
            int hand = c.Duel.User.Hand.Count, energy = c.Duel.User.Energy;
            Assert.That(await c.PlayCard(0, default), Is.False);
            Assert.That(c.Duel.User.Hand.Count, Is.EqualTo(hand));
            Assert.That(c.Duel.User.Energy, Is.EqualTo(energy));
            view.Gate.TrySetResult();
            Assert.That(await first, Is.True);
            Assert.That(c.IsBusy, Is.False);
        });

        [UnityTest] public IEnumerator PlayCard_Unaffordable_ReturnsFalse() => UniTask.ToCoroutine(async () =>
        {
            var view = new FakeView();
            var c = await Started(TestDecks.FieldDuel(cardCost: 4), view);
            Assert.That(await c.PlayCard(0, default), Is.False);
            Assert.That(view.Calls, Is.Empty);
        });

        [UnityTest] public IEnumerator Pass_UnknownTarget_ReturnsFalse() => UniTask.ToCoroutine(async () =>
        {
            var view = new FakeView();
            var c = await Started(TestDecks.FieldDuel(), view);
            Assert.That(await c.Pass("nobody", default), Is.False);
            Assert.That(view.Calls, Is.Empty);
        });

        [UnityTest] public IEnumerator Pass_WhenUserDefends_ReturnsFalse() => UniTask.ToCoroutine(async () =>
        {
            var view = new FakeView();
            var c = await Started(TestDecks.FieldDuel(userAttacks: false), view);
            Assert.That(await c.Pass("mate", default), Is.False);
            Assert.That(view.Calls, Is.Empty);
        });

        [UnityTest] public IEnumerator Pass_ValidTarget_Succeeds() => UniTask.ToCoroutine(async () =>
        {
            var view = new FakeView();
            var c = await Started(TestDecks.FieldDuel(), view);
            Assert.That(await c.Pass("mate", default), Is.True);
            Assert.That(c.Duel.Outcome.HasValue, Is.True);
        });

        [UnityTest] public IEnumerator CommandsBeforeBegin_ReturnFalse() => UniTask.ToCoroutine(async () =>
        {
            var view = new FakeView();
            var c = new DuelController(TestDecks.FieldDuel(), view);
            Assert.That(await c.PlayCard(0, default), Is.False);
            Assert.That(await c.Pass("mate", default), Is.False);
            Assert.That(await c.EndTurn(default), Is.False);
            Assert.That(view.Calls, Is.Empty);
        });

        [UnityTest] public IEnumerator BeginTwice_SecondReturnsFalse() => UniTask.ToCoroutine(async () =>
        {
            var view = new FakeView();
            var c = new DuelController(TestDecks.FieldDuel(), view);
            Assert.That(await c.Begin(default), Is.True);
            view.Calls.Clear();
            Assert.That(await c.Begin(default), Is.False);
            Assert.That(view.Calls, Is.Empty);
        });

        [UnityTest] public IEnumerator AfterOutcome_AllCommandsReturnFalse() => UniTask.ToCoroutine(async () =>
        {
            var view = new FakeView();
            var c = await Started(TestDecks.FieldDuel(), view);
            await Finish(c);
            Assert.That(c.Duel.Outcome.HasValue, Is.True);
            view.Calls.Clear();
            Assert.That(await c.PlayCard(0, default), Is.False);
            Assert.That(await c.Pass("mate", default), Is.False);
            Assert.That(await c.EndTurn(default), Is.False);
            Assert.That(await c.Begin(default), Is.False);
            Assert.That(view.Calls, Is.Empty);
        });

        [UnityTest] public IEnumerator Ended_RaisedOnce_AfterFinalSync() => UniTask.ToCoroutine(async () =>
        {
            var view = new FakeView();
            var c = await Started(TestDecks.FieldDuel(), view);
            int count = 0;
            string last = null;
            c.Ended += o => { count++; last = view.Calls.Last(); };
            await Finish(c);
            await c.EndTurn(default);
            Assert.That(count, Is.EqualTo(1));
            Assert.That(last, Is.EqualTo("Sync"));
        });

        [UnityTest] public IEnumerator Cancel_DuringPlayback_ReturnsFalseAndClearsBusy() => UniTask.ToCoroutine(async () =>
        {
            var view = new FakeView();
            var c = await Started(TestDecks.FieldDuel(), view);
            view.Gate = new UniTaskCompletionSource();
            var cts = new CancellationTokenSource();
            var task = c.PlayCard(0, cts.Token);
            Assert.That(c.IsBusy, Is.True);
            cts.Cancel();
            Assert.That(await task, Is.False);
            Assert.That(c.IsBusy, Is.False);
            Assert.That(view.Calls, Does.Not.Contain("Sync"));
        });

        [UnityTest] public IEnumerator ViewThrows_StillSyncs_AndClearsBusy() => UniTask.ToCoroutine(async () =>
        {
            var view = new FakeView();
            var c = await Started(TestDecks.FieldDuel(), view);
            view.Throw = true;
            LogAssert.Expect(LogType.Exception, new Regex("view boom"));
            Assert.That(await c.PlayCard(0, default), Is.True);
            Assert.That(view.Calls.Last(), Is.EqualTo("Sync"));
            Assert.That(view.Calls.Count(x => x.StartsWith("Animate")), Is.EqualTo(1));
            Assert.That(c.IsBusy, Is.False);
        });
    }
}
