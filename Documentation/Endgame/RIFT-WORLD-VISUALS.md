# Rift world visuals

## Existing artwork

No resource binaries are added to the repository. The running client resolves the following original resources through its normal package loader:

| Role | Existing resource | Verified frames | Use |
| --- | --- | ---: | --- |
| Progress globe | `magic/magic/magic5202.gso` | 20 distinct, one direction | Native blue pearl, 100 ms/frame, world sprite at the server pickup position |
| Warning motif and arrival seal | `magic/magic/magic0341.gso` | 8, one direction | First authored rune frame supplies the static danger masks; the arrival seal plays all native frames |
| Guardian arrival lightning | `effect/TFX303.eff` | Native layered template | Existing elite-spawn lightning; original `magic4060`, `magic4061`, `magic0342`, `magic4042` layers |
| Elite/leader identity | Native monster body and existing `TFX304` ring | Current native actor frame | Existing blue/gold glow materials, class-specific name suffix |

Cloud package entries were inspected with bounded range reads and decoded with the current production DIMG decoder. Selected source SHA-256 values:

- magic5202: `54862a7798ca0eab0f35b5d29f5a7da7460d30c84125cbf6e9895072f1c73a34`
- magic0341: `86d1f20649efe9c0075b598efd5ce9a2abe33e2c6c20265d010ed00191adf5b6`

## Presentation contract

- Original name plus `（精英）`, `（首领）`, or `（镇境劫影）`. No separate blue/gold/affix text marker.
- Class 1 uses the existing blue glow material; classes 2 and 3 use gold. The current monster body frame, facing, visibility and sorting order are retained. Cleanup restores the original name/materials.
- Ground warnings have no ability title, countdown sentence or instruction label. Their amber/red rune masks preserve the server circle, annulus or flat-ended rectangle. The annulus inner safe area remains fully transparent; the footprint never changes with remaining time.
- The warning texture is an authored-frame-derived static mask, not a new frame animation. Native pearl and arrival-seal animations use their actual full frame sequences.
- Standalone pearls use the native alpha effect material at z=0 and map-Y sorting, under the current world root; the arrival seal uses its authored additive material between floor and actor layers. The monster overlay is parented to the actual body transform and copies its sprite pivot, flip and sorting layer.
- Globes exist only in authoritative, ready in-instance active state. The existing stale-map-view guard prevents an old snapshot painting into a replacement map root, and cleanup deactivates objects before deferred destruction. Native cache pinning protects their standalone animator from effect-cache eviction. Server ID removal, phase change, map/floor change, disconnect and unbind destroy the owned objects. No proximity-based optimistic pickup or progress award occurs.
- Guardian arrival shows only its requested 5→1 numeral. Twelve deterministic positions around the frozen server center schedule native lightning over the five-second arrival window. Elapsed strikes are skipped on reconnect and long stalls; repeated snapshots cannot replay them. Owned parentage prevents cleanup from deleting a recycled unrelated effect. Arrival playback preserves another skill receipt's pending effect scale.
- Explicit monster Aim coordinates are propagated only for current server-identified Rift monster casts. Ordinary monsters and non-Aim casts preserve their previous behavior.

## Verification

Focused managed contracts execute the production panel, mask builder, identity/glow helper and cast receiver. They cover geometry, transparent safe center, actual animation frame selection, duplicate snapshots, collection removal, floor change, disconnect/reconnect, delayed frames, single-shot lightning, tint/name restoration and ordinary-cast compatibility.

The cloud previews are decoded native-asset composites, with warning pixels exported from the actual C# mask builder. They are not Unity Player screenshots. A full Unity rendering session and in-game visual acceptance are still required.
