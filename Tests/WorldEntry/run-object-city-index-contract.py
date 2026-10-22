#!/usr/bin/env python3
"""Run complete production package/path owners against synthetic packages.

Runs untouched production sources first, then the same sources with generated,
test-only loop counters. No Unity Player, Android device or real-asset scan.
"""
from pathlib import Path
import argparse
import os
import shutil
import subprocess
import tempfile
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--object-paths-source', type=Path, help='Alternate ObjectPaths source for regression proof')
parser.add_argument('--probes-only', action='store_true', help='Run only the generated loop-counter variant')
args = parser.parse_args()
DOTNET = os.environ.get('FFO_DOTNET') or shutil.which('dotnet')
if not DOTNET:
    raise SystemExit('Set FFO_DOTNET to an installed official .NET SDK dotnet executable.')


def instrument(source, method, counter):
    start = source.index(method)
    loop = source.index('foreach (string path in index.Keys)', start)
    body = source.index('{', loop) + 1
    return source[:body] + '\n                ObjectCityIndexProbe.' + counter + '++;' + source[body:]


with tempfile.TemporaryDirectory(prefix='ffo-city-index-contract-') as temporary:
    folder = Path(temporary)
    (folder / 'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    archive = (SOURCE / 'PkgArchive.cs').read_text()
    for probes in ((True,) if args.probes_only else (False, True)):
        production = folder / 'PkgArchive.cs'
        copied = archive
        if probes:
            copied = instrument(copied, 'internal IReadOnlyList<string> FindObjectCandidates(', 'IndexKeys')
            copied = instrument(copied, 'public List<string> FindAll(', 'LegacyKeys')
        production.write_text(copied)
        sources = [production, args.object_paths_source or SOURCE / 'ResLoader.ObjectPaths.cs', SOURCE / 'Gbk.cs',
                   Path(__file__).with_name('ObjectCityIndexContract.cs')]
        items = ''.join('<Compile Include="' + escape(str(path), {'"': '&quot;'}) + '" />' for path in sources)
        (folder / 'Contract.csproj').write_text(
            '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
            '<OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework>'
            '<EnableDefaultCompileItems>false</EnableDefaultCompileItems><LangVersion>9.0</LangVersion>'
            '<ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable>'
            '<DefineConstants>' + ('INDEX_PROBES' if probes else '') + '</DefineConstants>'
            '</PropertyGroup><ItemGroup>' + items + '</ItemGroup></Project>')
        print('Running ' + ('test-only loop counters' if probes else 'unchanged production sources'), flush=True)
        subprocess.run([DOTNET, 'run', '--project', str(folder / 'Contract.csproj'), '--configuration', 'Release'],
                       cwd=folder, check=True)
