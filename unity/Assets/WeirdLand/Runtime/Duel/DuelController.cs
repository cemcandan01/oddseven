using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using WeirdLand.Core;
using WeirdLand.Core.Duels;

namespace WeirdLand.Unity
{
    /// <summary>Sends commands to the core duel, plays the resulting events through the view, locks input meanwhile.</summary>
    public sealed class DuelController
    {
        private readonly IDuelView _view;
        private bool _begun;

        public DuelController(Duel duel, IDuelView view)
        {
            Duel = duel;
            _view = view;
        }

        public Duel Duel { get; }
        public bool IsBusy { get; private set; }
        public event Action<DuelOutcome> Ended;

        public UniTask<bool> Begin(CancellationToken ct)
        {
            if (IsBusy || _begun || Duel.Outcome.HasValue) return UniTask.FromResult(false);
            _begun = true;
            return Run(Duel.Begin, ct);
        }

        public UniTask<bool> PlayCard(int handIndex, CancellationToken ct)
        {
            if (!CanCommand() || !DuelPreview.CanPlay(Duel, handIndex)) return UniTask.FromResult(false);
            return Run(() => Duel.PlayCard(handIndex), ct);
        }

        public UniTask<bool> Pass(string targetId, CancellationToken ct)
        {
            if (!CanCommand() || !Duel.CanPass || !HasTarget(targetId)) return UniTask.FromResult(false);
            return Run(() => Duel.Pass(targetId), ct);
        }

        public UniTask<bool> EndTurn(CancellationToken ct)
        {
            if (!CanCommand()) return UniTask.FromResult(false);
            return Run(Duel.EndTurn, ct);
        }

        private bool CanCommand() => !IsBusy && _begun && !Duel.Outcome.HasValue;

        private bool HasTarget(string targetId)
        {
            if (targetId == null) return false;
            foreach (var p in Duel.UserPassTargets) if (p.Id == targetId) return true;
            return false;
        }

        private async UniTask<bool> Run(Func<IReadOnlyList<GameEvent>> command, CancellationToken ct)
        {
            IsBusy = true;
            try
            {
                var events = command();
                try
                {
                    foreach (var e in events) await _view.Animate(e, Duel, ct);
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
                _view.Sync(Duel);
                if (Duel.Outcome.HasValue) Ended?.Invoke(Duel.Outcome.Value);
                return true;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
