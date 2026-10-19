# Local resource-free client packaging

These commands build local candidates using an already installed, legitimately licensed Unity **6000.4.9f1**. They do not install/licence Unity, create signing keys, alter credentials, create tags, upload GitHub artifacts, publish releases, or deploy a server. Windows build targets do not authorize changing any production Windows server.

## Inputs and identity

- `--platform`: `android`, `windows`, or `macos`.
- `--version`: stable `MAJOR.MINOR.PATCH`, exactly matching `ProjectSettings/ProjectSettings.asset` `bundleVersion`. The entry does not rewrite the version, package identifier or Android `versionCode`.
- `--dispatch-url`: the explicit public HTTPS dispatch endpoint to bundle in `resource-bootstrap.json`. No production hostname is assumed. It cannot contain credentials, query parameters, fragments, or a loopback host. Existing runtime user overrides remain separate from this default.
- `--configuration formal` (default): non-development Player. `--configuration development` is explicit; only that mode may add `--development-http` for a private LAN IPv4 endpoint.
- `--output`: an optional fresh output. Desktop output must be a dedicated new application directory. Keep personal configuration and player data outside it.
- `--release --source-tag v1.0.0`: additionally require an existing tag matching the requested version, exact `HEAD`, and clean tracked/source checkout. Never creates or moves a tag. Ignored local resources are not inspected by this source check. Persisted changes to tracked signing/project settings block the check; do not bypass it with a blanket dirty-tree exception or commit credentials.

Without `--release`, this is a candidate build. Its `.local-build.json` records the actual commit and whether the source was dirty. No successful build receipt claims runtime acceptance or verified signatures.

Replace the example endpoint below with the intended public endpoint. These are command templates, not a published endpoint or authorization to run a release.

```sh
UNITY_EDITOR_BIN=/path/to/Unity ./scripts/ci/build-resource-free.sh \
  --platform android --version 1.0.0 \
  --dispatch-url https://distribution.example.com/dispatch.json
```

Equivalent Make entry:

```sh
make build-resource-free PLATFORM=android VERSION=1.0.0 \
  DISPATCH_URL=https://distribution.example.com/dispatch.json \
  UNITY_EDITOR_BIN=/path/to/Unity
```

Android uses ARM64 IL2CPP and preserves the existing application ID and integer versionCode. Formal mode requires an existing custom keystore file and configured alias in Unity; it never falls back to debug signing or reads/logs key bytes/passwords. Unity must successfully use the user's existing signing setup. The path/alias checks cannot prove certificate continuity. Independently verify the actual APK manifest, certificate, and versionCode against the installed baseline and store history before distributing an upgrade. APK structural checks do not parse the binary manifest or prove non-debuggable/signature state. The legacy `build-android-resource-free.sh` entry remains a development-candidate path.

## Desktop helper and trust pins

Use the source-owned `scripts/build-desktop-updater.sh` with the existing local .NET SDK to publish the self-contained helper. No installed .NET runtime is required on the end user's machine. Supply only these exact flat files in `--updater-directory`:

- Windows: `DesktopUpdater.exe`
- macOS: `DesktopUpdater-osx-x64`, `DesktopUpdater-osx-arm64`

No helper directory recursion is performed; links, extra files and local config are rejected. Set the stable desktop application identifier in the existing Unity project. Formal Windows requires the existing public Authenticode certificate SHA-256; formal macOS requires its existing public Developer ID Team ID. These identifiers are trust pins, not credentials, and do not themselves sign the output.

```sh
UNITY_EDITOR_BIN=/path/to/Unity ./scripts/ci/build-resource-free.sh \
  --platform windows --version 1.0.0 \
  --dispatch-url https://distribution.example.com/dispatch.json \
  --updater-directory /local/published/windows \
  --windows-signer-sha256 EXISTING_CERTIFICATE_SHA256

# Run on macOS. Uses the universal Intel + Apple silicon Player target.
UNITY_EDITOR_BIN=/path/to/Unity ./scripts/ci/build-resource-free.sh \
  --platform macos --version 1.0.0 \
  --dispatch-url https://distribution.example.com/dispatch.json \
  --updater-directory /local/published/macos \
  --mac-team-id EXISTING10CHARTEAM
```

Outputs default to `Build/ResourceFree/android/fake-fantasy-online.apk`, `Build/ResourceFree/windows/fake-fantasy-online/fake-fantasy-online.exe`, and `Build/ResourceFree/macos/fake-fantasy-online.app`.

Desktop builds write `ffo-desktop-install.json` at the Windows application root or macOS `Contents/Resources/`. Its application ID, executable relative path and trust pins must remain stable across upgrades; the version must increase. Package the whole Windows directory or whole `.app`, never only the executable.

Code-page assemblies and the install marker are finalized before downstream signing. Unlike the legacy macOS bundled-resource lane, the new entry does not overwrite signatures with ad-hoc signing. Sign all required executable components, including the self-contained helper, using the existing authorized platform process, then sign/notarize the completed app. Actual OS trust verification and device acceptance are separate release gates. Do not distribute a merely built/hashed candidate as a signed release.

## Update ZIP

After the platform signing process, create the strict updater archive:

```sh
python3 scripts/ci/package-desktop-update.py \
  Build/ResourceFree/windows/fake-fantasy-online \
  --platform windows --output Build/ResourceFree/windows-client-1.0.0.zip
```

For macOS, pass the `.app` and `--platform macos`. Archive roots equal the application folder/bundle name. ZIP permission bits retain executable modes. The packager rejects symbolic links, special files, traversal, case collisions, reserved device names, personal data/credential filenames, unexpected StreamingAssets, and mismatched install/bootstrap metadata. macOS framework links must be flattened in the build output **before final signing**, using a reviewed local preparation step; this script does not follow/flatten them or repair signatures.

The current helper accepts standard ZIP only, not ZIP64: at most 65,534 entries, 64 MiB central directory, 16 GiB total expanded content. The Python producer uses a conservative 2 GiB minus one byte limit for each file and compressed archive. Oversized content fails closed rather than emitting an unusable update. Archives and sidecars are candidates; their receipt explicitly says signing and production readiness are unverified. The installed updater independently verifies OS trust and pinned identity before replacement.

## Resource and recovery boundary

All platforms use the same bootstrap format. Builds temporarily move `Assets/StreamingAssets`, root `Assets/Resources`, and local branding out of Unity's import tree without enumerating the private resource contents. Only the small bootstrap marker, reviewed source-owned startup assets (including the AI background), generated CP936 decoder table, and exact desktop helper payload remain. Game art, local endpoint files, AddOns, and personal configs from the original StreamingAssets are not copied. The AI background stays inside the installer and is available before resource download.

The original directories and their `.meta` files are restored in `finally`, even if the build fails. Recoverable original bytes are retained outside `Assets` under ignored `LocalMobileBuild/` if restoration is obstructed or Unity is killed. A subsequent build refuses an outstanding backup; inspect and restore it instead of deleting it. Generated temporary directories are retained there separately for recovery and are never imported or uploaded.

## Verification performed by source tests

```sh
python3 -m unittest discover -s Tests/ClientBuild -p 'test_*.py' -v
Tests/ClientBuild/run-profile-contract.sh
Tests/ClientBuild/run-stage-contract.sh
bash -n scripts/ci/*.sh Tests/ClientBuild/*.sh
```

The C# contracts use Mono by default or the explicitly configured `FFO_DOTNET`, `FFO_DOTNET_CSC`, and `FFO_DOTNET_FRAMEWORK_DIR` toolchain. Tests exercise real filesystem staging/rollback and real ZIP read/write with synthetic, non-executable fixtures. They do not run Unity, generate a Player/APK, sign an artifact, verify notarization/Authenticode, or demonstrate device startup and updating. Full Editor source compilation against Unity API references is also separate from an actual Unity build.
