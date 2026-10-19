#!/usr/bin/env python3
"""Run selection presentation production methods and preserve action/source boundaries."""
from pathlib import Path
import os, subprocess, tempfile
ROOT=Path(__file__).resolve().parents[2]
BASE=ROOT/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
BASELINE='6d9d4a10fd614d1afda2c8e3ed757ff047e3397b'
def method(s,name):
 p=s.index('{',s.index(name));d=1;q=p+1
 while d:d+=(s[q]=='{')-(s[q]=='}');q+=1
 return s[p:q]
def original(path):return subprocess.check_output(['git','show',BASELINE+':'+str(path.relative_to(ROOT))],cwd=ROOT,text=True)
char=(BASE/'TitleFlow.CharacterSelection.cs').read_text();server=(BASE/'TitleFlow.Servers.cs').read_text()
for filename,names in [('TitleFlow.CharacterSelection.cs',['private PlayerAvatar SlotDoll','private void DrawDollInFrame','private void DrawSprite','private void MirrorSlotToCharData','private void EnterGame']),('TitleFlow.Servers.cs',['private int LevelOf','private int WorldLevel','private void BuildSrvTree','private void ConfirmServer','private void AutoPickServer'])]:
 p=BASE/filename;s=p.read_text();old=original(p)
 for name in names:assert method(s,name)==method(old,name),name
for filename in ['TitleFlow.Creation.cs','TitleFlow.AccountConnections.cs','TitleFlow.Resources.cs','BitmapFont.cs']:
 p=BASE/filename;assert p.read_text()==original(p),filename
assert '＋ 创建角色' not in char and 'smalllogo.gsa' not in char
assert 'SlotDoll(i)' in char and 'DrawSprite(fallback[row.Gender > 0 ? 1 : 0, head, hair]' in char
assert '!pendingDelete && GUI.Button(new Rect(x, 121f, 195f, 167f)' in char
assert 'GUI.enabled = previousEnabled && !entering && count != 0 && selSlot >= 0' in char
assert 'DrawSelectionButton(644f, 419f' in char and 419+33<455
assert 'if (doubleClick) ConfirmServer();' in server
assert 'Array.Empty<SrvLine>()' in server
presentation=(BASE/'TitleFlow.SelectionPresentation.cs').read_text()
for required in ['BitmapFont.EnsureForIndependentRuntime()', 'BitmapFont.CellOfForIndependentRuntime(c) < 0',
                 'BitmapFont.DrawOutline(', 'GUI.Label(rect, text, selectionText)', 'richText = false',
                 'finally { GUI.EndGroup(); }']:
 assert required in presentation,required
assert 'RegistrationPixelArt' not in presentation and 'Resources.Load' not in presentation
deletion=(BASE/'TitleFlow.DeletePresentation.cs').read_text()
for required in ['new Rect(227f, 236f, 346f, 128f)', 'new Rect(250f, 255f, 300f, 72f)',
                 'res/uires/common/msgboxbk.gsa', 'res/uires/common/affirmbtn.gsa',
                 'res/uires/common/cancelbtn.gsa', 'TextElementEnumerator',
                 '删除后不可恢复', 'ConsumeDeleteModalEvent()', 'DeleteTargetIsCurrent()']:
 assert required in deletion,required
assert char.count('action(TitleCharacterAction.Delete)')==1
assert 'action(' not in deletion and 'NetClient' not in deletion and 'Resources.Load' not in deletion
assert 'new Texture2D(' not in deletion and 'Texture2D.whiteTexture' not in deletion
with tempfile.TemporaryDirectory(prefix='selection-presentation-') as temp:
 exe=Path(temp)/'contract.exe';mono=Path(os.environ['FFO_MONO_ROOT'])/'bin/mono';framework=Path(os.environ['FFO_FRAMEWORK_DIR'])
 subprocess.run([str(mono),os.environ['FFO_CSC'],'-nologo','-noconfig','-nostdlib+','-langversion:9.0','-out:'+str(exe)] + ['-r:'+str(framework/n) for n in ['mscorlib.dll','System.dll','System.Core.dll']] + [str(BASE/n) for n in ['TitleFlow.SelectionPresentation.cs','TitleFlow.CharacterSelection.cs','TitleFlow.Servers.cs','TitleFlow.DeletePresentation.cs']] + [str(ROOT/'Tests/UiArtwork/SelectionPresentationContract.cs'),str(ROOT/'Tests/UiArtwork/DeletePresentationContract.cs')],check=True)
 subprocess.run([str(mono),str(exe)],check=True)
print('PASS selection source guards: original avatar/selection/network actions, creation page, original resources, slot click target, nonoverlap geometry')
