# Opt-in native Windows updater checks

Run `.github/workflows/windows-updater-native.yml` manually, or run
`./Tests/DesktopUpdater/WindowsNative/run-windows-native.ps1` in PowerShell 7
on a disposable Windows administrator test machine with .NET 8 and NTFS.
The workflow has no push, PR, schedule, release, signing or deployment trigger.

The harness compiles the production updater source graph unchanged and uses
`new DesktopPlatform()` with no injected implementations. It builds two real,
unsigned Windows PE fixture applications (1.0.0 and 1.0.1), then checks:

- Real file version metadata, extraction and same-volume directory replacement.
- Real NTFS Zone.Identifier streams retained through staging and replacement.
- ShellExecute launch with the production three-second survival check.
- A real early-exiting process causes rollback, retains the failed candidate,
  preserves downloaded resources and does not write an installed receipt.
- An unsigned binary cannot satisfy a pinned signer policy; the signed .NET
  host also fails an intentionally wrong certificate pin after WinVerifyTrust.
- The separately published, unmodified CLI refuses the elevated runner before
  preparation or application replacement.

GitHub-hosted Windows runners use an administrator token. The engine tests
therefore exercise the lower-level transaction and platform implementation,
NOT successful entry through the CLI's standard-user guard. The guard is
separately tested and is never removed or disabled. No account creation,
privilege changes, security-policy changes or certificate installation occurs.

Passing does not establish standard-user full-helper success, interactive
SmartScreen acceptance, full Unity/game startup, login, or macOS behavior.
MOTW is retained, not bypassed. If OS launch policy blocks a fixture, the test
fails rather than disabling that policy. A fixture stays alive for at most two
minutes; cleanup only stops processes whose executable is under that test's
unique temporary directory.
