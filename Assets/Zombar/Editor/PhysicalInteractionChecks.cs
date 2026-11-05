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
                int money=(int)typeof(ZombieBarGame).GetField("money",flags).GetValue(game);
                typeof(PhysicalBar).GetMethod("Update",flags).Invoke(bar,null);
                Check((int)typeof(ZombieBarGame).GetField("money",flags).GetValue(game)==money,"handoff cannot pay twice");
                Check(PhysicalPatron.All.Any(p=>p.GetComponentInChildren<SkinnedMeshRenderer>()!=null),"characters are rigged meshes");
                return "Grip exclusivity, throw physics, physical recipes, decanting, invalid mixture and one-time handoff";
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
