using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ZombarEditor
{
    public static class BarBatchValidation
    {
        public static void Run()
        {
            Directory.CreateDirectory("Builds/Validation");
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-refreshPortraits") >= 0)
                PhysicalBarSceneBuilder.Build();
            QuestBuild.Configure();
            QuestBuild.PrepareScene();
            SessionState.SetBool("Zombar.Validation.OptionsEnabled", EditorSettings.enterPlayModeOptionsEnabled);
            SessionState.SetInt("Zombar.Validation.Options", (int)EditorSettings.enterPlayModeOptions);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            Application.runInBackground = true;
            EditorApplication.EnterPlaymode();
        }

        private static void Capture()
        {
            Camera camera = Camera.main;
            var metrics = new System.Text.StringBuilder();
            metrics.AppendLine("Camera: " + camera.transform.position + "; FOV: " + camera.fieldOfView);
            foreach (var prop in UnityEngine.Object.FindObjectsByType<Zombar.BarProp>())
                metrics.AppendLine(prop.kind + ": " + prop.transform.position + "; collider: " + prop.GetComponent<Collider>().bounds.size);
            File.WriteAllText("Builds/Validation/scene-metrics.txt", metrics.ToString());
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
                CapturePortrait(camera, target, image);
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

        private static void CapturePortrait(Camera player, RenderTexture target, Texture2D image)
        {
            var patrons = UnityEngine.Object.FindObjectsByType<Zombar.PhysicalPatron>();
            if (patrons.Length == 0) throw new InvalidOperationException("No patron in portrait capture.");
            var patron = patrons[0];
            var head = Array.Find(patron.GetComponentsInChildren<Transform>(), bone => bone.name == "Head");
            if (head == null) throw new InvalidOperationException("Imported portrait is missing its head bone.");
            var focus = head.position + Vector3.up * .18f;
            var obj = new GameObject("Editorial camera");
            var cam = obj.AddComponent<Camera>();
            cam.CopyFrom(player);
            cam.fieldOfView = 38;
            cam.transform.position = focus + new Vector3(.32f,.08f,-.96f);
            cam.transform.LookAt(focus);
            cam.targetTexture = target;
            var lettering = UnityEngine.Object.FindObjectsByType<TextMesh>();
            foreach (var label in lettering) label.GetComponent<Renderer>().enabled = false;
            cam.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
            image.Apply();
            File.WriteAllBytes("Builds/Validation/cellar-in-game.png",image.EncodeToPNG());
            foreach (var label in lettering) label.GetComponent<Renderer>().enabled = true;
            cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(obj);
        }

    }
}

