# Android package installer contract and verification

This is the source-owned, standalone APK update handoff. It does not load assemblies,
replace game resources, use HybridCLR, grant installation permissions, silently install,
or publish/build a release. The root installer remains responsible for selecting and
boundedly downloading the release artifact from its trusted release plan.

## C# integration

Call the static `FakeFantasyOnline.Bootstrap.AndroidClientPackageInstaller` methods from
Unity's main thread. Other platforms and Editor return `Unsupported` without touching JNI.
All JNI failures are logged in full and presented as a short `Failed` snapshot.

1. `Prepare(localApkPath, expectedVersion, expectedSha256, expectedBytes)` copies an already
   downloaded APK from this app's own files/cache/external-files tree into private cache.
   The worker checks its exact length/digest, then Android-parsed package/version/signers.
2. `Poll()` reports a coherent immutable snapshot (`State`, `Message`, `Progress`). Progress
   is a copy/staging fraction or `-1`, never an installation-completion percentage.
3. `PermissionRequired` offers `OpenPermissionSettings()` on an explicit user click. Android
   26+ opens this package's unknown-source page; API 25 opens security settings. Returning
   only rechecks permission. It does not automatically begin an installation.
4. `Ready` offers `Begin()` on another explicit click. A worker rechecks package identity,
   copies into a `PackageInstaller` full-install session, rehashes bytes read back from
   that session, and rechecks installed identity and permission immediately before commit. Session ID and
   target identity are durably saved before the first large write, with callback identity
   added before commit. On startup only unrecorded sessions owned by this installer and
   targeting this app are reclaimed, including the narrow create-to-record crash window.
5. `AwaitingConfirmation` offers `OpenSystemConfirmation()` on an explicit click. The
   non-exported receiver stores the system-provided intent; it never launches UI from a
   background broadcast. The user decides in Android's own confirmation interface.
6. A submitted session is `AwaitingResult`, not success. `Succeeded` requires both a durable
   `STATUS_SUCCESS` observation and a fresh check of installed version/code/current signers.
   A process restart without that callback evidence is `Interrupted`, never inferred success.
   An explicit `Prepare` retry of `Interrupted` first abandons the old session and clears
   its callback/recovery record. Pending callback tokens and session IDs cannot revive an
   abandoned/replaced operation.
7. `Cancel()` invalidates in-flight preparation/staging and abandons unconfirmed sessions.
   Once `AwaitingResult` it preserves the OS result; the user must cancel in the system UI.
   It also preserves terminal success/failure. It never deletes the original download.
   Only its own disposable `verified-*.apk` cache snapshots are cleaned up.

The app's Unity `OnDestroy` may call `Cancel()` safely: it does not turn a submitted or
completed installation into a false cancellation. Missing/cancelled/blocked/storage/invalid/
incompatible system outcomes stay distinct. Native exceptions go to `FFO.PackageInstaller`.

## Trust and platform boundaries

- Package name is read from the running application's context, not hard-coded or taken
  from a release manifest. APK `versionName` must equal the expected release version.
  Its actual integer/long `versionCode` must be strictly greater than the installed one.
- API 28+ requests `GET_SIGNING_CERTIFICATES` and compares the entire current
  `getApkContentsSigners()` set. API 25–27 uses platform `GET_SIGNATURES`. Empty/null/duplicate
  signer data fails closed. Ordering is irrelevant; all DER certificate bytes must match.
  Certificate-rotation history membership and manifest-declared certificate hashes are
  deliberately insufficient for this strict same-current-key update policy.
- This relies on Android's verified archive parser, not merely reading a certificate from
  the ZIP. In AOSP Android 15, `PackageManager.getPackageArchiveInfo` maps signature flags to
  `PARSE_COLLECT_CERTIFICATES`; `ParsingPackageUtils` obtains signing details with
  `skipVerify=false`, ultimately calling `ApkSignatureVerifier.verify`. The OS installer
  separately validates the actual session APK before installing it.
- API 31+ explicitly sets `USER_ACTION_REQUIRED`. Earlier supported versions use the
  ordinary unprivileged `REQUEST_INSTALL_PACKAGES` flow. Device/profile owners and holders
  of privileged `INSTALL_PACKAGES` are refused because those configurations can bypass
  ordinary system confirmation. No update-without-user-action permission is declared.
- The explicit non-exported receiver is reached by a session-scoped, mutable PendingIntent
  (required for target SDK 35+). It is not one-shot because pending and terminal results
  reuse the same capability. Action, random data token and actual session ID are checked.
- No FileProvider, exported activity/receiver, file URI, broad storage permission, command
  shell installation or third-party binary dependency is introduced.
- The project explicitly enables the built-in `com.unity.modules.androidjni` package in
  `Packages/manifest.json` and locks it as a built-in dependency. This makes
  `UnityEngine.AndroidJNIModule` available to the Bootstrap assembly during actual Unity
  compilation; an asmdef with engine references enabled does not enable an omitted module.
- The `.androidlib` imports only on Android and supplies Java/manifest/consumer keep rules.
  Its Gradle module inherits the Unity compile/target/min SDK settings. Referenced APIs
  require compileSdk 33+; runtime guards cover project minSdk 25. This change does not pin
  or lower the project target SDK. The repository currently uses automatic target selection.
- A Play-distributed build would need a separate distribution/policy review for the
  restricted `REQUEST_INSTALL_PACKAGES` permission. These tests do not approve any store
  policy, release signature, production package name, device build or publication.

## Reproducible source-only checks

Run from the repository root:

```sh
Tests/Mobile/run-android-package-contract.sh
ANDROID_JAR=/path/to/official/platforms/android-35/android.jar \
  Tests/Mobile/run-android-package-contract.sh
source Documentation/tools/activate-dotnet-test-toolchain.sh
Tests/Mobile/AndroidPackageInstaller/compile-unity-bridge.sh
```

The Java runner uses a JDK's compiler module (`java -m jdk.compiler/...`) and writes all
classes/test doubles into a disposable temporary directory. It never downloads dependencies.
`ANDROID_JAR` is optional; omission explicitly skips API-reference compilation rather than
claiming a pass. The C# runner first validates the actual project manifest, lock and Bootstrap asmdef, then
uses the existing verified toolchain and Unity API references. Android receives the JNI
reference only after that check; Windows and Editor compile without any JNI module reference.

Verified on 2026-10-19:

- 18 pure Java policy/content assertions: package mismatch, release-version mismatch,
  same/downgrade versionCode, unknown/empty/duplicate/different/partial signers, signer order,
  malformed expected metadata, truncation, extra bytes, and wrong digest.
- 92 synthetic lifecycle assertions execute the production Java against clearly labelled
  Android doubles: unknown-source denial/return, API25 vs API35 branches, no automatic UI,
  duplicate Begin, authenticated callback identity, cancellation, pre-commit cache/session
  corruption, storage and recovery-record failure, privileged-context rejection, real
  `Interrupted -> Prepare -> Ready` recovery, no success from callback or restart alone,
  durable success observation and stale-verifier/terminal-outcome ordering. Tests reconstruct
  a persisted pre-write checkpoint to exercise interrupted-session recovery; they also
  verify orphan-session reclamation and that cache cleanup preserves symlinks/unrelated files.
- Ten source-contract tests (including six missing/invalid project-dependency negative cases) check manifest, strict signer policy, session readback,
  permission/foreground boundaries, persistence, cancellation and JNI platform guards.
- All plug-in Java compiles with official Google Android SDK Platform 35 revision 2 API
  references. SDK source: https://dl.google.com/android/repository/platform-35_r02.zip
  ZIP SHA-256: `0988cacad01b38a18a47bac14a0695f246bc76c1b06c0eeb8eb0dc825ab0c8e0`.
  `android.jar` SHA-256: `4566663c3876e022b4fa4ced8c8697c4ab1688267f090114fd92d027b32e619b`.
  SDK files are not committed.
- C# bridge compiles against the recovered actual Unity 6000.4.9f1 API references for
  Android without `UNITY_EDITOR`, standalone Windows, and Editor. No Unity process runs.

Synthetic `PackageInfo` and `PackageInstaller` doubles do not parse a real signed APK or
exercise Android OS UI. They cannot prove a device's signature verification, permission
screens, installation behavior, process death, IL2CPP/JNI marshalling, manifest merge or
Gradle/Unity import. No APK build, emulator/device, Unity execution, Mac/Win operation,
production deployment or Git push was performed by these checks.

Before shipping, authorized device acceptance must cover an actual signed upgrade with
matching package/current key and increasing code; tampered/unsigned/wrong-key/wrong-package/
old-code APKs; permission refusal/grant/revocation; user cancellation/back; installer activity
launch failure; disk full; app kill/relaunch before/after pending and terminal callbacks;
and supported API25/28/31/current-target devices. Check the merged manifest and minified
JNI entry-point retention in a real authorized build. The client and server's separate
login/version compatibility requirements remain unchanged by this installer.

## Official references

- [Unity Android Library creation and Gradle properties](https://docs.unity3d.com/6000.0/Documentation/Manual/android-library-plugin-create.html)
- [Unity Android Library import](https://docs.unity3d.com/6000.0/Documentation/Manual/android-library-project-import.html)
- [Android Session commit, readback and mutable callback contract](https://developer.android.com/reference/android/content/pm/PackageInstaller.Session)
- [Explicit user-action session requirement](https://developer.android.com/reference/android/content/pm/PackageInstaller.SessionParams#setRequireUserAction(int))
- [Current vs historical signing certificates](https://developer.android.com/reference/android/content/pm/SigningInfo)
- [Per-source installation permission](https://developer.android.com/reference/android/content/pm/PackageManager#canRequestPackageInstalls())
- [Legacy unknown-source setting](https://developer.android.com/reference/android/provider/Settings.Secure#INSTALL_NON_MARKET_APPS)
- [AOSP Android 15 archive signature flags](https://android.googlesource.com/platform/frameworks/base/+/refs/heads/android15-release/core/java/android/content/pm/PackageManager.java)
- [AOSP Android 15 verified APK signing details](https://android.googlesource.com/platform/frameworks/base/+/refs/heads/android15-release/core/java/com/android/internal/pm/pkg/parsing/ParsingPackageUtils.java)

## Dependency regression found by actual Unity build

The first actual Android build of the initial implementation reported CS1069: missing
`UnityEngine.AndroidJNIModule`. The standalone bridge test had explicitly supplied that
DLL and therefore masked the project's missing `com.unity.modules.androidjni` dependency.
The fix declares and locks the built-in module; it does not add a copied DLL, change
`overrideReferences`, or rely on a manual reference unavailable to Unity. The current
preflight rejects omission from the manifest/lock, an inconsistent registry/version
resolution, disabled engine references, or an excluded Android target. Non-Android bridge
compiles intentionally omit JNI references to exercise their platform guards.

These regression checks address the known module omission. A subsequent real Unity
Android build and device test are still required; a source-only compiler pass does not
certify the entire Unity package resolution, Gradle or IL2CPP pipeline.
