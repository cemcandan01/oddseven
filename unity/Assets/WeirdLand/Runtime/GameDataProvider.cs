using System.IO;
using UnityEngine;
using WeirdLand.Core.Data;

namespace WeirdLand.Unity
{
    public sealed class LoadedData
    {
        public GameData Game;
        public UiText Text;
    }

    /// <summary>Finds the repo's data/ folder (editor) or StreamingAssets copy (player) and loads it through the core.</summary>
    public static class GameDataProvider
    {
        public static string DataDirectory()
        {
#if UNITY_EDITOR
            return Path.GetFullPath(Path.Combine(Application.dataPath, "../../data"));
#else
            return Path.Combine(Application.streamingAssetsPath, "data");
#endif
        }

        public static LoadedData Load(string dir)
        {
            if (!Directory.Exists(dir))
                throw new DataException("Data directory not found: " + Path.GetFullPath(dir));
            return new LoadedData { Game = DataLoader.Load(dir), Text = DataLoader.LoadUiText(dir) };
        }
    }
}
