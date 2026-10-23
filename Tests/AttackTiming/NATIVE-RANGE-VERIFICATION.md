# Native attack identity versus effective reach

The pre-entry client capability bit 64 opts into an optional final U16 native weapon attack distance on SelfData (0x8003), PlayerAppearance (0x800a), and remote spawn. All existing fields and their ordering remain unchanged. Missing extensions use the old effective-distance behavior; an incomplete one-byte native tail fails decoding.

CharacterAppearanceRecord retains effective AttackDistance and nullable NativeAttackDistance. AttackIdentityDistance selects native when available. The local character and every remote weapon refresh preserve the two values separately, including repeated spawn and cosmetic snapshots. Target approach/range continues to use effective distance. Cadence retains the recovered ctype 16 exception and ctype 8 fast-weapon rule; pose and projectiles use native distance with the original ctype predicates.

Run:

    python3 Tests/AttackTiming/run-native-identity-contract.py
    python3 Tests/Endgame/run-ground-effect-contract.py

The first compiles all current Foundation, Domain, Application, and Protocol sources, plus actual cadence, pose, projectile selection, audit controller, and RemotePlayer action code. 938 runtime assertions cover original 120 → effective 180, 150/151 boundary, genuine ranged 200 → 300, all category-bit values plus unknown/composite values, local SelfData receipt, both appearance decoders, remote spawn, actual remote pose selection, attack speed, effective targeting, missing/truncated tails, and extension reset. Source guards cover remaining Unity lifecycle adapters and all SetWeapon forwarding sites. Ground-effect regression also passes.

Validation uses the cloud .NET 8 Roslyn toolchain and explicit Unity construction/art substitutes. It does not claim Unity rendering, full Presentation assembly, executable client, or in-game visual validation.
