# Mobile startup screen

The offline development APK checks every installed resource against its embedded SHA-256 manifest at startup. Previously this happened in a camera-less scene with only an IMGUI label. That left the framebuffer without an explicit clear/background during validation, a plausible cause of the initial garbage/stale-frame appearance. Confirm the device symptom with a fresh build; code inspection alone cannot exclude a driver issue.

The mobile installer now owns an early solid-clear camera and opaque, safe-area-aware loading overlay. Its navy/teal background is generated from built-in white pixels and requires no external resources. An optional small locally supplied `Assets/Resources/MobileStartupBackground.png` can provide existing approved artwork. It is excluded from Git under the existing resource/image policy; never add downloaded artwork or game packages to source control. Without that image the opaque background still works.

Progress is phase-specific, not a fabricated overall startup percentage:

- Checking resources: actual bytes hashed, aggregated across the manifest, including progress within large packages.
- Copying packaged resource: actual bytes streamed from the APK for the current missing/corrupt resource (bundled mode only).
- Verifying copied resource: actual bytes hashed before atomic activation of the installed content.
- Loading game: title preparation has no measurable byte denominator, so it displays a status without a percentage. Existing synchronous title initialization may briefly hold the frame; it does not claim timed progress.

Errors leave the opaque screen and actionable error text visible. The overlay is removed only after title initialization completes and a render boundary has passed. Desktop initialization remains unchanged. The existing integrity checks and offline USB-transfer requirements are preserved.

## Checks

- `bash Tests/Mobile/run-resource-contract.sh`: SHA-256 compatibility, byte progress, missing/corrupt files, empty files, activation and staging contracts.
- `python3 Tests/Mobile/run-startup-screen-source-contract.py`: startup ownership, clear behavior, phase data sources and handoff guards.
- Compile current engine-free, Presentation and Bootstrap sources against the recovered Unity references.

## Required device acceptance

1. Cold-start offline build with valid transferred resources; record from launch through login. Confirm opaque background on the first application frame and continuously advancing byte counts during large hashes.
2. Warm restart: checks still occur; no skipped integrity checks or artificial delay.
3. Delete/truncate one transferred file: readable failure stays on the background; client does not activate.
4. Bundled mode, empty install: copying and verification phases show their own byte counts. Interrupt during copy, restart, and confirm `.part` recovery/verified activation.
5. Rotate between allowed landscape orientations, including a notched display; text/bar remain within the safe area.
6. Confirm no flash when the actual login screen replaces the overlay. Check Android background/resume during verification and desktop startup separately.

Cloud compilation and contracts do not validate Android rendering, IL2CPP behavior, or device frame timing.
