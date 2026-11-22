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
    /// system, a ScreenSpaceCamera canvas scaled from 1920x1080, and AppFlow on the root object. The scene is
    /// the only entry in the build settings.
    /// </summary>
    public static class SceneBuilder
    {
        public const string ScenePath = "Assets/WeirdLand/Scenes/DuelSandbox.unity";

        [MenuItem("WeirdLand/Build Scenes")]
        public static void BuildAll()
        {
            PrefabBuilder.BuildAll();
            var cat = AssetDatabase.LoadAssetAtPath<VisualCatalog>(PrefabBuilder.CatalogPath);

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
            so.ApplyModifiedPropertiesWithoutUndo();

            EnsureFolder("Assets/WeirdLand/Scenes");
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new System.InvalidOperationException("Could not save " + ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[SceneBuilder] built " + ScenePath);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}
