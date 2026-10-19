# Desktop whole-application updates

Status: implementation and managed/filesystem contract tests, not a signed or released Player. macOS and Windows require their actual platform acceptance runs before distribution. This updates the entire application; no C# hot-update assembly or remote script is loaded.

## Operator build boundary

The desktop resource-free Player includes the updater from the same approved source revision. It does not download an updater from dispatch. Build the helper locally with an already-installed official stable .NET SDK 8 or newer:

```sh
scripts/build-desktop-updater.sh windows /absolute/new/windows-updater
scripts/build-desktop-updater.sh macos /absolute/new/macos-updater
```

The script pins an available stable SDK (8 or newer), targeting net8.0, publishes self-contained single files, and never installs SDKs, signs, uploads or deploys. Windows output is `DesktopUpdater.exe` (x64); macOS output contains `DesktopUpdater-osx-arm64` and `DesktopUpdater-osx-x64`. The selected SDK can restore the net8.0 targeting/runtime packs from the official package source if absent; this is a build dependency, not an SDK installation. No separately installed .NET runtime is required on the player's computer. The .NET host may extract its bundled runtime into its standard per-user cache.

Supply this exact flat directory to the resource-free build lane through `FFO_DESKTOP_UPDATER_DIRECTORY`. The lane writes the identity marker and copies helpers under StreamingAssets/DesktopUpdater. See the resource-free build documentation for the approved stable application ID, dispatch endpoint and platform signing identity inputs. The marker is `ffo-desktop-install.json` at the Windows application root or `Contents/Resources/ffo-desktop-install.json` inside a macOS bundle. It binds schema 1, platform, application version, stable install ID, relative executable, macOS Team ID and Windows signing certificate SHA-256.

Formal local candidates are still unsigned until the release operator performs the approved platform signing flow. Sign the actual helper binaries and application. macOS requires Developer ID signing and successful Gatekeeper assessment; notarize/staple as required by the OS/release process. Windows must pass Authenticode chain validation and the configured certificate digest. Certificate rotation or a changed Team ID requires a separately planned/manual transition: an update cannot silently weaken or replace the installed signing policy. No private key enters the marker, dispatch or helper build command.

Only package immutable, signed output using `scripts/ci/package-desktop-update.py`. Its root is exactly the dedicated Windows application folder name or macOS `.app` name. The archive is standard ZIP, not ZIP64, a self-extracting executable, MSI, DMG, PKG or tar. The build packager intentionally imposes Python's conservative standard ZIP limit below 2 GiB per file/archive, at most 65,534 entries and 64 MiB central directory, with at most 16 GiB expanded. This is sufficient for resource-free Players; game resource payloads are distributed separately. Links/special files are refused; any approved flattening must precede signing. macOS executable Unix permission bits must be present in ZIP metadata. Renaming an installed application folder/bundle changes the required archive root and is not an automatic-update migration.

The normal package downloader also accepts legacy installer extensions for manual installation. The new automatic helper accepts only a ZIP satisfying this contract.

## Player flow

1. The existing bounded, no-redirect download transport saves the complete package in the private ClientPackages cache. It verifies the dispatch version, length and SHA-256, and revalidates before handing off.
2. The player copies its bundled updater to `persistentDataPath/DesktopUpdates/<job-id>`, outside the application being replaced. It records the canonical install/data/package paths and parent PID/start identity. Only the running executable matching the installed marker can request this handoff.
3. The independent helper runs without elevation. It locks the installation, rechecks the complete package bytes, validates ZIP paths/entry limits/types, and extracts into a fresh sibling directory on the same filesystem as the application. User data, downloaded resources and package caches must be disjoint from the application tree. The helper never copies over a live application file.
4. Package marker, executable product/bundle version, install ID and signing policy must match the requested update. macOS checks `codesign --verify --deep --strict`, signed identifier/Team ID and `spctl --assess --type execute`. Windows uses WinVerifyTrust and, when set, the signing certificate digest. The helper preserves Internet provenance (Windows Zone.Identifier, macOS quarantine) and does not turn off platform security.
5. Only after successful preparation does the UI offer “退出并安装更新”. The user must click it. The helper waits for the original process to exit and rejects another running copy before replacing anything. Preparation cancellation, parent exit without confirmation, a 30-minute unanswered preparation or a 5-minute failed exit preserves the installed app.
6. Before committing, every staged file is hashed again and additions/deletions are rejected. The helper revalidates trust. It renames the old directory to a unique sibling backup, then renames the staged application into place. These individual same-filesystem renames are atomic; the pair is a journaled transaction, not a single atomic operation. If rename, trust or initial OS launch fails, it restores the old directory when possible and retains the failed new application.
7. LaunchServices (`open`) or Windows ShellExecute opens the new application with normal OS policy. A successful start request is not a full game-health or login check. No fallible transaction work runs after an accepted launch. The retained backup is never automatically deleted.

If the helper is absent, the installed layout is unmarked/renamed, the install location is unwritable/translocated, signature or trust validation fails, a platform policy blocks the launch, or the package is not a supported ZIP, the game offers an explicit manual folder-opening fallback. It never executes the downloaded file automatically, requests administrator access or tells the player to bypass a security warning. Older unsigned/ad-hoc installations may therefore need one approved manual installation before managed updates are usable.

Resource activation retains its existing all-files-verified boundary. An unmatched platform/application-version resource map cannot open the game, even if a stale “ready” root exists. Required application updates remain available without a compatible resource map. When resources finish successfully and no optional/required application-update choice remains, the bootstrap opens the title/login page automatically through the same verified-entry guard. It does not submit login credentials or change protocol/authentication versions.

## Failure and recovery

Every prepared job retains `request.json`, `transaction.json`, status text and its bundled helper in the player's private DesktopUpdates directory. The transaction contains only local paths and public application identity. Download caches, interrupted staging, old backups and failed new directories are retained rather than recursively deleted. This trades disk usage for recovery; cleanup must be an explicit later maintenance action.

A crash or power loss between the two renames can leave the application path missing. The old app remains at the recorded sibling `.update-backup-<job-id>` path. Quit all copies, then run the retained helper with the original job request:

```text
DesktopUpdater.exe --recover "C:\...\DesktopUpdates\<job-id>\request.json"
DesktopUpdater-osx-arm64 --recover "/.../DesktopUpdates/<job-id>/request.json"
```

Choose the retained helper matching the OS/process architecture. Recovery validates the journal/path/backup identity, rejects a live application, takes the same installation lock and restores the backup. If a new application exists, it is moved to a `.update-failed-<job-id>` sibling, never deleted. The helper does not elevate to recover protected directories. A filesystem failure during rollback can require manual recovery from the retained backup; logs/status name that condition. The updater does not promise power-loss durability of every directory rename on every filesystem, nor recover user data from an independent disk failure.

## Verified and unverified boundaries

Run:

```sh
source Documentation/tools/activate-dotnet-test-toolchain.sh
Tests/DesktopUpdater/run-desktop-updater-contract.sh
python3 Tests/DesktopUpdater/test_helper_build.py
Tests/ClientUpdate/run-package-ui-contract.sh
```

The desktop tests compile all actual helper source and exercise real ZIP parsing, full-file hashes, path rejection, identity/pin mismatch, staged mutation, cancellation, same-volume swaps, injected failures, retained user files and journal rollback. Both Windows/macOS archive fixtures are produced by the actual Python build packager and then consumed by the C# updater. OS trust and application launch are replaced in these portable tests; no synthetic executable is run. The four UI test builds cover Android, Windows, macOS and unsupported desktop fallback including an unmatched resource-map gate.

These tests ran with the available official .NET 6 runtime, Roslyn 4.8 and recovered Unity API reference profile, not with a Unity player. They do not prove .NET 8 self-contained publication, IL2CPP stripping, Windows/macOS process inspection, native signature APIs, notarization, antivirus/SmartScreen behavior or actual Player relaunch. Release acceptance must build the exact source tag locally and test initial install/update on each platform, protected/read-only/translocated targets, signature rejection, two running copies, cancellation, disk full, parent crash, deliberate termination between renames and rollback. Keep settings and existing verified resource hashes before/after as evidence. No production deployment is implied by source tests or packaging.

SDK selection follows [Microsoft’s version-selection policy](https://learn.microsoft.com/en-us/dotnet/core/versions/selection): a newer SDK can target earlier runtimes, and a self-contained publication includes the selected runtime. SDK-10 selection is covered by a mocked command test; the actual SDK-10 native publication still requires a local build.
