# Client package update: automatic startup and acceptance

## Startup contract

A configured startup checks `dispatch.json`. A strictly newer platform client is selected **before** downloading resources for the old client, whether the update is mandatory or optional. The normal route has no application confirmation steps:

`check → download/resume → exact length + SHA-256 → platform identity/signature verification → prepare → install → relaunch → verify compatible resources → title/login`

This opens the login screen; it never submits credentials. An optional compatible version can still be selected explicitly as recovery. Required versions and missing exact resource mappings never bypass the entry guard. No dispatch schema change, downloaded C# execution, or automatic version bump is introduced.

The UI retains real byte progress, network speed, cancel/pause, configuration and failure recovery. Transient network failures receive at most three attempts with 1 s / 3 s backoff. Integrity, trust, permission, malformed metadata and disk errors do not get automatic retries. Existing identity-bound partial downloads are retained and revalidated. On mobile, a transfer interrupted by backgrounding resumes on returning; an explicit Pause stays paused. Desktop updates run without focus.

Before native preparation, `persistentDataPath/client-update-attempt.txt` records a hash of the old application version and artifact identity. If a failed, interrupted or rolled-back install returns to that old app, startup stops rather than repeatedly reinstalling/exiting. A deliberate retry is permitted. A different artifact or a genuinely newer running app has a different identity. Existing resource/settings/account data are not deleted.

## Desktop

The trusted helper already bundled in the running app performs preparation. When it reports ready, the app writes the existing commit sentinel once and exits; the independent helper waits for that exact process to exit, installs transactionally and attempts to reopen the game. Existing backup/rollback and uncertain-launch recovery are unchanged. Failed preparation never quits the game.

All existing digest, archive path, install identifier, strictly increasing version and signing checks remain. The established macOS **empty Team pin/ad-hoc lane** retains `codesign --verify --deep --strict`, signing identifier and bundle-version checks. A **nonempty Team pin** remains strict, including matching Team ID and `spctl`; neither a package nor dispatch can downgrade an installed pin. The existing Windows publisher pin policy is unchanged. No quarantine removal, root/UAC bypass, security-policy change or downloaded executable launch is added.

Supported native automatic desktop updates use the existing verified ZIP/marked-install contract. Unsupported package types, absent helpers, unwritable installs or OS security restrictions stop with recovery guidance instead of pretending success. OS authentication, UAC, Gatekeeper/SmartScreen or managed-device decisions cannot be bypassed.

## Android

The source-owned `.androidlib` retains independent private-copy hash/size checks, exact package name, exact target version name, increasing versionCode and exact current signer-set verification. Signer rotation remains conservatively rejected. The OS additionally validates APK installability and signature.

The app automatically starts preparation, opens install-source settings **once if required**, resumes after a grant, and opens a pending OS confirmation **once if required**. Denial, Back, cancellation or a terminal native failure does not cause repeated prompts. Android retains the decision.

On API 31+, the session requests documented `USER_ACTION_NOT_REQUIRED` with `UPDATE_PACKAGES_WITHOUT_USER_ACTION`; eligible ordinary self-updates may proceed without confirmation. Eligibility depends on Android's rules, target SDK and ownership. The session still handles `STATUS_PENDING_USER_ACTION`. Older/ineligible/managed devices may require an OS install confirmation. No privileged installation, accessibility, overlay or permission bypass is used.

Confirmed success requires installed versionCode/versionName/current-signers readback, plus the system result or protected `MY_PACKAGE_REPLACED` broadcast. A non-exported replacement receiver handles process loss. A durable token permits **one** ordinary launcher request for that verified update. Android background-activity policy may block it; then opening the app from the launcher is unavoidable. No universal unattended-install/relaunch guarantee is made.

References: [Android SessionParams](https://developer.android.com/reference/android/content/pm/PackageInstaller.SessionParams#setRequireUserAction(int)) and [background activity launch policy](https://developer.android.com/guide/components/activities/secure-bal).

## Build and migration handoff

Use the approved public source plus this patch only; do not merge unrelated feature branches. Existing 1.0.3 binaries cannot acquire new behavior merely by republishing metadata. Their first migration still uses the old UI. The automatic UX begins when running a build containing this patch and updating to a strictly newer real build.

The proposed next public version is **1.0.4 / Android code 10**, subject to checking the latest published version and current code before release. Do not repack 1.0.3 under the same version. This patch intentionally does not alter project versions or public metadata. Release/version/tag/signing/publication steps require the authorized release workflow.

On the licensed Unity 6000.4.9f1 build executor:

1. Apply the commit/patch to the approved source in an isolated clean worktree. Verify changed files and source SHA. Apply the approved version/code change as a separately reviewable release step.
2. Rebuild the native desktop helpers from `Tools/DesktopUpdater/DesktopUpdater.csproj` for `win-x64`, `osx-x64`, `osx-arm64`, self-contained single-file; use the existing reviewed build/signing process. Do not weaken or invent pins. Stage exactly `DesktopUpdater.exe`, `DesktopUpdater-osx-x64`, `DesktopUpdater-osx-arm64` for their respective platforms.
3. Use `scripts/ci/build-resource-free.sh --help` and the existing platform release lane. The formal wrapper requires nonempty signing pins; it is **not** the existing empty-Team ad-hoc lane. Preserve the approved ad-hoc build/staging/signing process for that lane, not a fabricated Team ID or wrapper bypass. Build Android with its existing package/signing identity and a strictly larger versionCode.
4. Inspect real outputs: app/executable version, install marker, bootstrap version, helpers, architecture, resource-free contents, APK package/versionCode/targetSdk/current signing digest and merged receivers/permissions. Run `scripts/ci/verify-resource-free-apk.py` and `scripts/ci/package-desktop-update.py --help` as applicable; structural checks are not OS acceptance.
5. Hash and size the final signed/packaged outputs, not pre-signing files. Prepare reviewable platform descriptors and exact new-version resource mappings. Do not publish until actual acceptance below succeeds and publication is authorized.

## Source checks

```sh
source Documentation/tools/activate-dotnet-test-toolchain.sh
bash Tests/ClientUpdate/run-artifact-download-contract.sh
bash Tests/ClientUpdate/run-network-recovery-contract.sh
bash Tests/ClientUpdate/run-package-ui-contract.sh
bash Tests/DesktopUpdater/run-desktop-updater-contract.sh
bash Tests/Mobile/run-android-package-contract.sh
bash Tests/Mobile/run-client-update-contract.sh
bash Tests/Mobile/run-update-recovery-contract.sh
bash Tests/Mobile/run-range-resource-contract.sh
bash Tests/Mobile/run-remote-resource-contract.sh
python3 -m unittest discover -s Tests/ClientBuild -p 'test_*.py' -v
```

The UI tests use synthetic bytes, Unity/JNI stubs and a managed host; native Java contracts use stubs unless `ANDROID_JAR` is supplied. These are not a Unity build, full-game update, real OS trust test, or device acceptance.

## Required real-game acceptance (private metadata first)

Use **two real full-game builds**, both containing this patch, with matching install/package/signing identity and strictly increasing product versions (and Android versionCode). Keep higher QA versions private, not public metadata. Also verify the one-time migration from the currently published older updater. Do not substitute a fixture for the game.

- Windows and macOS: launch older patched game, leave it alone/unfocused; observe automatic download/hash/prepare, one commit/exit, replacement, automatic reopen and **new version at the real login screen**. Preserve existing settings/resources and compatible login. Test both mandatory and optional descriptors, including no old-version resource mapping.
- macOS: existing empty-Team ad-hoc lane separately from nonempty strict Team lane. Record actual `codesign`, identifier/version, installed marker and updater result. Windows: preserve Authenticode policy/MOTW, test actual install permissions/OS prompts. System-only authorization is an explicit observed exception, not success by simulation.
- Interrupt download/network, regain connectivity within the bounded retry window, pause/resume explicitly, background/resume Android, restart mid-download; verify revalidated resume. After budget exhaustion, keep truthful recovery UI without a loop.
- Corrupt length/hash, change artifact identity, use wrong identifier/version/signers, remove helper, exhaust storage and deny write access: never install/quit on failed verification. Retry is manual after terminal failure.
- Kill before/after native commit; simulate helper rollback or failed relaunch, start old version again: one-time durable fence must prevent reinstall loops and data loss. Verify recovery from retained old application.
- Android API 25/30 and API 31+ eligible/ineligible self-update cases: permission already granted, missing permission grant/deny/Back; pending OS confirmation accept/cancel; same/lower code/wrong signer refusal; process death after session commit; repeated callbacks/broadcasts; OS-blocked background launch. Verify actual new APK and login, not merely callback success. Document every OS-only action still needed on each tested device.
- Confirm no automatic credential submission and no unrelated feature merge. Only then propose publication with exact commit, final artifact hashes/sizes and observed platform limitations.
