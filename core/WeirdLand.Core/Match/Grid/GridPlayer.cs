using System.Collections.Generic;
using WeirdLand.Core.Data;
using WeirdLand.Core.Duels;

namespace WeirdLand.Core.Match
{
    public sealed class GridPlayer
    {
        public PlayerDef Def { get; }
        public bool IsUser { get; }
        public Cell Pos { get; internal set; }
        public int Stun { get; internal set; }
        internal List<CardInstance> Deck { get; }

        internal GridPlayer(PlayerDef def, bool isUser, Cell pos, List<CardInstance> deck)
        {
            Def = def; IsUser = isUser; Pos = pos; Deck = deck;
        }
    }
}
