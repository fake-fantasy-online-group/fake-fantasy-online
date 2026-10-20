#!/usr/bin/env python3
"""Compile actual SysMsgBox lifecycle/drawing against minimal Unity API stubs.
Requires a .NET runtime and Roslyn csc.dll; does not launch Unity or simulate a phone.
"""
import argparse, json, pathlib, subprocess, tempfile
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--dotnet', required=True)
p.add_argument('--csc', required=True)
a=p.parse_args()
repo=pathlib.Path(__file__).resolve().parents[2]
src=repo/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
listing=subprocess.check_output([a.dotnet,'--list-runtimes'],text=True)
line=next(x for x in listing.splitlines() if x.startswith('Microsoft.NETCore.App '))
_,version,location=line.split(' ',2)
refs=pathlib.Path(location.strip('[]'))/version
with tempfile.TemporaryDirectory(prefix='modal-foreground-') as temp:
    temp=pathlib.Path(temp)
    out=temp/'Contract.dll'
    args=['-nologo','-target:exe','-out:'+str(out)]
    args+=['-r:'+str(x) for x in refs.glob('*.dll')]
    args+=[str(pathlib.Path(__file__).with_name('ForegroundContract.cs')),
           str(src/'SysMsgBox.cs'),str(src/'SysMsgBox.Drawing.cs')]
    rsp=temp/'args.rsp';rsp.write_text('\n'.join('"'+v+'"' for v in args))
    subprocess.run([a.dotnet,a.csc,'@'+str(rsp)],check=True)
    (temp/'Contract.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{
        'framework':{'name':'Microsoft.NETCore.App','version':version}}}))
    subprocess.run([a.dotnet,str(out)],check=True)
