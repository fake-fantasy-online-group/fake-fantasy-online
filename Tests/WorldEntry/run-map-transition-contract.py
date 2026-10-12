#!/usr/bin/env python3
"""Execute production map construction/transition iterators with deterministic Unity substitutes.
This tests source control flow, cancellation and packet interleavings, not Unity rendering.
"""
from pathlib import Path
import argparse
import os
import shutil
import subprocess
import tempfile

root = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--source', type=Path, help='Alternate GameBootstrap.cs for regression proof')
args = parser.parse_args()
source = (args.source or root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/GameBootstrap.cs').read_text()
owner_source = (root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/ReferenceFirstWorldWindowOwner.cs').read_text()
map_read = owner_source[owner_source.index('            string mapPath = '):owner_source.index('            // BuildWorld consumes the actual returned owner')]
construction = source[source.index('    internal sealed class BuildWorldCheckpoints'):source.index('    // The real controller is available')]
transition = source[source.index('    public void SwitchMapAuthoritative'):source.index('    private void ReportPos(')]
# Execute the actual final-exit prefix, before substituting unrelated UI teardown.
exit_start = source.index('    private void ReleaseWorld(bool preserveReceiptOverlay')
exit_prefix = source[source.index('{', exit_start) + 1:source.index('        bool hadWorldState', exit_start)]
sky = (root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/GameBootstrap.Sky.cs').read_text()
mono = os.environ.get('FFO_MONO') or shutil.which('mono')
csc = os.environ.get('FFO_CSC') or shutil.which('csc')
if not mono or not csc:
    raise SystemExit('Set FFO_CSC and put Mono on PATH (see Documentation/tools/README.md).')
framework = os.environ.get('FFO_FRAMEWORK_DIR')
refs = ['-noconfig']
if framework:
    refs += ['-nostdlib+'] + ['-r:' + str(Path(framework) / name) for name in ('mscorlib.dll', 'System.dll', 'System.Core.dll')]
with tempfile.TemporaryDirectory(prefix='map-transition-contract-') as folder:
    path = Path(folder)
    (path / 'Production.cs').write_text('''using System; using System.Collections; using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets { public partial class GameBootstrap {
%s
%s
public void ExitForTest() { %s worldRoot = null; window = null; pcRef = null; CurMapId = 0; }
}
public sealed partial class ReferenceFirstWorldWindowOwner {
internal byte[] ReadConstructionMapForTest(ResLoader loader, string file) {
var resources = new TestResources { Core = loader };
var mapDescriptor = new TestMapDescriptor { file = file };
%s
return mapBytes;
}
} }
''' % (construction, transition, exit_prefix, map_read))
    (path / 'Sky.cs').write_text(sky)
    exe = path / 'Contract.exe'
    compiler = [mono, csc] if csc.lower().endswith('.exe') else [csc]
    subprocess.run(compiler + refs + ['-nologo', '-langversion:9.0', '-target:exe', '-out:' + str(exe),
        str(path / 'Production.cs'), str(path / 'Sky.cs'), str(Path(__file__).with_name('MapTransitionContract.cs')),
        str(root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/PlayerController.Startup.cs'),
        str(root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/PlayerController.TransferFrame.cs')], check=True)
    subprocess.run([mono, str(exe)], check=True)
