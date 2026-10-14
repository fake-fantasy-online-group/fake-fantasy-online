# Mobile platform foundation (Android first)

This is source-level infrastructure, not a shipped mobile player. No Android APK, iOS build, device run, Unity import, IL2CPP execution, rendering/performance acceptance or store submission has been completed by these managed checks.

## Boundary

- `Foundation/ClientResourcePaths` selects the installed mobile root; desktop calls return the original StreamingAssets path unchanged.
- `Foundation/MobileResourceManifest` is pure managed code: bounded TSV schema, safe relative paths, destination uniqueness, length/SHA-256 validation, verified temporary-file commit.
- `Bootstrap/MobileResourceInstaller` uses UnityWebRequest and DownloadHandlerFile for each bundled file. Android StreamingAssets is commonly a `jar:file:…!/assets` URL and **must not be opened through FileStream**. Only the small, bounded manifest is read into memory. Package transfers and SHA-256 operate in streaming mode; hashing runs off the main thread.
- `ClientBootstrap` waits for installation before constructing the view/network/plugin graph. Failure displays a startup error and stops. A new manifest hash gets a separate `persistentDataPath/MobileResources/<sha256>` directory; the root activates only after every manifest entry validates. Restart retries missing/corrupt files and checks already installed files. Existing versions are retained; cleanup is intentionally not automatic in this initial implementation. Full startup hashing and duplicate old-version storage need real-device performance/storage measurement.
- `Presentation/ReferenceAssets/ResourceFileRoot` translates paths without changing recovered archive precedence, cache or content authority. Mobile package discovery uses only the installed `data` directory; desktop discovery ordering remains unchanged.
- Local bundled AddOns are installed with resources. The existing optional plugin transport/runtime has not been certified for mobile/IL2CPP.

## Locally supplied inputs

Resources remain ignored and are not committed, downloaded, or uploaded by this implementation. Provide the existing desktop resource layout under `Assets/StreamingAssets`:

- `ReferenceClient/packages`: zpatch_tower10.pkg, zpatch_petart.pkg, res_q21_000.pkg, res_q21_001.pkg, res_05diff.pkg, res_ff21.pkg
- `ReferenceClient/music`: the existing 51 OGG tracks
- `gameassets`, including `tables/mapmusic.json`
- `sndeffect`, optional `sndeffect24_pcm`, and `AddOns`
- `mobile-dispatch.txt`: one explicit HTTP(S) dispatch URL reachable from the phone
- Existing local branding icon required by `ClientBuild.PrepareClientScene`

The build generates `mobile-resources.tsv`, hashes each file, and restores/removes the generated manifest afterward. Package destinations are translated to `data/` and music to `Music/`, matching the existing desktop output layout. Unexpected top-level StreamingAssets content fails validation for review. The manifest is an integrity/incomplete-copy check of app-bundled content, **not** a signed remote updater. Do not include credentials or secrets in any bundled file.

`mobile-dispatch.txt` is required because a phone's 127.0.0.1/localhost addresses the phone, not the development computer. Loopback, unspecified hosts and embedded credentials are rejected. The endpoint is configuration, not proof of reachability: LAN routing, DNS, server binding and TLS still need device verification. Existing cleartext restrictions, ATS/network security policy, signing and credentials are not changed. HTTPS is preferable; HTTP may remain blocked by the app/platform policy. Desktop LocalDefault still uses its existing loopback endpoint.

## Build entry

With Unity 6000.4.9f1 and its Android Build Support, SDK, NDK and JDK already installed:

```sh
make build-android UNITY_EDITOR_BIN=/path/to/Unity ANDROID_BUILD_PATH=/path/to/client.apk
```

Calls `FakeFantasyOnline.Editor.MobileClientBuild.BuildAndroid`: ARM64, IL2CPP, APK output. Backend, architecture and app-bundle selection are restored in `finally`. It does not install tools, configure release signing, select a production server, change security policies, or build iOS. Existing desktop build entry points are retained. Resource sizes may exceed APK/store limits; asset delivery or external resource hosting is a separate design, not silently added here.

## Reproducible managed checks

```sh
source Documentation/tools/activate-dotnet-test-toolchain.sh
bash Tests/Mobile/run-resource-contract.sh
bash Tests/Endgame/run-managed-compile.sh
python3 Documentation/tools/compile-current-presentation.py
python3 Tests/Mobile/compile-platform-boundaries.py
```

The resource contract covers path traversal, duplicate destination/schema rejection, checksums, incomplete transfer preservation, corruption detection, root activation and desktop/default versus explicit mobile endpoint behavior. Boundary compilation includes all Bootstrap and Editor source using recovered Unity API references. These checks do not execute UnityWebRequest, a Unity Player or Android/IL2CPP.

Required next acceptance: clean install and restart on a real Android ARM64 device; interrupted copy/disk-full recovery; real multi-GB resource timing and memory; encrypted-package and character encoding behavior under IL2CPP; dispatch/login/world entry; audio; optional plugin/AOT behavior; pause/resume and background termination. iOS packaging, signing and physical-device acceptance remain separate work.

## Offline development APK (default Android mode)

The existing six packages total 5,416,772,272 bytes. Local Build Tools 36.0.0 aapt2 rejected the 2,950,872,895-byte res_q21_000.pkg even without compression, while a same-name 17 MB control succeeded. This is not a Unity or IL2CPP pass. Splitting an individual asset would not establish that the complete APK is installable.

Android builds now default to `FAKE_FANTASY_ONLINE_ANDROID_RESOURCE_MODE=offline`. The development APK embeds only the expected full manifest under StreamingAssets. It exports `<output.apk>.resources.tsv` for local transfer. The unchanged original resource files remain on the build machine, and never go to GitHub. This is a USB-based acceptance workflow, not a production download/update service or a Play delivery solution.

The build moves the original StreamingAssets directory (including any resource links) to `LocalMobileBuild/OriginalStreamingAssets`, exposes a tiny manifest-only stage during BuildPlayer, then restores the original directory in `finally`. It does not rewrite resource files or follow links for deletion. An Editor crash leaves the original backup intact; the next build refuses to proceed until that original directory is restored. Generated tiny stages remain under LocalMobileBuild with a `.generated-<id>` suffix. Do not delete the original backup as a cleanup step. Desktop builds must not run concurrently with this Android staging operation.

On first device launch, obtain the exact `FFO_MOBILE_RESOURCE_ROOT=` line from the game's log. Missing resources produce a visible startup error and never enter game loading. Stop the game before transfer. Then, with an explicitly connected and user-authorized device:

```sh
python3 Documentation/tools/push-mobile-offline-resources.py \
  --adb /path/to/adb --device EXACT_SERIAL \
  --manifest Build/Android/fake-fantasy-online.apk.resources.tsv \
  --source Assets/StreamingAssets \
  --remote-root EXACT_PATH_FROM_GAME_LOG
```

The helper checks every host file against the exported manifest before transfer. Each device file is pushed to `.part`, SHA-256 checked on-device, and then renamed. It does not install APKs, enable USB debugging, request broad storage permissions, or configure networking. Restart the game afterward: the APK's embedded manifest independently validates all installed files on a background thread before activating the version directory. Interruption or corrupt files remain inactive; rerun transfer and restart. Resources use application-specific storage; uninstall may remove them. First-boot hash timing, actual adb access to app-specific external storage, device free space and lifecycle behavior require real-device acceptance.

For small fixtures only, `FAKE_FANTASY_ONLINE_ANDROID_RESOURCE_MODE=bundled` retains the existing streaming APK copy path; a conservative 1 GiB aggregate preflight prevents accidental giant bundled builds. Both modes still require an explicitly configured valid mobile-dispatch.txt. A syntactically valid address is not proof of phone reachability. No localhost substitution, firewall change or public endpoint is performed.

Checks added: offline mode/version identity, incomplete/corrupt/repaired directory verification, source directory preservation and interrupted-staging rejection; transfer helper path/schema and streaming-hash checks. These are managed/host tests, not device verification.
