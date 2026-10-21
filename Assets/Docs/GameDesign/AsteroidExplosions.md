# Asteroid explosions

Implemented and verified in Unity 6000.6.0f1 on 2026-10-21.

## Inspected assets and integration

Inspected all five original asteroid PNGs and their import settings, all three asteroid prefabs (Medium and Large are variants of Small), Health, AsteroidController, AsteroidCollision, Bullet, AsteroidSpawner, DestroyBelowScreen, PlayerHealth and the project render pipeline.

| Prefab | Source | World sprite size | Existing HP / player damage | Explosion scale | Rock fragments / sparks |
| --- | --- | --- | --- | --- | --- |
| Small | Asteroid_1, 486 x 461 px | 0.486 x 0.461 | 10 / 10 | 1.000 | 7 / 12 |
| Medium | Asteroid_4, 438 x 469 px | 0.5694 x 0.6097 | 30 / 20 | 1.255 | 12 / 20 |
| Large | Asteroid_3, 537 x 505 px | 0.8055 x 0.7575 | 50 / 30 | 1.657 | 20 / 32 |

Original art is hand-painted, angular, outlined rock with tan/orange faces, purple-brown shadows and turquoise mineral cracks. Medium is grey; its fragment renderer uses low saturation and a grey tint. Import scale is 100 pixels per unit, with bilinear filtering.

`Health.Die()` calls the optional `AsteroidDeathVfx` component immediately before the existing Destroy call. Only Bullet currently calls this Health damage API. There is no OnDestroy hook: bottom exit, Player collision, scene unload and direct destruction never trigger the effect. HP arithmetic, damage, score, movement and spawning remain unchanged. The existing obsolete serialized destroyY entry is removed by Unity when saving Small; the current controller has no such field.

The detached effect copies the asteroid's world position, sizes itself from SpriteRenderer world bounds, and uses the source sorting layer/order. Its four particle systems emit one burst each, have no continuous emission or looping, and always simulate even when off-screen. The root destroys itself after all systems finish (longest particle lifetime 0.95 seconds). No colliders, gameplay logic, runtime material allocations or shared-material mutations are added. Scaled-time animation respects pause.

## Created files

- `Assets/Art/Sprites/Enemy/Asteroids/Asteroid_Explosion_Atlas.png`: generated transparent RGBA atlas, 1254 x 1254; flash / shard / spark / dust quadrants. Imported uncompressed, without mipmaps, using clamp and bilinear sampling.
- `Assets/Art/Materials/Asteroids/Explosion_Flash.mat`
- `Assets/Art/Materials/Asteroids/Explosion_Fragments.mat`
- `Assets/Art/Materials/Asteroids/Explosion_Sparks.mat`
- `Assets/Art/Materials/Asteroids/Explosion_Dust.mat`
- `Assets/Code/Shaders/AsteroidParticle.shader`: URP unlit alpha-blended particles with atlas UVs and optional fragment desaturation.
- `Assets/Level/Prefabs/Enemy/Asteroid_Explosion.prefab`: reusable four-layer Particle System prefab; no AnimationClip or Animator is needed.
- `Assets/Code/Scripts/Enemy/AsteroidDeathVfx.cs`
- `Assets/Code/Scripts/Enemy/AsteroidExplosion.cs`
- `Assets/Code/Editor/AsteroidExplosionAssets.cs`: explicit rebuild menu; no automatic modifications on import.
- `Assets/Tests/Editor/AsteroidExplosionVerification.cs`: repeatable isolated Play Mode checks.
- This document, plus Unity .meta files for new assets/folders.

Modified: `Assets/Code/Scripts/Enemy/Health.cs` and the three `Assets/Level/Prefabs/Enemy/Asteroid_{Small,Medium,Large}.prefab` files. Existing unrelated working-tree changes were preserved. No Git commit or push was performed.

## Verification

Run **Tools > Space Explorer > Verify Asteroid Explosions** from Edit Mode with saved scenes. The check rebuilds the VFX assets, tests an isolated scene using real asteroid/bullet prefabs and actual physics collisions, then restores the original scene setup. It does not instantiate game managers or modify player progress/history.

Final run:

```text
PASS: compiled shader, four one-shot systems, all prefab/material/texture references valid.
PASS Small: real bullet kill, nonlethal silence, exact position, scale=1.000, fragments=7, complete cleanup.
PASS Small: bottom exit and real Player collision destroy without VFX; damage unchanged.
PASS Medium: real bullet kill, nonlethal silence, exact position, scale=1.255, fragments=12, complete cleanup.
PASS Medium: bottom exit and real Player collision destroy without VFX; damage unchanged.
PASS Large: real bullet kill, nonlethal silence, exact position, scale=1.657, fragments=20, complete cleanup.
PASS Large: bottom exit and real Player collision destroy without VFX; damage unchanged.
PASS: repeated lethal hit emits once; pause/resume retains and then cleans effect.
PASS: rendered original sprites and 0.04/0.18/0.45s VFX through the project render pipeline; preview cleaned.
PASS: all asteroid explosion checks; no runtime errors.
```

Visually inspected rendered PNGs at all three effect ages, including grey Medium debris and progressively larger bursts. Source PNG alpha was checked: real transparency and partial-alpha glow are present. Runtime checks verify both the effect root and all child ParticleSystems disappear. Reports and rendered previews are generated under `Temp/AsteroidExplosionVerification/` (Unity may clear Temp).

A persistent copy of the 0.18-second render (Small / Medium / Large, left to right) is saved at `docs/AsteroidExplosionPreview.png` in the project root.

Not verified: a standalone player build, target-device performance, or a complete manual playthrough of all four levels. The actual prefab, bullet-trigger, Player collision and project rendering paths were exercised in Editor Play Mode. Batchmode could not open the project because the user's existing Editor held its lock; verification ran successfully in that Editor instead.

## Image generation provenance

Used the built-in imagegen tool, with true transparent background requested. No CLI fallback or placeholder art was used. Final prompt:

> Use case: stylized-concept. Asset type: production Unity 2D particle texture atlas, transparent PNG. Create a square 2x2 grid of exactly four isolated game particle sprites, each centered in its own quadrant with generous fully transparent padding and absolutely no overlap between quadrants, no labels, no dividers, no checkerboard painted into image. Upper left: a compact soft explosive flash, ivory-white hot core with warm amber-orange radial glow, irregular pointed hand-painted burst edges, no rocks, no smoke. Upper right: one single angular broken asteroid shard, tan-orange rock planes, dark desaturated purple-brown shadow and thin dark outline, a tiny luminous turquoise mineral fissure, chunky hand-painted cartoon space game style. Lower left: a single narrow tapered spark streak, oriented vertically, brilliant ivory head and amber tail, compact clean silhouette and soft glow. Lower right: a single soft billowing dust puff in desaturated purple-brown, subtle hand-painted texture, semi-transparent wispy edges, no rocks. All four are standalone individual reusable particle textures, matching stylized tan/brown angular asteroids with dark purple shadows and turquoise mineral accents. True alpha transparency, clean edges suitable for small on-screen sprites. Flat facing camera, 2D, no perspective scene, no text, no watermark, no background.
