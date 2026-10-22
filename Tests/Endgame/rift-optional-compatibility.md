# Rift optional presentation compatibility

A `rift.state` snapshot carries authoritative progress and optional presentation
metadata together. Unsupported or malformed optional rows must not veto progress
or unrelated known visuals.

## Compatibility boundary

- Additional keys are accepted in Rift business responses and nested Rift views.
  `rift.page`, `rift.context`, and other protocol domains keep their existing
  exact envelope validation.
- Aura classification does not constrain the number of ability IDs. Unknown
  nonnegative IDs are retained within a 32-entry budget, but do not enable an
  unknown renderer. Existing known IDs continue to work.
- Unknown or unsafe aura, globe, telegraph, hazard, impact, and guardian-arrival
  entries are omitted. `SkippedVisualEntries` reports these omissions.
- Ambiguous duplicate actor/effect identities are not resolved by first/last row
  order. All entries sharing the identity are omitted.
- A conflicting immutable hazard identity is retired, not redrawn with changed
  geometry or an extended deadline. Its tombstone lasts through the latest
  conflicting deadline observed, including across absent/replayed snapshots.
- Unsupported effects do not imply a blast, heal, buff, or other gameplay action.
  Combat remains server-authoritative.

This does not accept arbitrary bad messages. Transport byte, aggregate JSON
node/depth, page integrity, native actor identity, run/floor/map scope, numeric
safety, and bounded list checks remain enforced. Malformed/oversized array
containers and invalid core state still reject the snapshot. This is scoped to
optional Rift presentation, not a blanket relaxation of every game DTO.

## Regression commands

With a .NET 8 SDK installation:

```sh
FFO_DOTNET=/path/to/dotnet bash Tests/Endgame/run-rift-optional-visual-contract.sh
FFO_DOTNET=/path/to/dotnet bash Tests/Endgame/run-rift-page-contract.sh
```

The optional-contract runner also supports `FFO_MONO_ROOT`. These are pure
managed DTO/state tests with actual production sources and Go-produced fixtures;
they do not launch Unity or assert on-device rendering.
