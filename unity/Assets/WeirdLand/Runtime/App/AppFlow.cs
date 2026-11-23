using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using WeirdLand.Core.Data;
using WeirdLand.Core.Duels;

namespace WeirdLand.Unity
{
    /// <summary>
    /// Sandbox entry point: loads the data and shows a small menu with two entries. "Düello" opens the default
    /// duel (mavi vs kirmizi, FieldAttack, ST against CM, fresh seed) exactly as before; "Maç (ızgara)" starts a
    /// grid match, mavi vs the first other team, seed 1. With _startGrid set (GridMatch scene) the grid match
    /// opens directly. Back buttons return to the menu.
    /// </summary>
    public class AppFlow : MonoBehaviour
    {
        public const string UserTeam = "mavi";
        public const uint GridSeed = 1;

        [SerializeField] private Canvas _canvas;
        [SerializeField] private bool _startGrid;

        private LoadedData _data;
        private DuelScenario _scenario;
        private uint _seed;
        private RectTransform _menu;

        public DuelScreen DuelView { get; private set; }
        public GridMatchController GridView { get; private set; }
        public bool MenuShown => _menu != null && _menu.gameObject.activeSelf;

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
            _scenario = new DuelScenario(UserTeam, "kirmizi", ScenarioKind.FieldAttack, Slot.ST, Slot.CM);
            BuildMenu();
            if (_startGrid) OpenGrid();
            else ShowMenu();
        }

        private void BuildMenu()
        {
            var cat = VisualCatalog.Load();
            _menu = UiFactory.Stretch(UiFactory.Rect("Menu", _canvas.transform));
            var title = UiFactory.Label("Title", _menu, 64, Color.white);
            title.fontStyle = FontStyles.Bold;
            UiFactory.Center(title.rectTransform, new Vector2(0, 200), new Vector2(900, 100));
            title.text = _data.Text.Get(GridUiKeys.MenuTitle);
            var duel = UiFactory.Button(cat, _menu, "Menu_Duel", _data.Text.Get(GridUiKeys.MenuDuel), OpenDuelSandbox);
            UiFactory.Center((RectTransform)duel.transform, new Vector2(0, 40), new Vector2(420, 90));
            var grid = UiFactory.Button(cat, _menu, "Menu_Grid", _data.Text.Get(GridUiKeys.Entry), OpenGrid);
            UiFactory.Center((RectTransform)grid.transform, new Vector2(0, -70), new Vector2(420, 90));
        }

        public void ShowMenu()
        {
            if (DuelView != null) DuelView.gameObject.SetActive(false);
            if (GridView != null) GridView.gameObject.SetActive(false);
            _menu.gameObject.SetActive(true);
        }

        public void OpenDuelSandbox()
        {
            _menu.gameObject.SetActive(false);
            if (DuelView == null)
            {
                var go = new GameObject("DuelScreen", typeof(RectTransform)) { layer = UiFactory.UiLayer };
                go.transform.SetParent(_canvas.transform, false);
                DuelView = go.AddComponent<DuelScreen>();
                DuelView.RetrySameSeed += () => OpenDuel(_seed);
                DuelView.RetryNewSeed += () => OpenDuel(NewSeed());
                DuelView.BackRequested += ShowMenu;
            }
            OpenDuel(NewSeed());
        }

        public void OpenGrid()
        {
            _menu.gameObject.SetActive(false);
            if (GridView == null)
            {
                var go = new GameObject("GridMatch", typeof(RectTransform)) { layer = UiFactory.UiLayer };
                go.transform.SetParent(_canvas.transform, false);
                GridView = go.AddComponent<GridMatchController>();
                GridView.BackRequested += ShowMenu;
            }
            GridView.Open(_data, UserTeam, OpponentTeam(), GridSeed);
        }

        /// <summary>The scenario's opponent when it exists, else the first other team by id (stable order).</summary>
        private string OpponentTeam()
        {
            if (_data.Game.Teams.ContainsKey(_scenario.OpponentTeamId)) return _scenario.OpponentTeamId;
            return _data.Game.Teams.Keys.Where(k => k != UserTeam).OrderBy(k => k, StringComparer.Ordinal).First();
        }

        private static uint NewSeed() => (uint)Environment.TickCount;

        private void OpenDuel(uint seed)
        {
            _seed = seed;
            DuelView.Open(DuelScenarios.Create(_data.Game, _scenario, seed), _data, seed);
        }
    }
}
