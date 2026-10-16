#!/usr/bin/env python3
"""Guard the original IM details route, artwork, channel tabs and mobile fit.
Source checks complement the actual compact renderer interaction contract; they
are not Unity rendering, scrolling or device touch validation.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[2]
source = root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
def read(name):
    return (source / name).read_text()

mobile = read('ChatUI.Mobile.cs')
original = read('ChatUI.GuiOwner.cs')
history = read('Win05.History.cs')
definitions = read('Win05.Initialization.cs')
window = read('Win05.Window.cs')
fit = read('Win05.MobileSkills.cs')
checks = [
    ('messages.Contains' not in mobile and 'mobileExpandPressed = expand.Contains(current.mousePosition);' in mobile
     and 'DrawLines(mobileLines, messages, true, false);' in mobile,
     'message list has no opening handler or interactive rows'),
    ('sideBtn[0]' in mobile and 'DrawMobileChatIcon(new Rect(expand.x' in mobile,
     'upper-right expand button reuses the original viewbtn asset'),
    ('Win05.Open("historybar");' in mobile and 'Win05.Open("historybar");' in original,
     'expand icon tap uses the same entry as original viewbtn'),
    ('DrawPanel(messages)' not in mobile and 'ChatExpanded = true' not in mobile,
     'expand icon tap never substitutes a taller HUD message frame'),
    ('res/uires/chatbar/historybar.gsa' in definitions,
     'original history frame remains the resource owner'),
    (all(label in definitions for label in ['("systemtab", "系统")', '("teamtab", "队伍")',
      '("broadtab", "广播")', '("uniontab", "家族")', '("whispertab", "私聊")',
      '("nearbytab", "附近")', '("worldtab", "世界")']),
     'original channel artwork/table retained'),
    ('if (cur == "historybar") DrawHistory(L, def);' in window,
     'historybar uses its original tabbed renderer'),
    ('chatTab = i;' in history and 'foreach (var line in ChatUI.Snapshot())' in history
     and 'if (!MatchTab(line.Item1, want)) continue;' in history,
     'original tabs switch channel-filtered IM records'),
    ('PrepareMobileSkillWindow(def);' in window and
     'if (!MobileClientPresentation.Enabled)' in fit and
     'MobilePanelLayout.Fit(new Rect(0, 0, width, height), safe' in fit,
     'common Win05 mobile path fits the entire original frame to landscape safe area'),
]
for passed, message in checks:
    assert passed, message
print(f'PASS original IM history route/source: {len(checks)} checks. No Unity execution.')
