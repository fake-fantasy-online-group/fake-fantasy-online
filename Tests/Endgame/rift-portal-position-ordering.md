# Rift portal movement ordering

This narrow change adds a portal-only forced variant of native movement
reporting, `GameBootstrap.FlushMoveReport(true)`, immediately before the physical
portal sends `rift.advance`. No server
position radius, delay, portal geometry, retry policy, or server movement
validation is changed. The physical gate bypasses local send throttles; skills
keep the original zero-argument throttled path.

## Reproduced failure

The client tests the seeded portal at 45 pixels, but reports ordinary movement
every 0.3 seconds. A player can enter the 45-pixel area while the server still
has a previous position outside its 160-pixel admission radius. For example,
with the exit at (200, 300), moving from (395, 300) to (245, 300) in 0.25 seconds
used to enqueue only `rift.advance` before the next periodic position report.
After an `exit` rejection, remaining inside does not submit a second mutation;
a fresh leave/re-enter edge is required. This is independent of map resources
and was reproduced in both 86a988b and e5f513e-equivalent source.

The regression compiles the production portal, transfer, position reporting,
and Endgame state sources together. Only Unity services, the transport, and
the server's already independently checked 160-pixel position predicate are
substituted. It asserts `move` precedes `rift.advance` for both native and seeded
physical gates, and preserves the existing pending/rejection edge semantics.

```sh
FFO_DOTNET=/path/to/dotnet bash Tests/Endgame/run-rift-portal-position-contract.sh
```

The companion wire-order test compiles the unchanged production session and
gateway movement wrappers, portal sender, complete host executor, native payload
writers, and `Send` / `PaceTx` / `DrainTx` bodies. It observes the same sender's
0x1017 position packet before its 0x10d0 advance packet, with consecutive native
sequence numbers, both immediately and with the 45-packet transmit window full.
An occupied gateway send semaphore does not defer movement. Crypto and the final
socket write are substituted, so this proves ordered queue entry, not delivery
or server acknowledgement.

```sh
FFO_DOTNET=/path/to/dotnet python3 Tests/Endgame/run-rift-portal-wire-order-contract.py
```

## Explicit limits

- Ordinary skill `FlushMoveReport()` keeps its 0.06-second and 24-pixel
  throttles. The portal-only forced variant skips both to cover legal same-frame
  scheduling: periodic Bootstrap Update may first report the old position and
  clear its timer, then PlayerController moves 150 pixels in a 250ms frame at
  600px/s. The production movement clock does not cap that frame delta.
- A force still uses the same movement packet, host executor, connection, and
  ordered native transmit queue. It changes no server admission/validation.
- This does not blindly retry a rejected mutation or clear an uncertain nonce.
- Missing/stale inventory snapshots, optional sidecar parse failures, and map
  geometry mismatches are separate issues.
- The triggering evidence is deterministic production-source execution, not a
  Unity Player, device, live account, or proof of the cause of a particular run.
