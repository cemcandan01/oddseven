using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using WeirdLand.Core;
using WeirdLand.Core.Ai;
using WeirdLand.Core.Data;
using WeirdLand.Core.Duels;
using WeirdLand.Core.Match;

namespace WeirdLand.Unity.Tests
{
    /// <summary>
    /// Builds the same camera / canvas / event system the sandbox scene has, opens a duel on a DuelScreen with
    /// real data, and tears everything down again. Animation timings run at 1% speed while a rig is up.
    /// </summary>
    public static class TestRig
    {
        public const float FastScale = 0.01f;
        public const int MaxDecisions = 200;
        public const int MaxIdleFrames = 3000;

        private static readonly List<GameObject> Spawned = new List<GameObject>();
        private static float? _savedScale;
        private static LoadedData _data;

        public static Camera Camera { get; private set; }
        public static Canvas Canvas { get; private set; }

        public static LoadedData Data => _data ??= GameDataProvider.Load(GameDataProvider.DataDirectory());

        public static (DuelScreen screen, Duel duel) Create(ScenarioKind kind, uint seed)
            => Create(kind, seed, DuelScenarios.UserSlots(kind)[0]);

        public static (DuelScreen screen, Duel duel) Create(ScenarioKind kind, uint seed, Slot userSlot)
        {
            var canvas = CreateCanvas();
            var go = new GameObject("DuelScreen", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            var screen = go.AddComponent<DuelScreen>();
            var scenario = new DuelScenario("mavi", "kirmizi", kind, userSlot, DuelScenarios.DefaultEnemySlot(kind, userSlot));
            var duel = DuelScenarios.Create(Data.Game, scenario, seed);
            screen.Open(duel, Data, seed);
            return (screen, duel);
        }

        /// <summary>A grid match, mavi vs kirmizi, on a fresh rig canvas; kickoff starts playing at once.</summary>
        public static GridMatchController CreateGrid(uint seed)
        {
            var canvas = CreateCanvas();
            var go = new GameObject("GridMatch", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            var g = go.AddComponent<GridMatchController>();
            g.Open(Data, "mavi", "kirmizi", seed);
            return g;
        }

        /// <summary>Yields until the grid controller is idle, autoplaying any duel it opens on the way.</summary>
        public static IEnumerator Settle(GridMatchController g)
        {
            for (int i = 0; i < MaxDecisions; i++)
            {
                for (int f = 0; f < MaxIdleFrames && g.IsBusy && !g.DuelOpen; f++) yield return null;
                if (!g.DuelOpen) break;
                var duel = g.Match.CurrentDuel;
                yield return Autoplay(g.DuelView, duel, 1);
                for (int f = 0; f < MaxIdleFrames && g.DuelOpen && g.Match.CurrentDuel == duel; f++) yield return null;
            }
            Assert.That(g.IsBusy, Is.False, "grid controller stayed busy");
        }

        /// <summary>Settles until it is the user's move (or the match is over).</summary>
        public static IEnumerator UserTurn(GridMatchController g)
        {
            yield return Settle(g);
        }

        /// <summary>Orthographic camera, ScreenSpaceCamera canvas scaled from 1920x1080 (match 0.5), Input System event system.</summary>
        public static Canvas CreateCanvas()
        {
            var cat = VisualCatalog.Load();
            if (!_savedScale.HasValue) _savedScale = cat.Timings.Scale;
            cat.Timings.Scale = FastScale;

            var camGo = Track(new GameObject("TestCamera"));
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = cat.Background;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            cam.transform.position = new Vector3(0, 0, -10);

            var canvasGo = Track(new GameObject("Canvas", typeof(RectTransform)));
            canvasGo.layer = 5;
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            Track(new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)));

            Camera = cam;
            Canvas = canvas;
            return canvas;
        }

        /// <summary>Destroys everything the rig built and restores the catalog's timing scale (the asset is never saved).</summary>
        public static void Teardown()
        {
            foreach (var go in Spawned)
                if (go != null) Object.Destroy(go);
            Spawned.Clear();
            if (_savedScale.HasValue)
            {
                VisualCatalog.Load().Timings.Scale = _savedScale.Value;
                _savedScale = null;
            }
            Camera = null;
            Canvas = null;
        }

        private static GameObject Track(GameObject go)
        {
            Spawned.Add(go);
            return go;
        }

        /// <summary>Yields frames until the controller is idle; fails the test if it never gets there.</summary>
        public static IEnumerator Idle(DuelScreen screen)
        {
            for (int i = 0; i < MaxIdleFrames && (screen.Controller == null || screen.Controller.IsBusy); i++) yield return null;
            Assert.That(screen.Controller, Is.Not.Null, "screen has no controller");
            Assert.That(screen.Controller.IsBusy, Is.False, "controller stayed busy");
        }

        /// <summary>Clicks what the action says, through the same hooks the buttons use.</summary>
        public static void Act(DuelScreen screen, DuelAction action)
        {
            switch (action)
            {
                case PlayCardAction p:
                    screen.ClickCard(p.HandIndex);
                    break;
                case PassAction p:
                    screen.ClickPass();
                    screen.ClickPassTarget(p.TargetId);
                    break;
                default:
                    screen.ClickEndTurn();
                    break;
            }
        }

        /// <summary>
        /// Plays the duel with GoodPolicy until the result overlay is up. Each iteration waits for the
        /// controller to go idle and then makes one decision; at most <see cref="MaxDecisions"/> decisions.
        /// </summary>
        public static IEnumerator Autoplay(DuelScreen screen, Duel duel, uint seed)
        {
            var policy = new GoodPolicy();
            var rng = new Rng(seed);
            for (int i = 0; i < MaxDecisions && !screen.ResultShown; i++)
            {
                yield return Idle(screen);
                if (screen.ResultShown || duel.Outcome.HasValue) break;
                Act(screen, policy.Decide(duel, null, rng));
                yield return null;
            }
            yield return Idle(screen);
        }
    }
}
