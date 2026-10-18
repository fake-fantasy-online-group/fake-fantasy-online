# Settings menu routing verification — 2026-10-18

Both desktop control-bar and mobile expanded-menu settings buttons use the shared `GameHud.ActivateControlButtonForIndependentRuntime` dispatcher, which now toggles the existing `SysOptionDlg`. Both O-key owners use the same menu. Its original System Settings button opens the existing detailed settings window. Opening either window hides the other, so repeated entry/resume cannot leave a second settings window behind.

Original return, character/server selection, death/revive, exit and confirmation owners are unchanged. Escape retains its existing close order and arena-specific fallback. The existing detailed settings apply/cancel/close behavior remains unchanged (closing details resumes the game). Menu artwork and button positions are unchanged; mobile menu drawing and hit testing share a safe-area fit, following the existing detailed settings implementation. No mobile movement or combat input files were changed.

Commands (from repository root):

```sh
source Documentation/tools/activate-dotnet-test-toolchain.sh
bash Tests/SettingsMenu/run-settings-menu-contract.sh
python3 Tests/SettingsMenu/check-entry-coverage.py
FFO_TEST_BASE="$PWD" python3 Tests/Mobile/run-more-panel-contract.py
python3 Documentation/tools/compile-current-presentation.py
```

Results:

- PASS 18 executable assertions against production shared dispatcher, O-key owner, menu lifecycle and button handlers, plus narrow-safe-area geometry and physical hit tests, using explicit Unity doubles.
- PASS static wiring of desktop/mobile entrypoints, both O owners, Escape close and arena routes, original action owners, unchanged detailed settings layout and non-overlapping native menu button bounds.
- PASS 90 existing additional mobile ordinary-panel routing assertions. Updated its dependency extraction to include the menu's actual safe-area resolver, as already done for detailed settings.
- PASS all 1199 current Presentation sources compiled with recovered real Unity references and official Roslyn/.NET toolchain; zero errors, existing warnings.
- PASS git diff whitespace check.

These are managed tests and source/geometry checks, not a Unity runtime test. No Unity Editor scene execution, rendered device validation, or Player/APK build occurred. Any separately supplied resource-rendered previews must be labeled offline reconstructions.
