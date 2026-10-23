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

## Not claimed

This is managed surrogate validation, not Unity rendering/play-mode QA. The cloud workspace does not contain original skillshow/skillshow05/skillsound JSON or skillevent.txt. The tests use explicit synthetic table rows to execute the real parsers and routing; they do not claim real asset-table coverage for 10409 ice arrow, 10319 light bolt or arbitrary copied skills. Native-table presence, sprite rendering, sound audibility and any script-only native skill require an asset-equipped runtime check. No Mac access, resource downloads, push, merge, publish or version bump was performed.
