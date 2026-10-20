# Modal foreground sorting contract

Run the production `SysMsgBox.cs` and `SysMsgBox.Drawing.cs` with minimal Unity API
stubs using a .NET runtime and Roslyn compiler:

```sh
python3 Tests/SystemMessageBox/run-foreground-contract.py \
  --dotnet /path/to/dotnet --csc /path/to/csc.dll
```

The test verifies that background, message text, and both native buttons all draw
at an explicit negative depth, that the callback does not restore its sorting to
an ordinary window's depth, and that cancellation/explicit confirmation retain
their existing semantics. Repaint never purchases anything.

Unity documents GUI.depth as the sorting depth of the currently executing GUI
behaviour; lower values appear above higher values:
https://docs.unity3d.com/ScriptReference/GUI-depth.html

This test checks the production source's API contract, not native Unity sorting.
The reported phone incident still requires device validation: open the shopping
confirmation, verify the entire modal is visible, then cancel or press Back.
Do not approve an actual purchase as part of that validation.

This patch does not modify window registration, MonoBehaviour GUIDs, component
activation, resource loading, or transaction/network processing. It is separate
from the missing-button-artwork fallback candidate.
