# Building support depth contracts

These tests exercise the production `Build` / object admission / authored slice,
footprint, static fallback and portal dispatch with data-only Unity substitutes.
They are **not Unity Player screenshots, device tests, or FPS benchmarks**.
No original image, mask, PKG, avatar or private download location is bundled.

## Run

Activate the repository's documented managed toolchain, then:

```sh
source Documentation/tools/activate-dotnet-test-toolchain.sh
bash Tests/BuildingLayers/run-building-layer-contract.sh
bash Tests/Endgame/run-portal-occlusion-contract.sh
bash Tests/Endgame/run-managed-compile.sh
python3 Documentation/tools/compile-current-presentation.py
```

Use distinct `FFO_COMPILE_DIR` and `FFO_PRESENTATION_COMPILE_DIR` directories when
running alongside another checkout. The focused scripts use unique temporary
build directories and synthetic masks.

## What is protected

- The reported CL0401 board uses its **authored clip array**, not the footprint
  branch. Its exact ID, path, dimensions, trim, frame count and 42 native clips
  are guarded. The source slices' native coalescing remains authoritative for
  all non-support geometry; the repair is not a blanket ground layer.
- Other approved profiles affect only explicit source-coordinate support or
  component spans. Collision gaps identify candidates, not safe geometry.
  Front foliage, rails, posts, sidewalls, awnings and portal islands must retain
  their respective reviewed component depths.
- Narrow components and stair-edge wedges use explicitly selected 1px columns
  to avoid swallowing diagonal foreground; ordinary profiles use 10px columns. Both preserve the actual
  source column's original ceiling before grouping.
- Additional authored-slice repairs preserve the exact clip array (including
  zero and nonuniform entries), clip base, native coalescing and per-column
  ceiling. A placement pivot is not substituted for the sprite's clip base.
- Static profiles require their recorded source anchor and placement pivot
  offset. If a newly compatible footprint appears, they yield to the normal
  path; unsupported collision formats are never guessed or rewritten.
- Native visual PZ projection, actor-relative ordering, HD1/2/4 coordinates,
  exactly-once source-pixel coverage, original-texture ownership and fade group
  registration are checked. Geometry caches that resolve depth include PZ;
  portal caches retain local depth and `WasRear`, applying the actual portal cap
  per instance. The paired SWS4701 static fallback validates its placement
  signature before selecting the aperture cache; admitted support and native
  fallback partitions cannot share a cache entry. A newly compatible native
  footprint bypasses that static-only refinement.
- Portal grouping only combines equal original ceilings and identical local
  component rules. Source-coordinate rebuilding avoids confusing the
  Unity-unit `Sprite.vertices` getter with pixel-unit `OverrideGeometry` input.
  A conservative per-mesh bound prevents ushort triangle-index overflow.

Negative controls deliberately bypass the CL, ordinary-support, static or
additional authored-support
admission paths, disable the portal actor/jamb refinements, or regress the
mixed-PZ cache key. Paired static-support admission and its matched/nonmatched
signature cache have separate negative controls. Each must fail its corresponding
contract.

## Why explicit source components are required

The native formats do not provide one universal walking-surface anchor. Authored
clips, supported collision footprints and static pivot fallbacks are different
production paths. In particular, an authored clip base need not equal the map
placement's pivot offset. Preserving that distinction fixes the dispatch and
cache contracts; it does not identify which pixels depict a stair top, a tree
canopy or a railing. Collision gaps also include non-support openings, and some
verified support tops have no gap at all.

The bounded configurations therefore describe reviewed local components, not a
new interpretation of all mask gaps or a family-wide ground layer. Each path,
ID, dimensions, trim, frame, clip and applicable pivot signature is checked
before using a configuration. A mismatch falls back to the existing native
path. Configuration counts are not a claim that every asset or every point of
an asset has passed gameplay validation.

## Visual acceptance boundary

The source-art audit separately replays the actual production quads and native
actor body/head layers from existing package references. Each acceptance records
its actual map/object/PZ, chosen feet, original map-cell flag, and foreground
controls. A clear collision cell alone is not proof of a walkable support top;
an unused asset or a placement with no verified accessible sample is diagnostic,
not a gameplay pass. Nonzero actor overlap may be correct foreground occlusion.

The first mask-gap candidate cohort is not an exhaustive classification of all
building formats. Unsupported headers, negative lattice coordinates, and
compound porch/stair assets require their own explicit inventory and source
inspection. Do not label those assets normal just because a selector rejected
them. Configuration or contract success is not a replacement for visual review.

The temporary original-source preview directories and contact sheets must stay
out of commits, resource bundles and publication manifests. Earlier evidence
that accidentally used CL0401's inactive footprint branch is withdrawn and must
not be cited as a reproduction of its production path.
