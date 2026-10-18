# Level-up presentation receipt restoration

The independent client prewarms the original named `升级特效` template in `EffectPlayer` (recovered static constructor token `0600029d`, usage 17152), but no level-increase receipt requested it. The separate `LevelUpFX` class only shows frame zero of `magic0139.gso`; it has no recovered in-assembly caller and is not substituted for the named effect.

`ReferenceWorldReceiveOwner.ReceivePlayerAttributeFields` now compares the previous authoritative level with element zero after the existing seven scalar stores. A positive previous level, increased new level and attached local player trigger exactly one `EffectPlayer.SpawnOn(0, "升级特效")`. Existing effect parsing, sprite animation, keyframes, attachment and retirement are unchanged. Entry uses its separate method and emits no pulse. Duplicate snapshots, level decreases, short/null arrays and detached worlds emit none. A multi-level increase produces one pulse.

This is a presentation repair for the independent client, not a claim that the recovered original attribute dispatch contained this extra call. No server packet, level mutation, new artwork, generated particles, or alternate effect name was introduced.

## Verification

`Tests/WorldEntry/run-level-up-effect-contract.py` compiles the actual three production receipt methods with recording substitutes for the Unity-dependent effect boundary. Eleven assertions pass. Running the same test against the pre-change source fails at `live level increase`, proving it exercises the missing call.

The test does not prove rendering, availability of installed base resource packages, the user's exact “soulspiral” appearance, or Unity/device behavior. With installed original resources, manually verify a live level-up while moving, a repeated stats update, and relogging. The original template remains loaded through the existing resource package/patch precedence and missing assets remain missing rather than silently replaced.

## Quest completion remains unresolved

No original quest-completion orb template name or caller could be established from the current client source, the old server tree at `2ed1004`, or the supplied 1.6.0 macOS resource manifest. The original global metadata was downloaded only to cloud scratch and its MD5 verified as `6ad00956c6d43254ae2810fb2cf25813`; it includes the named level-up effect but no `SoulSpiral` identifier. The manifest omits base `res_q21*.pkg`/`res_05diff.pkg` packages. No quest visual is invented or wired to an unverified name. Recover its original resource mapping before implementing that separate effect.
