# Mobile title input boundary

Mobile-only branches use `MobileTitleTextInput` for the existing login account/password,
existing account form rows, and character creation name. Desktop retains the recovered
IMGUI fields, password-mask reconstruction, original geometry and authentication flow.
No new login product, credential storage, credential logging, protocol, or network
request is introduced. Existing remember-account/password behavior remains unchanged.

- Native keyboard: Default type supports the OS Chinese IME; single-line, autocorrection
  disabled, passwords opened with `secure=true`. IMGUI draws only a password mask. The
  original limits remain 20 for login account, 40 for password, 24 for account-form plain
  rows, and 30 for character name. UTF-16 truncation does not split a surrogate pair.
- Native Done/LostFocus commits the latest full text. Native Canceled restores the value
  captured when that field opened. Outside release commits the current edit and closes
  the keyboard. Waiting for release avoids moving the layout between press and release.
  Tapping an already active field does not reopen the keyboard. Page changes, notice
  overlay, component disable and app pause release keyboard ownership. Non-Android app
  focus loss also dismisses; Android focus loss while the keyboard owns focus does not,
  because opening the keyboard itself triggers that callback ([Unity documentation](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MonoBehaviour.OnApplicationFocus.html)).
  Lifecycle dismissal retains the last text already observed by the title draw loop;
  it does not submit login/creation or store a separate credential copy.
- Mobile title coordinates fit the screen safe area. While editing, the same scale is
  retained and the focused field is panned above the keyboard. A zero-height OS keyboard
  report reserves the lower 45% as a conservative fallback. Keyboard status/area behavior
  still needs device testing, particularly floating keyboards and Android resizing.
- Fields expand by four logical pixels above and below within the existing row spacing.
  Server world tiles remain adjacent 113x30 hit rectangles; line tiles are 86x40;
  character slots remain 195x167. This preserves unambiguous selection and reuses existing
  callbacks. Dense server rows and character hair/color controls still require real-device
  usability acceptance; expanding those hit areas blindly would overlap neighboring
  actions. This is not a full mobile title redesign or a minimum physical target claim.
- Desktop simulation uses a real IMGUI PasswordField when native keyboards are unsupported;
  this is useful for source integration but does not validate the OS keyboard path.

## Source checks

```
source Documentation/tools/activate-dotnet-test-toolchain.sh
bash Tests/Mobile/run-title-input-contract.sh
python3 Documentation/tools/compile-current-presentation.py
```

The contract compiles the actual input adapter with deterministic Unity substitutes and
uses fictional values only. It covers secure/default options, masking, Chinese text,
Done/Canceled/LostFocus, repeated focused taps, switching fields/pages, outside release,
UTF-16 limits, keyboard-safe-area intersection and focused-field panning. It does not
exercise Unity IMGUI input dispatch or a physical IME. Full Presentation compilation uses
recovered real Unity API references, not Unity execution.

Required device acceptance: Chinese composition/selection and replacement; secure keyboard
behavior, paste and middle deletion; Back/Done/Cancel; account/password focus changes;
login and creation submit immediately after editing; app background/resume; keyboard resize,
notches and rotation; server and character selection touch accuracy. APK/IL2CPP and devices
have not been built/run by these checks.
