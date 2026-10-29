#!/usr/bin/env python3
"""Run actual history/menu control bodies with minimal deterministic boundary stubs.
Not Unity execution. No tested method is replaced or edited by this harness.
"""
from pathlib import Path
import os
import subprocess
import tempfile

root = Path(__file__).resolve().parents[2]
src = root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'

def block(text, signature):
    start = text.index(signature)
    cursor = text.index('{', start) + 1
    depth = 1
    while depth:
        depth += (text[cursor] == '{') - (text[cursor] == '}')
        cursor += 1
    return text[start:cursor]

init = (src / 'Win05.Initialization.cs').read_text()
state = init[init.index('    private static int chatTab ='):init.index('    private static readonly List<(string, bool)> _ntBlocks')]
menu = (src / 'DialogUI.cs').read_text()
methods = '\n'.join(block(menu, signature) for signature in (
    'private void BuildMenu()', 'private bool ActivateOption(int index)',
    'internal bool ActivateOption(ReferenceNpcDialogOption option)',
    'public void Close()', 'private static bool IsSpiritMerchant(string name)',
))
methods += '\n' + block((src / 'DialogUI.Travel.cs').read_text(), 'private bool BuildTravelMenu()')
options = (src / 'ReferenceFirstWorldNpcDialog.cs').read_text()
options = options[options.index('public sealed class ReferenceNpcDialogOption'):options.rfind('\n}')]
header = 'using System; using System.Collections.Generic; using UnityEngine; using FakeFantasyOnline.Application; using Random = UnityEngine.Random;\nnamespace FakeFantasyOnline.Presentation.ReferenceAssets {\n'
with tempfile.TemporaryDirectory(prefix='native-real-controls-') as tmp:
    tmp = Path(tmp)
    actual = tmp / 'ActualBodies.cs'
    actual.write_text(header + 'public partial class Win05 {\n' + state + '\n}\n' +
                      'public sealed partial class DialogUI {\n' + methods + '\n}\n' + options + '\n}\n')
    exe = tmp / 'Contract.exe'
    refs = ['-r:' + str(Path(os.environ['FFO_FRAMEWORK_DIR']) / name)
            for name in ('mscorlib.dll', 'System.dll', 'System.Core.dll')]
    mono = str(Path(os.environ['FFO_MONO_ROOT']) / 'bin/mono')
    subprocess.run([mono, os.environ['FFO_CSC'], '-nologo', '-noconfig', '-nostdlib+',
                    '-langversion:9.0', '-out:' + str(exe)] + refs + [str(actual),
                    str(src / 'Win05.History.cs'), str(src / 'DialogUI.Options.cs'),
                    str(src / 'DialogUI.FamilyCreation.cs'),
                    str(root / 'Tests/NativeInteractions/NativeControlsContract.cs')], check=True)
    subprocess.run([mono, str(exe)], check=True)
