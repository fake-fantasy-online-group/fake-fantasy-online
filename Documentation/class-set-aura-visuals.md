# Selected class-set aura visuals

These are visual identities only. This change does not create aura bonuses,
assign numeric values, or activate a new server rule.

| Class | Server VisualKey / buff name | Native effect | Full-template cycle |
|---|---|---|---|
| Warrior | 套装光环·战士 | TFX074_40 | 2110 ms |
| Swordsman | 套装光环·剑客 | TFX028_40 | 1600 ms |
| Assassin | 套装光环·刺客 | TFX7012_40 | 1500 ms |
| Mage | 套装光环·术士 | TFX7015_40 | 1700 ms |
| Healer | 套装光环·药师 | TFX7018_40 | 2100 ms |

Use existing source-bound equipment aura membership. The authoritative server
owns eligibility, team/range, expiry and removal; visuals grant no stats.
Existing `equipmentAuraStatuses` sends the `VisualKey` as the status/buff name,
with source-bound remainSec -2. `ActorEffects.SyncAuras` keys by that identity,
so snapshot renewal does not restart or duplicate an emitter. Removing the key
stops it. Use native base attachment (PositionMode 0); selected aliases resolve
to mode 3 at the actor base/mount offset, with actor-relative layer sorting.
Do not send head attachment mode 4 for these full-body/base-authored templates.

Only these five exact class-set identities use existing EffectPlayer loopMode 1
with selfDestroy=false, starting from time zero. All authored layers, colors,
scales, offsets, spline motion and sprite speeds are unchanged. The engine's
full-template clock repeats after lifeMs (last keyframe time + 100 ms).
**Some selected effects intentionally fade out and reappear each cycle.**
This is not a seamless sustained-loop redesign or an opaque end-frame freeze.
Ordinary buffs, direct TFX identities and previous equipment aliases retain
SettleToEnd and their original transparent/settled end states.

This shared presentation owner is used for local and remote actors on Android,
Windows and macOS. No per-platform patch or new renderer is introduced. Existing
clients do not recognize these aliases; deploying server rules requires the
matching updated shared client, or a separately approved rollout plan.

## Cloud verification

`Tests/Endgame/run-class-set-aura-contract.py` compiles the actual production
mapping and SyncAuras method with minimal Unity doubles. It also compiles the
actual loop-clock block from EffectPlayer.Update. Checks cover five mappings,
native attachment/sorting, 24-hour simulated clock bounds, renewal identity,
source removal, ordinary-buff regression and hidden-actor state. Existing
`run-aura-buff-contract.py` remains passing.

These are source-executing cloud contracts, **not Unity Player or device tests**.
Offline previews compose original EFF and GSO assets and reproduce the full
loop clock; they are not screenshots of the running game. Assets and instance
configuration are not bundled in this code change.

## Blue dragon-person complete-body appearance

Approved model: 1371, from `avatar/npc/npc1371{action:02}.gsn`.
Existing appearance protocol is reused: Body=1371 and Cap/Face/Backpack/WeaponR/
WeaponL=0. Head/Hair/Gender and player identity, equipment storage, skills and
combat weapon type/range stay unchanged. `PlayerAvatar.Build` normalizes only a
value-copy of appearance, skips human-head asset reads/composition for 1371,
and uses the complete body bounds for nameplate height. Local PlayerController,
RemotePlayer and the shared ReferencePlayerAvatarRuntime all use this owner.
A later ordinary appearance snapshot rebuilds the original head/equipment.
Missing transformed action art cannot fall back to a normal human body.

The server decides transformation eligibility, including source equipment
removal/breakage. Death remains a dragon with its death action while the server
still reports that body. Map-entry appearance uses the same fields. This client
change does not auto-dismount or change existing mount priority; riding uses the
native action-13 pose when selected by the existing controller.

`run-dragon-transform-contract.py` executes actual production Build/action
methods with lightweight asset/Unity doubles to test body-only composition
eligibility, input immutability, shared owners, all action slots, missing-action
isolation and normal appearance restoration. It does not substitute for a Unity
Player test. Original model files are decoded separately for offline inspection
and are not committed to the open-source repository.

## Private local-development bootstrap

Local test builds may inject `FFO_BUILD_CONFIGURATION=development`,
`FFO_BUILD_DEVELOPMENT_HTTP=1` and `FFO_RESOURCE_DISPATCH_URL` together, using
localhost, canonical 127.0.0.1 or ::1 with the local dispatch port. No instance
address is committed. Runtime additionally checks `Debug.isDebugBuild` before
allowing this opt-in. Formal builds still reject loopback, even HTTPS, and
reject developmentHttp=true. Wildcard listeners, localhost subdomains, non-
canonical IPv4 and credentials/query/fragment URLs remain disallowed. This
changes no OS permissions or server listening interface.
