# Unique ground drop presentation

Applies to all ground items received in the world, including ordinary fields, dungeons, and rifts. The trigger is the server's validated `domain.UniqueOf(stack)` identity, never the item template ID, localized name, map ID, or quality color. Ordinary five-affix yellow items and valid runewords do not qualify.

## Wire compatibility

- The existing `0x1091` four-byte client capability mask keeps its original bits `31`; bit 5 (`32`) opts into ground presentation v1.
- The new client sends capability `63` immediately before `0x1004` world entry, so the initial ground snapshot can carry identity. Sending the existing post-entry capability again is harmless.
- Unadvertised/old clients receive byte-identical legacy `0x8013` packets. A new client connected to an old server keeps normal ground rendering but cannot identify uniques or play the new effects until that server supports the extension.
- An opted-in `0x8013` has the unchanged legacy prefix through quality, followed by `version:u8=1 | flags:u8 | uniqueId:i32 | dropToken:i64`. Bit 0 of flags means first creation; other bits are invalid.
- New clients accept exactly 0, 1, or 15 bytes after `kind`: missing quality, legacy quality, or quality plus v1. Truncated tails, unknown versions/flags, negative IDs/tokens, and fresh events with zero token are rejected by both receive paths.
- `spawnEvent` is a silent projection. Only `freshGroundSpawnEvent` marks first creation. Tokens are allocated process-wide once per ground lifetime, rather than using inventory UIDs or scene-local entity IDs. Dropping the same item again creates a new token.

## Reused artwork and audio

- Gold column: `object/DH/DH_FY_1501.gso`, 20 original frames, 200 ms per frame, scale 0.65. Original foot anchor `(54,179)` and per-frame offsets are preserved by the production decoder/sprite factory. Source inspected: SHA-256 `d7837a14b4916e710e5d4d5bb35458a60d701b7cc20ae788042698cb1ed5769d`.
- No source images or sounds are committed. The column is independently borrowed into the normal effect sprite budget; it does not change placed map portal objects, masks, or sort rules.
- Sound: existing `sndeffect/1002.wav` (case-insensitive extension), with existing `sndeffect24_pcm` fallback. This ID already supplies system-request click feedback. Its audio file was not available for listening in the cloud verification; physical sound/tone remains a Unity-player acceptance item.

## Ownership and replay prevention

- The item entity owns one sustained column below its icon. Pickup/removal cancels effects and pending audio immediately, before Unity's deferred GameObject destruction. Disable/destroy unsubscribe from art-quality changes.
- Missing artwork retries at 500 ms without consuming another sound edge. Rebuilding for art-quality changes or re-enabling an item recreates only the column. Shared decoded frames/materials use the existing effect cache and safe-point eviction.
- A process-lifetime receipt set observes authoritative unique drop tokens, including silent snapshots. Duplicate creation, re-entry, reconnect, and frame reconstruction cannot turn the same token into a new audio edge. It is intentionally not a high-water mark, since different scenes' events may arrive out of order.
- Audio is requested once at receive time. Async completion checks exact entity lifetime and expires after two seconds; pickup, map cleanup, identity replacement, or disable cancels it. Failed/missing sound assets do not create a retry/replay loop.

## Verification

- `Tests/WorldEntry/run-unique-drop-presentation-contract.py` executes production parsing, native receive, receipt journal, attached-column factory, and guarded audio with deterministic Unity boundaries. It covers strict tail lengths, precise 64-bit tokens, ordinary/unique identity, duplicate/snapshot/new-lifetime behavior, async cancellation/expiry, missing-resource recovery, quality changes, and disposal.
- Server `ground_presentation_test.go` suites cover legacy capability compatibility, optional wire identity, valid runewords/ordinary yellow exclusion, fresh creation on different maps, and silent snapshots.
- Production-decoded original pixels were composited at the source pivot/scale for static and animated previews. This is not a Unity GPU/gameplay capture. A full Unity build/play session and physical audio output remain unverified in the cloud environment.
