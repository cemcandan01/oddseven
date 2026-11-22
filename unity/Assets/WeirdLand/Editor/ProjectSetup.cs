using System.IO;
using UnityEditor;
using UnityEngine;

namespace WeirdLand.Unity.EditorTools
{
    /// <summary>
    /// One-shot project setup, run in batch mode by tools/unity.sh setup.
    /// AssetDatabase.ImportPackage finishes on a later editor tick, so the
    /// batch run is ended from the import callbacks instead of -quit.
    /// </summary>
    public static class ProjectSetup
    {
        public static void Run()
        {
            AssetDatabase.importPackageCompleted += OnImportCompleted;
            AssetDatabase.importPackageFailed += OnImportFailed;
            AssetDatabase.ImportPackage(
                Path.GetFullPath("Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage"),
                false);
        }

        static void OnImportCompleted(string packageName)
        {
            Unsubscribe();
            EditorSettings.serializationMode = SerializationMode.ForceText;
            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectSetup] done: " + packageName);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void OnImportFailed(string packageName, string error)
        {
            Unsubscribe();
            Debug.LogError("[ProjectSetup] import failed: " + packageName + ": " + error);
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }

        static void Unsubscribe()
        {
            AssetDatabase.importPackageCompleted -= OnImportCompleted;
            AssetDatabase.importPackageFailed -= OnImportFailed;
        }
    }
}
