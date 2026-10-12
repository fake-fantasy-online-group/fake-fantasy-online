#!/usr/bin/env python3
"""Execute actual number consumption/frame projection/spawn code with deterministic Unity substitutes."""
from pathlib import Path
import argparse, os, shutil, subprocess, tempfile
root = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--source', type=Path, help='Alternate InitialFrame source for regression proof')
args = parser.parse_args()
base = root / 'Assets/FakeFantasyOnline/Runtime'
frame = (args.source or base / 'Presentation/ReferenceAssets/ReferenceFirstWorldInitialFrame.cs').read_text()
def member(source, signature):
    start = source.index(signature)
    opening = source.index('{', start)
    depth = 1
    end = opening + 1
    while depth:
        if source[end] == '{': depth += 1
        elif source[end] == '}': depth -= 1
        end += 1
    return source[start:end]
apply = member(frame, '    internal bool ApplyAuthoritativeCombatFeedbackRevision(')
# Baseline executes the old body with the new call-site signature, without altering consumption.
apply = apply.replace('long numbersVersion)', 'long numbersVersion, ClientSessionCoordinator session = null)')
ready = member(frame, '    private bool TrySpawnCombatNumberForCurrentWorld(') if '    private bool TrySpawnCombatNumberForCurrentWorld(' in frame else ''
feedback = member((base / 'Presentation/ReferenceAssets/ReferenceWorldCombatFeedback.cs').read_text(), '    public bool TrySpawnWorldNumber(')
number = member((base / 'Application/GameSessionContracts.cs').read_text(), 'public sealed class WorldCombatNumberState')
mono = os.environ.get('FFO_MONO') or shutil.which('mono')
csc = os.environ.get('FFO_CSC') or shutil.which('csc')
if not mono or not csc: raise SystemExit('Source the managed toolchain env.sh first.')
refs = ['-noconfig']
framework = os.environ.get('FFO_FRAMEWORK_DIR')
if framework:
    refs += ['-nostdlib+'] + ['-r:' + str(Path(framework) / x) for x in ('mscorlib.dll', 'System.dll', 'System.Core.dll')]
with tempfile.TemporaryDirectory(prefix='experience-replay-contract-') as folder:
    out = Path(folder)
    (out / 'Production.cs').write_text('''using System; using System.Collections.Generic; using FakeFantasyOnline.Application; using UnityEngine;
namespace FakeFantasyOnline.Application { %s }
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
public partial class ReferenceFirstWorldInitialFrame { %s %s }
internal partial class ReferenceWorldCombatFeedback { %s }
}''' % (number, apply, ready, feedback))
    files = [out / 'Production.cs', Path(__file__).with_name('ExperienceReplayContract.cs'),
        base / 'Application/WorldCombatNumberPresentation.cs',
        base / 'Application/ClientSessionCoordinator.CombatNumberPresentation.cs',
        base / 'Protocol/PrivateServerGateway.CombatNumberPresentation.cs']
    for name in ['CombatNumber.Spawn.cs', 'CombatNumber.SpawnOn.cs', 'CombatNumber.PlayerTransform.cs']:
        files.append(base / 'Presentation/ReferenceAssets' / name)
    exe = out / 'Contract.exe'
    compiler = [mono, csc] if csc.endswith('.exe') else [csc]
    subprocess.run(compiler + refs + ['-nologo', '-langversion:9.0', '-target:exe', '-out:' + str(exe)] + list(map(str,files)),check=True)
    subprocess.run([mono,str(exe)],check=True)
# Assert lifecycle integration not included in the Unity-substitute harness.
if not args.source:
    session = (base / 'Application/ClientSessionCoordinator.cs').read_text()
    gateway = (base / 'Protocol/PrivateServerGateway.cs').read_text()
    assert 'ClearCombatNumberPresentationJournal();' in member(gateway, '    private void DisposeConnectionCore(bool closeNativeConnection)')
    world_return = (base / 'Protocol/PrivateServerGateway.WorldReturn.cs').read_text()
    assert 'ClearCombatNumberPresentationJournal();' in member(world_return, '    private void ClearDepartedPlayerSnapshots(')
    assert 'ClearCombatNumberPresentationJournal();' in member(world_return, '    public void SendTitleWorldReturnForIndependentRuntime()')
    for name in ['NoteTransportDisconnected()', 'NoteTransportDisconnectedFromWorld()', 'ReturnToLogin()', 'DisposeWithGatewayReleaseForIndependentRuntime(Action releaseGateway)']:
        assert 'ResetCombatNumberPresentation();' in member(session, '    public void ' + name), name
    assert 'combatNumbersVersion,\n                session))' in frame, 'live poll must use connection consumer'
    assert 'standaloneCombatNumbers.Clear();' in frame
    print('PASS experience lifecycle reset and live-poll wiring')
