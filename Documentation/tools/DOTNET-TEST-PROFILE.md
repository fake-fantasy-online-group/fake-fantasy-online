# Explicit .NET managed-test profile

This optional profile executes the existing managed shell/Python test entry
points with official Microsoft .NET Runtime 6.0.36 and Roslyn 4.8.0. It uses the
verified Unity 6000.4.9f1 `unity-4.8-api` reference profile recovered from the
existing private compiler-input transfer archive. It is not a Mono installation,
a Mono 4.7.2 compatibility pass, or Unity Editor/Player/render validation.

## Provenance and activation

- Runtime: Microsoft's official [Linux x64 download](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-6.0.36-linux-x64-binaries-targz),
  with SHA-512 pinned in `bootstrap-dotnet-test-runtime.sh` and verified against
  official `6.0/releases.json` before execution.
- Compiler: the original `Microsoft.Net.Compilers.Toolset` 4.8.0 NuGet package,
  SHA-256 `37333f4f1e2ce55e621355d6da651dc23d4cb5f94a8f76b9478816e87f110ad9`.
  Entry point: `tasks/netcore/bincore/csc.dll`; actual version
  `4.8.0-7.23558.1 (e0917286)`.
- Reference archive: `batch18-managed-toolchain-transfer.tar.gz`, SHA-256
  `b83e452e5ae3d8671a65981fd58f783328a47713bbf1d9eb4a09d2d48587a91d`.
  All 427 listed payload files were verified by size/SHA-256 before extraction.
  Its native runtime is intentionally absent; this profile supplies the
  separately verified Linux .NET runtime.

From the client checkout, with those files restored beneath
`FFO_TOOLCHAINS_ROOT` (default `/workspace/shared/toolchains`):

```sh
source Documentation/tools/activate-dotnet-test-toolchain.sh
bash Documentation/tools/verify-dotnet-test-toolchain.sh
bash Tests/Endgame/run-managed-compile.sh
bash Tests/Endgame/run-rift-hazard-contract.sh
bash Tests/Endgame/run-ui-contract.sh
```

Activation performs no network access. `bootstrap-dotnet-test-runtime.sh`
downloads only its pinned Microsoft runtime if it is not already cached. It
does not download Debian packages, select mirrors, install system services,
change security/network settings, or install/start Unity. .NET 6 is end-of-life;
this pinned profile is for isolated historical test compatibility, not a
production service or deployment recommendation.

## What the adapter preserves

Legacy scripts invoke `FFO_MONO_ROOT/bin/mono` or `FFO_MONO`; this profile binds
both to an explicitly documented command adapter. No Mono binary is supplied or
claimed. The legacy `mcs.exe` argument selects Roslyn with the verified reference
profile; existing explicit Roslyn references and options are passed unchanged.
No source selection, test body, assertion, fixture, define, or exit code is
rewritten. Tests receive their original command arguments and working directory.
`FFO_CSC` is an explicitly created `csc.exe` symlink to the verified netcore
`csc.dll`; this preserves Python runners that select a managed host by suffix.
`FFO_DOTNET_CSC` always names the actual DLL. The adapter resolves the alias and
executes that DLL using `dotnet exec`.

The adapter writes a .NET runtime configuration beside each generated test
assembly and actually runs it with `dotnet exec`. Previously compiled project
dependencies are copied beside temporary executables for .NET resolution. It
never loads framework reference DLLs as implementations. Every compilation and
execution has its original/effective command and exit code recorded under
`FFO_MANAGED_TRACE_DIR`; outputs use a separate `dotnet-managed-compile` directory.

The smoke test includes real C# 9 compilation/execution, a test program returning
7, and deliberately invalid source. Both negative controls must remain failures.
Passing this profile establishes its own source/contract coverage; it does not
establish Mono-specific behavior or an actual Unity runtime/rendered result.
