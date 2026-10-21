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
to ground mode 2, excluding saddle/head offsets, with actor-relative layer sorting.
The five set display names are aliases of the same selected visuals.
These five identities stay grounded even if a stale sender supplies mode 4;
ordinary buffs retain their original mode behavior. Ground-ring translations in
original pixels are warrior -47, swordsman -1.5, assassin 0, mage +11, healer +4.4
(Y up). They compensate original ring GSO pivot and EFF position/scale, not
character height. The complete effect is translated, preserving its upper ornaments,
colors, scale, native layer planes, and original fade/loop timing. No source EFF is changed.

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

## Existing server.json local game connection

Game-server selection is independent of resource updating. Both formal and
 development clients read the original `{ "host": "127.0.0.1", "port": 19000 }`
local file contract in this exact order:

1. `Application.persistentDataPath/server.json` (external user configuration).
2. `Application.streamingAssetsPath/server.json` (original bundled location).
3. With neither file present, the existing release/dispatch flow is unchanged.

The host may be a user-selected valid DNS name or IP address, not only loopback.
The port must be an integer from 1 to 65535. Bad higher-priority configuration
shows an error; it never silently falls back to another server. Downloaded game
resource roots are not searched for this file. No permanent appearance, account,
resource or saved update address is changed.

With local configuration, the actual native game address resolver publishes its
local server directory and host/port without requesting game discovery HTTP.
The resource updater still uses its original HTTPS dispatch/manifest rules and
saved update endpoint. No new HTTP adapter, global HTTP permission change,
forced embedded endpoint, or test-only build flag is involved. Local direct
connections do not infer an HTTP notice endpoint from the game TCP address.

The cloud `run-local-server-contract.py` executes the actual ClientBootstrap
selection block, configuration parser and native resolver, then connects a real
loopback TCP fixture. It covers external/legacy/no-file priority, invalid local
configuration, zero local discovery requests and restoring ordinary dispatch by
removing configuration. Unity host wiring is checked separately; actual packaged
Player and authenticated login remain local acceptance steps.

### Direct local login: optional announcement UI regression

The local server configuration intentionally has no HTTP announcement URI. The title
binding now accepts that absent URI, and the title changelog iterator terminates before
creating a request. `NoticeBoard.Fetch` already has the corresponding empty-endpoint
guard. No fake HTTP URL or production announcement fallback is inferred.

`Tests/ClientBuild/run-local-title-notice-contract.py` compiles and executes the exact
production Bootstrap selection block, local options parser, title `Awake`, owner
publication, input binding, login action, announcement button, and complete changelog
and NoticeBoard lifecycle implementations. Unity, texture loading, and login transport
are doubles. It checks that null announcements do not interrupt resource initialization
or the Awake completion callback, the character surface is published, login can invoke
connection/send after that callback, and neither announcement iterator requests an
empty URL. Existing remote announcement and login behavior is also exercised.

This is not a Unity Player, rendered button click, real SetupNet lifecycle, or authenticated
server login test. The separate local-server contract exercises the actual resolver
and a real loopback TCP socket; final Player startup/login still requires local validation.

### Pending entity skill dispatch regression

A separate pre-existing targeting defect affected authoritative entity skills, not
only transformed warriors. `Update` reached `CastAtTarget` after approaching the target;
that method re-entered `RequestSkill`, which re-queued the same request. The caller
then cleared it, so no network writer ran. The consumer now sends the entity request
only after checking its current target, range and cast pause; other modes keep their
existing routing. It does not remove MP, learned-skill, cooldown or target checks.

`run-pending-entity-cast-contract.py` reproduces the missing send on baseline 56d81c3
(with `FFO_CAST_BASELINE=56d81c3`) and tests the repaired request/pending-stage/consumer/
network-writer composition, including approach, pause/replay, single dispatch, rejected
targets, friendly/self casts and ground casts. F-key and mouse share the original
hotbar admission call sites. Unity input/rendering and transport are doubles here;
this is not a claim of completed in-game casting validation.
