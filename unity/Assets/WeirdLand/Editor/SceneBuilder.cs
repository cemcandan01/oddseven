using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WeirdLand.Unity.EditorTools
{
    /// <summary>
    /// Generates the sandbox scene from code (tools/unity.sh scenes): orthographic camera, Input System event
    /// system, a ScreenSpaceCamera canvas scaled from 1920x1080, and AppFlow on the root object. Two scenes:
    /// DuelSandbox (menu) and GridMatch (grid match on start), both in the build settings.
    /// </summary>
    public static class SceneBuilder
    {
        public const string ScenePath = "Assets/WeirdLand/Scenes/DuelSandbox.unity";
        public const string GridScenePath = "Assets/WeirdLand/Scenes/GridMatch.unity";

        [MenuItem("WeirdLand/Build Scenes")]
        public static void BuildAll()
        {
            PrefabBuilder.BuildAll();
            var cat = AssetDatabase.LoadAssetAtPath<VisualCatalog>(PrefabBuilder.CatalogPath);
            EnsureFolder("Assets/WeirdLand/Scenes");
            BuildScene(cat, ScenePath, false);
            BuildScene(cat, GridScenePath, true);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(GridScenePath, true),
            };
            AssetDatabase.SaveAssets();
        }

        /// <summary>Sandbox scene (menu first) or GridMatch scene (AppFlow opens the grid match directly).</summary>
        private static void BuildScene(VisualCatalog cat, string path, bool startGrid)
        {

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = cat.Background;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            camGo.transform.position = new Vector3(0, 0, -10);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var app = new GameObject("App");
            var canvasGo = new GameObject("Canvas", typeof(RectTransform)) { layer = UiFactory.UiLayer };
            canvasGo.transform.SetParent(app.transform, false);
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

            var flow = app.AddComponent<AppFlow>();
            var so = new SerializedObject(flow);
            so.FindProperty("_canvas").objectReferenceValue = canvas;
            so.FindProperty("_startGrid").boolValue = startGrid;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (!EditorSceneManager.SaveScene(scene, path))
                throw new System.InvalidOperationException("Could not save " + path);
            Debug.Log("[SceneBuilder] built " + path);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}
