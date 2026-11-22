using System.Threading;
using Cysharp.Threading.Tasks;
using WeirdLand.Core;
using WeirdLand.Core.Duels;

namespace WeirdLand.Unity
{
    /// <summary>What the duel controller needs from the screen: play one event, then snap to state.</summary>
    public interface IDuelView
    {
        UniTask Animate(GameEvent e, Duel duel, CancellationToken ct);
        void Sync(Duel duel);
    }
}
