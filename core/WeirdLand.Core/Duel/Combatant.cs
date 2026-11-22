using System.Collections.Generic;
using System.Collections.ObjectModel;
using WeirdLand.Core.Data;

namespace WeirdLand.Core.Duels
{
    public class Combatant
    {
        private readonly IReadOnlyDictionary<StatusId, int> _statusesView;
        private readonly IReadOnlyList<CardInstance> _drawView;
        private readonly IReadOnlyList<CardInstance> _handView;
        private readonly IReadOnlyList<CardInstance> _discardView;

        public Combatant()
        {
            _statusesView = new ReadOnlyDictionary<StatusId, int>(StatusMap);
            _drawView = DrawList.AsReadOnly();
            _handView = HandList.AsReadOnly();
            _discardView = DiscardList.AsReadOnly();
        }

        public PlayerDef Player { get; internal set; }
        /// <summary>True when this combatant is the user's side; used for event flags.</summary>
        public bool IsUser { get; internal set; }
        public int Hp { get; internal set; }
        public int MaxHp { get; internal set; }
        public int Block { get; internal set; }
        public int Energy { get; internal set; }
        public int KondisyonSpent { get; internal set; }
        public int PowerScalePercent { get; internal set; } = 100;

        internal Dictionary<StatusId, int> StatusMap { get; } = new Dictionary<StatusId, int>();
        internal List<CardInstance> DrawList { get; } = new List<CardInstance>();
        internal List<CardInstance> HandList { get; } = new List<CardInstance>();
        internal List<CardInstance> DiscardList { get; } = new List<CardInstance>();

        public IReadOnlyDictionary<StatusId, int> Statuses => _statusesView;
        public IReadOnlyList<CardInstance> DrawPile => _drawView;
        public IReadOnlyList<CardInstance> Hand => _handView;
        public IReadOnlyList<CardInstance> Discard => _discardView;

        public int StatusOf(StatusId s) => StatusMap.TryGetValue(s, out var n) ? n : 0;
    }
}
