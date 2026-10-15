#!/usr/bin/env python3
"""Compile original NPC geometry/actions and verify that mobile uses the original painter."""
from pathlib import Path
import os, subprocess, tempfile
root=Path(__file__).resolve().parents[2]; p=root/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
def method(source,signature):
 a=source.index(signature); b=source.index('{',a)+1; depth=1
 while depth:
  depth+=(source[b]=='{')-(source[b]=='}'); b+=1
 return source[a:b]
orig=(p/'DialogUI.cs').read_text()
base=subprocess.check_output(['git','show','7c6cef3^:Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/DialogUI.cs'],cwd=root,text=True)
# Everything after placement is the original renderer, including portrait, help,
# close, bitmap text, option artwork and the original GUI.Button callbacks.
paint=method(orig,'    private void OnGUI()')
golden=method(base,'    private void OnGUI()')
assert paint[paint.index('        GUI.DrawTexture(panel, win);'):]==golden[golden.index('        GUI.DrawTexture(panel, win);'):]
for signature in ['    private bool ActivateOption(int index)', '    private static Rect OptionRowRect(', '    private void DrawOptionFrame(', '    private void ShowOffer(TaskClient.Offer offer)', '    private void OnTaskOffers(string npc)']:
 assert method(orig,signature)==method(base,signature), signature
assert 'DrawMobileDialog' not in orig and 'HandleMobilePrimaryDown' not in orig
assert 'if (MobileClientPresentation.Enabled) return true;' in orig
assert 'DialogUI.VisibleS ||' in (p/'MobileClientPresentation.cs').read_text()
s=(p/'DialogUI.Mobile.cs').read_text()
for signature in ['    private bool ActivateOption(int index)', '    public Rect WindowRect(', '    public Rect OptionRect(', '    private float OptionHeight(', '    private static Rect OptionRowRect(', '    private static bool ContainsInclusive(', '    private static bool IsFinite(', '    internal bool HandlePrimaryDown(']:
 s=s.replace('public sealed partial class DialogUI\n{','public sealed partial class DialogUI\n{\n'+method(orig,signature),1)
with tempfile.TemporaryDirectory() as tmp:
 tmp=Path(tmp); source=tmp/'Mobile.cs'; source.write_text(s); exe=tmp/'Contract.exe'
 refs=['-r:'+str(x) for x in sorted(Path(os.environ['FFO_FRAMEWORK_DIR']).glob('*.dll'))]
 subprocess.run([os.environ['FFO_MONO'],os.environ['FFO_CSC'],'-noconfig','-nostdlib+','-nologo','-langversion:9.0','-out:'+str(exe)]+refs+[str(source),str(p/'MobileNpcDialogLayout.cs'),str(root/'Tests/Mobile/NpcDialogContract.cs')],check=True)
 subprocess.run([os.environ['FFO_MONO'],str(exe)],check=True)
print('PASS original NPC renderer and task action source isolation')
