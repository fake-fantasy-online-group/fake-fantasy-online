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

# The 更多 panel stays exactly as at baseline 1180319: no extra hotkey toggles on mobile.
upd = src('UIManager.Update.cs')
for key, call in [('D', 'DuelDlg.Toggle()'), ('X', 'Win05.ChangeSetHotkey()'), ('F12', 'HelpWinDlg.Toggle()'), ('Insert', 'HeadHpBar.Toggle()')]:
    a = upd.index('KeyCode.' + key + ')'); check(call in upd[a:a + 120], 'desktop ' + key + ' hotkey unchanged (' + call + ')')
check(not (ra / 'GameHud.MobileMoreToggles.cs').exists() and not (ra / 'GameHud.MobileMoreToggles.cs.meta').exists(), 'more-panel toggle file removed')
all_src = '\n'.join(f.read_text() for f in ra.glob('*.cs'))
for name in ['DrawMobileMoreToggles', 'MobileMoreToggleLabels', 'ActivateMobileMoreToggle', 'MoreMenuEntryCount', 'MenuEntryCount', 'ToggleTitleVisibility']:
    check(name not in all_src, 'more-panel toggle symbol absent: ' + name)
check('MenuColumnsForWidth(MenuAvailableWidth,Scale,\n        GameHud.ControlButtonCountForIndependentRuntime)' in mcp and 'int count=GameHud.ControlButtonCountForIndependentRuntime;' in mcp, 'more panel lays out only the original ctrlbar entries')
check('DrawMobileMoreToggles' not in src('GameHud.Mobile.cs'), 'mobile HUD draws no extra more-panel entries')

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
# Tap-vs-drag row selection: desktop keeps press-to-select, mobile selects on a drag-free release.
check('if (!MobileClientPresentation.Enabled) return e.type == EventType.MouseDown;' in md, 'desktop rows still select on press')
check('return e.type == EventType.MouseUp && !GestureDragged;' in md, 'mobile rows select on a drag-free release')
check('gestureMoved || LastDragFrame >= gestureBeganFrame' in md, 'a scroll drag or finger travel cancels the tap')
for name, count in [('HelpWinDlg.cs', 1), ('MailDlg.cs', 1), ('ComposeDlg.Content.cs', 1), ('HairSalonDlg.cs', 1), ('JobRightDlg.cs', 1),
                    ('PetForgeDlg.Content.cs', 1), ('StallConfigDlg.Window.cs', 1), ('TaskDlg.Window.cs', 2), ('TransportUI.cs', 1)]:
    check(src(name).count('MobileDragScroll.RowSelect(Event.current) && ') == count, name + ' list rows use RowSelect')
# World map fits the safe area on mobile only.
check('if (MobileClientPresentation.Enabled)\n        {\n            // The 800x600 map' in wm and 's = MobileFitScale(s, safe);' in wm, 'world map fit is mobile only')
check('Mathf.Min(factor, Mathf.Min(safe.width / NW, safe.height / NH))' in wm, 'world map never grows beyond the UI factor')
check('winX = Mathf.Round((Screen.width - NW * s) * 0.5f);\n        winY = Mathf.Round((Screen.height - NH * s) * 0.5f);' in wm, 'desktop world map placement unchanged')
# Change-set take-out by touch reuses the right-click request.
check('bool mobileTake = MobileClientPresentation.Enabled && !pending.HasValue;' in chg, 'change-set take-out is mobile only')
check(chg.count('NetClient.I.ChangeSetTakeForIndependentRuntime(') == 2 and 'ev.type == EventType.MouseDown && ev.button == 1 && entry.HasValue' in chg, 'touch take-out reuses the right-click request; right-click kept')
check(chg.find('MobileChangeSetSelected = MobileChangeSetSelected == part ? -1 : part;') < chg.find('takeRect = MobileChangeSetTakeRect(r, i, s); takePart = part;'), 'cell tap selection is applied before the take button is laid out in the same event')
check('ArtworkSkin.Button(takeRect, "\u53d6\u56de", s)' in chg, 'explicit \u53d6\u56de button')
print('PASS mobile touch quick-wins source contract: %d checks' % checks)
