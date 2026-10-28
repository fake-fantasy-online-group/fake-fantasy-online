#!/usr/bin/env python3
"""Run actual BuildMenu/offer refresh/trial callback with explicit UI/network doubles."""
from pathlib import Path
import os, subprocess, tempfile
root = Path(__file__).resolve().parents[2]
src = root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/DialogUI.cs'
def block(signature, path=src):
    source = path.read_text(); start = source.index(signature)
    end = source.index('{', start) + 1; depth = 1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}'); end += 1
    return source[start:end]
actual = 'using System; using System.Collections.Generic; public partial class DialogUI {\n' + '\n'.join(block(s) for s in ['private static bool IsSpiritMerchant(', 'private void BuildMenu()', 'private void OnTaskOffers(string npc)', 'private void AddTrialOption']) + '\n' + block('private void AddSpiritWorkbenchOptions()', src.with_name('DialogUI.Spirit.cs')) + '\n}'
framework = Path(os.environ['FFO_FRAMEWORK_DIR']); mono = Path(os.environ['FFO_MONO_ROOT']) / 'bin/mono'
with tempfile.TemporaryDirectory(prefix='trial-menu-') as tmp:
    tmp = Path(tmp); (tmp/'Actual.cs').write_text(actual); exe = tmp/'TrialMenu.exe'
    subprocess.run([str(mono), os.environ['FFO_CSC'], '-noconfig', '-nostdlib+', '-nologo', '-langversion:9.0', *['-r:'+str(framework/n) for n in ['mscorlib.dll','System.dll','System.Core.dll']], '-out:'+str(exe), str(tmp/'Actual.cs'), str(Path(__file__).with_name('TrialMenuContract.cs'))], check=True)
    subprocess.run([str(mono), str(exe)], check=True)
