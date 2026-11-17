using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Zombar;

namespace ZombarEditor
{
    public static class PhysicalInteractionChecks
    {
        private static void Check(bool condition,string message) { if(!condition) throw new Exception("PHYSICAL CHECK FAILED: "+message); }
        public static string Run()
        {
            var bar=PhysicalBar.Instance;
            var game=ZombieBarGame.Instance;
            var shaker=bar.shaker;
            var glass=BarProp.All.First(p=>p.kind==PropKind.Glass);
            var handObject=new GameObject("Test hand");
            var hand=handObject.AddComponent<BarHand>();
            hand.enabled=false;
            try
            {
                Check(shaker.Grab(hand),"object can be gripped");
                Check(!shaker.Grab(hand),"held object cannot be acquired twice");
                shaker.Release(new Vector3(1,0,0));
                Check(!shaker.IsHeld && !shaker.Body.isKinematic && shaker.Body.linearVelocity.x>.9f,"release restores physics and throw velocity");
                shaker.ResetHome();
                Check(bar.AddIngredient(shaker,PropKind.Eye),"eye enters shaker");
                Check(bar.AddIngredient(shaker,PropKind.BloodBottle),"blood enters shaker");
                shaker.Grab(hand);
                for(int i=0;i<8;i++) shaker.SampleShake(Vector3.up * .2f, 10+i*.2f);
                Check(shaker.recipe==-1 && shaker.mixed==0,"slow motion does not mix a drink");
                for(int i=0;i<5;i++) shaker.SampleShake(Vector3.up * (i%2==0?2:-2), 20+i*.2f);
                Check(shaker.recipe==-1 && shaker.mixed==4,"four reversals are insufficient");
                shaker.SampleShake(Vector3.down * 2,21.2f);
                Check(shaker.recipe==0 && shaker.mixed==5,"five physical reversals resolve Blood Flip");
                shaker.Release(Vector3.zero);
                Check(bar.TransferDrink(shaker,glass) && glass.recipe==0 && shaker.recipe==-1,"drink transfers once to glass");
                Check(!bar.TransferDrink(shaker,glass),"empty shaker cannot duplicate drinks");
                shaker.eyes=2;shaker.blood=0;shaker.flesh=0;
                Check(!bar.FinishMix(shaker),"invalid recipe rejected");
                shaker.ResetHome();

                for (int index = 0; index < CocktailRecipes.Names.Length; index++)
                {
                    shaker.ResetHome(); glass.ResetHome();
                    var portions = CocktailRecipes.Portions[index];
                    for (int n=0; n<portions.x; n++) Check(bar.AddIngredient(shaker,PropKind.BloodBottle), "blood portion");
                    for (int n=0; n<portions.y; n++) Check(bar.AddIngredient(shaker,PropKind.Eye), "eye portion");
                    for (int n=0; n<portions.z; n++) Check(bar.AddIngredient(shaker,PropKind.Flesh), "flesh portion");
                    var motor = bar.GetComponent<BarMixer>();
                    Check(motor.Press(), "mixer starts for recipe " + index);
                    typeof(BarMixer).GetField("finishesAt",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(motor,Time.time - .01f);
                    typeof(BarMixer).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(motor,null);
                    Check(shaker.recipe == index, "motor resolves recipe " + index);
                    Check(bar.TransferDrink(shaker,glass) && glass.recipe == index, "transfer " + index);
                }
                shaker.ResetHome(); glass.ResetHome();
                var intake = shaker.transform.position + Vector3.up * .25f;
                bar.SpawnIngredient(PropKind.Eye, intake);
                typeof(PhysicalBar).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(bar,null);
                Check(shaker.eyes == 1, "released eye enters visible intake through runtime Update");
                var bottle = BarProp.All.First(p => p.kind == PropKind.BloodBottle);
                bottle.Grab(hand);
                bottle.transform.rotation = Quaternion.Euler(0,0,115);
                bottle.transform.position = shaker.transform.position + Vector3.up * .34f - bottle.transform.up * .28f;
                typeof(PhysicalBar).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(bar,null);
                Check(shaker.blood == 1 && shaker.eyes == 1, "tilted held bottle pours into shaker");
                bottle.ResetHome();
                var mixer = bar.GetComponent<BarMixer>();
                Check(mixer.Press(), "button starts motor");
                Check(!shaker.Grab(hand), "cup locked while motor runs");
                typeof(BarMixer).GetField("finishesAt",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(mixer,Time.time - .01f);
                typeof(BarMixer).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(mixer,null);
                Check(shaker.recipe == 0, "motor prepares first order");
                shaker.Grab(hand);
                shaker.transform.rotation = Quaternion.Euler(0,0,115);
                shaker.transform.position = glass.transform.position + Vector3.up * .3f - shaker.transform.up * .25f;
                typeof(PhysicalBar).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(bar,null);
                Check(glass.recipe == 0 && shaker.recipe == -1, "held tilted shaker decants into glass");
                shaker.ResetHome();
                shaker.eyes = 2;
                shaker.Grab(hand);
                shaker.transform.rotation = Quaternion.Euler(0,0,115);
                shaker.transform.position = new Vector3(1.35f,1.35f,-.45f) - shaker.transform.up * .25f;
                typeof(PhysicalBar).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(bar,null);
                Check(shaker.eyes == 0 && shaker.recipe == -1, "invalid mixture discarded by pouring over sink");
                shaker.ResetHome();
                var room = GameObject.Find("Bar enclosure");
                Check(room != null && room.GetComponentsInChildren<BoxCollider>().Length == 5, "solid room envelope");
                Check(Physics.Raycast(new Vector3(0,1,-2),Vector3.back,3,~0,QueryTriggerInteraction.Ignore), "rear wall blocks exit");
                Check(Physics.Raycast(new Vector3(-5,1,-2),Vector3.left,3,~0,QueryTriggerInteraction.Ignore), "left wall blocks exit");
                Check(Physics.Raycast(new Vector3(5,1,-2),Vector3.right,3,~0,QueryTriggerInteraction.Ignore), "right wall blocks exit");
                Check(Physics.Raycast(new Vector3(0,1,5),Vector3.forward,3,~0,QueryTriggerInteraction.Ignore), "front wall blocks exit");

                const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
                var patrons=(System.Collections.IList)typeof(ZombieBarGame).GetField("patrons",flags).GetValue(game);
                var first=patrons[0];
                var type=first.GetType();
                string recipe=(string)type.GetField("recipe").GetValue(first);
                string id=(string)type.GetField("id").GetValue(first);
                var physical=PhysicalPatron.All.First(p=>p.id==id);
                glass.recipe=Array.IndexOf(PhysicalBar.Recipes,recipe);
                glass.quality=65;
                glass.transform.position=physical.ReceivePosition;
                typeof(PhysicalBar).GetMethod("Update",flags).Invoke(bar,null);
                Check(patrons.Count==1 && glass.recipe==-1,"physical glass contact serves correct client and consumes drink");
                Physics.SyncTransforms();
                Check(Vector3.Distance(glass.transform.position,glass.home)<.001f &&
                    Vector3.Distance(glass.Body.position,glass.home)<.001f,"served glass returns home through physics sync");
                int money=(int)typeof(ZombieBarGame).GetField("money",flags).GetValue(game);
                typeof(PhysicalBar).GetMethod("Update",flags).Invoke(bar,null);
                Check((int)typeof(ZombieBarGame).GetField("money",flags).GetValue(game)==money,"handoff cannot pay twice");
                Check(PhysicalPatron.All.Any(p=>p.GetComponentInChildren<SkinnedMeshRenderer>()!=null),"characters are rigged meshes");
                return "Nine recipes, visible intake, solid enclosure, grip exclusivity, throw physics, decanting, invalid mixture and one-time handoff";
            }
            finally
            {
                shaker.ResetHome();glass.ResetHome();
                UnityEngine.Object.Destroy(handObject);
                game.RestartRound();
            }
        }
    }
}

