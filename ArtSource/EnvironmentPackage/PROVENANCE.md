# CC0 environment models — Necro Pub

Acquired 2026-11-10. These are third-party models, not models authored by this project.

## Poly Haven — CC0 1.0

License: https://polyhaven.com/license

- Bottle / BottleAlt: https://polyhaven.com/a/wine_bottles_01 (Rico Cilliers; label design Jurita Burger).
- Glass: https://polyhaven.com/a/brass_goblets
- Pendant: https://polyhaven.com/a/hanging_industrial_lamp
- Lantern: https://polyhaven.com/a/Lantern_01
- Stool: https://polyhaven.com/a/bar_chair_round_01
- Barrel: https://polyhaven.com/a/wine_barrel_01
- Cabinet: https://polyhaven.com/a/GothicCabinet_01

Original glTF dependency MD5 values were checked against the Poly Haven files API. Original 2K maps are retained under ArtSource/EnvironmentReview. Roughness/metal channels are repacked for Unity URP; source geometry is rescaled and selected high-density objects decimated. No source download is overwritten.

## Other CC0 models

- Counter: https://vibeassets.io/a/th0sl00gfq — Medieval tavern bar counter. The provider identifies this model as Meshy-generated; downloaded as an existing model with 1K PBR maps.
- Backbar: https://vibeassets.io/a/7ig9jaduzt — Tavern back-bar shelf with bottles and tankards.
- Tray: https://vibeassets.io/a/t85ze0haxs — Ornate wooden dice tray, adapted as the ingredient tray.
- Window: https://grab3d.com/models/furniture/window/ — Double casement window, CC0; original 2K materials, frame stained dark and glass tinted for the bar in Unity.

## Editable sources and validation

Packed Blender copies: ArtSource/Blender/ExternalEnvironment.
Source acquisition: ArtSource/Blender/fetch_bar_environment.py.
Texture conversion: ArtSource/Blender/package_bar_environment.py.
Mesh export and triangle/UV checks: ArtSource/Blender/export_bar_environment.py.
Geometry report: validation.json. Byte integrity roster: sha256.json.

FBX exports retain UVs and material names. This package's static validation does not establish a Quest frame rate; headset profiling is still required.

## Interactive bar tools

- BloodFlask: Small glass bottle / Leather-Strapped Potion Bottle by shuvalov.di, licensed CC BY 4.0 (https://creativecommons.org/licenses/by/4.0/). Original: https://sketchfab.com/3d-models/761e522abb934b0a98063a9851728180 . GLB distribution: https://renderlyst.site/models/small-glass-bottle . Adaptation: decimated, resized, leather and glass atlas composited over dark blood for mobile rendering.
- Hatchet: https://polyhaven.com/a/hatchet — CC0, original mesh and texture maps, resized for the bartender.
- CocktailShaker: Metallic Rocket Cocktail Shaker by beeclark. https://sketchfab.com/3d-models/379336c0082e4c42ab2ae009ed92f563 . CC BY 4.0 (https://creativecommons.org/licenses/by/4.0/). Obtained via the public allenai/objaverse GLB archive; adapted to an 18 cm prop and URP materials for Unity.
