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

Five main-manifest positions still lack enough evidence for an additional repair:

- qw0086 library572/573: ice-wing material intersects the effect; a transparent
  doorway and solid foreground ice must be distinguished before changing depth.
- qw0154 library1207/1469 and qw0349 library433: tree/platform occlusion requires
  terrain, path and live-fade context.

The server's teleport_links JSON was inaccessible. Supplemental SQL declarations
and static map art do not establish dynamic-NPC presence or runtime transport
behavior. Neither these limits nor the five unresolved cases are counted as a
complete-world pass.

## Automated checks and Player boundary

`Tests/Endgame/run-portal-occlusion-contract.sh` compiles production aperture,
profile and footprint code. It verifies independent double-precision polygon
classification, concave gaps, exactly-once source-pixel partition, triangles,
1x/2x/4x scale and trimming, old-four-profile pixel identity, real-world coordinate
pair admission, identity/size/clip guards, ambiguous/unpaired rejection, original
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
