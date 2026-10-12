#!/usr/bin/env python3
"""Exercise production tooltip row composition without Unity or game definitions."""
from pathlib import Path
import os, runpy, subprocess, tempfile
root = Path(__file__).resolve().parents[2]
method = runpy.run_path(str(Path(__file__).with_name('run-skill-icon-contract.py')))['method']
source = (root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/Win05.SkillTipText.cs').read_text()
production = method(source, 'private static string ComposeTip(')
fixture = r'''
public static int Main() {
 var rows = new List<(string, char, bool)> { ("标题", '\u0002', true), ("内容", '\0', false), ("说明", '\0', true) };
 if (ComposeTip(rows) != "\u0007\u0002标题\n内容\n\u0007说明") throw new Exception("center-before-color row protocol changed");
 if (ComposeTip(new List<(string, char, bool)>()) != "") throw new Exception("empty rows changed");
 if (ComposeTip(new List<(string, char, bool)> { ("  authored spacing", '\0', false) }) != "  authored spacing") throw new Exception("authored content changed");
 Console.WriteLine("PASS 3 production tooltip composition assertions (not Unity rendering)"); return 0;
}
'''
with tempfile.TemporaryDirectory(prefix='skill-tooltip-contract-') as folder:
 out=Path(folder); cs=out/'Contract.cs';exe=out/'Contract.exe'
 cs.write_text('using System; using System.Collections.Generic; using System.Text; class Contract {\n'+production+'\n'+fixture+'\n}')
 mono=str(Path(os.environ['FFO_MONO_ROOT'])/'bin/mono'); framework=Path(os.environ['FFO_FRAMEWORK_DIR'])
 subprocess.run([mono,os.environ['FFO_CSC'],'-nologo','-noconfig','-nostdlib+','-langversion:9.0','-out:'+str(exe)]+['-r:'+str(framework/n) for n in ('mscorlib.dll','System.dll','System.Core.dll')]+[str(cs)],check=True)
 subprocess.run([mono,str(exe)],check=True)
