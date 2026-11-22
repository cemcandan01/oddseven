using System.Collections.Generic;
using WeirdLand.Core.Data;
using WeirdLand.Core.Duels;

namespace WeirdLand.Core.Match
{
    public class PlayerState
    {
        public PlayerDef Def;
        public int Kondisyon = MatchRules.StartKondisyon;
        public int Hype;
        public bool Alev;
        public List<CardInstance> Deck = new List<CardInstance>();
    }
}
