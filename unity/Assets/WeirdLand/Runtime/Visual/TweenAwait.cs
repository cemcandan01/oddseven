using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PrimeTween;

namespace WeirdLand.Unity
{
    /// <summary>Awaits a PrimeTween tween from async code; cancelling stops the tween.</summary>
    public static class TweenAwait
    {
        public static async UniTask Wait(Tween t, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (!t.isAlive) return;
            try
            {
                await t.ToYieldInstruction().ToUniTask(cancellationToken: ct);
            }
            catch (OperationCanceledException)
            {
                if (t.isAlive) t.Stop();
                throw;
            }
            // A tween stopped from outside (screen teardown) ends the enumerator normally; still honour the token.
            ct.ThrowIfCancellationRequested();
        }
    }
}
