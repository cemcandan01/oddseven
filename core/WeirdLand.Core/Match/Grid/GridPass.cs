using System;
using System.Collections.Generic;
using WeirdLand.Core.Data;

namespace WeirdLand.Core.Match
{
    internal static class GridPass
    {
        // Non-keeper opponents standing on or next to any interior cell of the pass line.
        // Order: first line cell they touch, then player-list (Slot) order.
        public static IReadOnlyList<GridPlayer> Interceptors(GridMatch m, GridPlayer from, GridPlayer to)
        {
            var result = new List<GridPlayer>();
            var line = GridGeometry.Line(from.Pos, to.Pos);
            for (int i = 1; i < line.Count - 1; i++)
                foreach (var p in m.Players)
                {
                    if (p.IsUser == from.IsUser || p.Def.Slot == Slot.GK || result.Contains(p)) continue;
                    if (GridGeometry.Distance(p.Pos, line[i]) <= 1) result.Add(p);
                }
            return result;
        }

        public static int ReceiverMarkers(GridMatch m, GridPlayer from, GridPlayer to)
        {
            int n = 0;
            foreach (var p in m.Players)
                if (p.IsUser != from.IsUser && p.Def.Slot != Slot.GK && GridGeometry.Adjacent(p.Pos, to.Pos)) n++;
            return n;
        }

        public static double Chance(GridMatch m, GridPlayer from, GridPlayer to)
        {
            double c = MatchRules.PassBase
                - MatchRules.PassPerDistance * GridGeometry.Distance(from.Pos, to.Pos)
                + MatchRules.PassPerStat * from.Def.Stats.Passing
                - MatchRules.PassPerInterceptor * Interceptors(m, from, to).Count
                - MatchRules.PassPerReceiverMarker * ReceiverMarkers(m, from, to);
            return Math.Max(MatchRules.PassMin, Math.Min(MatchRules.PassMax, c));
        }

        // Who gets the ball when the pass fails.
        public static GridPlayer PickInterceptor(GridMatch m, GridPlayer from, GridPlayer to)
        {
            var cands = Interceptors(m, from, to);
            if (cands.Count > 0) return Nearest(cands, to);
            var adj = new List<GridPlayer>();
            var field = new List<GridPlayer>();
            var any = new List<GridPlayer>();
            foreach (var p in m.Players)
            {
                if (p.IsUser == from.IsUser) continue;
                any.Add(p);
                if (p.Def.Slot == Slot.GK) continue;
                field.Add(p);
                if (GridGeometry.Adjacent(p.Pos, to.Pos)) adj.Add(p);
            }
            if (adj.Count > 0) return Nearest(adj, to);
            return Nearest(field.Count > 0 ? field : any, to);
        }

        private static GridPlayer Nearest(IReadOnlyList<GridPlayer> list, GridPlayer to)
        {
            GridPlayer best = null;
            int bd = int.MaxValue;
            foreach (var p in list) // strict < keeps first on ties
            {
                int d = GridGeometry.Distance(p.Pos, to.Pos);
                if (d < bd) { bd = d; best = p; }
            }
            return best;
        }
    }
}
