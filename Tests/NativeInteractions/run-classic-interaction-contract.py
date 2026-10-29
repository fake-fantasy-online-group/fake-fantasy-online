#!/usr/bin/env python3
"""Execute production entry/action methods with deterministic substitutes, not Unity."""
from pathlib import Path
import os, subprocess, tempfile
root=Path(__file__).resolve().parents[2]; p=root/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
BASE='36aab0ec93b1912905bd1e77165a63c951e50a6c'
def block(s,signature):
 a=s.index(signature);b=s.index('{',a);i=b+1;depth=1
 while depth:
  depth+=(s[i]=='{')-(s[i]=='}');i+=1
 return s[a:i]
def old(name): return subprocess.check_output(['git','show',BASE+':'+str((p/name).relative_to(root))],cwd=root,text=True)
mail=(p/'MailDlg.cs').read_text();family=(p/'FamilyCreateDlg.cs').read_text();history=(p/'Win05.History.cs').read_text();init=(p/'Win05.Initialization.cs').read_text()
assert '"投票"' not in init and 'DrawVote(' not in history and 'DrawGmFeedback(' not in history
assert '("gmtab", "GM")' in init and 'MatchTab(line.Item1, want)' in history
assert 'if (ChatTabs[chatTab].Item2 == "公告")' in history and 'chatTab = 6;' in init
assert 'StartCompose(' not in block(mail,'private void DrawList(')
assert 'Texture("replybtn")' in block(mail,'private void DrawRead(')
assert 'FamilyCreateDlg.Toggle' not in (p/'FamilyDlg.cs').read_text()
assert 'AddFamilyCreationOption();' in block((p/'DialogUI.cs').read_text(),'private void BuildMenu(')
for name in ('ItemActions.Handle.cs','ItemActions.WindowPrefix.cs'):
 assert 'if (HandleMailPaper(id, tab, slot)) return true;' in (p/name).read_text()
# Only entry routing changed; original send/confirm/cancel and inventory sort actions survive.
for sig in ('private void StartCompose(', 'private void DoSend(', 'private void DrawRead(', 'private void DrawCompose('):assert block(mail,sig)==block(old('MailDlg.cs'),sig),sig
for sig in ('private void OnGUI(',):assert block(family,sig)==block(old('FamilyCreateDlg.cs'),sig),sig
for name,sig in [('InventoryDlg.OnGUI.cs','if (GUI.Button(sort,'),('StoreDlg.Drawing.cs','if (WhUI.Click(sort, mouse))')]:assert block((p/name).read_text(),sig)==block(old(name),sig)
for name in ('NetClient.FamilyCreateCommand.cs','NetClient.GmFeedbackCommands.cs'):
 if (p/name).exists():assert (p/name).read_text()==old(name)
methods='\n'.join(block(mail,s) for s in ('private void StartCompose(', 'private void DoSend('))
methods+='\n'+'\n'.join(line for line in mail.splitlines() if line.strip().startswith(('private static long NonNegative(', 'private static long MoneyValue(')))
confirm=block(family,'if (FamUI.Btn(L(38f, 363f')
cancel=block(family,'if (FamUI.Btn(L(160f, 363f')
with tempfile.TemporaryDirectory(prefix='classic-native-contract-') as tmp:
 tmp=Path(tmp); prod=tmp/'Actions.cs';prod.write_text('using System;using System.Threading;using System.Collections.Generic;using UnityEngine; namespace FakeFantasyOnline.Presentation.ReferenceAssets { public partial class MailDlg {'+methods+'} public partial class FamilyCreateDlg { public static void DrawConfirm(){'+confirm+'} public static void DrawCancel(){'+cancel+'} } }')
 exe=tmp/'Contract.exe';refs=['-r:'+str(Path(os.environ['FFO_FRAMEWORK_DIR'])/n) for n in ('mscorlib.dll','System.dll','System.Core.dll')]
 subprocess.run([str(Path(os.environ['FFO_MONO_ROOT'])/'bin/mono'),os.environ['FFO_CSC'],'-nologo','-noconfig','-nostdlib+','-langversion:9.0','-out:'+str(exe)]+refs+[str(prod),str(p/'MailDlg.Paper.cs'),str(p/'ItemActions.MailPaper.cs'),str(p/'DialogUI.FamilyCreation.cs'),str(root/'Tests/NativeInteractions/ClassicInteractionContract.cs')],check=True)
 subprocess.run([str(Path(os.environ['FFO_MONO_ROOT'])/'bin/mono'),str(exe)],check=True)
print('PASS original action preservation, native entry routing and channel isolation guards')
