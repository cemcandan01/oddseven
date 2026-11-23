using System;
using System.Collections.Generic;
using WeirdLand.Core.Data;

namespace WeirdLand.Core.Match
{
    internal static class GridMovement
    {
        private const int KeeperMinY = 2;
        private const int KeeperMaxY = 5;

        internal static void Step(GridMatch m, List<GameEvent> ev)
        {
            var carrier = m.Carrier;
            if (carrier == null) return;
            bool attackingIsUser = carrier.IsUser;
            var presser = FindPresser(m, carrier);

            // defending team first, then attacking team; Slot order inside each (Players is in Slot order)
            for (int pass = 0; pass < 2; pass++)
            {
                bool attackingPass = pass == 1;
                foreach (var p in m.Players)
                {
                    if ((p.IsUser == attackingIsUser) != attackingPass) continue;
                    if (p == carrier || p.Stun > 0) continue;
                    Cell target;
                    if (p.Def.Slot == Slot.GK)
                        target = new Cell(GridGeometry.Home(Slot.GK, p.IsUser).X,
                            Math.Max(KeeperMinY, Math.Min(KeeperMaxY, carrier.Pos.Y)));
                    else if (p == presser) target = carrier.Pos;
                    else target = ShiftedHome(p, carrier.Pos);

                    var to = StepToward(m, p, target, attackingPass, p.Def.Slot == Slot.GK);
                    if (to.Equals(p.Pos)) continue;
                    var from = p.Pos;
                    p.Pos = to;
                    ev.Add(new PlayerMoved(p.Def.Id, from.X, from.Y, to.X, to.Y));
                }
            }
        }

        internal static Cell ShiftedHome(GridPlayer p, Cell carrierPos)
        {
            var home = GridGeometry.Home(p.Def.Slot, p.IsUser);
            int shift = (int)Math.Round((carrierPos.X - 5.5) * MatchRules.ShiftFactor, MidpointRounding.AwayFromZero);
            int x = Math.Max(0, Math.Min(GridGeometry.Width - 1, home.X + shift));
            return new Cell(x, home.Y);
        }

        private static GridPlayer FindPresser(GridMatch m, GridPlayer carrier)
        {
            GridPlayer best = null; int bestD = int.MaxValue;
            foreach (var p in m.Players)
            {
                if (p.IsUser == carrier.IsUser || p.Def.Slot == Slot.GK || p.Stun > 0) continue;
                int d = GridGeometry.Distance(p.Pos, carrier.Pos);
                if (d < bestD) { best = p; bestD = d; }
            }
            return best;
        }

        private static Cell StepToward(GridMatch m, GridPlayer p, Cell target, bool attackingOffBall, bool keeper)
        {
            int curD = GridGeometry.Distance(p.Pos, target);
            Cell best = p.Pos; int bestD = curD, bestOpp = int.MaxValue; bool found = false;
            for (int x = p.Pos.X - 1; x <= p.Pos.X + 1; x++)
                for (int y = p.Pos.Y - 1; y <= p.Pos.Y + 1; y++)
                {
                    var c = new Cell(x, y);
                    if (c.Equals(p.Pos) || !GridGeometry.InBounds(c) || m.PlayerAt(c) != null) continue;
                    if (keeper && x != p.Pos.X) continue; // keepers hold their home x
                    int d = GridGeometry.Distance(c, target);
                    if (d >= curD) continue;
                    int opp = attackingOffBall ? AdjacentOpponents(m, p, c) : 0;
                    // loops go x then y, so strict comparisons keep the x-then-y tie-break
                    if (!found || d < bestD || (d == bestD && opp < bestOpp))
                    {
                        best = c; bestD = d; bestOpp = opp; found = true;
                    }
                }
            return best;
        }

        private static int AdjacentOpponents(GridMatch m, GridPlayer p, Cell c)
        {
            int n = 0;
            foreach (var o in m.Players)
                if (o.IsUser != p.IsUser && GridGeometry.Adjacent(o.Pos, c)) n++;
            return n;
        }
    }
}
