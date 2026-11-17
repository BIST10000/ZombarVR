using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR.Management;

namespace ZombarEditor
{
    public static class DesktopBuild
    {
        [MenuItem("НЕКРО-ПАБ/Собрать Windows для проверки без шлема")]
        public static void BuildWindows()
        {
            QuestBuild.PrepareScene();
            Directory.CreateDirectory("Builds/Desktop");
            var xr = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            bool startXR = xr != null && xr.InitManagerOnStart;
            var loaders=xr!=null&&xr.Manager!=null?xr.Manager.activeLoaders.ToList():null;
            var backend=PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
            try
            {
                if (xr != null) { xr.InitManagerOnStart = false; EditorUtility.SetDirty(xr); }
                if(loaders!=null) { xr.Manager.TrySetLoaders(new List<XRLoader>());EditorUtility.SetDirty(xr.Manager); }
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
                AssetDatabase.SaveAssets();
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = new[] { "Assets/Scenes/ZombieBar.unity" },
                    locationPathName = "Builds/Desktop/NecroPub.exe",
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.Development
                });
                File.WriteAllText("Builds/Desktop/build-result.txt", report.summary.result+"\n"+
                    report.summary.totalErrors+" errors\n"+report.summary.totalSize+" bytes");
                if(report.summary.result!=BuildResult.Succeeded) throw new BuildFailedException("Desktop build failed");
                File.Copy("Assets/Zombar/Art/External/Environment/PROVENANCE.md","Builds/Desktop/ASSET-CREDITS.md",true);
                Debug.Log("Desktop rehearsal ready: Builds/Desktop/NecroPub.exe");
            }
            finally
            {
                if(loaders!=null) { xr.Manager.TrySetLoaders(loaders);EditorUtility.SetDirty(xr.Manager); }
                if(xr!=null) { xr.InitManagerOnStart=startXR;EditorUtility.SetDirty(xr);AssetDatabase.SaveAssets(); }
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,backend);
            }
        }
    }
}
