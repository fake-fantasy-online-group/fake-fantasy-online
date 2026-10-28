#!/usr/bin/env python3
"""Source wiring guards; not a substitute for the executable UI contract."""
from pathlib import Path
root = Path(__file__).resolve().parents[2]
p = root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
book = (p / 'EndgamePanel.PowerBook.cs').read_text()
apply = book[book.index('internal static bool ApplyPowerScroll'):book.index('internal static bool RecoverPowerBookFromNpc')]
assert 'target?.Legendary == null' not in apply
for guard in ('Current.State.Pending', 'Current.State.InventorySnapshotCurrent', 'powerSourceGeneration != state.Generation', 'target == null', 'target.Locked', 'source.Locked', 'source.UID == target.UID', '!MatchesNative(source)', '!MatchesNative(target)'):
    assert guard in apply, guard
assert 'new EndgameRequest("powerbook.apply", state.Revision, targetUID: target.UID, runeUID: source.UID)' in apply
assert '暗金装备或羽翼' in apply
routes = (p / 'AvatarFuse.Targets.cs').read_text()
assert 'if (!Active) return;' in routes
for location in ('"equipped", 0, equipSlot', '"bag", targetTab, targetBagIdx'):
    assert location in routes
# Both paths consume targeting mode before submitting, so a repeated mouse event cannot replay.
for section in routes.split('public static void ')[1:]:
    assert section.index('Active = false;') < section.index('if (kind == 3)')
    assert 'CursorManager.I?.SetAvatarMode(false);' in section
state = (p / 'AvatarFuse.State.cs').read_text()
cancel = state[state.index('public static void Cancel()'):]
assert 'Active = false;' in cancel and 'SetAvatarMode(false)' in cancel
assert 'ApplyPowerScroll' not in cancel and 'NetClient' not in cancel
bag = (p / 'InventoryDlg.OnGUI.cs').read_text()
assert 'AvatarFuse.Active && tab == 2 && !isPet' in bag
assert 'AvatarFuse.PickTargetBag' in bag and 'AvatarFuse.Cancel()' in bag
worn = (p / 'EquipDlg.OnGUI.cs').read_text()
assert 'AvatarFuse.PickTarget(item.slot,item.name,item.avatar > 0)' in worn
assert 'AvatarFuse.Cancel()' in worn
print('PASS wing scroll source wiring: bag/equipped routing, repeated input, cancellation, unchanged UID/revision and native/lock/snapshot guards; no C# execution claimed.')
