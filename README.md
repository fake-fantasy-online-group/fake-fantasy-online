# Retired depth-refinement logic — code-only archive

This branch archives only our parameter-driven algorithm code, extracted from
retired local rendering work. It is not a complete backup of the 110 configured
repairs, not a working game client, not Tencent's rendering implementation, and
not the paused experimental replacement renderer.

Included: support-band partitioning, source-ceiling preservation, complementary
polygon/foreground partitioning, per-instance ordering, and compatible mesh
chunk grouping. The extraction removes Unity integration and all resource
selectors; callers must supply every geometric input. It is explanatory source,
not a byte-for-byte copy or a claim of whole-client equivalence.

Intentionally excluded: all original resource identifiers, paths, dimensions,
clip/triple tables, per-asset outlines/anchors/signatures, resource-derived test
fixtures, game images/packages/binaries, logs, player data and credentials.
The tiny test inputs were invented solely for this archive. The complete old
version remains only in the existing private checkpoint, not this public branch.

Files: `DepthPartition.cs`, `DepthPartitionContract.cs`, `run-contract.sh`, and
this README. The archive tree contains only these four files. Its parent is the already-public
initial source commit; that pre-existing public history remains reachable. No
private checkpoint history or additional resource configuration is uploaded.

Run with a C# 9 compiler and compatible runtime:

```sh
CSC=/path/to/csc RUNTIME=/path/to/mono ./run-contract.sh
```

The checks validate only small synthetic algorithm cases. They are not gameplay,
Unity, device or visual acceptance. No branch merge, release or deployment is
part of this archive.
