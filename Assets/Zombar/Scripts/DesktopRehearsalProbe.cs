#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.XR;

namespace Zombar
{
    // Opt-in packaged-player check. Does not run during normal play or in a Quest build.
    public sealed class DesktopRehearsalProbe : MonoBehaviour
    {
        static int exceptions;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-rehearsal-smoke")<0)return;
            Application.logMessageReceived+=(message,trace,type)=> { if(type==LogType.Exception||type==LogType.Assert)exceptions++; };
            Application.runInBackground=true;
            new GameObject("Desktop build validation").AddComponent<DesktopRehearsalProbe>();
        }
        IEnumerator Start()
        {
            int start=Time.frameCount;
            for(int i=0;i<60;i++)yield return null;
            var stream=GameObject.Find("Струя крови")?.GetComponent<LineRenderer>();
            var music=GameObject.Find("Музыка бара")?.GetComponent<AudioSource>();
            bool musicPlaying=music!=null && music.isPlaying && music.timeSamples>0;
            bool okay=musicPlaying&&exceptions==0&&stream!=null&&stream.sharedMaterial!=null&&
                Camera.main!=null&&PhysicalBar.Instance!=null&&ZombieBarGame.Instance!=null&&
                BarProp.All.Count>=12&&PhysicalPatron.All.Count>=2&&!XRSettings.isDeviceActive&&Time.frameCount>start+40;
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            File.WriteAllText(Path.Combine(folder,"runtime-check.txt"),(okay?"PASS":"FAIL")+
                " desktop player without VR\nFrames: "+(Time.frameCount-start)+"\nProps: "+BarProp.All.Count+
                "\nPatrons: "+PhysicalPatron.All.Count+"\nXR active: "+XRSettings.isDeviceActive+
                "\nExceptions: "+exceptions+"\nBlood stream material: "+(stream!=null&&stream.sharedMaterial!=null)+
                "\nMusic playing: "+musicPlaying+"\nMusic sample: "+(music!=null?music.timeSamples:0));
            if(okay)
            {
                var camera=Camera.main;var target=new RenderTexture(1440,900,24);
                var old=camera.targetTexture;var active=RenderTexture.active;
                camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                var png=new Texture2D(1440,900,TextureFormat.RGB24,false);
                png.ReadPixels(new Rect(0,0,1440,900),0,0);png.Apply();
                File.WriteAllBytes(Path.Combine(folder,"desktop-runtime.png"),png.EncodeToPNG());
                camera.targetTexture=old;RenderTexture.active=active;target.Release();Destroy(target);Destroy(png);
            }
            Application.Quit(okay?0:1);
        }
    }
}
#endif
