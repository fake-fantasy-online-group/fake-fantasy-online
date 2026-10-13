# Native inventory and sidecar identity

The native inventory is authoritative for the description currently shown in a
bag or equipment slot. A matching item-table ID and slot do not identify a
particular equipment instance: two rolls of the same base can occupy that slot
at different times.

The supported server's `pushInventory` sends the native inventory before its
endgame snapshot, on the same ordered connection. The client uses that existing
publication boundary; no new wire fields or client-generated item identities are
introduced.

- `PrivateServerGateway.PublishInventoryState` invalidates the previous sidecar
  association and any outstanding cube quote.
- Some existing server paths (`warehouse`, `item_lock`, `hair`, `wardrobe`)
  publish native inventory directly without a sidecar. After the host finishes
  this frame's receive pump, one coalesced read-only `snapshot` request recovers
  those changes. A complete or already assembling push suppresses that request.
  Sending it consumes the automatic attempt even if transport fails; only a new
  native publication or an explicit user refresh can request again.
- `EndgameClientState.InventorySnapshotCurrent` requires a complete snapshot,
  matching authoritative revision, and no intervening native publication.
- Bag/equipment overlays, workbench item selection, and plugin UID selection use
  this common fence. Historical snapshot rows can still be observed; the plugin
  exposes `inventorySnapshotCurrent` so they cannot be mistaken for current rows.
- During a replacement or reconnect, the native tooltip remains readable and
  unchanged. Old sidecar names, lore, sockets, and UIDs are not applied to it.
- A newer assembling revision cannot be displaced by an older response. Retired
  snapshot IDs are kept in a bounded, generation-scoped set so interrupted tails
  and repeated publications cannot revive an old association.
- A missing or malformed replacement does not restore old authority. The existing
  15-second timeout exposes a refresh message; there is no indefinite auto-retry.
- An explicit failed snapshot ends that automatic read and its timer. The
  server's precise error (for example `snapshot_size` or `storage`) remains
  visible instead of becoming a later timeout. Manual refresh is available,
  while any unrelated immutable pending mutation remains unresolved.

## Thread ownership

`NativeNetClientHost.Pump` invokes the session/gateway pump, whose
`NetClient.Update` synchronously dispatches native inventory and endgame packets
on the host thread, then invokes `TickEndgameForIndependentRuntime`. Unity UI
`Update`/`OnGUI` reads the same host-owned state. The world-entry admission fence
keeps later inbox packets paused until entry publication completes.

The retained startup response is the exception: an asynchronous continuation can
resume off-host. Its inventory publication now awaits the existing
`IHostExecutor.RunAsync` through `PublishInventoryStateOnHostAsync`; cancellation
before dispatch prevents the old entry from mutating a new session. Endgame
collection changes occur outside `_worldStateSync`, which protects immutable
observation publication rather than the Endgame host-thread owner.

## Rune descriptions

Rune catalog values now use ordinary attribute formatting. Special recovery
cadence and eligibility are taken from the server's existing `description`
field, rather than inferred from a particular item ID. Native card descriptions
retain their actual persisted values and are not replaced by catalog values.
The UI regression includes moving recovery to another rune ID with a 1.5-second
description, including an owned value different from the catalog reference.

## Verification

The same production-source reproduction at the prior HEAD displayed an old
instance name and lore on a new same-base native row while a replacement snapshot
was incomplete. The corrected code preserves the new native tooltip verbatim.
`EndgameUiContract` covers same-base bag/equipment replacement, partial chunks,
older/newer revision interleaving, interrupted tails, departure, reconnect, and
bounded timeout. `EndgameHostContract` verifies the actual gateway publication
and packet-dispatch boundary, the four native-only paths' receive pattern,
coalesced recovery, no-retry timeout, and off-host entry handoff/cancellation.
These are managed contracts with deterministic
Unity substitutes; they do not establish Unity rendering or Player acceptance.
