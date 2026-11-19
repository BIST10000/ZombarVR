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
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-refreshHandProps") >= 0)
                ExternalEnvironmentBuilder.UpdateHandProps();
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-refreshEnvironment") >= 0)
                ExternalEnvironmentBuilder.Build();
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-refreshPortraits") >= 0)
                PhysicalBarSceneBuilder.Build();
            else if (Array.IndexOf(Environment.GetCommandLineArgs(), "-refreshCharacters") >= 0)
                ExternalCharacterBuilder.BuildAll();
            QuestBuild.PrepareScene();
            SessionState.SetBool("Zombar.Validation.OptionsEnabled", EditorSettings.enterPlayModeOptionsEnabled);
            SessionState.SetInt("Zombar.Validation.Options", (int)EditorSettings.enterPlayModeOptions);
            // Recreate runtime registries exactly as a fresh player launch does.
            // Configuring XR build defines here can trigger a second reload during Play entry.
            EditorSettings.enterPlayModeOptionsEnabled = false;
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
            var patron = Array.Find(patrons, p => !p.giant) ?? patrons[0];
            var head = Array.Find(patron.GetComponentsInChildren<Transform>(), bone => bone.name.EndsWith("Head"));
            if (head == null) throw new InvalidOperationException("Imported portrait is missing its head bone.");
            var focus = head.position + Vector3.up * .08f;
            var obj = new GameObject("Editorial camera");
            var cam = obj.AddComponent<Camera>();
            cam.CopyFrom(player);
            cam.fieldOfView = 38;
            cam.transform.position = focus + new Vector3(.32f,.08f,-.96f);
            cam.transform.LookAt(focus);
            cam.targetTexture = target;
            var lettering = UnityEngine.Object.FindObjectsByType<TextMesh>();
            foreach (var label in lettering) label.GetComponent<Renderer>().enabled = false;
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>();
            var canvasStates = Array.ConvertAll(canvases, canvas => canvas.enabled);
            foreach (var canvas in canvases) canvas.enabled = false;
            cam.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
            image.Apply();
            File.WriteAllBytes("Builds/Validation/cellar-in-game.png",image.EncodeToPNG());
            cam.fieldOfView = 62;
            cam.transform.position = new Vector3(0,1.68f,-1.3f);
            cam.transform.LookAt(new Vector3(0,1.5f,-4.2f));
            cam.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
            image.Apply();
            File.WriteAllBytes("Builds/Validation/rear-wall.png",image.EncodeToPNG());
            cam.transform.position = new Vector3(0,1.9f,-3.3f);
            cam.transform.LookAt(new Vector3(.15f,1.65f,.55f));
            cam.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
            image.Apply();
            File.WriteAllBytes("Builds/Validation/titan-and-patron.png",image.EncodeToPNG());
            cam.transform.position = new Vector3(-2.85f,1.63f,-2.6f);
            cam.transform.LookAt(new Vector3(.2f,1f,1.4f));
            cam.Render(); RenderTexture.active = target;
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
            image.Apply();
            File.WriteAllBytes("Builds/Validation/taproom-wide.png",image.EncodeToPNG());
            cam.transform.position = new Vector3(.20f,1.76f,1.08f);
            cam.transform.LookAt(new Vector3(.15f,2.48f,6.08f));
            cam.fieldOfView=65;
            cam.Render(); RenderTexture.active=target;
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
            File.WriteAllBytes("Builds/Validation/patron-wall.png",image.EncodeToPNG());
            CaptureProps(cam,target);
            CaptureGrips(cam,target);
            var sheet = new Texture2D(1080,540,TextureFormat.RGB24,false);
            int column = 0;
            foreach (var name in new[] {"Eye","Flesh"})
            {
                var visual = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("PhysicalBar/Props/"+name),
                    new Vector3(.75f,1.255f,-.19f),Quaternion.identity);
                var ingredientCenter = visual.GetComponentInChildren<Renderer>().bounds.center;
                cam.fieldOfView = 40;
                cam.transform.position = ingredientCenter + new Vector3(.01f,.10f,-.18f) * (name=="Eye"?.52f:.84f);
                cam.transform.LookAt(ingredientCenter);
                cam.Render(); RenderTexture.active = target;
                sheet.ReadPixels(new Rect((target.width-540)/2,(target.height-540)/2,540,540),column++*540,0);
                UnityEngine.Object.DestroyImmediate(visual);
            }
            sheet.Apply();
            File.WriteAllBytes("Builds/Validation/ingredients.png",sheet.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(sheet);
            var cup = Zombar.PhysicalBar.Instance.shaker;
            var lastCupPosition = cup.transform.position;
            var lastCupRotation = cup.transform.rotation;
            cup.transform.SetPositionAndRotation(cup.home, Quaternion.identity);
            var demoEye = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("PhysicalBar/Props/Eye"),cup.home + new Vector3(.075f,0,-.05f),Quaternion.identity);
            var demoFlesh = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("PhysicalBar/Props/Flesh"),cup.home + new Vector3(.132f,0,-.05f),Quaternion.identity);
            cam.transform.position = cup.home + new Vector3(.17f,.32f,-.47f);
            cam.transform.LookAt(cup.home + new Vector3(.052f,.098f,0));
            cam.fieldOfView = 39;
            cam.Render(); RenderTexture.active = target;
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
            image.Apply();
            File.WriteAllBytes("Builds/Validation/ingredient-scale.png",image.EncodeToPNG());
            cup.transform.SetPositionAndRotation(lastCupPosition,lastCupRotation);
            UnityEngine.Object.DestroyImmediate(demoEye);
            UnityEngine.Object.DestroyImmediate(demoFlesh);
            foreach (var label in lettering) label.GetComponent<Renderer>().enabled = true;
            for (int i = 0; i < canvases.Length; i++) canvases[i].enabled = canvasStates[i];
            cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(obj);
        }

        static void CaptureProps(Camera cam,RenderTexture target)
        {
            var kinds = new[] { Zombar.PropKind.BloodBottle, Zombar.PropKind.Shaker, Zombar.PropKind.Cleaver };
            const int side=540;
            var sheet=new Texture2D(side*kinds.Length,side,TextureFormat.RGB24,false);
            cam.fieldOfView=47;
            for(int i=0;i<kinds.Length;i++)
            {
                var prop=Zombar.BarProp.All.Find(p=>p.kind==kinds[i]);
                var focus=prop.GetComponent<BoxCollider>().bounds.center;
                Vector3 offset=kinds[i]==Zombar.PropKind.Cleaver ? new Vector3(.04f,.58f,-.26f) :
                    new Vector3(.10f,.18f,-(kinds[i]==Zombar.PropKind.Shaker?.43f:.62f));
                cam.transform.position=focus+offset; cam.transform.LookAt(focus);
                cam.Render();RenderTexture.active=target;
                sheet.ReadPixels(new Rect((target.width-side)*.5f,(target.height-side)*.5f,side,side),i*side,0);
            }
            sheet.Apply();File.WriteAllBytes("Builds/Validation/hand-props.png",sheet.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(sheet);
        }

        // Use the live grab and movement paths so this view also catches incorrect pivots.
        static void CaptureGrips(Camera cam,RenderTexture target)
        {
            var hand=Array.Find(UnityEngine.Object.FindObjectsByType<Zombar.BarHand>(),
                h=>h.node==UnityEngine.XR.XRNode.RightHand);
            var kinds=new[] { Zombar.PropKind.BloodBottle,Zombar.PropKind.Shaker,Zombar.PropKind.Cleaver };
            var sheet=new Texture2D(1620,540,TextureFormat.RGB24,false);
            var mode=Physics.simulationMode;
            var handPosition=hand.transform.position;var handRotation=hand.transform.rotation;
            try
            {
                Physics.simulationMode=SimulationMode.Script;
                var bottle = Zombar.BarProp.All.Find(p => p.kind == Zombar.PropKind.BloodBottle);
                var counter = GameObject.Find("CC0 Environment").transform.Find("Counter").GetComponent<BoxCollider>();
                bottle.ResetHome();
                hand.transform.SetPositionAndRotation(bottle.home + Vector3.down * .30f, Quaternion.identity);
                if (!bottle.Grab(hand)) throw new InvalidOperationException("Cannot grip bottle for collision test");
                for (int step = 0; step < 45; step++)
                {
                    bottle.SendMessage("FixedUpdate");
                    Physics.Simulate(Time.fixedDeltaTime);
                }
                if (bottle.Body.position.y < counter.bounds.max.y - .035f)
                    throw new InvalidOperationException("Held bottle penetrated the counter");
                bottle.ResetHome();
                for(int i=0;i<kinds.Length;i++)
                {
                    var prop=Zombar.BarProp.All.Find(p=>p.kind==kinds[i]);
                    hand.transform.position=prop.transform.TransformPoint(-prop.gripOffset);
                    Physics.SyncTransforms();
                    if(!hand.TryGrab() || hand.Held!=prop)
                        throw new InvalidOperationException("Could not grip "+kinds[i]+" at the authored grip point");
                    hand.transform.SetPositionAndRotation(new Vector3(.16f,1.58f,-.32f),
                        Quaternion.Euler(-8,kinds[i]==Zombar.PropKind.Cleaver?64:18,-10));
                    for (int step=0; step<35; step++)
                    {
                        prop.SendMessage("FixedUpdate");
                        Physics.Simulate(Time.fixedDeltaTime);
                    }
                    // Show the just-simulated pose immediately, without waiting for interpolation.
                    prop.transform.SetPositionAndRotation(prop.Body.position,prop.Body.rotation);
                    var focus=prop.GetComponent<BoxCollider>().bounds.center;
                    cam.fieldOfView=48;cam.transform.position=focus+new Vector3(.06f,.1f,-.69f);cam.transform.LookAt(focus);
                    cam.Render();RenderTexture.active=target;
                    sheet.ReadPixels(new Rect((target.width-540)/2,(target.height-540)/2,540,540),540*i,0);
                    prop.ResetHome();
                }
                sheet.Apply();File.WriteAllBytes("Builds/Validation/hand-props-held.png",sheet.EncodeToPNG());
            }
            finally
            {
                if(hand.Held!=null)hand.Held.ResetHome();
                hand.transform.SetPositionAndRotation(handPosition,handRotation);
                Physics.simulationMode=mode;
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

    }
}






