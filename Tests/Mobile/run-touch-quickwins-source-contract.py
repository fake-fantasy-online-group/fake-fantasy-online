#!/usr/bin/env python3
"""Source contract for the mobile touch quick wins: every new behaviour is gated on
MobileClientPresentation.Enabled (or isMobilePlatform), reuses the desktop action it
mirrors, and keeps true modal dialogs modal."""
from pathlib import Path
import re
root = Path(__file__).resolve().parents[2]
ra = root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
checks = 0
def src(name): return (ra / name).read_text()
def check(ok, name):
    global checks; checks += 1
    if not ok: raise SystemExit('FAIL ' + name)
def expr(text, name):
    a = text.index('public static bool ' + name + ' =>'); return text[a:text.index(';', a)]

mcp = src('MobileClientPresentation.cs')
check('public static bool ModalOpen => TrueModalOpen || (!LegacyWindowsOwnOnlyTheirRect && LegacyWindowOpen);' in mcp, 'ModalOpen keeps legacy windows modal off mobile')
check('internal static bool LegacyWindowsOwnOnlyTheirRect => Enabled;' in mcp, 'rect-only ownership is mobile only')
true_modal = expr(mcp, 'TrueModalOpen'); legacy = expr(mcp, 'LegacyWindowOpen')
for name in ['SysMsgBox.Visible', 'DealDlg.Visible', 'MailDlg.ComposeVisible', 'ShopDlg.Picking', 'InventoryDlg.SplitOpen',
             'MoneyInputDlg.Visible', 'PasswdDlg.Visible', 'SecCodeAskDlg.Visible', 'FamilyCreateDlg.Visible', 'HornDialog.Visible',
             'RenameDlg.Visible', 'ReviveDlg.Visible', 'DialogUI.VisibleS', 'PlayerActionMenu.Visible', 'QueryDlg.Visible',
             'SelectPopupDlg.Visible', 'TransportUI.VisibleS', 'WorldMapDlg.Visible', 'DuelDlg.Visible', 'ReportDlg.Visible']:
    check(name in true_modal, 'true modal keeps ' + name)
legacy_names = re.findall(r'(\w+)\.(?:Visible|get_Visible\(\))', legacy)
check(len(legacy_names) == 15, 'fifteen rect-owned legacy windows')
for cls in legacy_names:
    check(cls + '.' not in true_modal, cls + ' is not also truly modal')
    files = [p for p in ra.glob(cls + '*.cs') if re.match(re.escape(cls) + r'(\.|$)', p.stem)]
    check(any('WinZ.Frame(this' in p.read_text() for p in files), cls + ' registers its own rect with WinZ')

ns = src('NativeScroll.cs')
for m in re.finditer(r'MobileDragScroll\.(PairView|DragRows|Drag)\(', ns):
    line_start = ns.rfind('\n', 0, m.start()); prev = ns[ns.rfind('\n', 0, line_start - 1):m.start()]
    check('MobileClientPresentation.Enabled' in prev or 'MobileClientPresentation.Enabled' in ns[ns.rfind('if (', 0, m.start()):m.start()], 'NativeScroll drag call is mobile gated')
check('if (!MobileClientPresentation.Enabled) return r;' in ns, 'scrollbar hit expansion is mobile only')
check('trackHit = track;' in ns, 'track hit stays native so list rows keep taps')
md = src('MobileDragScroll.cs')
check(md.count('if (!MobileClientPresentation.Enabled') >= 2, 'MobileDragScroll is inert off mobile')
check('if (MobileClientPresentation.Enabled)\n            listScroll = MobileDragScroll.Drag(this, viewport' in src('MailDlg.cs'), 'mail list drag gated')

chrome = src('MobileLegacyWindowChrome.cs'); esc = src('UiEsc.cs')
check('if (!UnityEngine.Application.isMobilePlatform) return;' in chrome, 'close chrome is only installed on phones')
check('if (!MobileClientPresentation.Enabled || MobileClientPresentation.TrueModalOpen)' in chrome, 'close chrome yields to true modals')
for cls, closer in re.findall(r'\{ typeof\((\w+)\), (\w+\.\w+) \}', chrome):
    if cls == 'FamilyStashDlg': check('public static void Close()' in src('FamilyStashDlg.cs'), 'stash close exists'); continue
    if cls in ('EquipFuseDlg', 'PetForgeDlg', 'AttrWashDlg', 'GossipDlg', 'MasterListDlg', 'VoiceSetDlg', 'PetArousalDlg', 'HelpWinDlg', 'FigureDlg', 'SysConfigDlg'):
        continue  # not in UiEsc's chain; closer is the window's own static close
    check(closer + '();' in esc, cls + ' closes exactly like Esc (' + closer + ')')
check(chrome.count('typeof(') >= 20, 'close chrome covers the legacy window set')

hud = src('GameHud.MobileMoreToggles.cs'); upd = src('UIManager.Update.cs')
for key, call in [('D', 'DuelDlg.Toggle()'), ('X', 'Win05.ChangeSetHotkey()'), ('F12', 'HelpWinDlg.Toggle()'), ('Insert', 'HeadHpBar.Toggle()')]:
    a = upd.index('KeyCode.' + key + ')'); check(call in upd[a:a + 120], 'desktop ' + key + ' calls ' + call)
    check(call + ';' in hud, 'more panel reuses ' + call)
check('PlayerController.ToggleTitleVisibility();' in hud and 'ToggleTitleVisibility();' in src('PlayerController.TitleToggleKey.cs'), 'title entry shares the Z body')
check('internal const int MoreMenuEntryCount = 5;' in mcp and 'MobileMoreToggleLabels = { "\u51b3\u6597", "\u6362\u5957\u88c5", "\u79f0\u53f7", "\u8840\u6761", "\u5e2e\u52a9" }' in hud, 'five labelled entries')

bag = src('InventoryDlg.BagDestinations.cs')
check('DropBagItemWithConfirmation((byte)HeldTab, heldSlot, heldId, heldName, heldBound, x, y, false);' in bag, 'desktop discard keeps its prompt rules')
check('bool confirmed = bound || fused.Length > 0 || preciousQuality || item.count > 1;' in bag, 'request flag matches desktop')
dest = src('InventoryDlg.MobileDestinations.cs')
for call in ['MailDlg.TryDropAttach(', 'ComposeDlg.TryMobileRefine(', 'Win05.BeginMobileChangeSet(', 'DropBagItemWithConfirmation(']:
    check(call in dest, 'long press reuses ' + call)
check('live.bound, x, y, true);' in dest and 'IsLocked(bagTab, bagSlot)' in dest, 'mobile discard always confirms and refuses locked items')
check('return TryDropRefine(' in src('ComposeDlg.State.cs') or 'TryDropRefine(tab, bagSlot, RefineDropRect().center)' in src('ComposeDlg.State.cs'), 'refine uses the desktop drop')
chg = src('Win05.NativeChangeEquipment.cs')
check('var pending = MobileClientPresentation.Enabled ? MobilePendingChangeSet : null;' in chg, 'change-set cell tap is mobile only')

spirit = src('EndgamePanel.Spirit.cs')
check('MobileClientPresentation.Enabled &&\n            ArtworkSkin.Button(MobileSpiritTakeBackRect(rect)' in spirit and 'ClearSpiritSlot(material); Event.current.Use();' in spirit, 'spirit take-back shares the right-click body')
wm = src('WorldMapDlg.cs')
check('private bool MobileBackVisible => MobileClientPresentation.Enabled && view > 0;' in wm and wm.count('MobileBackStep();') == 2, 'world map back shares the right-click step')
print('PASS mobile touch quick-wins source contract: %d checks' % checks)
