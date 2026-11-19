using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Zombar;

namespace ZombarEditor
{
    // Runs against the real scene in Play mode. No headset required for game rules.
    public static class BarSmokeChecks
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        static object Read(object obj, string field) => obj.GetType().GetField(field, Flags).GetValue(obj);
        static void Set(object obj, string field, object value) => obj.GetType().GetField(field, Flags).SetValue(obj, value);
        static object Call(object obj, string method, params object[] args) => obj.GetType().GetMethod(method, Flags).Invoke(obj, args);
        static void Check(bool pass, string message) { if (!pass) throw new Exception("SMOKE FAILED: " + message); }

        public static string Run()
        {
            var game = ZombieBarGame.Instance;
            if (!Application.isPlaying || game == null) throw new Exception("Open ZombieBar and enter Play first.");
            var passed = new List<string>();
            try
            {
                Check(UnityEngine.Object.FindObjectsByType<BarTarget>().Length == 0, "No legacy action buttons remain");
                Check(PhysicalBar.Instance != null && BarProp.All.Count >= 12,
                    "Physical workbench populated; instance=" + PhysicalBar.Instance + "; props=" + BarProp.All.Count);
                passed.Add("Physical workbench replaces all action buttons");
                var initialPatrons = (IList)Read(game, "patrons");
                int titans = 0;
                foreach (var client in initialPatrons)
                {
                    var root = (GameObject)Read(client, "root");
                    var animator = root.GetComponentInChildren<Animator>();
                    Check(animator.avatar != null && animator.avatar.isHuman && animator.avatar.isValid,
                        "External character has valid animated Humanoid rig");
                    foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>())
                        foreach (var material in renderer.sharedMaterials)
                            Check(material.GetTexture("_BaseMap") != null, "External character retains its authored texture");
                    if (!(bool)Read(client, "giant")) continue;
                    titans++;
                    var meshes = root.GetComponentsInChildren<SkinnedMeshRenderer>();
                    Check(Array.Exists(meshes, r => r.sharedMesh.name.ToLowerInvariant().Contains("mutant")),
                        "Titan uses the distinct Mutant mesh");
                    Bounds bounds = meshes[0].bounds;
                    foreach (var renderer in meshes) bounds.Encapsulate(renderer.bounds);
                    Check(bounds.size.y > 2.6f, "Titan is visibly taller than normal patrons");
                }
                Check(titans == 1, "First shift contains one guaranteed titan");
                passed.Add("External textured Humanoid characters and guaranteed full-size Mutant titan");
                var environment = GameObject.Find("CC0 Environment");
                Check(environment != null, "Imported environment exists");
                foreach (string model in new[] { "Counter", "Window", "Pendant", "Backbar", "Stool" })
                {
                    var item = environment.transform.Find(model);
                    Check(item != null && item.GetComponentsInChildren<MeshFilter>().Length > 0, "Imported " + model);
                    Check(Array.Exists(item.GetComponentsInChildren<Renderer>(), r =>
                        Array.Exists(r.sharedMaterials, m => m.GetTexture("_BaseMap") != null)), "Authored PBR maps for " + model);
                }
                var bottle = BarProp.All.Find(p => p.kind == PropKind.BloodBottle);
                Check(bottle.GetComponent<BoxCollider>().size.y > .26f && bottle.GetComponent<BoxCollider>().size.y < .31f,
                    "Replacement bottle collider and pour height agree");
                passed.Add("CC0 environment imports, PBR materials and playable bottle dimensions");
                passed.Add(DesktopInputChecks.Run());
                passed.Add(PhysicalInteractionChecks.Run());
                game.RestartRound();
                var patrons = (IList)Read(game, "patrons");
                var first = patrons[0];
                string[] recipes = { "Кровавый флип", "Гнилой сауэр", "Глазной хайбол" };
                Call(game, "Brew", Array.IndexOf(recipes, (string)Read(first, "recipe")));
                Check(!string.IsNullOrEmpty((string)Read(game, "readyDrink")), "drink prepared");
                Call(game, "ServePatron", (string)Read(first, "id"));
                Check((int)Read(game, "money") > 20 && (int)Read(game, "servedCount") == 1, "correct order pays");
                passed.Add("Recipe consumption and order reward");

                game.RestartRound();
                first = patrons[0];
                string wrong = recipes[(Array.IndexOf(recipes, (string)Read(first, "recipe")) + 1) % 3];
                Set(game, "readyDrink", wrong);
                float patience = (float)Read(first, "patience");
                Call(game, "ServePatron", (string)Read(first, "id"));
                Check((float)Read(first, "patience") < patience && (int)Read(game, "money") == 20, "wrong order penalty");
                passed.Add("Wrong order loses patience");

                Call(game, "DamagePlayer", "test");
                Check(game.LeftArmDisabled || game.RightArmDisabled, "hand damage");
                Call(game, "DamagePlayer", "test");
                Check((bool)Read(game, "eyeInjured"), "eye damage");
                Call(game, "RestoreBody");
                Check(!game.LeftArmDisabled && !game.RightArmDisabled && (int)Read(game, "playerHealth") == 3, "restore body");
                passed.Add("Hand injury, eye injury and healing");

                game.RestartRound();
                Call(game, "HarvestHelperArm");
                int stock = (int)Read(game, "flesh");
                Call(game, "HarvestHelperArm");
                Check((int)Read(game, "flesh") == stock, "regeneration cooldown");
                game.RestartRound();
                Check((float)Read(game, "helperArmReady") == 0, "cooldowns reset");
                passed.Add("Helper regeneration cooldown and restart");

                first = patrons[0];
                string id = (string)Read(first, "id");
                Call(game, "PunchPatron", id);
                Set(game, "punchReady", 0f);
                Call(game, "PunchPatron", id);
                Check((bool)Read(first, "corpse"), "defeated zombie");
                int count = patrons.Count;
                Call(game, "HarvestCorpse", id);
                Check(patrons.Count == count - 1 && (int)Read(game, "flesh") == 4, "corpse harvested once");
                Call(game, "HarvestCorpse", id);
                Check((int)Read(game, "flesh") == 4, "no duplicate loot");
                Call(game, "SpawnPatron", true);
                var giant = patrons[patrons.Count - 1];
                Set(game, "punchReady", 0f);
                Call(game, "PunchPatron", (string)Read(giant, "id"));
                Check(!(bool)Read(giant, "corpse") && (int)Read(game, "playerHealth") == 2, "giant cannot be defeated");
                int titanHealth = (int)Read(giant, "health");
                Set(game, "playerHealth", 100);
                Set(game, "strengthLevel", 99);
                for (int hit = 0; hit < 20; hit++)
                {
                    Set(game, "punchReady", 0f);
                    game.PhysicalPunch((string)Read(giant, "id"));
                }
                Check(!(bool)Read(giant, "corpse") && (int)Read(giant, "health") == titanHealth,
                    "Twenty maximum-strength physical punches cannot kill or damage the titan");
                passed.Add("Combat, corpse loot and invulnerable giant");

                game.RestartRound();
                Call(game, "StartFire");
                Call(game, "ExtinguishFire");
                Check(!(bool)Read(game, "fireActive") && (int)Read(game, "blood") == 1, "blood extinguishes fire");
                Set(game, "floodActive", true);
                Call(game, "PumpFlood"); Call(game, "PumpFlood"); Call(game, "PumpFlood");
                Check(!(bool)Read(game, "floodActive"), "flood drained");
                passed.Add("Fire consumes blood; flood requires three pumps");

                game.RestartRound();
                Call(game, "UpgradeStrength");
                Check((int)Read(game, "strengthLevel") == 1 && (int)Read(game, "money") == 8, "perk cost");
                Call(game, "BuyEquipment", BarAction.BuyIceBox);
                Check(!(bool)Read(game, "iceBox") && (int)Read(game, "money") == 8, "cannot overspend");
                Set(game, "money", 40);
                Call(game, "BuyEquipment", BarAction.BuyIceBox);
                Call(game, "BuyEquipment", BarAction.BuyIceBox);
                Check((bool)Read(game, "iceBox") && (int)Read(game, "money") == 24, "equipment charged once");
                passed.Add("Perk and equipment economy");

                Call(game, "DamagePlayer", "test"); Call(game, "DamagePlayer", "test"); Call(game, "DamagePlayer", "test");
                Check(game.GameOver, "three hits end shift");
                game.RestartRound();
                Check(!game.GameOver && patrons.Count == 2, "new shift playable");
                passed.Add("Game over and restart");
                var music = GameObject.Find("Музыка бара").GetComponent<AudioSource>();
                Check(music.clip != null && music.clip.length > 1 && music.loop && music.playOnAwake && music.spatialBlend == 0, "background track configured");
                Check(Array.FindAll(UnityEngine.Object.FindObjectsByType<AudioSource>(), a => a.clip == music.clip).Length == 1, "new shift has a single music source");
                passed.Add("Looping stereo bar music survives shift restart · " + music.clip.length.ToString("F1") + " s");
            }
            finally { game.RestartRound(); PhysicalBar.Instance?.ResetProps(); }
            string result = "PASS " + passed.Count + " scenarios\n" + string.Join("\n", passed);
            System.IO.Directory.CreateDirectory("Builds/Validation");
            System.IO.File.WriteAllText("Builds/Validation/gameplay-checks.txt", result);
            if (System.IO.File.Exists("Builds/Validation/failure.txt")) System.IO.File.Delete("Builds/Validation/failure.txt");
            return result;
        }
    }
}

