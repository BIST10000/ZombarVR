#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace Zombar
{
    // Included only in Editor, never in an APK. Drives validation from actual game frames.
    public sealed class BarValidationDriver : MonoBehaviour
    {
        private int frames;
        private bool checkedRules;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "ZombarEditor.BarBatchValidation.Run") < 0) return;
            if (FindAnyObjectByType<BarValidationDriver>() == null)
                new GameObject("Batch validation driver").AddComponent<BarValidationDriver>();
        }

        private void Update()
        {
            if (++frames < 12 || ZombieBarGame.Instance == null) return;
            try
            {
                var assembly = Assembly.Load("Assembly-CSharp-Editor");
                if (!checkedRules)
                {
                    var result = assembly.GetType("ZombarEditor.BarSmokeChecks").GetMethod("Run").Invoke(null, null);
                    File.AppendAllText("Builds/Validation/gameplay-checks.txt", "\nRuntime Update frames: " + frames + "; global frame: " + Time.frameCount);
                    Debug.Log(result);
                    checkedRules = true;
                    frames = 0;
                }
                else
                {
                    assembly.GetType("ZombarEditor.BarBatchValidation").GetMethod("Capture", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
                    Finish(0);
                }
            }
            catch (Exception e)
            {
                Directory.CreateDirectory("Builds/Validation");
                File.WriteAllText("Builds/Validation/failure.txt", e.ToString());
                Debug.LogException(e);
                Finish(1);
            }
        }

        private static void Finish(int result)
        {
            SessionState.SetBool("Zombar.Validation.Active", false);
            EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool("Zombar.Validation.OptionsEnabled", false);
            EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)SessionState.GetInt("Zombar.Validation.Options", 0);
            EditorApplication.Exit(result);
        }
    }
}
#endif
