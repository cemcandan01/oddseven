using System.Collections.Generic;
using WeirdLand.Core.Match;

namespace WeirdLand.Core.Ai
{
    public abstract record GridAction;
    public record GridAdvance(Cell To) : GridAction;
    public record GridPassTo(string TargetId) : GridAction;

    public interface IGridPolicy
    {
        GridAction Decide(GridMatch m, Rng rng);
    }

    /// <summary>Spec 6.7: best of pass score and advance score, fixed tie order.
    /// Never draws from the given Rng: the decision is a pure function of the match state.</summary>
    public sealed class GreedyGridPolicy : IGridPolicy
    {
        // Columns from x to the last column in the carrier side's attack direction.
        private static double Closeness(int x, bool userSide)
        {
            int dist = userSide ? GridGeometry.Width - 1 - x : x;
            return 1.0 - dist / (double)(GridGeometry.Width - 1);
        }

        public GridAction Decide(GridMatch m, Rng rng)
        {
            var carrier = m.Carrier;
            bool side = carrier.IsUser;
            GridAction best = null;
            double bestScore = double.NegativeInfinity;

            // Advances first, in ReachableCells order (x then y); strict > keeps the earlier on ties.
            foreach (var c in m.ReachableCells())
            {
                int adj = 0;
                foreach (var p in m.Players)
                    if (p.IsUser != side && p.Def.Slot != Data.Slot.GK && p.Stun == 0 && GridGeometry.Adjacent(p.Pos, c)) adj++;
                double score = Closeness(c.X, side) - 0.25 * adj;
                if (score > bestScore) { bestScore = score; best = new GridAdvance(c); }
            }

            // Passes: tie order x, y, slot (players are in slot order, so sort explicitly by cell).
            GridPlayer bestRecv = null;
            double bestPass = double.NegativeInfinity;
            foreach (var p in m.PassTargets())
            {
                double score = GridPass.Chance(m, carrier, p) * Closeness(p.Pos.X, side);
                if (score > bestPass || (score == bestPass && Before(p, bestRecv)))
                { bestPass = score; bestRecv = p; }
            }
            if (bestRecv != null && (best == null || bestPass > bestScore))
                best = new GridPassTo(bestRecv.Def.Id);
            return best;
        }

        private static bool Before(GridPlayer a, GridPlayer b)
        {
            if (a.Pos.X != b.Pos.X) return a.Pos.X < b.Pos.X;
            if (a.Pos.Y != b.Pos.Y) return a.Pos.Y < b.Pos.Y;
            return (int)a.Def.Slot < (int)b.Def.Slot;
        }
    }

    /// <summary>Uniform choice among reachable cells and pass targets, using only the given Rng.</summary>
    public sealed class RandomGridPolicy : IGridPolicy
    {
        public GridAction Decide(GridMatch m, Rng rng)
        {
            var options = new List<GridAction>();
            foreach (var c in m.ReachableCells()) options.Add(new GridAdvance(c));
            foreach (var p in m.PassTargets()) options.Add(new GridPassTo(p.Def.Id));
            return options[rng.Next(options.Count)];
        }
    }
}
