#!/usr/bin/env python3
"""Compile actual movement and portal sender methods with their actual host and FIFO owners."""
from pathlib import Path
import os
import subprocess
import tempfile

root = Path(__file__).resolve().parents[2]
runtime = root / 'Assets/FakeFantasyOnline/Runtime'
dotnet = Path(os.environ['FFO_DOTNET'])
sdk = dotnet.parent
compiler = sorted(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))[-1]
framework = sorted(sdk.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0'))[-1]


def method(source, signature):
    start = source.index(signature)
    opening = source.index('{', start)
    depth = 1
    position = opening + 1
    while depth:
        if source[position] == '{':
            depth += 1
        elif source[position] == '}':
            depth -= 1
        position += 1
    return source[start:position]


session = (runtime / 'Application/ClientSessionCoordinator.cs').read_text()
gateway = (runtime / 'Protocol/PrivateServerGateway.cs').read_text()
endgame = (runtime / 'Protocol/PrivateServerGateway.Endgame.cs').read_text()
wire = (runtime / 'Protocol/NetClient.cs').read_text()
production = '''using System; using System.Threading; using System.Threading.Tasks;
using FakeFantasyOnline.Foundation; using FakeFantasyOnline.Application;
namespace FakeFantasyOnline.Application { public sealed partial class ClientSessionCoordinator { %s } }
namespace FakeFantasyOnline.Protocol {
public sealed partial class PrivateServerGateway { %s %s }
public sealed partial class NetClient { %s }
}
''' % (
    method(session, '    public Task<Result> ReportPlayerMoveAsync('),
    method(gateway, '    public async Task<Result> ReportPlayerMoveAsync('),
    method(endgame, '    public bool SendEndgame('),
    '\n'.join(method(wire, signature) for signature in (
        '    public void PlayerMove(', '    private void Send(',
        '    private void PaceTx(', '    private void DrainTx(')),
)
with tempfile.TemporaryDirectory(prefix='rift-portal-wire-order-') as temporary:
    out = Path(temporary)
    extracted = out / 'ProductionSenders.cs'
    extracted.write_text(production)
    dll = out / 'Contract.dll'
    references = ['-r:' + str(path) for path in sorted(framework.glob('*.dll'))]
    sources = [runtime / 'Foundation/Result.cs', runtime / 'Foundation/HostExecutor.cs',
               runtime / 'Foundation/JsonData.cs', runtime / 'Foundation/BoundedJson.cs',
               *sorted((runtime / 'Application').glob('Endgame*.cs')),
               runtime / 'Protocol/PrivateServerEndgameCodec.cs',
               runtime / 'Protocol/NetClient.Buf.Write.cs', extracted,
               Path(__file__).with_name('RiftPortalWireOrderContract.cs')]
    subprocess.run([str(dotnet), str(compiler), '-nologo', '-noconfig', '-nostdlib+',
                    '-langversion:9.0', *references, '-out:' + str(dll),
                    *map(str, sources)], check=True)
    (out / 'Contract.runtimeconfig.json').write_text(
        '{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.0"}}}')
    subprocess.run([str(dotnet), str(dll)], check=True)
