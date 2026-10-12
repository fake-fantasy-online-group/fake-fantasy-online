# Cloud managed-test toolchain recovery

These scripts restore the **engine-free source compilation and contract-test** tools after a disposable cloud workspace is replaced. They do not install or run Unity, create a Unity Player, start a game server, or modify application sources.

## Restore and use

Prerequisites: Debian-compatible Linux x86_64 with Bash, Python 3, curl, dpkg-deb, unzip, and the normal C runtime/zlib. No sudo or system package installation is needed.

From the client repository:

```sh
bash Documentation/tools/bootstrap-client-test-toolchains.sh /workspace/shared/client-toolchains
source /workspace/shared/client-toolchains/env.sh
bash Tests/Endgame/run-managed-compile.sh
bash Tests/Endgame/run-contracts.sh
```

To run all existing managed shell contracts:

```sh
# Build the engine-free assemblies before contracts that reference them.
bash Tests/Endgame/run-managed-compile.sh || exit
for contract in Tests/Endgame/*.sh; do
  [[ "$contract" == */run-managed-compile.sh ]] && continue
  bash "$contract" || exit
done
```

The generated environment exports `FFO_MONO_ROOT`, `FFO_CSC`, `FFO_FRAMEWORK_DIR`, `FFO_COMPILE_DIR`, `MONO_CFG_DIR`, and `MONO_GAC_PREFIX`. Compilation output defaults to the toolchain directory, outside `Assets`. `FFO_FRAMEWORK_DIR` points to the packaged Mono `4.7.2-api` reference profile. The cloud runtime is a privately extracted Debian Mono prefix with the layout expected by the test scripts; it is not Unity's bundled Mono distribution. Native desktop/UI or third-party native Mono dependencies are outside this bootstrap's scope.

Use the Mono `4.7.2-api` profile with these scripts. Microsoft's Windows net472 NuGet reference package includes native COM+ support DLLs and a multi-module EnterpriseServices assembly. Because these test scripts pass every root-level DLL as a managed compiler reference, substituting that Windows package directly produces Roslyn metadata errors. No source or test-script modification is needed with the Mono profile.

The server repository has a separate `docs/tools/bootstrap-go-test-toolchain.sh`. Source its generated `env.sh` as well when running Go-produced fixture checks. Do not assume an executable named `/usr/bin/go` is the Go language SDK; verify `go version` after sourcing the environment.

## Pinned inputs and trust

- Debian Mono runtime and mcs: `6.12.0.199+dfsg-6`, from `https://deb.debian.org/debian/`.
- Microsoft Roslyn compiler package: `Microsoft.Net.Compilers.Toolset` `4.8.0`, from the official NuGet v3 flat container. Actual compiler version: `4.8.0-7.23558.1 (e0917286)`.
- .NET Framework 4.7.2 API reference assemblies: Mono's `4.7.2-api` profile, included in the verified Debian `mono-devel` package.

Every downloaded archive is SHA-256 checked before extraction. Debian filenames and hashes are recorded in `mono-debian-packages.lock.json`. On 2026-10-12, that lock was generated from the official trixie amd64 Packages index after verifying its digest against the Debian InRelease file and verifying InRelease signatures against the operating system's Debian archive keyring.

Provenance digests:

- `trixie/InRelease`: `0584fba32e13e0ab8285fb16c27adea1ec03a73669c18702821094fd6ca86675`
- `trixie/main/binary-amd64/Packages.xz`: `7778d3e3f303b7ddb8ce0fe7c8d57473a076c6bf2e8f241f75421d2396352498`
- Roslyn NuGet archive: `37333f4f1e2ce55e621355d6da651dc23d4cb5f94a8f76b9478816e87f110ad9`

The bootstrap reuses only cached archives with matching hashes and stops on a failed download or hash mismatch. It does not change proxy/security settings, bypass network restrictions, execute Debian maintainer scripts, or use credentials. If a pinned package is eventually removed from its official source, refresh the lock using a newly verified official index; do not substitute an unverified mirror.

Downloaded third-party packages retain their own licenses. This bootstrap is a test-tool recovery mechanism, not permission to redistribute third-party binaries.

## Recovery verification (2026-10-12)

- Both bootstraps completed successfully; rerunning the client bootstrap reused and rechecked the pinned cached packages.
- Actual versions: Mono JIT/mcs `6.12.0.199`; Roslyn `4.8.0-7.23558.1 (e0917286)`; Go `go1.27.2 linux/amd64`.
- `run-managed-compile.sh` passed for all seven engine-free assemblies, covering 645 actual source files.
- All 11 `Tests/Endgame/*.sh` scripts passed after running managed compilation first. The tests include 23,654 skill-tooltip checks, 2,860 sword-targeting checks, and 1,042 panel-action checks, using their documented deterministic Unity/art/font substitutes.
- Stub-related unassigned-field warnings remained warnings; no Unity/Player build or real rendering validation was performed.
