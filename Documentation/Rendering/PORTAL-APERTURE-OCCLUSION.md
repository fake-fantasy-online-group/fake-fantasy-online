# Audited portal aperture occlusion

## Scope and cause

In ordinary SpriteRenderer sorting (IsoY off), animated map-library portals keep
production `sy + decoded clipBy` order. Some surrounding static building images
contain both a rear wall/floor and a nearer frame. Ordering each whole collision
column as foreground clips the effect along artificial vertical lines.

The repair is asset-specific. It does not raise portals globally or modify asset
pixels, package data, collision masks, navigation, trigger polygons, actor sorting,
or the alternate IsoY path. Asset identity requires exact model ID, path, logical
dimensions, supported trim bounds, and exactly one matching audited portal.

## Geometry and ordering

- The original four DXWG profiles remain unchanged: 0301, 3102, 0501 and 0502,
  paired with FY1501. The first two are stair entrances, the latter two arches.
- Additional pixel-traced profiles cover SWS4701, DFYW1301, JXMG0124–0127 and
  HJ2401. Pairings are explicit: SWS accepts FY0201/FY1801/FY1901; the other new
  polygons accept FY1701. FY1601 inside DXWG0501 is deliberately unchanged.
- Rear-plane mesh pixels are lowered to `min(originalOrder, portalOrder - 1)`.
  Columns use all even/odd polygon intersections; a concave gap, including the
  HJ2401 notch, never becomes a solid min/max rectangle.
- Separately traced foreground silhouettes exclude teeth, statues, chains and
  braziers from the rear plane. On the four JXMG rock doors, only those explicit
  silhouettes can use `max(originalOrder, portalOrder + 1)` to prevent a single
  foreground tooth/brazier being split across the portal by the old footprint
  columns. Other foreground pixels keep their original order.
- SWS4701 has an unsupported nonzero collision-mask header. The shared mask
  parser remains conservative. This exact image alone can split its original
  whole-object pivot-Y layer; its foreground retains that original pivot depth.
- TTD6701 is a separately authored wooden rear wall/floor image. Its exact model,
  dimensions, texture bounds and native seven-entry clip array are checked before
  pairing with FY0201. Original native slice rectangles are retained; only their
  order is capped behind the portal. The doorway's independent front-column
  objects are untouched. Decoded FY0201 clipBy is 176, distinct from GSM py.
- Missing, unsupported or ambiguous geometry retains the existing renderer path.
  Meshes borrow original textures, use rect-local pixels, account for trim and HD
  scale, cache by texture, and are destroyed on renderer disposal.

## Cloud validation

The audit uses original DimgDecoder pixels and the actual C# placement/mesh code,
compiled with Unity substitutes. Screenshots are original-object-layer
reconstructions, not a Unity Player run. Raw art and generated screenshots remain
outside Git. Terrain, dynamic NPC/actors, live fade, shaders and Unity tie-breaking
are not generally simulated; separately recovered embedded scenes include their
original cover and embedded slices.

The main portal manifest has 639 positions across 293 maps. Each has an explicit
visual-review record. An independent embedded-format repair recovers another nine
portal positions in seven maps; see the embedded-frame parser contract. Additional
SQL/NPC-entry checks remain distinct from confirmed GSM portal instances.

The main audit identifies 25 confirmed layer defects across the eight additional
building models. Frame-by-frame regression is necessary: initial frame-zero-only
profiles left upper-platform clipping on the four rock doors and were refined
against the full original animation frames. Normal foreground occlusion is
preserved, rather than using a zero-covered-pixel acceptance rule.

### 2026-10-13 follow-up on the five uncertain positions

Original map `.msk`, `.link` and `.prc` bytes were recovered from the already
cached package region, without downloading resources. Original GSO pixels,
collision masks and freshly compiled production placement establish:

- **qw0086 library572/573: normal solid-foreground occlusion in this static
  reconstruction.** The authored trigger polygons occupy the visible transparent
  doorway. The overlapping side ice wings are real opaque art and blocked
  terrain, not a wrongly foregrounded rear wall. They remain unchanged.
- **qw0154 library1207/1469: confirmed upper-surface ordering defects, repaired.**
  The legacy trigger polygons and walkable map cells occupy the wooden stump's
  upper face. Treating the whole tree's collision-column front edge as depth
  covers that support surface with the effect behind it. Exact HYYSC2101,
  HYYSC2102 and HYYSC1602 profiles lower only traced upper-face pixels. The
  source front lip, side wall, roots, flowers and foliage retain original depth.
  The repair applies only to a unique paired FY1701 with verified dimensions.
  Unlike an arch, its anchor must be inside the surface interval. HYYSC2102 is
  the adjoining right half of the same surface: its shared left-edge continuation
  accepts an anchor at most 109 logical pixels left and within its 158-pixel
  shared face height, so an effect spanning the asset seam is not cut in two.
- **qw0349 library433: still inconclusive; unchanged.** Original PZ is -655;
  the portal's world anchor (2756,3171) is walkable but its screen-projected
  anchor (2756,3826) is blocked and overlaps real tree branches. The four
  occluding flower-tree assets have empty object masks and retain their authored
  pivot depth. The available link declarations lack matching original trigger
  polygons. Neither drawing the entire effect above the trees nor silently
  changing its authored PZ is justified by this evidence.

Scope regression compiles and executes actual placement for all 47 occurrences
of the three stump models across four maps. Only the three intended objects
around the two qw0154 portals change; 44 unpaired occurrences are identical.
All 15 FY1701 frames at each repaired position were reconstructed using original
DecodeAnim pixels and offsets. Every changed final pixel lies inside that frame's
portal alpha. Compositing the scene with the portal omitted gives **zero changed
pixels**, preserving the surrounding artwork rather than just improving a
portal-visibility score.

A rejected broad surface-lowering experiment exposed roots previously behind the
platform; the final implementation retains `min(originalOrder, portalOrder - 1)`
and excludes the front bevel. This is a portal repair, not a general actor-floor
sorting redesign. Actors at the paired portal foot sort above the lowered upper
face; the old side-wall/foliage order is retained. Arbitrary positions behind that
foot on the platform and live walking/fade still need separate Player acceptance.
Static footprint and pivot-fallback branches do not currently register the
player-dependent fade owner, so a promised fade cannot be used to dismiss these
cases.

The server's teleport_links JSON was inaccessible. Supplemental SQL declarations
and static map art do not establish dynamic-NPC presence or runtime transport
behavior. Neither these limits nor the remaining inconclusive case is counted as a
complete-world pass.

## Automated checks and Player boundary

`Tests/Endgame/run-portal-occlusion-contract.sh` compiles production aperture,
profile and footprint code. It verifies independent double-precision polygon
classification, concave gaps, exactly-once source-pixel partition, triangles,
1x/2x/4x scale and trimming, old-four-profile pixel identity, real-world coordinate
pair admission, identity/size/clip guards, bounded support-face and adjoining-edge anchors,
ambiguous/unpaired rejection, original
SWS pivot preservation, TTD native-clip guards, geometry reuse and disposal.
Three-part production placement also checks that promoted pixels belong only to
the explicit foreground silhouettes and that front/back actor-order extrema are
unchanged. This is not proof of walking around each doorway in a Player.

The cloud production-layer exporter additionally checks actual branch dispatch,
native slice grouping, map admissions and sorting. Independent comparison against
HEAD's previous renderer found the old four-profile actual scenes' geometry,
coordinates and orders unchanged. Full-width 4x meshes remain below the ushort
vertex-index limit.

Player acceptance remains: inspect every affected entrance across animation frames,
move actors in front of and behind foreground teeth/columns/braziers, leave and
return with the camera, and check fade and material behavior. No claim of Unity
build, live gameplay, or universal portal correctness is made by the cloud audit.
