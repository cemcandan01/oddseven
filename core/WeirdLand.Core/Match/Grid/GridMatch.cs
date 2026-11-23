using System;
using System.Collections.Generic;
using WeirdLand.Core.Data;
using WeirdLand.Core.Duels;

namespace WeirdLand.Core.Match
{
    public enum GridPhase { AwaitingAction, InDuel, Ended }

    public sealed class GridMatch
    {
        private readonly GameData _data;
        private readonly Rng _rng;
        private readonly List<GridPlayer> _players = new List<GridPlayer>();

        public IReadOnlyList<GridPlayer> Players => _players;
        public GridPlayer Carrier { get; private set; }
        public bool UserHasBall { get; private set; }
        public GridPhase Phase { get; private set; } = GridPhase.AwaitingAction;
        public int Move { get; private set; }
        public int UserScore { get; private set; }
        public int OppScore { get; private set; }
        public Duel CurrentDuel { get; private set; }

        public GridMatch(GameData data, TeamDef user, TeamDef opponent, uint seed)
        {
            _data = data;
            _rng = new Rng(seed);
            AddTeam(user, true);
            AddTeam(opponent, false);
        }

        private void AddTeam(TeamDef team, bool isUser)
        {
            foreach (Slot s in Enum.GetValues(typeof(Slot)))
            {
                var def = team.BySlot(s);
                if (def == null) continue;
                _players.Add(new GridPlayer(def, isUser, GridGeometry.Home(s, isUser),
                    DuelSetupHelpers.BuildDeck(_data, def)));
            }
        }

        public IReadOnlyList<GameEvent> Kickoff()
        {
            foreach (var p in _players) p.Pos = GridGeometry.Home(p.Def.Slot, p.IsUser);
            UserHasBall = _rng.Chance(0.5);
            Carrier = _players.Find(p => p.IsUser == UserHasBall && p.Def.Slot == Slot.CM);
            Phase = GridPhase.AwaitingAction;
            return new List<GameEvent> { new GridKickoff(UserHasBall, Carrier.Def.Id) };
        }

        public GridPlayer PlayerAt(Cell c)
        {
            foreach (var p in _players) if (p.Pos.Equals(c)) return p;
            return null;
        }
    }
}
