#!/usr/bin/env python3
"""Native panel source isolation: mobile changes inputs/fit, never replaces panel artwork."""
from pathlib import Path
import hashlib, subprocess
root=Path(__file__).resolve().parents[2]
p=root/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
checks=0
def check(ok, name):
 global checks
 checks+=1
 if not ok: raise AssertionError(name)
def read(name): return (p/name).read_text()
inv=read('InventoryDlg.OnGUI.cs');mobile=read('InventoryDlg.Mobile.cs');skill=read('Win05.SkillDialog.cs');equip=read('EquipDlg.OnGUI.cs')
check('DrawMobileInventory(Event.current); return;' not in inv,'bag cannot bypass original DrawOpenWindow')
check('DrawOpenWindow(ev);' in inv and 'i < Slots' in inv and '(i % Cols) * 40f + 14f' in inv,'original 100-cell native bag painter stays owner')
check('mobilePage' not in mobile and 'DrawMobileDetails' not in mobile and 'DrawMobileSplit' not in mobile,'no replacement pagination/details/split panels')
check('DrawMobilePage' not in mobile and 'ShowSlotTooltip(item,' in mobile,'bag reuses exact native tooltip/pet/comparison builder')
check('ShowMobileSkillTooltip();' in skill and 'if (mobile && mobileSkills.Skill > 0)' not in skill,'selection never replaces native skill tree')
check('DrawMobilePage' not in read('Win05.MobileSkills.cs') and 'BuildSkillTip(info, level)' in read('Win05.MobileSkills.cs'),'native skill tooltip contents stay original')
check('轻点查看' not in skill,'original authored tooltip instructions retained')
check('GUI.DrawTexture(f.L(0,0,257,506), bk);' in equip and 'DrawDoll(f)' in equip,'original character frame and doll stay owner')
check('!MobileClientPresentation.Enabled) DrawSlotAction' in equip,'desktop input stays original while touch selection is separate')
check('MobilePanelLayout.Fit(new Rect(0, 0, 423, 501)' in mobile,'bag fits uniformly at original dimensions')
check('147 * s' in mobile and '41 * s' in mobile and 'SplitDraw();' in inv,'original split artwork/size stays owner')
check('if (mobile)' in inv and 'if (!mobile)' in inv,'desktop layout is not globally replaced')
# Original renderer/layout/frame are byte-identical; mobile safe-area/scroll uses them.
def body(text, signature):
 start=text.index(signature);start=text.index('{',start);depth=0
 for i in range(start,len(text)):
  depth+=(text[i]=='{')-(text[i]=='}')
  if depth==0:return text[start:i+1]
base=subprocess.check_output(['git','show','HEAD:Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/UITip.cs'],cwd=root,text=True)
for signature in ['private static void Render(', 'private static (List<(string, Color, bool)>, float, float) Layout(', 'private static void DrawBox(']:
 check(body(read('UITip.cs'),signature)==body(base,signature),'native tooltip golden unchanged: '+signature)
print('PASS native panel appearance/source isolation:',checks,'checks')
