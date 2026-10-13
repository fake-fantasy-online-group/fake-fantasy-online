# Native entry routing acceptance

Scope: client `8066014` plus the bounded hello-failure fix described below; cloud managed tests only. No new native opcode, capability, packet field, or map transition lifecycle is introduced. Checked-in configuration is evidence of authored routes, not proof of live database deployment.

## Actual business entry routes

| Entry | Actual client sender | Ownership until first map build |
|---|---|---|
| Ordinary dungeon NPC “进入…” | `DialogUI.Travel.AddDungeonEntryOptions`, table-backed coordinates and native party check → `GameBootstrap.DoTeleport` | Client builds, then native `0x100a(map,x,y,entered=1)`. Server `ClientMapReady` admission resolves the instance or sends a correction on rejection. |
| Trial enter / guard travel / exit | `DialogUI.AddTrialOption` and `DialogUI.Travel` → `0x1006 Str(trial/trialgo/trialleave)` | Server `Session.onGMCommand` → `scene.TrialAction`; client waits for `0x803a`. Exit requires its existing confirmation. |
| Ordinary outdoor polygon | `PlayerController.TransferFrame`, empty `proc.cmd` → bound `onTransfer` → `SwitchMap` | Client builds before ready. `ReferenceFirstWorldWindowOwner` binds the actual callback; the server's configured client-owned gate does not emit a competing teleport. |
| Transport NPC/list | `DialogUI.ShowTransferMenu` → `0x1071(list)`; `TransportUI.DoTeleport` → `0x1072(list,index)` | Server validates the opened-list/NPC context, price, destination and instance. No client destination or speculative map build. |
| New-character village choice | `DialogUI.Travel.BuildTravelMenu/ShowVillageMenu`, exact “传送仙子” rows | Local map-owner callback: 桃源东61(6923,1763), 桃源西37(5978,2432), 长乐东62(4863,3529), 长乐西38(4965,3681); then ready. This is not the transport-list path. |
| Living return / death return | `InventoryDlg.ActivateOrdinaryItem` → `NetClient.UseItem` → `0x1015(item,tab,slot)`; `ReviveDlg` option0 → `0x1018(0)` | Server `UseReturn`/revive configuration chooses the landing and sends `0x803a`. Return scroll3381 and book10025 keep their original payloads. |
| GM teleport | `ChatUI.SendNow` strips the slash → `GmCmd` → `0x1006 Str(command)` | Server GM permission, in-world and destination checks precede `scene.GMCommand`; no client map prebuild. |

Relevant production sources are under `Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets`; packet writers are under `Runtime/Protocol`. All integer map/landing fields remain native I32, not float payloads.

### Floors 1/2/3 are not one sender type

Checked-in server migration65 gives 天空之泉20801→20802→20803 ordinary client-owned polygons. 失落神殿20901→20902 also prebuilds; its floor2→3 uses 古代石碑 list109/index0, and floor3 exit uses list133/index0 (migration0 transport rows). Those two wait for the server. `scene/transfer.go` and `scene/dungeon.go` preserve the private instance through their respective routes.

### Explicit unsupported surface

Native `taoyu`, `taoyu hard`, and `dungeonhard N` senders exist, but the current `Session.onGMCommand`/parser has no command handler. This does **not** mean ordinary 塔狱 has no entrance: `data/gamedb.go` maps map507's `Trap20502`, and the server-owned movement gate invokes existing `onEnterDungeon` admission.

Legacy hard-mode semantics are not implemented: client hard NPC target IDs exist, but current DungeonDef/loader/router have no matching hard-difficulty rule. These options remain an unimplemented/product-rules item. This change does not invent difficulty multipliers, broaden entry permissions, or add duplicate normal 塔狱 admission.

## Rift ownership and delayed information

The real PlayerController portal branch calls `EndgamePanel.TryTransferRiftPortalFromWorld` **before** ordinary map/script handling. Unknown context holds the gate; known private context with missing/old run information also holds it. Only authoritative public context restores ordinary routing. Run/floor revisions, immutable mutation IDs, pending failures and server receipts remain separate from map arrival.

Server ordering is explicit: successful hello → scene context → snapshot; private transfer context precedes `0x803a`, and public exit context follows it. Same-map/different-instance transfer uses the same authoritative Enter path. Fresh private reconnect is different: it has no retained negotiated portal capability and uses SelfEntered (`0x8033/0x8003`), so it can build before receiving hello/context.

### Confirmed fresh-reconnect failure and bounded repair

Actual seeded admitted reconnect produced private `SelfEntered.Scene={7,90}` without pre-hello context. Actual `Session.OnPacket` against a full actor mailbox produced three distinct hello/busy wire payloads. Replaying those bytes through production codec, EndgameClientState and PlayerController reproduced an ordinary public callback after retry exhaustion.

The client now remembers valid current-server hello/capability or temporary-failure evidence within the connection. Exhaustion after busy/pending/storage, later transport failure or timeout keeps unknown ownership; it does not guess public. Automatic hello attempts remain bounded at three, and the failure message directs reconnection. Reset clears the evidence. A successful legacy hello without portal capability still permits normal gates; a silent legacy server keeps its historical compatibility path.

A backend Load held open gives no first reply, then pending on attempts2/3; actual fixture replay covers that sequence. A separate actor-queue stall can keep all accepted requests silent. The companion server change synchronously admits valid hello with the existing pending response before posting to the actor, preventing this current-server silence from masquerading as a legacy peer. No new wire schema is needed. Invalid/not-in-world/unsupported requests keep their previous boundaries.

Late successful answers to earlier attempts remain valid; late old pending/busy must not overwrite a completed negotiation or an explicitly newer refresh. The original native map construction/ready sequence is untouched.

**Deployment requirement:** deploy the client state fix and server admission response together. Client `8066014` alone still treats exhausted pending replies as public compatibility; the server-only change does not fix that client. Conversely, the client-only change cannot distinguish an entirely silent current actor from a legacy server. This cloud verification does not mean a local client or live server has been updated.

## Tests and boundaries

- `python3 Tests/Endgame/run-rift-routing-race-contract.py`: actual DTO/state, native transfer and plugin production code, with deterministic Unity/native-owner substitutes.
- `python3 Tests/Endgame/run-native-entry-sender-contract.py`: actual native menu callbacks, facade methods, protocol senders and Buf serialization; the socket is substituted.
- `python3 Tests/Endgame/run-rift-reconnect-busy-contract.py --fixture <real-server-export>/hello-busy-1.payload.bin --admission-fixtures <real-server-export>`: real server response envelopes through real codec/state/native routing. Includes three pending→busy pairs with identical request IDs, held-Load pending2/3 and delayed private success, queued-actor pending and delayed public success, old pending/busy after success and during a newer refresh, Reset, silence-only compatibility and late evidence withdrawing that guess.
- Existing portal, plugin, NPC, UI, DTO and host/managed-compile contracts remain regression requirements.

Generate fresh wire fixtures against the paired server source before the replay (run in its `server` directory):

```sh
source /workspace/shared/cloud-test-env.sh
export RIFT_CONTEXT_BUSY_FIXTURE_DIR=/tmp/rift-reconnect-pending-fixed
go test ./internal/game/scene -run '^TestRiftFreshReconnect' -count=1
go test ./internal/session -run '^TestRiftHello' -count=1
```

Then run in the client checkout:

```sh
source /workspace/shared/cloud-test-env.sh
python3 Tests/Endgame/run-rift-routing-race-contract.py
python3 Tests/Endgame/run-native-entry-sender-contract.py
python3 Tests/Endgame/run-rift-reconnect-busy-contract.py \
  --fixture /tmp/rift-reconnect-pending-fixed/hello-busy-1.payload.bin \
  --admission-fixtures /tmp/rift-reconnect-pending-fixed
```

The private actor reconnect and full/congested Session transport fixtures are composed checks, not a complete credential/login/storage/socket/Unity run through one actor. Server-native tests separately cover actual route dispatch and map-ready snapshots. These tests do not establish visual smoothness, live collision, all-map art, or full six-class live gameplay acceptance. List109/133 server coverage and hard-mode nonimplementation must be reported separately rather than inferred from sender success.
