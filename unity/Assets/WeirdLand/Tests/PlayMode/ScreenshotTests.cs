using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using WeirdLand.Core.Duels;

namespace WeirdLand.Unity.Tests
{
    /// <summary>
    /// Screenshot tool, not a check: run through `tools/unity.sh shot` (sets WEIRDLAND_SHOTS=1). For every scenario
    /// kind at seed 1 it saves unity/Screenshots/&lt;kind&gt;-1.png once the first turn has been dealt (opening
    /// animations done), -2 after a card is played, and -3 on the result screen. Shot 1 is not taken after an
    /// EndTurn: with the rig's default slots an idle first turn already ends three of the four duels.
    /// </summary>
    public class ScreenshotTests
    {
        private const int Width = 1920, Height = 1080;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            TestRig.Teardown();
            yield return null;
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Capture()
        {
            if (Environment.GetEnvironmentVariable("WEIRDLAND_SHOTS") != "1")
                Assert.Ignore("Set WEIRDLAND_SHOTS=1 (tools/unity.sh shot) to take screenshots.");

            var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Screenshots"));
            Directory.CreateDirectory(dir);
            foreach (ScenarioKind kind in Enum.GetValues(typeof(ScenarioKind)))
            {
                var (screen, duel) = TestRig.Create(kind, 1);
                yield return TestRig.Idle(screen);
                yield return Shoot(Path.Combine(dir, kind + "-1.png"));

                for (int i = 0; i < screen.HandCardCount; i++)
                {
                    if (!DuelPreview.CanPlay(duel, i)) continue;
                    screen.ClickCard(i);
                    break;
                }
                yield return TestRig.Idle(screen);
                yield return Shoot(Path.Combine(dir, kind + "-2.png"));

                yield return TestRig.Autoplay(screen, duel, 1);
                yield return Shoot(Path.Combine(dir, kind + "-3.png"));

                TestRig.Teardown();
                yield return null;
            }
        }

        /// <summary>Draws the rig camera into a 1920x1080 target (the canvas re-lays itself out for it) and saves a PNG.</summary>
        private static IEnumerator Shoot(string path)
        {
            var cam = TestRig.Camera;
            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                cam.targetTexture = rt;
                yield return null; // let the canvas scaler pick up the target size
                Canvas.ForceUpdateCanvases();
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Debug.Log("[Screenshot] " + path);
            }
            finally
            {
                RenderTexture.active = previous;
                if (cam != null) cam.targetTexture = null;
                UnityEngine.Object.Destroy(tex);
                rt.Release();
                UnityEngine.Object.Destroy(rt);
            }
        }
    }
}
