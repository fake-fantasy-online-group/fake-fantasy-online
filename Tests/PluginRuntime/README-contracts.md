# Transport-neutral contracts tests

These standalone C# tests compile PluginApi plus the JSON codec and dispatcher with the installed Mono compiler. They do not launch Unity, connect to a server, or invoke gameplay.

```sh
UNITY_EDITOR_DIR=/path/to/Unity/Editor Tests/PluginRuntime/run-game-contracts.sh
```

Coverage:
- Signed/unsigned 64-bit and decimal precision, exponent/floating-point round trips, Unicode/escapes and culture-invariant JSON.
- Strict malformed/duplicate-key rejection, finite numbers, exact integer range, bounded document/string/node/depth sizes, cyclic input, unsupported CLR types, escaped output expansion.
- Deep immutable snapshots, request IDs/scene revisions, event batch/UI copies, unique widget IDs and finite geometry.
- Optional observation-domain and filter discovery schema.
- Main-thread ownership, queue budget, action exception isolation, immediate queued cancellation, cancellation while executing, backlog rejection, and disposal.
- 200 cancellation-versus-pump races and pending-task disposal.

Limits and semantics:
- JSON document and aggregate text cap: 1,048,576 UTF-16 characters; per-string cap: 262,144; nesting cap: 32; node cap: 65,536; numeric token cap: 256 characters. Network transports should also bound incoming bytes before decoding.
- Number tokens without a fraction/exponent must fit exactly in long, ulong, or decimal. Other finite numeric tokens prefer exact decimal, then double. Infinite results and nonzero underflow to zero are rejected.
- Parse emits JSON-shaped mutable collections; API DTO constructors freeze a deep copy. Serialize only supports JSON-shaped values; protocol adapters project DTOs explicitly, without reflection.
- InvokeAsync always queues, including on the owner thread. Pump must run on the constructing thread. Never synchronously block that thread on an unpumped task.
- Cancellation withdraws queued jobs. Once execution is claimed, a synchronous action runs to its actual result; cancellation does not fabricate an action outcome.
- Dispose faults queued jobs and prevents future submission; it cannot undo an action already executing.
