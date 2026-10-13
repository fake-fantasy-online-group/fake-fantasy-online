#!/usr/bin/env python3
"""Compile actual native menus/facades/protocol Buf writers, substituting UI/socket owners."""
from pathlib import Path
import os,subprocess,tempfile
root=Path(__file__).resolve().parents[2];r=root/'Assets/FakeFantasyOnline/Runtime';p=r/'Presentation/ReferenceAssets';w=r/'Protocol'
def block(path,signature):
 s=path.read_text();start=s.index(signature);a=s.index('{',start);depth=1;i=a+1
 while depth:depth+=(s[i]=='{')-(s[i]=='}');i+=1
 return s[start:i]
# Complete actual owner methods/types, rather than rewritten callback labels.
extra='using System;using System.Collections.Generic;\nnamespace FakeFantasyOnline.Application {\n'+block(r/'Application/GameSessionContracts.cs','public enum WorldChatSendKind')+'\n'+block(r/'Application/GameSessionContracts.cs','public sealed class WorldChatSendRequest')+'\n}\n'
extra+='namespace FakeFantasyOnline.Protocol {\n'+block(w/'WireContracts.cs','internal static class ProtocolOpcodes')+'\n}\n'
extra+='namespace FakeFantasyOnline.Presentation.ReferenceAssets { public sealed partial class DialogUI {\n'+block(p/'DialogUI.cs','private void AddTrialOption')+'\n'+block(p/'DialogUI.cs','private void ShowTransferMenu()')+'\n}\npublic partial class TransportUI {\n'+block(p/'TransportUI.cs','private void DoTeleport()')+'\n}\npublic partial class InventoryDlg {\n'+block(p/'InventoryDlg.OnGUI.cs','private void ActivateOrdinaryItem(ref int bagTab, Item it, int gridIndex)')+'\n}\n}\n'
sources=[p/n for n in ('DialogUI.Initialization.cs','DialogUI.Travel.cs','NetClient.ChatSending.cs','NetClient.Teleport.cs','NetClient.UseItem.cs','NetClient.Revive.cs')]
sources += [w/n for n in ('NetClient.Buf.Write.cs','NetClient.GmCmd.cs','NetClient.Teleport.cs','NetClient.UseItem.cs','NetClient.SystemMenu.cs','NetClient.SocialExtraCommands.cs')]
sources += [root/'Tests/Endgame/NativeEntrySenderContract.cs']
mono=Path(os.environ['FFO_MONO_ROOT'])/'bin/mono';framework=Path(os.environ['FFO_FRAMEWORK_DIR'])
refs=['-noconfig','-nostdlib+']+['-r:'+str(framework/n) for n in ('mscorlib.dll','System.dll','System.Core.dll')]
with tempfile.TemporaryDirectory(prefix='native-entry-senders-') as d:
 t=Path(d);(t/'ActualMethods.cs').write_text(extra);exe=t/'Contract.exe'
 subprocess.run([str(mono),os.environ['FFO_CSC'],*refs,'-nologo','-langversion:9.0','-out:'+str(exe),str(t/'ActualMethods.cs'),*map(str,sources)],check=True)
 subprocess.run([str(mono),str(exe)],check=True)
# These UI callers are not reimplemented in the harness; keep their live connections explicit.
assert 'NetClient.I.ReviveForIndependentRuntime(0)' in (p/'ReviveDlg.cs').read_text()
assert 'NetClient.I.GmCmdForIndependentRuntime(text.Substring(1))' in (p/'ChatUI.Sending.cs').read_text()
assert 'AddTrialOption("进入妖魔结界(试炼)", "trial")' in (p/'DialogUI.cs').read_text()
print('PASS native trial, death-return and slash-command UI sender wiring guards')
