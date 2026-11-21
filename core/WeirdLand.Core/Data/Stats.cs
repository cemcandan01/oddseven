using System;

namespace WeirdLand.Core.Data
{
    public class Stats
    {
        public int Speed { get; set; }
        public int Power { get; set; }
        public int Passing { get; set; }
        public int Dribbling { get; set; }
        public int Shooting { get; set; }
        public int Defending { get; set; }
        public int Aerial { get; set; }
        public int Near { get; set; }
        public int Far { get; set; }

        public int Get(Stat s)
        {
            switch (s)
            {
                case Stat.Speed: return Speed;
                case Stat.Power: return Power;
                case Stat.Passing: return Passing;
                case Stat.Dribbling: return Dribbling;
                case Stat.Shooting: return Shooting;
                case Stat.Defending: return Defending;
                case Stat.Aerial: return Aerial;
                case Stat.Near: return Near;
                case Stat.Far: return Far;
                default: throw new ArgumentOutOfRangeException(nameof(s));
            }
        }
    }
}
