# External agents and the shared Game API

Lua addons and external processes use the same `IGameApi` observations, discovery,
action receipts and event journal. The TCP adapter is not another gameplay engine:
`PluginGameApi` routes supported actions through existing native gameplay owners.
All calls into that API run on the main game thread. Socket workers only frame,
parse, queue and serialize messages.

This implementation is source-tested with fake gameplay. It has not been validated
in a Unity runtime or against a live game server. External compatible game assets
and a server are still required to play.

## Explicit opt-in

The bridge is **disabled by default**. Enable it in the addon manager, or launch an
already-built client with `--agent-bridge`. The default endpoint is
`127.0.0.1:17845`; `--agent-port=17845` changes the configured port, but does not by
itself enable the bridge. The manager can stop it again. Bootstrap accepts ports
1024–65535. The C# constructor also permits port zero for ephemeral test listeners.

The listener always binds IPv4 loopback. There is no remote listener setting,
authentication, TLS, credential storage or OS/network configuration change. Any
process running as a local user that can reach this port can issue gameplay
commands. Enable it only on a trusted computer. Do not expose or forward it onto
another network. Remote access requires a separate, deliberately reviewed design.

The C# facade is:

```csharp
var dispatcher = new MainThreadGameDispatcher(); // Construct on the game thread.
var bridge = new GameAgentServer(gameApi, dispatcher, 17845); // Does not listen.
bridge.Start(); // Explicit opt-in.
// Each game frame: gameApi.Tick(); dispatcher.Pump();
// On stop/disposal: bridge.Dispose(); dispatcher.Dispose();
```

The API instance can simultaneously serve Lua and the external bridge. The
bootstrap already owns this wiring. Do not create a second copy of the gameplay
state. `IsRunning`, `Port`, `ConnectedClients` and `LastError` expose bridge state.
`Stop` closes sockets and withdraws pending dispatches; `Start` may be used again.
`Dispose` is final. A synchronous game API call that has already started cannot be
preempted. Already submitted game-server actions are not undone by disconnecting.

## Wire protocol: ffo-agent/1

Open a full-duplex TCP connection. Send UTF-8 JSON objects, each followed by LF.
CRLF is also accepted. Do not pretty-print a request across multiple lines or
half-close the sending side while waiting for a response. Keep the connection open
until replies arrive. JSON duplicate keys, invalid UTF-8, nonfinite numbers,
excessive nesting and ambiguous/out-of-range number representations are rejected.

Each request is:

```json
{"id":"message-1","method":"observe","params":{"domain":"player","arguments":{}}}
```

`id` is a nonblank string of at most 128 UTF-16 characters, used only for response
correlation. The same bound applies to method names, domain names, action names,
request IDs and action IDs. `params` may be omitted for `describe` and is otherwise
an object. Unknown fields are rejected instead of ignored. Every request normally
has exactly one response; transport admission errors can have `id:null` followed
by a disconnect.

Successful envelope:

```json
{"id":"message-1","ok":true,"result":{"domain":"player","sceneRevision":7,"revision":24,"data":{}}}
```

Failed envelope:

```json
{"id":"message-1","ok":false,"error":{"code":"invalid_params","message":"domain is required."}}
```

`ok:true` means the API call returned. An `act` result can still be a `rejected`
receipt. Inspect its `state`, `code` and `message`. `api_error` means no usable
result was returned; it does **not** prove that no side effect occurred.

Machine-readable envelopes are in [request.schema.json](request.schema.json) and
[response.schema.json](response.schema.json). Action and observation argument
schemas are available live in `describe`; availability can change as gameplay
state changes.

### Methods

- `describe`, no parameters: protocol version, method names, live action
  definitions, and observation definitions. Actions include `name`, `description`,
  `arguments` (JSON Schema), `available` and `unavailableReason`. Queries include
  `domain`, `description` and `arguments`. `observationDiscoveryAvailable:false`
  means an alternative `IGameApi` does not implement the optional catalog.
- `observe`: `domain` string, optional `arguments` object. Result contains `domain`,
  `sceneRevision`, `revision` and domain-specific `data`.
- `act`: `name` string, required `requestId` string, optional `arguments` object and
  `expectedSceneRevision` nonnegative integer. Result is an action receipt.
- `status` (alias `action-status`): required `actionId`. Returns the current receipt.
- `cancel`: required `actionId`. Returns `{actionId,cancelled,receipt}`.
  `cancelled:false` is meaningful: sent, completed, unknown or otherwise
  non-cancellable operations cannot be revoked. Successful cancellation applies
  only to cancellable local work.
- `events`: required `afterSequence` nonnegative integer, optional `maxCount`
  integer 1–512 (default 128). Returns `{events,nextSequence,resyncRequired}`.
  Each event contains `sequence`, `sceneRevision`, `name`, `data`.

### Observation coverage and scene safety

Discover the current schema rather than assuming every domain is ready at login.
Current domains are `world`, `player`, `entities`, `npcs`, `loot`, `inventory`,
`equipment`, `skills`, `buffs`, `target`, `quests`, `dialogue`, `party`, `ui`, `endgame`,
`endgame.items`, `endgame.catalog`, and `endgame.rift`. Read the
availability/reason fields inside snapshots when the required native owner or
world session is missing.

Common filters include `limit` (default 128, max 512), `sinceRevision`,
`sinceQueryKey`, and `expectedSceneRevision`. Spatial domains provide map-pixel
radius/center and stable ID/name filters; inventory and quests have their own
filters. List responses include counts and `truncated`; request narrower filters
when truncated. The returned query schemas are authoritative for allowed fields.

A snapshot uses `deltaMode:"replace"` for a full replacement. To request an
unchanged response, send both the exact previous `revision` as `sinceRevision` and
the exact previous `data.queryKey` as `sinceQueryKey`, with the same query filters.
Matching revisions alone are insufficient when filters changed. Changed scenes
report `resyncRequired:true` and require new snapshots.

Map positions use top-left map pixels, x right and y down. Stable entity IDs include
kind, scene revision and native identity; treat them as opaque. Entity list
positions are latest server snapshots, not claimed live interpolated transforms.
The target observation can report the current native actor position. Never reuse
entity IDs from a previous scene. When choosing an action from a snapshot, supply
its `sceneRevision` as `expectedSceneRevision`; a stale decision will be rejected.

### Actions, retries and completion

Three identifiers have different jobs:

1. Envelope `id`: matches one response to one message.
2. `requestId`: identifies one intended gameplay operation, including retries.
3. `actionId`: returned by the API and used for status/cancel.

Generate a globally unique `requestId` and save the entire action payload **before
sending** it. If a connection is lost after send, the outcome is uncertain. A retry
must use the same request ID, name, arguments and expected scene revision; a new
ID can execute the action again. Identical retained requests return the same action
ID without resending. Changed content with a retained ID is rejected. IDs are
shared with Lua callers, so prefix them by client/addon identity or use UUIDs.

The current native adapter keeps at most 1024 action records and retains active
work while evicting older settled records. Once an ID is evicted or the API/client
restarts, deduplication is no longer guaranteed. Do not blindly replay old actions:
re-observe and reconcile the world first. `status:unknown` may mean the receipt is
no longer retained.

Receipt states are `queued`, `sent`, `running`, `completed`, `rejected`,
`cancelled`, `timedout`, and `unknown`. **Sent only means submission through the
actual native gateway; it is never proof of combat success, inventory mutation,
quest acceptance or reward.** Some sent operations do not have a correlated final
server acknowledgement and remain sent. Inspect subsequent observations/events.
For local movement, completed refers to observed local arrival, not a promise that
the server accepted every movement update.

Supported actions include native navigation/stop, target selection, combat
approach, skill queue/direct cast, pickup approach, inventory use/sort/split,
equipment, NPC/dialogue, quest requests, revive, native chat channels and whisper
to observed players when their owners are available.
Use `describe.actions`, including `available` and schemas, for exact names and
arguments. Unsupported commands such as native-window opening and inventory placement
are explicitly disabled. There is no invented success fallback. `chat.send` requires a native channel 0–3
and text; `chat.whisper` requires an observed player entity ID. Both reject slash
commands and remain sent rather than claiming delivery. Dialogue and quest actions
require the matching observed revision guards. Some item uses open a native
confirmation or other local UI: inspect `completionScope`, `requestSent` and
`confirmationVisible`; local completion is not proof that an item was consumed.

The shared native endgame adapter also exposes guarded catalog/socket/cube/rift
actions and UID-safe native equipment. See [ENDGAME.md](ENDGAME.md) for exact
argument fields, ordered rune inputs, server quote execution, pending-request
reconciliation and connection-generation safety. Its correlated authoritative
receipts may establish server completion; an initial send never does.

### Event cursors and resynchronization

Every client holds its own sequence cursor. `events` does not consume or clear
another client's events. Save `nextSequence` after processing a batch. Poll again
with that value to receive the next bounded batch. Sequences are integers: use an
integer-safe JSON parser if your language defaults to floating-point numbers.

The journal is bounded. If `resyncRequired:true`, the requested history is no
longer complete (or the scene/API changed). Refresh the snapshots your planner
needs, then continue with the returned cursor. Also invalidate scene-scoped state
when a scene revision changes. Events describe observed state and action updates;
they are not a guarantee of all upstream server events.

### Resource limits and backpressure

Default server limits:

- 8 connected clients, 8 waiting frames per client plus one dispatched request
- 65,536 request bytes, 1,048,576 response bytes, excluding LF
- 120 seconds without input, 10 seconds waiting for game-thread dispatch,
  5 seconds blocked sending to a client
- Shared dispatcher capacity 1,024 jobs; bootstrap may choose a smaller capacity
- JSON nesting at most 32 and at most 65,536 values/property nodes

Requests from one connection are dispatched serially. Independent connections can
interleave on the main thread. Reduce request rate rather than flooding a stopped
or paused game. Frame overflow and per-client queue overflow send an error if
possible and disconnect. Excess connections close immediately. A too-large
response produces `response_too_large`; request fewer items rather than expecting
truncation of arbitrary JSON. Error responses may omit correlation when necessary
to remain within a configured byte limit.

`dispatch_timeout` and dispatcher `queue_full` mean that queued call did not run.
On EOF/disconnect, queued callbacks are cancelled even if the game thread is not
pumping. A callback that already started keeps its actual result. To stop a local
operation after it started, use its `actionId` and `cancel`; sent operations cannot
be unsent. Stopping the bridge does not cancel gameplay actions created earlier.

## Python client and an agent loop

The stdlib-only [example client](../../examples/agents/game_agent.py) defaults to
read-only observation. No package install or credentials are required:

```sh
python3 examples/agents/game_agent.py --describe
python3 examples/agents/game_agent.py --domain entities --arguments '{"radius":300,"limit":32}'
python3 examples/agents/game_agent.py --status 'act_returned_by_api'
python3 examples/agents/game_agent.py --cancel 'act_returned_by_api'
```

An explicitly requested action can be sent with `--action`, for example after
observing a current position and scene revision:

```sh
python3 examples/agents/game_agent.py --action movement.move \
  --arguments '{"x":640,"y":480,"tolerance":4,"timeoutSeconds":15}' \
  --scene-revision 7 --request-id 'my-agent-unique-navigation-decision' --wait 2
```

Those coordinates and revision are illustrative, not safe destinations inferred
from the game. Replace them with a decision grounded in your current snapshots.
The CLI prints the retry identity and payload before sending and does not pretend a
bounded status wait reached completion.

To integrate a planner/LLM, import `GameAgent` and use a sense/decide/act loop:

1. Discover available actions and observation schemas.
2. Observe the player/world and bounded relevant targets or inventory.
3. Choose one available action using observed IDs, guards and revision.
4. Persist that decision and a unique request ID; call `agent.act`.
5. Track the returned action ID, events and relevant new observations.
6. On a gap, scene change, rejection or uncertain outcome, re-observe before making
   another decision. Cancel only owned, cancellable work when a plan changes.

The planner supplies decisions; the client does not invent a quest solver or an
unverified navigation route. Lua and Python decisions produce identical API
receipts and flow through the same game owners.

## Standalone tests

Run without launching the Unity Editor or building a Player:

```sh
UNITY_EDITOR_DIR=/path/to/Unity/Editor Tests/PluginRuntime/run-agent-transport-contract.sh
```

The script uses only the bundled managed compiler and Mono runtime, plus Python 3.
It creates ephemeral loopback listeners with fake data and shuts them all down.
Coverage includes discovery, observations, repeated/conflicting requests, receipt
lookup, truthful cancellation, independent event cursors/gaps, main-thread calls,
framing/Unicode, malformed/oversize input, bounded output/queues/clients, dispatch
expiry, disconnect/Stop cancellation and restart. The Python client and its CLI
also run against a temporary C# fake server. These are transport/contract tests,
not proof of Unity visual behavior, native gameplay success, or server compatibility.

## Ordinary party operations

Discover `party` observations and the live `party.*` actions before using them.
`party` exposes the native roster, scene-qualified member and leader identities,
local membership/leadership, and exact decimal-string `expectedPartyRevision`.
Use `limit`/`offset` for bounded member pages; do not interpret a truncated page
as the full roster. No query sends a party request or drains a native UI queue.

Available ordinary adapters are `party.create`, `party.invite`, `party.join`,
`party.leave`, `party.dismiss`, `party.kick`, and `party.set_leader`.
All require `expectedPartyRevision` from the current native party snapshot.
Create accepts `name` with the native UI's maximum of 16 characters (empty is
allowed; whitespace is trimmed). Create/join require an empty roster. Invite
requires local membership; leave requires a nonleader, while dismiss/kick/leadership transfer require local
leadership. Targeted actions require scene-qualified `entityId` and exact
`expectedName`; invite/join resolve a current remote actor, kick/transfer resolve
a current roster member, and leadership transfer requires an online member.
Persistent character identity takes precedence over recycled scene IDs, matching
the native social UI. Self-targeting is rejected. Before enqueueing a write, the
adapter rechecks the captured party root, revision, local identity and role.
The native receive owner can still publish a change after enqueueing; the server
remains authoritative. Existing request-ID, scene, cancellation and retained
receipt semantics apply unchanged.

These are normal native client commands. A `sent` receipt proves only a successful
gateway write. Observe `party` again to establish current membership before
entering a party-gated trial. Do not repeat an uncertain create/join under a new
request ID. Invitations and join requests can need approval from another player;
the adapter does not auto-accept requests or bypass server eligibility. There is
no new party-list matchmaking API, pending invitation/approval adapter, follow
command, or atomic create-and-invite batch. Create and subsequent invitations are
separate deliberate decisions using fresh observations.
