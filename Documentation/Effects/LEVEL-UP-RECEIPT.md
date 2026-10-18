# Level-up presentation receipt restoration

The independent client prewarms the original named `升级特效` template in `EffectPlayer` (recovered static constructor token `0600029d`, usage 17152), but no level-increase receipt requested it. The separate `LevelUpFX` class only shows frame zero of `magic0139.gso`; it has no recovered in-assembly caller and is not substituted for the named effect.

`ReferenceWorldReceiveOwner.ReceivePlayerAttributeFields` now compares the previous authoritative level with element zero after the existing seven scalar stores. A positive previous level, increased new level and attached local player trigger exactly one `EffectPlayer.SpawnOn(0, "升级特效")`. Existing effect parsing, sprite animation, keyframes, attachment and retirement are unchanged. Entry uses its separate method and emits no pulse. Duplicate snapshots, level decreases, short/null arrays and detached worlds emit none. A multi-level increase produces one pulse.

This is a presentation repair for the independent client, not a claim that the recovered original attribute dispatch contained this extra call. No server packet, level mutation, new artwork, generated particles, or alternate effect name was introduced.

## Verification

`Tests/WorldEntry/run-level-up-effect-contract.py` compiles the actual three production receipt methods with recording substitutes for the Unity-dependent effect boundary. Eleven assertions pass. Running the same test against the pre-change source fails at `live level increase`, proving it exercises the missing call.

The test does not prove rendering, availability of installed base resource packages, the user's exact “soulspiral” appearance, or Unity/device behavior. With installed original resources, manually verify a live level-up while moving, a repeated stats update, and relogging. The original template remains loaded through the existing resource package/patch precedence and missing assets remain missing rather than silently replaced.

## Original-art verification (2026-10-18)

A later cloud-only bounded HTTP Range inspection reached the original resource archive and indexed all 84,703 entries across `res_q21_000.pkg`, `res_q21_001.pkg`, `res_05diff.pkg` and `res_ff21.pkg`. Original `升级特效.eff` and `飞升特效.eff` are byte-identical and use `magic0103`, `magic0106` and `magic0107`: decoded frame sheets show an expanding rune, a rising white/blue humanoid spirit, and colored glowing particles. The template duration is 1,910 ms; its spirit layer rises from Z35 to Z80. The perceived swirl is baked into sprite frames, not an explicit circular XY path. These genuine frames support the requested upgrade appearance. Frame sheets are layer evidence, not a Unity composite or device playback test.

Original `magic0139` contains five yellow/white “Level Up!” lettering frames. It is not an orb/spiral substitute. A bounded original ARM64 direct B/BL scan found no direct caller of `LevelUpFX.Play`; `EffectPlayer.SpawnOn` has direct calls from `NetClient.Dispatch` and `CombatWorld.ShowEntityStatus`. This is not an indirect-call or full native-control-flow proof, nor proof of a historical level-increase hook.

## Quest completion: bounded search finished, visual match unconfirmed

The original archive's complete effect directory, all 154 Lua files, and 685 selected script/config payloads were inspected. No quest/task/experience/reward named effect or original task-completion-to-effect binding was established. Task-panel labels, GM `finishtask`, and the experience-bar tutorial highlight are not completion playback.

Genuine sprite frames were decoded for bounded orb, success, collection, soul/revival and healing candidates. The nearest visual candidate, `宠物_回魂.eff`, staggers `magic1035` blue rotating curved ribbon/star trails at Z10/50/100 over a ground ring. It is multiple ribbon/star trails, not a sufficiently confident match for the specifically requested trailing orb. Generic fireball, sparkle, light-shaft and success graphics were rejected. No candidate was assigned to quest completion and no replacement art was generated. A historical binding is not inherently required for a new authoritative-task-event hook; the remaining blocker is confidently identifying the requested visual.

Evidence retained outside Git: original package indices, decoded payload hashes, exact HTTP range hashes, sprite PNG/GIF previews, script-search results, and `package-effect-findings.md` under the cloud effects-reference workspace. Only bounded ranges were fetched, so the provided full 5.5 GB archive SHA256 was not independently verified. No resource binaries are committed. Research is complete for these accessible bounded sources; quest-orb restoration remains partial pending a confirmed visual/asset match.
