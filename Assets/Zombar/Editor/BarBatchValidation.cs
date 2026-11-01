using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ZombarEditor
{
    [InitializeOnLoad]
    public static class BarBatchValidation
    {
        private static double deadline;
        private static int startFrame;
        private static bool captured;
        private static bool previousOptionsEnabled;
        private static EnterPlayModeOptions previousOptions;

        static BarBatchValidation()
        {
            if (!SessionState.GetBool("Zombar.Validation.Active", false)) return;
            deadline = EditorApplication.timeSinceStartup + 120;
            previousOptionsEnabled = SessionState.GetBool("Zombar.Validation.OptionsEnabled", false);
            previousOptions = (EnterPlayModeOptions)SessionState.GetInt("Zombar.Validation.Options", 0);
        }

        public static void Run()
        {
            Directory.CreateDirectory("Builds/Validation");
            QuestBuild.Configure();
            QuestBuild.PrepareScene();
            previousOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            previousOptions = EditorSettings.enterPlayModeOptions;
            SessionState.SetBool("Zombar.Validation.Active", true);
            SessionState.SetBool("Zombar.Validation.OptionsEnabled", previousOptionsEnabled);
            SessionState.SetInt("Zombar.Validation.Options", (int)previousOptions);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            Application.runInBackground = true;
            deadline = EditorApplication.timeSinceStartup + 120;
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            try
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Play-mode validation timed out.");
                EditorApplication.QueuePlayerLoopUpdate();
                Application.runInBackground = true;
                if (!Application.isPlaying || Zombar.ZombieBarGame.Instance == null) return;
                if (startFrame == 0) { startFrame = Time.frameCount; return; }
                if (Time.frameCount < startFrame + 10) return;
                if (!captured)
                {
                    string result = BarSmokeChecks.Run();
                    File.AppendAllText("Builds/Validation/gameplay-checks.txt", "\nFrames advanced: " + startFrame + " -> " + Time.frameCount);
                    Debug.Log(result);
                    captured = true;
                    startFrame = Time.frameCount;
                    return;
                }
                Capture();
                Finish(0);
            }
            catch (Exception e)
            {
                File.WriteAllText("Builds/Validation/failure.txt", e.ToString());
                Debug.LogException(e);
                Finish(1);
            }
        }

        private static void Capture()
        {
            Camera camera = Camera.main;
            var target = new RenderTexture(1440, 900, 24);
            var previous = camera.targetTexture;
            var active = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                foreach (var label in UnityEngine.Object.FindObjectsByType<Zombar.BarLabelLayout>()) label.Fit();
                camera.Render();
                RenderTexture.active = target;
                var image = new Texture2D(1440, 900, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1440, 900), 0, 0);
                image.Apply();
                File.WriteAllBytes("Builds/Validation/necro-pub.png", image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);
            }
            finally
            {
                camera.targetTexture = previous;
                RenderTexture.active = active;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void Finish(int code)
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool("Zombar.Validation.Active", false);
            EditorSettings.enterPlayModeOptionsEnabled = previousOptionsEnabled;
            EditorSettings.enterPlayModeOptions = previousOptions;
            EditorApplication.Exit(code);
        }
    }
}
