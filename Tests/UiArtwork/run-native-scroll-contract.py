#!/usr/bin/env python3
"""Run production NativeScroll.cs with deterministic Unity API stubs, not Unity/IMGUI."""
from pathlib import Path
import argparse, os, shutil, subprocess, tempfile
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--repo',type=Path,help='Repository root; defaults to enclosing checkout/current directory')
p.add_argument('--unity-editor',type=Path,help='Optional Unity Editor directory used ONLY to locate its Mono compiler/runtime')
p.add_argument('--mono',type=Path,help='Mono runtime; otherwise use PATH or Unity bundled Mono')
p.add_argument('--csc',type=Path,help='C# compiler executable; otherwise use PATH or Unity bundled csc.exe')
a=p.parse_args()
candidates=[Path.cwd(),*Path(__file__).resolve().parents]
repo=a.repo.resolve() if a.repo else next((q for q in candidates if (q/'Assets/FakeFantasyOnline').is_dir() and (q/'.git').exists()),None)
if repo is None:p.error('Cannot locate checkout; pass --repo PATH')
mono=a.mono; csc=a.csc
if a.unity_editor:
    mono_roots=[a.unity_editor/'Data/MonoBleedingEdge',a.unity_editor/'Contents/MonoBleedingEdge',a.unity_editor/'MonoBleedingEdge']
    home=next((q for q in mono_roots if (q/'bin/mono').is_file()),None)
    if home is None:p.error('Cannot locate bundled Mono beneath --unity-editor')
    mono=mono or home/'bin/mono';csc=csc or home/'lib/mono/4.5/csc.exe'
mono=mono or shutil.which('mono');csc=csc or shutil.which('csc') or shutil.which('mcs')
if not mono or not csc:p.error('Need Mono and a C# compiler. Set --mono/--csc, or --unity-editor to use installed Unity toolchain without launching Unity.')
source=repo/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/NativeScroll.cs'
fixture=Path(__file__).with_name('NativeScrollContract.cs')
with tempfile.TemporaryDirectory(prefix='native-scroll-contract-') as folder:
    exe=Path(folder)/'NativeScrollContract.exe'
    compiler=[str(mono),str(csc)] if str(csc).lower().endswith('.exe') else [str(csc)]
    subprocess.run(compiler+['-nologo','-target:exe','-out:'+str(exe),str(fixture),str(source)],check=True)
    subprocess.run([str(mono),str(exe)],check=True)
