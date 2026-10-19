# Android resource-free candidate builds

## Scope and acceptance status

This opt-in path builds an **Android ARM64 IL2CPP development candidate** from the source checkout without privately supplied game assets or `Assets/LocalBranding/AppIcon.png`. It does not create GitHub Releases, publish tags, upload game assets, or deploy a server. Windows/macOS resource-free packaging is not implemented or verified by this workflow; their existing build entries keep the locally supplied resource layout.

**Current licensing status: Unity Personal session setup is pending.** The workflow is not yet runnable with Personal, and does not ask a Personal user to configure Pro credentials. Implementation and engine-free tests are present. The first Unity build, GitHub Actions run, APK installation, resource-host download, and device acceptance remain **not executed** until an eligible Unity license is configured and a maintainer runs the workflow. A successful managed compilation is not a Unity import, IL2CPP build, or Android runtime test.

## Version and candidate identity

- `ProjectSettings/ProjectSettings.asset` → `bundleVersion` is the single application-version source, currently `1.0.0`. Desktop builders no longer overwrite it, and the Android bootstrap reads `PlayerSettings.bundleVersion`.
- The application version change does **not** migrate the login protocol. The current protocol marker remains whatever `NetClient.ClientVersion` declares; client/server migration and paired compatibility acceptance are separate work. The artifact metadata reports this marker explicitly.
- Resource-free mode uses the distinct application ID `org.fakefantasyonline.resourcefreecandidate`, development build options, ARM64, IL2CPP, an APK rather than AAB, and Unity's default development signing. No production signing credential is read. The existing Android integer versionCode is retained; this is not an assigned release-upgrade number.
- A build is a candidate, not a production release. APK structure checks do not verify the signing certificate, installation/update behavior, resource completeness, or server compatibility.

## Local command

Use a legitimately licensed, exact **Unity 6000.4.9f1** Editor with Android Build Support, its SDK/NDK, and OpenJDK installed:

```sh
UNITY_EDITOR_BIN=/path/to/Unity make build-android-resource-free
```

Or run `scripts/ci/build-android-resource-free.sh` with the same environment. The script invokes `FakeFantasyOnline.Editor.CiClientBuild.BuildAndroidResourceFree`. It expects Python 3 for the post-build APK check. `FAKE_FANTASY_ONLINE_ANDROID_BUILD_PATH` can override the default `Build/Android/resource-free-candidate.apk`.

The original `make build-android` still defaults to offline mode and requires the local full resource set. Existing `offline` and `bundled` modes retain their resource manifest and local icon checks. `FAKE_FANTASY_ONLINE_ANDROID_RESOURCE_MODE=resource-free` is an additional explicit selection for the original mobile entry.

Resource-free mode moves the known `Assets/StreamingAssets` and `Assets/Resources` directories into `LocalMobileBuild/ResourceFree`, without walking or hashing their game-resource contents. It creates a fresh StreamingAssets containing only `resource-bootstrap.json`. It generates only `Resources/gbk_table.bytes` from the Editor's standard CP936 codec: 126 lead bytes × 190 trail bytes × little-endian UTF-16 = 47,880 bytes. This is a core text-decoder table, not game art or a downloaded executable. Generation fails on missing or incorrect codec support; Chinese and extended-character samples are verified. The source-owned client scene supplies the camera and the independent startup UI.

The original, source-owned startup artwork is also bundled in every Player at `Assets/FakeFantasyOnline/Runtime/Presentation/Resources/FakeFantasyOnline/Bootstrap/TaoistCloudSanctuary.png`. Its nested Resources location is independent of the private root Resources and StreamingAssets directories. All platform build entries validate the known Presentation/Resources tree; resource-free staging also validates it before moving anything. The exact allowlist contains this image, the three existing source shaders, the seven-glyph registration subset with its license/notice, and their Unity import metadata. Missing, empty, oversized, linked or extra files/directories in that source-owned tree fail the build. Private root Assets/Resources and StreamingAssets are not walked or inspected by this check. The subset is editable text, not a complete font or game-art collection; its license and notice remain bundled with it. `EmbeddedClientBackground` loads the image once and draws an opaque, aspect-cover background for resource download/verification and the pre-character title pages, with an independent solid-color runtime fallback. The image is not part of a downloaded resource manifest and never depends on installed game resources. The original 1448 × 1086 PNG is retained without source recompression; Unity import disables mipmaps and CPU readability, keeps its dimensions, and permits normal platform texture compression.

Original directories and affected Android settings are restored in `finally`, also after a failed build. If Unity is killed, the original resource bytes remain in the durable backup. A new run refuses an outstanding backup. Inspect and restore those original directories to their exact source paths before retrying; do not delete them. Tiny `.generated-*` directories retain the temporary bootstrap/encoding stage for inspection. Builds should run from a clean checkout, and any new `Resources` directories or scene asset dependencies require a separate review before claiming the binary remains resource-free.

## Bootstrap and resource-host configuration

The embedded JSON contains `schemaVersion: 1`, `mode: "resource-free"`, the application version, `resourceManifestUrl`, `dispatchUrl`, and `developmentHttp`. The manual workflow leaves both URLs empty and HTTP disabled, so users can configure the resource host and game entry in the independent startup UI.

For a local build only, set `FFO_RESOURCE_MANIFEST_URL` and `FFO_RESOURCE_DISPATCH_URL` to credential-free HTTPS URLs. Query strings, user-info, fragments, loopback, and invalid endpoints are rejected by the runtime configuration validator. Do not put passwords, signed URLs, or other secrets in an APK. For explicit private-LAN testing, `FAKE_FANTASY_ONLINE_ANDROID_DEVELOPMENT_HTTP=1` additionally enables the restricted development HTTP path. This is not enabled in CI.

Create a remote resource manifest separately, on a machine holding the passive game resources:

```sh
python3 scripts/generate-resource-manifest.py /path/to/materialized-resources \
  --client-version 1.0.0 --output /path/to/materialized-resources/mobile-resources.tsv
```

The file begins with `FFO-MOBILE-1` and `@client<TAB>android<TAB><exact application version>`. Each following row records source path, destination path, byte length, and SHA-256. Serve the manifest from the root of that resource tree so its relative source paths resolve correctly. The command writes a new local file only; it never uploads or overwrites one. Asset rights, a legitimate HTTPS host, and the correct complete resource set are prerequisites outside this build workflow.

The generator maps `ReferenceClient/packages/*.pkg` to `data/` and `ReferenceClient/music/*.ogg` to `Music/`. It allows passive images, tables, sound, and narrowly scoped font `.idx`/`.wid` files, following the runtime allowlist. It excludes `AddOns`, endpoint configuration, Unity `.meta` files, and existing mobile manifests. Unknown/code files, symlinks, unsafe paths, case and prefix collisions, and oversized sets are rejected. It reads game resources only when explicitly invoked; the resource-free APK build never invokes it. Hashes identify content and detect corruption; they are not publisher signatures. The current installer reuses completed verified files but restarts an interrupted current file rather than claiming byte-range resume.

## GitHub Actions security boundary

`client-static.yml` runs on pull requests and manual dispatch. It has no secrets and does not launch Unity. It runs Python contract tests and an engine-free staging/GBK test using Ubuntu's Mono compiler plus its CJK codec package.

`android-resource-free.yml` is **workflow_dispatch only**, and its Unity job accepts only the repository default branch. Only deliberately reviewed default-branch code is given the license. Both workflows default to `contents: read`; checkout does not persist credentials. There are no release, tag, deployment, package-write, or production-environment operations.

The default `personal-pending` mode fails safely before pulling the large Unity image. A supported and authorized Personal session flow must be implemented and validated before it can be used. No user-session secret is requested, captured, or uploaded by this implementation.

For a separately selected, legitimately eligible **Pro-only alternative**, the existing guard requires:

1. Repository variable `UNITY_CI_LICENSE_MODE` set explicitly to `pro-ulf`.
2. Repository secret `UNITY_LICENSE` containing a legitimately provisioned Pro ULF license suitable for this CI runner and license plan.

These checks establish configuration presence only, not license validity. Unity must accept the license before building. Do not paste licenses into issues, chat, source code, or artifact files; do not copy a developer-machine license into CI on the assumption it is transferable. No real license has been supplied or validated for this implementation. The workflow does not generate a license, request a serial, or silently use a Personal flow. A missing/unsupported configuration fails early with a Personal-setup-pending error, without downloading Unity. It does not suggest that a Personal user purchase or configure Pro.

The credential is provided only to the disposable container. Activation input and logs stay in a temporary container directory and are never uploaded. After successful structural verification, the workflow uploads only the candidate APK, `.build.json`, and `.sha256` for seven days. It does not upload raw Editor/activation logs, game-resource staging, the checkout, or a Library cache.

Unity 6.4's traditional Editor command-line activation documentation applies to Pro and explicitly excludes Personal. The newer Unity CLI has a browser sign-in/session mechanism; using Personal requires separately confirming and configuring the supported user-session licensing flow. It must not be approximated by inventing an old Personal ULF workflow. The current workflow intentionally remains blocked for Personal.

## Verified toolchain evidence

Checked on 2026-10-19:

- Project: `6000.4.9f1`, revision `f7258d6eebbe`, matching the [official Unity release page](https://unity.com/releases/editor/whats-new/6000.4.9f1).
- The [Docker Hub tag API](https://hub.docker.com/v2/repositories/unityci/editor/tags/ubuntu-6000.4.9f1-android-3) returned `ubuntu-6000.4.9f1-android-3`, active `linux/amd64`, total compressed image size 8,411,980,126 bytes, last updated `2026-06-11T13:04:02.10025Z`.
- The workflow pins its returned top-level manifest digest: `sha256:0ef7c5f0a026609ca8c0faaa75d677736a3edb02aaaf333a0862b6b2bd7d0d7c`. The reported amd64 image digest was `sha256:2dd3467cc309369af4dff722313cd38a8ec1f087dc96be9c05c9f966ddf83391`. Existence is verified; a container pull/build has not been performed here.
- [GameCI's Docker documentation](https://game.ci/docs/docker/docker-images/) identifies `unityci/editor` as the supported image repository. Its [Editor Dockerfile](https://github.com/game-ci/docker/blob/main/images/ubuntu/editor/Dockerfile) installs the named Unity version and platform modules with child modules, stores an exact version file, and supplies the `unity-editor` wrapper. The runtime script checks the version file and Android module before building.
- [Unity 6.4 licensing instructions](https://docs.unity3d.com/6000.4/Documentation/Manual/ManagingYourUnityLicense.html) and [Unity CLI sign-in documentation](https://docs.unity.com/en-us/unity-cli/use-unity-cli) establish the licensing distinction above.

A future image update must reverify the exact Unity version, Android modules, and immutable digest; do not replace the pin with `latest`. The CI image is approximately 8.4 GB compressed, so disk space and build time remain first-run operational checks.

## Verification commands and limits

```sh
python3 -m unittest discover -s Tests/ClientBuild -p 'test_*.py' -v
Tests/ClientBuild/run-stage-contract.sh
bash -n scripts/ci/*.sh Tests/ClientBuild/*.sh
```

The C# contract can use ordinary `mcs`/`mono` with the CJK encoding package, or an explicitly configured compatible managed test host through `FFO_CSC`, `FFO_MONO_ROOT`, and `FFO_FRAMEWORK_DIR`. It tests directory isolation/restoration, absent resource directories, failed-build cleanup, interrupted backup refusal, table layout, and the unchanged runtime `Gbk` decoder. Python tests cover workflow boundaries, missing-license failure, passive manifests, source-version consistency, and synthetic APK validation cases. The APK verifier checks zip assets, ARM64 native library paths, bootstrap identity/version, and a SHA-256 sidecar; it does not parse every Unity serialized asset or prove a native library is runnable.

Still required: actual Unity import/IL2CPP build; inspect the real APK; launch on an ARM64 device; verify asset-independent startup, endpoint errors, download progress/cancel/retry/storage exhaustion/corruption, resource activation and recovery; then test game startup with the complete authorized resource set and the paired server. None of these acceptance steps is implied by source tests or a candidate artifact.
