# Imported zombie characters

Originating library: Adobe Mixamo.
Commercial and personal game use described by Adobe: https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html
Acquisition source: https://huggingface.co/datasets/Linzhan/Mixamo-Animations-Characters
Source revision: 7f134bc734557b1d9516cb476d25ea1f577e45c1
Downloaded 2026-11-08 for this Unity game.

Characters retain their supplied mesh, skin weights, UVs and 2048px color/normal maps:
- Warzombie_F_Pedroso.fbx — ordinary customer, PatronMale.prefab.
- Zombiegirl_W_Kurniawan.fbx — ordinary customer, PatronFemale.prefab.
- Yaku_J_Ignite.fbx — regenerating assistant, Helper.prefab.
- Mutant.fbx — 3.05 m invulnerable customer, Titan.prefab.

Motions: Zombie_Idle, Zombie_Punching, Zombie_Death, Zombie_Walk from the same library and archive. Unity retargets motions through Humanoid avatars.

Source FBXs are byte-preserved. Blender extracts embedded maps into PNGs and packs editable copies under ArtSource/Blender/ExternalCharacters. Unity builds URP materials and prefabs with normalized heights. Imported models are third-party assets, not authored by this project.

SHA-256 roster: ArtSource/ExternalReview/admitted-sha256.json.
The GitHub GLB candidates under ArtSource/ExternalReview were used for preliminary visual review only. The game uses the full-resolution FBXs above.
