# Optional legendary child skill visuals

The pre-entry ClientCapabilities mask adds bit 128. The server freezes that opt-in before entry and does not emit the new message to old-capability clients. Legacy 0x800e casts, 0x8045 hits, 0x80f1 ground effects and their payloads are unchanged.

0x80f2 has exactly 33 bytes: eight little-endian i32 values (caster, skill, target, map, origin X/Y, center X/Y), then u8 mode. Mode 0 is release-only; mode 1 is impact-only after a real landed server hit. Invalid mode, missing identity/map, truncation and surplus bytes fail closed. Target 0 and absent actor transforms are supported because coordinates are authoritative.

Child messages use the existing ordered visual-edge queue and cursor, not the skill cast state queue. The owning map is checked before presentation. Release uses original SkillEffectDB fly/atk phases and SkillSoundDB.PlayFire, never cast scripts, actor pose/facing mutation, preparation audio, CD or skill behavior. Impact uses only original hit phases and their own cosmetic delay, not projectile travel time or release audio. Server-native 0x8045 hits remain separate; mode 1 is used only when the server has no configured native hit or explicitly requests the native impact-only feedback. No gameplay timers are introduced.

The reused native phase runner is changed only behind child-only flags. Ordinary script casts and ordinary data casts retain their existing behavior, including their normal automatic hit phases. A missing skill table row logs once rather than inventing an EFF name or replaying an ambiguous hit-producing script.

## Cloud validation

- `python3 Tests/Endgame/run-child-skill-release-contract.py`: 67 managed assertions. Compiles all Foundation/Domain/Application/Protocol files plus the actual child presenter, SkillCaster, SkillEffectDB and SkillSoundDB; compiles the exact production visual synchronization method extracted from the owner. Unity object creation, asset IO and effect rendering are explicit surrogates.
- Coverage: all wire fields, malformed payloads, gateway publication, revision preservation, source/target identity, fixed server origin/center, local/remote/missing actors, correct map, once-only sync including forced repaints, release-only art/audio, server-landed native impact-only art, no duplicate hits or extra fly wait, no cast/CD publication, ordinary cast behavior unchanged.
- `python3 Tests/AttackTiming/run-native-identity-contract.py`: 938 assertions pass.
- `python3 Tests/Endgame/run-ground-effect-contract.py`: pass.
- Server companion tests verify old-cap suppression and mode 1 only on actual landed hits. These are separate from the client tests.

## Original-resource follow-up

Original `ov_skillshow.bin` was subsequently obtained from the user-identified resource archive. Its header is `(0x5566, 1, 100, 350)`. Original row 85 (10319 light bolt) contains fly effect 47 / delay 550 ms and hit effect 47 / delay 0; row 104 (10409 ice arrow) contains fly effect 40 / delay 370 ms and hit effect 40 / delay 0. Both have no attack phase. Original `skillevent.txt` has no custom `Skill10319` or `Skill10409` script.

`prepare-native-child-skill-fixture.py --bin "$ORIGINAL_SKILLSHOW" --script "$ORIGINAL_SKILLEVENT" --output-dir "$PRIVATE_OUTPUT"` validates the binary header/rows and writes a deterministic, private BIN-derived compact fixture and provenance hashes. Phase groups map to native suffixes 40/20/30/50; original position fields 2/3/10 are NOT phase suffixes. This derived fixture is not claimed to be the exact published `skillshow.json`.

Run the existing suite with `FFO_NATIVE_CHILD_FIXTURE="$PRIVATE_OUTPUT/native-child-skillshow-fixture.json"`. This executes 22 additional assertions (89 total) through the actual SkillEffectDB parser and actual gateway/cursor/phase runner: `TFX047_20` then landed-only `TFX047_30`, and `TFX040_20` then landed-only `TFX040_30`. It verifies source/center, cosmetic projectile duration, no duplicate hit, no repeated projectile wait, and once-only dispatch. Both the base 67 and extended 89 assertion runs pass.

The four EFF files and all eight referenced GSO paths were also found in the original archive extraction: light-bolt fly uses magic0047; its hit uses magic0300/magic4009. Ice-arrow fly uses magic0040; its hit uses magic0182/magic0280/magic0281/magic0303. Fixtures, extracted resources and detailed provenance remain private artifacts outside this repository.

Original `ov_sound.bin` contains an ice-arrow row with sing 1304 and fly/hit 1507, but no light-bolt row. This is source evidence only, not verification of the published compact sound-table projection or audible assets; no unverified audio mapping was added to production or fixtures.

## Not claimed

This remains managed surrogate validation, not Unity rendering/play-mode QA. The exact published compact JSON tables, sound audibility, and coverage for arbitrary copied/script-only native skills remain unverified. The original native table and EFF/GSO presence for 10319/10409 are now verified, closing that earlier resource-availability gap. No Mac access, push, merge, publish or version bump was performed.
