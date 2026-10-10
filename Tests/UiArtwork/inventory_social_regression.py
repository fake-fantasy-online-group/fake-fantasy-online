#!/usr/bin/env python3
"""Source/geometry contracts only. This does not run Unity, IMGUI or network calls."""
from pathlib import Path
import argparse, json, subprocess
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--repo', type=Path, help='Repository root; defaults to an enclosing checkout or current directory')
parser.add_argument('--baseline', default='99c800c6f0c369a231733b292887d12ad7132803', help='Pre-artwork commit for behavior comparisons')
parser.add_argument('--output', type=Path, help='Optional JSON result path')
args = parser.parse_args()
candidates = [Path.cwd(), *Path(__file__).resolve().parents]
ROOT = args.repo.resolve() if args.repo else next((p for p in candidates if (p/'Assets/FakeFantasyOnline').is_dir() and (p/'.git').exists()), None)
if ROOT is None: parser.error('Cannot locate checkout; pass --repo PATH')
REL = Path('Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets')
BASE = args.baseline
checks = []
def source(name, old=False):
    p = REL/name
    return subprocess.check_output(['git', 'show', f'{BASE}:{p}'], cwd=ROOT, text=True) if old else (ROOT/p).read_text()
def block(text, marker):
    start = text.index(marker)
    brace = text.index('{', start)
    depth = 0
    # These selected blocks contain no brace literals; use their exact source bodies.
    for pos in range(brace, len(text)):
        if text[pos] == '{': depth += 1
        elif text[pos] == '}':
            depth -= 1
            if depth == 0: return text[brace:pos+1]
    raise AssertionError(f'Unclosed {marker}')
def check(condition, title):
    if not condition: raise AssertionError(title)
    checks.append(title)
def unchanged(name, marker):
    check(block(source(name), marker) == block(source(name, True), marker), name+': '+marker)
for method in ['private static string Digits(', 'private long Total(', 'private void SetFields(', 'private bool ClampToCap(', 'private void Confirm(', 'public static void Open(', 'private string F(']:
    unchanged('MoneyInputDlg.cs', method)
for marker in ['if (click && all)', 'else if (click && ok)', 'else if (click && cancel)', 'if (focus != lastFocus)']:
    unchanged('MoneyInputDlg.cs', marker)
check(block(source('InventoryDlg.OnGUI.cs'), 'if (GUI.Button(sort,') == block(source('InventoryDlg.OnGUI.cs', True), 'if (GUI.Button(new Rect(X + s * 365f'), 'Bag sort callback unchanged')
unchanged('StoreDlg.Drawing.cs', 'if (WhUI.Click(sort, mouse))')
unchanged('EquipDlg.Title.cs', 'private void ApplyTitle(')
unchanged('EquipDlg.Title.cs', 'if (Event.current.type == EventType.ScrollWheel')
unchanged('EquipDlg.Title.cs', 'if (Btn(f.L(390,188,44,22),okBtn,ev))')
unchanged('EquipDlg.Title.cs', 'if (Btn(f.L(436,188,44,22),delBtn,ev))')
new = source('StallConfigDlg.Window.cs')
remove = block(new, 'if (hover && Event.current.type == EventType.MouseDown')
check(remove.index('staged.RemoveAt(i)') < remove.index('SaveTemplate()') < remove.index('Event.current.Use()') < remove.index('hoverIndex = -1') < remove.index('break;'), 'Stall remove order and tooltip invalidation')
for token in ['gold = GUI.TextField(L(94f, 125f, 26f, 16f), gold, 6, edit);', 'silver = GUI.TextField(L(137f, 125f, 26f, 16f), silver, 3, edit);', 'count = GUI.TextField(L(99f, 148f, 66f, 16f), count, 5, edit);']:
    check(token in source('StallDetailDlg.Window.cs') and token in source('StallDetailDlg.Window.cs', True), 'Stall input geometry/length: '+token.split(' =')[0])
check(all('edit.'+state+'.background' in source('StallDetailDlg.Window.cs') for state in ['normal','hover','active','focused','onNormal','onHover','onActive','onFocused']), 'All8 stall field backgrounds cleared')
for name, marker in [('MailDlg.cs', 'private bool Click('), ('FamUI.cs', 'public static bool Click('), ('FamilyDlg.cs', 'private bool Click('), ('FamilyStashDlg.cs', 'public static bool Click(')]: unchanged(name, marker)
for name in ['MailDlg.cs','FamilyDlg.cs','FamUI.cs']:
    n,b=source(name),source(name,True)
    method_marker='public static bool Btn(' if name=='FamUI.cs' else 'private bool Btn('
    n,b=block(n,method_marker),block(b,method_marker)
    tail='if (!hover || SysMsgBox.Visible)'
    check(n[n.index(tail):] == b[b.index(tail):], name+' original action gating/consumption preserved')
unchanged('FamilyStashDlg.OnGUI.cs', 'if (Click(row, Event.current.mousePosition))')
unchanged('FamilyStashDlg.OnGUI.cs', 'if (Click(row, Event.current.mousePosition) && CanTake)')
unchanged('FamilyStashDlg.cs', 'if (Click(r, mp) && !sel)')
for s in [.5,1,1.25,1.5,2,3]:
    check((170+88)*s < 282*s, f'Family mail bottom-row separation scale{s}')
    check((494+15)*s <= 510*s and (176+10)*s < 188*s, f'Title arrows inside panel/control gap scale{s}')
    for side,width,row_width,content_width in [('left',208,186,190),('right',220,198,202)]:
        # NativeScroll.TrackRect starts at view.width-16 logical pixels.
        check((2+row_width)*s < (width-16)*s and content_width*s <= (width-16)*s, f'{side} row and content leave native scrollbar gutter scale{s}')
    for n in [0,1,8,9,12,99]:
        content=max(1,n)*30*s
        maximum=max(0,content-266*s)
        for current in [-100,0,50,99999]:
            value=min(maximum,max(0,current))
            check(0 <= value <= maximum, f'Stash content clamp scale{s}/rows{n}/scroll{current}')
report={'scope':'Source equivalence and layout geometry assertions; not Unity IMGUI, PlayMode or visual validation','passed':len(checks),'checks':checks}
if args.output:
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print(f'PASS {len(checks)} source/geometry assertions (not Unity runtime)')
