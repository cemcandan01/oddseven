using System;
using System.IO;
using UnityEngine;
using WeirdLand.Core.Data;
using WeirdLand.Core.Duels;

namespace WeirdLand.Unity
{
    /// <summary>
    /// Sandbox entry point (first version): loads the data and opens the default duel, mavi vs kirmizi,
    /// FieldAttack, ST against CM, with a fresh seed. The scenario picker and error panel come in Task 9.
    /// </summary>
    public class AppFlow : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;

        private LoadedData _data;
        private DuelScenario _scenario;
        private uint _seed;

        public DuelScreen DuelView { get; private set; }

        private void Start()
        {
            if (_canvas == null) _canvas = GetComponentInChildren<Canvas>();
            try
            {
                _data = GameDataProvider.Load(GameDataProvider.DataDirectory());
            }
            catch (Exception ex) when (ex is DataException || ex is IOException)
            {
                Debug.LogError("[AppFlow] " + ex.Message);
                return;
            }

            var go = new GameObject("DuelScreen", typeof(RectTransform)) { layer = UiFactory.UiLayer };
            go.transform.SetParent(_canvas.transform, false);
            DuelView = go.AddComponent<DuelScreen>();
            DuelView.RetrySameSeed += () => OpenDuel(_seed);
            DuelView.RetryNewSeed += () => OpenDuel(NewSeed());
            DuelView.BackRequested += () => OpenDuel(NewSeed()); // no picker yet: back starts a fresh duel

            _scenario = new DuelScenario("mavi", "kirmizi", ScenarioKind.FieldAttack, Slot.ST, Slot.CM);
            OpenDuel(NewSeed());
        }

        private static uint NewSeed() => (uint)Environment.TickCount;

        private void OpenDuel(uint seed)
        {
            _seed = seed;
            DuelView.Open(DuelScenarios.Create(_data.Game, _scenario, seed), _data, seed);
        }
    }
}
