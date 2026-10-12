# Palace portal aperture occlusion

## Cause and scope

The ordinary renderer uses SpriteRenderer sorting (IsoY is off). DH_FY_1501 is a
20-frame map-library animation, not an EffectPlayer overlay. Its `sy + clipBy`
orders are 4619 and 3587 at the two qw0919 gates; these equal the map's authored
`py`. Their surrounding JZ_DXWG_0301 and JZ_DXWG_3102 images have no authored clip
array, so the JZ fallback uses the collision mask's frontmost occupied cell in
each column as the depth of that **entire** column. This also puts the doorway's
rear wall and stairs in front of the light, creating vertical clipping.

A whole-sprite `py` fallback is insufficient: the second frame has `py=3588`,
one ahead of its portal. Globally raising portals would break real wall occlusion.

## Narrow fix

- Explicit visual-aperture polygons cover only the rear wall/stairs in these two
  verified image IDs and paths. Their surrounding columns and arch remain foreground.
- Apply only to static library objects without authored clips, expected source
  dimensions/trim bounds, a valid collision footprint, and exactly one actual
  matching portal anchored within the frame. No matching portal, ambiguity, unknown
  art, tile objects, missing mask, and IsoY mode retain the existing path.
- Partition each existing strip into two complementary geometries, borrowing its
  original texture. Only the rear geometry is lowered to at most portal order - 1.
  Foreground geometry and the animated portal keep their original orders.
- OverrideGeometry uses rect-local pixels; Unity applies pivot and PPU. Untrimmed
  profile coordinates account for nLeft/nTop and HD scale. No asset pixels, packages,
  collision masks, navigation, or trigger polygons are changed.
- Meshes are cached per source texture, per-instance orders are calculated afresh,
  and both meshes are destroyed on renderer disposal.

## Validation and limits

`Tests/Endgame/run-portal-occlusion-contract.sh` compiles the actual profile,
aperture renderer, and footprint source with deterministic Unity substitutes.
It tests exactly-once pixel partition, triangle connectivity, API rect-space
coordinates, 1x/2x/4x scale, trimming, non-default PPU, paired/unpaired/ambiguous
placement, translated instances, geometry cache reuse, and disposal. Source
checks verify the production hookup and authored-clip precedence. These are not
a Unity Player run.

A separate cloud-only check decoded the original GSO images and reconstructed
all 20 animation frames with the production polygon coordinates and sort rules.
The two qw0919 gates and the ordinary qw0920 entrance recover the doorway light;
qw0920's already-correct elevated gate is unchanged in all 20 frames. Independent
review found bright portal coverage reduced from 47.6–55.6% to 2.0–6.2% for the
first qw0919 gate and 79.7–82.4% to 0.4–1.6% for the second. The remaining covered
pixels belong to the preserved foreground frame. These images are offline pair
composites, not full-scene or Player screenshots. Raw art and generated evidence
stay outside Git.

The source-package scan inspected 377 GSMs. These two frame IDs occur in qw0919,
qw0920, and pw20901; the frame in pw20901 is not paired with the observed portals.
Other large frame assets (0501/0502) overlap portals in qw0921–qw0923 and
pw20901. They are candidates needing separate profiles and verification, not
claimed fixed here. Package-override coverage and a running Player remain separate
validation boundaries; this patch does not claim a global portal repair.

Player acceptance: rebuild this revision, test both qw0919 gates across all
animation frames and camera leave/return, check front-column occlusion while
walking around the doorway, then test both qw0920 gates and a normal building.
