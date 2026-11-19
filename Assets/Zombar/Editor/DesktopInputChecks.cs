using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR;
using Zombar;

namespace ZombarEditor
{
    public static class DesktopInputChecks
    {
        const BindingFlags Private = BindingFlags.Instance|BindingFlags.NonPublic;
        static void Tick(object instance) => instance.GetType().GetMethod("Update",Private).Invoke(instance,null);
        static void Send<T>(UnityEngine.InputSystem.InputDevice device,T state) where T:struct,IInputStateTypeInfo
        {
            InputSystem.QueueStateEvent(device,state);
            InputSystem.Update();
            device.MakeCurrent();
        }
        static void Check(bool okay,string message) { if(!okay)throw new Exception("DESKTOP CHECK FAILED: "+message); }
        public static string Run()
        {
            Check(!XRSettings.isDeviceActive,"Headset-free run");
            var input=UnityEngine.Object.FindAnyObjectByType<BarPlayerInput>();
            var camera=Camera.main;
            var right=UnityEngine.Object.FindObjectsByType<BarHand>().First(h=>h.node==XRNode.RightHand);
            var left=UnityEngine.Object.FindObjectsByType<BarHand>().First(h=>h.node==XRNode.LeftHand);
            var bottle=BarProp.All.First(p=>p.kind==PropKind.BloodBottle);
            var oldKeyboard=Keyboard.current;var oldMouse=Mouse.current;
            var background=InputSystem.settings.backgroundBehavior;
            var editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();
            var pos=input.transform.position;var rot=input.transform.rotation;var cameraRot=camera.transform.localRotation;
            try
            {
                keyboard.MakeCurrent();mouse.MakeCurrent();
                InputSystem.Update();
                Send(keyboard,new KeyboardState(Key.W));
                Tick(input);
                Check(Vector3.Distance(input.transform.position,pos)>0.0001f,"W moves the bartender; key="+keyboard.wKey.isPressed+
                    "; dt="+Time.deltaTime+"; view="+typeof(BarPlayerInput).GetField("view",Private).GetValue(input)+"; position="+input.transform.position);
                Send(keyboard,new KeyboardState());
                Send(mouse,new MouseState {delta=new Vector2(40,0)}.WithButton(MouseButton.Right));
                Tick(input);
                Check(Quaternion.Angle(input.transform.rotation,rot)>1,"RMB plus mouse delta turns view");
                Send(mouse,new MouseState());Tick(right);
                bottle.transform.position=right.transform.position;bottle.Body.position=bottle.transform.position;
                Physics.SyncTransforms();
                Send(mouse,new MouseState().WithButton(MouseButton.Left));Tick(right);
                Check(right.Held==bottle&&bottle.IsHeld,"LMB physically grabs imported bottle");
                Send(keyboard,new KeyboardState(Key.F));
                for(int i=0;i<90;i++)Tick(right);
                Check(Vector3.Dot(right.transform.up,Vector3.up)<.2f,"F reaches pouring tilt");
                Send(mouse,new MouseState());Tick(right);
                Check(!bottle.IsHeld&&!bottle.Body.isKinematic,"LMB release restores bottle physics");
                var cup = PhysicalBar.Instance.shaker;
                var simulation = Physics.simulationMode;
                try
                {
                    Send(keyboard,new KeyboardState(Key.LeftCtrl));
                    for (int n=0;n<120;n++) Tick(input);
                    Check(camera.transform.position.y < pos.y+.65f,"crouch lowers eye height");
                    camera.transform.localRotation = Quaternion.Euler(48,0,0);
                    Tick(right);
                    Physics.simulationMode = SimulationMode.Script;
                    var drop = right.transform.position;
                    drop.y = pos.y+.45f;
                    cup.transform.SetPositionAndRotation(drop,Quaternion.identity);
                    cup.Body.position = drop;
                    cup.Body.rotation = Quaternion.identity;
                    Physics.SyncTransforms();
                    for (int n=0;n<90;n++) Physics.Simulate(Time.fixedDeltaTime);
                    Check(cup.Body.position.y < pos.y+.16f,"mixer cup falls to the floor");
                    Send(mouse,new MouseState().WithButton(MouseButton.Left)); Tick(right);
                    Check(right.Held == cup,"crouched desktop hand grips fallen cup");
                    Send(keyboard,new KeyboardState());
                    for (int n=0;n<120;n++)
                    {
                        Tick(input); Tick(right); cup.SendMessage("FixedUpdate");
                        Physics.Simulate(Time.fixedDeltaTime);
                    }
                    Check(camera.transform.position.y > pos.y+1.6f && cup.Body.position.y > pos.y+.65f,"stand and lift cup from floor");
                }
                finally
                {
                    cup.ResetHome();
                    Physics.simulationMode = simulation;
                    camera.transform.localRotation = cameraRot;
                }
                Send(keyboard,new KeyboardState(Key.Tab));Tick(input);
                Check(BarPlayerInput.DesktopHand==XRNode.LeftHand,"Tab selects the other hand");
                Send(keyboard,new KeyboardState());Tick(right);Tick(left);
                Check(left.GetComponentsInChildren<Renderer>().Any(r=>r.enabled)&&
                    !right.GetComponentsInChildren<Renderer>().Any(r=>r.enabled),"Selected desktop hand is tracked and visible");
                return "Desktop input: crouch, grip fallen mixer cup, stand and lift, WASD movement, mouse look, physical bottle grab/release, F tilt and Tab hand switching";
            }
            finally
            {
                bottle.ResetHome();input.transform.SetPositionAndRotation(pos,rot);camera.transform.localRotation=cameraRot;
                typeof(BarPlayerInput).GetMethod("ResetDesktopHand",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
                typeof(BarHand).GetField("desktopTurn",Private).SetValue(right,Quaternion.identity);
                InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.backgroundBehavior=background;
                InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;
                if(oldMouse!=null)oldMouse.MakeCurrent();if(oldKeyboard!=null)oldKeyboard.MakeCurrent();
            }
        }
    }
}
