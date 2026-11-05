# Zombie characters

Author: Quaternius. Pack: Ultimate Animated Character Pack (Nov 2019), uploaded by Quaternius to OpenGameArt.

Source and CC0 declaration: https://opengameart.org/content/animated-characters-pack
Archive: https://opengameart.org/sites/default/files/ultimate_animated_character_pack_by_quaternius.zip
License: CC0 1.0 — https://creativecommons.org/publicdomain/zero/1.0/
Archive SHA-256: 0295062CBC72DA52EE804F39AC38694901A40458011F9B2638FC7EF13ED509DA

Original files: FBX/Zombie_Male.fbx and FBX/Zombie_Female.fbx, preserved unchanged. Their bodies, skeletons, and motions are used in Adapted/.

The production FBXs replace the original head geometry. ArtSource/Blender/sculpt_surface.py constructs the carved facial surface and anatomy-aligned color and normal maps. build_portraits.py and portrait_details.py add the eyes, lids, teeth, sutures, collector's cap, hood and mortuary clothes, then bind the meshes to the original skeleton. costume_maps.py produces the leather and twill textures. These meshes and maps were authored locally for this project. The older adapt_zombies.py is the previous proportion-only prototype and does not reproduce the current characters.
