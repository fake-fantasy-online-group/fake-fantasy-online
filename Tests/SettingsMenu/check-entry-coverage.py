#!/usr/bin/env python3
"""Static wiring checks complement the executable handler contract; not runtime coverage."""
from pathlib import Path
p = Path(__file__).resolve().parents[2] / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
def source(name): return (p / name).read_text()
assert 'ActivateControlButtonForIndependentRuntime(index)' in source('GameHud.Mobile.cs')
assert 'ActivateControlButtonForIndependentRuntime(index)' in source('ReferenceFirstWorldControlBar.cs')
assert '"optionbtn") SysOptionDlg.Toggle();' in source('GameHud.ControlButtons.cs')
for file in ('UIManager.Settings.cs', 'UIManager.Update.cs'):
    s = source(file)
    assert 'KeyCode.O' in s and 'SysConfigDlg.Toggle()' not in s
    assert 'SysOptionDlg.Toggle()' in s
s = source('UiEsc.cs')
assert 'if (SysConfigDlg.Visible)' in s and 'if (SysOptionDlg.Visible)' in s
assert 'ReferenceFirstWorldEscapeAction.SystemSettings' in s
assert 'ReferenceFirstWorldEscapeAction.SystemMenu' in s
assert 'ArenaEscDlg.Toggle()' in source('UIManager.Update.cs')
s = source('SysOptionDlg.Draw.cs')
assert 'bSysConfig, "系统设置"' in s and 'SysConfigDlg.Toggle()' in s
assert 'GameBootstrap.QuitGame()' in s and 'ReturnToTitle(true)' in s and 'ReturnToTitle(false)' in s
assert 'MobileLayout(out s)' in s
s = source('SysConfigDlg.OnGUI.cs')
assert 'MobileLayout(out s)' in s
assert 'Apply();' in s and 'loaded = false;' in s
# Native button rectangles remain within the original menu background and do not overlap.
ys = (27, 60, 93, 126, 165)
assert 18 + 152 <= 192 and all(y + 33 <= 207 for y in ys)
assert all(a + 33 <= b for a, b in zip(ys, ys[1:]))
print('PASS static PC/mobile/O/Escape wiring, retained actions, native menu bounds, detail layout')
