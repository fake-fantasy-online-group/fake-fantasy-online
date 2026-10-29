#!/usr/bin/env python3
"""Compile the actual travel row predicate, lookup, and untouched native entry callback."""
from pathlib import Path
import hashlib, os, subprocess, tempfile
ROOT=Path(__file__).resolve().parents[2]
P=ROOT/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
def method(s,signature):
    a=s.index(signature);b=s.index('{',a)+1;depth=1
    while depth:
        depth+=(s[b]=='{')-(s[b]=='}');b+=1
    return s[a:b]
menu=(P/'DialogUI.cs').read_text()
start=menu.index('        if (curTrans > 0')
end=menu.index('        if (!TryAddDungeonTransportOptions()) AddDungeonEntryOptions();',start)+len('        if (!TryAddDungeonTransportOptions()) AddDungeonEntryOptions();')
rows=menu[start:end]
assert rows.count('"传送"')==1 and rows.count('AddDungeonEntryOptions();')==1
callback=method((P/'DialogUI.Travel.cs').read_text(),'    private void AddDungeonEntryOptions()')
# Frozen current-main callback, not a golden for future quest-policy work. A changed
# callback requires an explicit audit update rather than silently widening eligibility.
assert hashlib.sha256(callback.encode()).hexdigest()=='6ea8abec802151fb8dd84dee75fb5074f5a660a8542b4b95fc097f14ee33e2f4'
source='using UnityEngine; namespace FakeFantasyOnline.Presentation.ReferenceAssets { public sealed partial class DialogUI { private void BuildTravelRows() {\n'+rows+'\n}\n'+callback+'\n}}'
with tempfile.TemporaryDirectory() as tmp:
    tmp=Path(tmp);(tmp/'Actual.cs').write_text(source);exe=tmp/'Contract.exe'
    contracts=(ROOT/'Assets/FakeFantasyOnline/Runtime/Application/GameSessionContracts.cs').read_text()
    states='using System; using System.Collections.Generic; using System.Collections.ObjectModel; namespace FakeFantasyOnline.Application {'+method(contracts,'public sealed class WorldTransportDestinationState')+method(contracts,'public sealed class WorldTransportState')+'}'
    (tmp/'States.cs').write_text(states)
    assert 'ResetDungeonTransport();' in menu and 'BeginDungeonTransport();' in menu
    adapter=(P/'ReferenceFirstWorldNpcFunctionDialogs.cs').read_text()
    assert 'if (DialogUI.ConsumeDungeonTransport(next)) return true;' in adapter
    assert 'DoTeleport' not in (P/'DialogUI.DungeonTransport.cs').read_text()

    mono=os.environ.get('FFO_MONO') or str(Path(os.environ['FFO_MONO_ROOT'])/'bin/mono')
    refs=['-r:'+str(Path(os.environ['FFO_FRAMEWORK_DIR'])/n) for n in ('mscorlib.dll','System.dll','System.Core.dll')]
    subprocess.run([mono,os.environ['FFO_CSC'],'-noconfig','-nostdlib+','-nologo','-langversion:9.0','-out:'+str(exe),*refs,str(tmp/'Actual.cs'),str(tmp/'States.cs'),str(P/'DialogUI.DungeonTransport.cs'),str(P/'DungeonEntrances.Lookup.cs'),str(P/'DungeonEntrances.NestedData.cs'),str(Path(__file__).with_name('DungeonEntryContract.cs')),str(Path(__file__).with_name('AuthoritativeRouteFixture.cs'))],check=True)
    subprocess.run([mono,str(exe)],check=True)
