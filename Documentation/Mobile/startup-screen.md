# Mobile startup screen

The offline development APK hashes every installed resource against its embedded SHA-256 manifest on first launch or after the manifest changes. Subsequent launches use a private verification receipt and check each file’s existence, size and UTC modification time. Only changed files are rehashed. Previously this happened in a camera-less scene with only an IMGUI label. That left the framebuffer without an explicit clear/background during validation, a plausible cause of the initial garbage/stale-frame appearance. Confirm the device symptom with a fresh build; code inspection alone cannot exclude a driver issue.

The mobile installer now owns an early solid-clear camera and opaque, safe-area-aware loading overlay. Its navy/teal background is generated from built-in white pixels and requires no external resources. An optional small locally supplied `Assets/Resources/MobileStartupBackground.png` can provide existing approved artwork. It is excluded from Git under the existing resource/image policy; never add downloaded artwork or game packages to source control. Without that image the opaque background still works.

Progress is phase-specific, not a fabricated overall startup percentage:

- Checking resource metadata: one background scan of all installed files; unchanged files require no payload reads.
- Checking resources: actual bytes hashed, aggregated across only the files needing verification, including progress within large packages.
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
2. Warm restart: metadata checks still occur, but unchanged resources read zero hash bytes. Touch one resource and confirm only that file rehashes; change the embedded manifest and confirm all files rehash.
3. Delete/truncate one transferred file: readable failure stays on the background; client does not activate.
4. Bundled mode, empty install: copying and verification phases show their own byte counts. Interrupt during copy, restart, and confirm `.part` recovery/verified activation.
5. Rotate between allowed landscape orientations, including a notched display; text/bar remain within the safe area.
6. Confirm no flash when the actual login screen replaces the overlay. Check Android background/resume during verification and desktop startup separately.

Cloud compilation and contracts do not validate Android rendering, IL2CPP behavior, or device frame timing.

## Verification receipt and trust limits

The receipt format is versioned and bound to `MobileResourceManifest.Version`, which is the SHA-256 fingerprint of the complete embedded manifest text (not an app version label). It stores ordered expected hashes/lengths and observed timestamps, never paths. Only the embedded manifest selects paths and expected content. Malformed, truncated, stale or checksum-invalid receipts fall back to full verification. Missing or size/mtime-changed files cannot use a cached result. Metadata is checked before and after hashing and again before activation.

A new receipt is flushed to a temporary file and atomically moved/replaced only after every resource succeeds. Interrupted `.part` receipts are ignored. A previous complete receipt may remain after a failed update; changed metadata still requires verification. A cache write/activation consistency failure stops startup rather than publishing an unverified resource root.

This is a startup optimization for app-private files, not an anti-tamper scheme. It does not detect silent corruption that preserves length and mtime or an adversary capable of forging app-private receipts. Its checksum detects accidental receipt modification, not forgery. First install/manifest update still hashes all resources, including non-PKG tables, fonts, music and sounds. Existing standalone full `VerifyDirectory` checks remain available; normal startup uses the receipt flow.
