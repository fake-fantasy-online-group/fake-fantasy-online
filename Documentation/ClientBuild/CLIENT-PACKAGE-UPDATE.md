# Client package updater: implementation and local acceptance

## Scope

The startup page keeps its single `dispatch.json` address. Its platform entry supplies a strictly newer stable application version, a package URL, exact positive byte length and full SHA-256. Package bytes are downloaded into `persistentDataPath/ClientPackages`, never into the running application's files or the game resource root. Resume identity includes platform, URL, version, length and digest. Pausing or losing the process retains bounded partial bytes; retry resumes only the same identity. A full digest and length check precedes atomic publication, and every handoff rehashes the completed file. Hash integrity is relative to the trusted dispatch, not independent proof of publisher identity.

The download button does not install. After verification, another explicit action starts platform handoff. Progress and speed use actual transfer bytes; local hashing/copying reports progress with no fabricated network speed. Required minimum versions still block game entry even after download or installer submission. Compatible optional updates can return to the existing resource-ready client.

| Platform | Downloaded file types | Handoff |
| --- | --- | --- |
| Android | `.apk` | Private copy, package/version/current-signers checks, user-controlled system installer |
| Windows | `.exe`, `.msi`, `.zip` | Request opening the containing directory; user quits and installs manually |
| macOS | `.dmg`, `.pkg`, `.zip` | Same manual directory handoff |
| Linux | `.AppImage`, `.tar.gz`, `.deb`, `.rpm`, `.zip` | Same manual directory handoff |

Desktop handoff does not execute/extract packages, check publisher signatures, replace running files or prove that a file manager opened. Android has no silent-install path and does not grant unknown-source permission. `hotupdateManifestUrl` remains unused; no remote C# DLL is downloaded or executed.

## Android safety and integration

The project manifest and lock explicitly enable Unity’s built-in `com.unity.modules.androidjni` so the Bootstrap JNI bridge can compile in the actual Android player. The source-owned `.androidlib` adds `REQUEST_INSTALL_PACKAGES` and a non-exported, explicit-result receiver. It has no downloaded binary/plugin dependency. The bridge is compiled out on non-Android players and in the Editor. The real Android PackageInstaller path is available only where the platform supports an interactive install. Policy restrictions remain enforced by Android.

Before handoff, an independent private copy is length/hash checked. The archive package name must equal the currently installed application; its version name must equal dispatch; its integer version code must be greater than the installed code; and its current signer set must exactly match the installed set. Signer rotation is intentionally rejected by this conservative policy. Neither package IDs nor certificates are hardcoded. The system installer additionally enforces APK validity and signature checks.

Preparation, settings permission, session staging, system confirmation, pending result and confirmed success are separate states. The app never treats merely downloading or submitting a session as successful installation. Pending confirmation is opened only by an explicit button. Success requires a system success result plus installed package/version/signature readback. Process loss without that evidence remains interrupted. Retry must abandon/reset the interrupted session before preparing again. Cancellation and system refusal retain the currently installed app.

The resource-free development build preserves the current Unity project application identifier and uses default development signing. It never switches package identity merely because assets are delivered remotely. A deliberately separate candidate must use an explicitly selected project identifier; that is a separate installation, not an upgrade of the original package. The project application version is `1.0.0`, Android `versionCode=2`. The verified historical test predecessor is `com.fakefantasyonline.fakefantasyonline`, application `0.1.0`, code 1. The newly produced APK must still be inspected to prove it has the same package and signing certificate; retaining project settings is not an APK-verification result. A matching version label alone does not establish an upgrade path. No signing credentials belong in source, logs or command arguments.

## Local build entry

Use the explicitly approved source revision and an already licensed Unity `6000.4.9f1` installation with Android support. This document does not authorize checking out a different revision, creating a tag, publishing, or executing the resulting APK. Set paths and the non-secret dispatch URL deliberately:

```sh
export UNITY_EDITOR='/path/to/Unity6000.4.9f1/Unity'
export FFO_RESOURCE_DISPATCH_URL='https://trusted.example/dispatch.json'
export FAKE_FANTASY_ONLINE_ANDROID_BUILD_PATH="$PWD/Build/Android/client-1.0.0-code2.apk"
"$UNITY_EDITOR" -batchmode -nographics -quit -projectPath "$PWD" \
  -executeMethod FakeFantasyOnline.Editor.CiClientBuild.BuildAndroidResourceFree \
  -logFile "$PWD/LocalMobileBuild/android-code2.log"
python3 scripts/ci/verify-resource-free-apk.py "$FAKE_FANTASY_ONLINE_ANDROID_BUILD_PATH"
```

The build method consumes the project identifier/version/code without overriding them and logs the resolved identifier. The structural APK checker leaves package identity/signature unverified; use Android SDK tools to check the actual manifest and signing certificate. It uses ARM64/IL2CPP, creates an APK, stages only source-owned startup assets, restores staging/settings on exit, and does not require signing passwords on the command line. Development LAN HTTP is disabled unless separately opted in with `FAKE_FANTASY_ONLINE_ANDROID_DEVELOPMENT_HTTP=1`; HTTPS is preferred. The existing offline/bundled entries also read the same project version/code.

Before any installation, use the local Android SDK `apksigner verify --verbose --print-certs` and `apkanalyzer manifest` commands to compare the new APK with the authorized predecessor. Retain only non-secret version/package/length/hash/certificate-digest evidence. Verify the merged manifest contains the intended permission and non-exported receiver, and verify the `.androidlib` is absent from desktop outputs.

## Acceptance matrix

Source-only tests are repeatable without Unity or a device:

```sh
source Documentation/tools/activate-dotnet-test-toolchain.sh
bash Tests/ClientUpdate/run-artifact-download-contract.sh
bash Tests/ClientUpdate/run-package-ui-contract.sh
bash Tests/Mobile/run-android-package-contract.sh
python3 -m unittest discover -s Tests/ClientBuild -p 'test_*.py' -v
```

An official SDK `android.jar` can be supplied with `ANDROID_JAR=...` for Java API compilation. Pure-source contracts and API compilation do not prove Unity Gradle import, IL2CPP/JNI integration or Android runtime behavior. Before release, perform these separately on an authorized disposable device/test profile:

1. Build/import with the fixed Unity toolchain, inspect actual APK package/versionCode/signature/manifest, then confirm source-owned startup renders with no downloaded resources.
2. For in-app upgrade testing, use two authorized test builds with the same package and signing certificate and strictly increasing product version and version code. `1.0.0/code2` cannot test upgrading to itself. Do not publish test fixtures as production metadata.
3. Download a valid newer fixture; pause/resume; disconnect/retry; change dispatch identity; restart mid-download; corrupt the partial/completed file; use wrong length/hash; exhaust available storage. No invalid package may reach platform handoff. Speed must stop during hashing and when paused.
4. Deny the install-source permission, return without granting it, grant it deliberately, and confirm that returning never installs automatically. Cancel preparation, session staging, and the system confirmation. Existing app/resources must remain usable; a required upgrade must remain blocked.
5. Reject same/lower version code, mismatched dispatch version name, different package name and wrong signing key. Confirm no install confirmation is offered for these failures. Exercise malformed APK and device/administrator install restrictions.
6. Kill/restart around session staging, pending confirmation and result delivery. Missing callbacks must not become success; interrupted sessions must recover via explicit retry. Only an actual successful system result and matching installed metadata can report completion.
7. After successful update, manually relaunch and verify the new app version, retained configuration/resources, login compatibility and resource gate. App version changes alone do not change the historical wire/auth marker.
8. On each supported desktop OS, confirm download/hash/retry, opening only the containing directory, truthful manual-install guidance and no running-file replacement. OS file-manager behavior remains platform acceptance, not a source-test guarantee.
