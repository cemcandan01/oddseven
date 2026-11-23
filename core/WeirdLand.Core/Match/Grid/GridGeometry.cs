using System;
using System.Collections.Generic;
using WeirdLand.Core.Data;

namespace WeirdLand.Core.Match
{
    public readonly struct Cell : IEquatable<Cell>
    {
        public int X { get; }
        public int Y { get; }
        public Cell(int x, int y) { X = x; Y = y; }
        public bool Equals(Cell other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is Cell c && Equals(c);
        public override int GetHashCode() => X * 31 + Y;
        public static bool operator ==(Cell a, Cell b) => a.Equals(b);
        public static bool operator !=(Cell a, Cell b) => !a.Equals(b);
        public override string ToString() => "(" + X + "," + Y + ")";
    }

    public static class GridGeometry
    {
        public const int Width = MatchRules.GridWidth;
        public const int Height = MatchRules.GridHeight;

        public static bool InBounds(Cell c) => c.X >= 0 && c.X < Width && c.Y >= 0 && c.Y < Height;

        public static int Distance(Cell a, Cell b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

        public static bool Adjacent(Cell a, Cell b) => Distance(a, b) == 1;

        public static IReadOnlyList<Cell> Line(Cell from, Cell to)
        {
            var result = new List<Cell>();
            int dx = Math.Abs(to.X - from.X), dy = Math.Abs(to.Y - from.Y);
            int sx = from.X < to.X ? 1 : -1, sy = from.Y < to.Y ? 1 : -1;
            int err = dx - dy;
            int x = from.X, y = from.Y;
            while (true)
            {
                result.Add(new Cell(x, y));
                if (x == to.X && y == to.Y) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x += sx; }
                if (e2 < dx) { err += dx; y += sy; }
            }
            return result;
        }

        public static Cell Home(Slot slot, bool userSide)
        {
            int x, y;
            switch (slot)
            {
                case Slot.GK: x = 0; y = 3; break;
                case Slot.LB: x = 2; y = 1; break;
                case Slot.RB: x = 2; y = 6; break;
                case Slot.CM: x = 4; y = 3; break;
                case Slot.LW: x = 5; y = 0; break;
                case Slot.RW: x = 5; y = 7; break;
                case Slot.ST: x = 6; y = 4; break;
                default: throw new ArgumentOutOfRangeException(nameof(slot));
            }
            return new Cell(userSide ? x : Width - 1 - x, y);
        }

        public static bool InShotZone(Cell c, bool forUser)
        {
            bool xOk = forUser ? c.X >= Width - 2 : c.X <= 1;
            return xOk && c.Y >= 2 && c.Y <= 5;
        }

        public static int AttackDir(bool userSide) => userSide ? 1 : -1;
    }
}
