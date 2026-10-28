#!/usr/bin/env python3
"""Execute unchanged production tooltip layout/wrap + font measurement with original local glyph metrics, never bundle original font assets."""
from pathlib import Path
import os, subprocess, tempfile
root=Path(__file__).resolve().parents[2]
base=root/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
def method(path,signature):
    source=path.read_text();start=source.index(signature);end=source.index('{',start)+1;depth=1
    while depth:
        depth+=(source[end]=='{')-(source[end]=='}');end+=1
    return source[start:end]
tip=base/'UITip.cs';font=base/'BitmapFont.cs'
code='using System; using System.Collections.Generic; using System.Text; using UnityEngine;\nstatic partial class UITip {\n'+'\n'.join(method(tip,x) for x in ['private static (List<(string, Color, bool)>, float, float) Layout(', 'private static List<string> WrapByWidth(', 'private static int TooltipNumberEnd(', 'private static int TooltipNumericEnd(', 'public static Color32 QualityColor(', 'private static Color LineColor('])+'\n}\n'
code+='static partial class BitmapFont {\n'+'\n'.join(method(font,x) for x in ['private static int CellOf(', 'private static float Adv(', 'internal static float Measure('])+'\npublic static float Measure(string s,float scale)=>Measure(s,0,s==null?0:s.Length,scale);\n}\n'
assert os.environ.get('FFO_NATIVE_FONT_DIRECTORY'),'Set FFO_NATIVE_FONT_DIRECTORY to authorized local original gbk12.idx/wid; originals are not test artifacts'
framework=Path(os.environ['FFO_FRAMEWORK_DIR']);mono=Path(os.environ['FFO_MONO_ROOT'])/'bin/mono'
with tempfile.TemporaryDirectory(prefix='tooltip-numeric-wrap-') as tmp:
    tmp=Path(tmp);actual=tmp/'Actual.cs';actual.write_text(code);exe=tmp/'Contract.exe'
    subprocess.run([str(mono),os.environ['FFO_CSC'],'-noconfig','-nostdlib+','-nologo','-langversion:9.0',*['-r:'+str(framework/n) for n in ['mscorlib.dll','System.dll','System.Core.dll']],'-out:'+str(exe),str(actual),str(Path(__file__).with_name('TooltipNumericWrapContract.cs'))],check=True)
    subprocess.run([str(mono),str(exe)],check=True)
