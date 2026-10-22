#!/usr/bin/env python3
"""Compile real name-tag sources and pinned legacy sources with deterministic API stubs.
Requires .NET 10 SDK. No NuGet packages, Unity, GPU, device or frame-time measurement.
"""
from pathlib import Path
import argparse
import hashlib
import os
import shutil
import subprocess
import tempfile
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
BASELINE = '954f8d0e8f403abdd00802e2524ddd45ac92d390'
BASELINE_HASHES = {
    'BitmapFont.cs': '02fda0b0fc01813f0d51c99d4672a2d6ee626e28d95c7573cb9277c0b883f244',
    'MonsterEntity.NameTag.cs': 'e3557239ed75c6981ff741bcb155f8b431d33385f3196963bd9005979fbd5a61',
}
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--dotnet', default=os.environ.get('DOTNET') or shutil.which('dotnet'))
args = parser.parse_args()
if not args.dotnet:
    raise SystemExit('Set DOTNET or --dotnet to an installed .NET 10 SDK executable.')
env = os.environ.copy()
env.setdefault('DOTNET_CLI_HOME', str(Path(tempfile.gettempdir()) / 'ffo-dotnet-home'))
env['HOME'] = env['DOTNET_CLI_HOME']
env['XDG_RUNTIME_DIR'] = str(Path(env['DOTNET_CLI_HOME']) / 'run')
env['DOTNET_CLI_TELEMETRY_OPTOUT'] = '1'
env['DOTNET_GENERATE_ASPNET_CERTIFICATE'] = 'false'
Path(env['XDG_RUNTIME_DIR']).mkdir(parents=True, exist_ok=True)
production = [SOURCE / name for name in BASELINE_HASHES] + [SOURCE / 'GameHud.TargetNameColor.cs', SOURCE / 'ISelectable.cs']
shader = ROOT / 'Assets/FakeFantasyOnline/Runtime/Presentation/Resources/FakeFantasyOnline/ReferenceNameTag.shader'
identity = {path: hashlib.sha256(path.read_bytes()).hexdigest() for path in production + [shader]}
if 'return tex2D(_MainTex, input.uv) * input.color;' not in shader.read_text():
    raise SystemExit('Shader tint contract changed; review shader equivalence oracle before running.')
with tempfile.TemporaryDirectory(prefix='ffo-name-tag-') as directory:
    temp = Path(directory)
    (temp / 'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    legacy_paths = []
    for name, expected in BASELINE_HASHES.items():
        relative = (SOURCE / name).relative_to(ROOT)
        data = subprocess.check_output(['git', 'show', f'{BASELINE}:{relative}'], cwd=ROOT)
        actual = hashlib.sha256(data).hexdigest()
        if actual != expected:
            raise SystemExit(f'Unexpected legacy source for {name}: {actual}')
        text = data.decode().replace('class BitmapFont', 'class LegacyBitmapFont')
        text = text.replace('class MonsterEntity', 'class LegacyMonsterEntity').replace('BitmapFont.', 'LegacyBitmapFont.')
        path = temp / ('Legacy' + name)
        path.write_text(text)
        legacy_paths.append(path)
        print(f'BASELINE {name} commit={BASELINE} sha256={actual}', flush=True)
    for override in (False, True):
        target = temp / ('future-alpha' if override else 'real-colors')
        target.mkdir()
        files = [p for p in production if not (override and p.name == 'GameHud.TargetNameColor.cs')]
        files += legacy_paths + [ROOT / 'Tests/UiPerformance/NameTagContract.cs', ROOT / 'Tests/UiPerformance/NameTagContractStubs.cs']
        constants = 'NAME_TAG_COLOR_OVERRIDE' if override else ''
        items = '\n'.join(f'<Compile Include="{escape(str(path))}" />' for path in files)
        project = target / 'NameTagContract.csproj'
        project.write_text(f'''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems><ImplicitUsings>disable</ImplicitUsings>
    <Nullable>disable</Nullable><LangVersion>9.0</LangVersion><DefineConstants>{constants}</DefineConstants>
  </PropertyGroup><ItemGroup>{items}</ItemGroup>
</Project>''')
        subprocess.run([args.dotnet, 'build', str(project), '--configuration', 'Release', '--nologo', '--verbosity', 'quiet'], env=env, check=True)
        subprocess.run([args.dotnet, str(target / 'bin/Release/net10.0/NameTagContract.dll')], env=env, check=True)
for path, before in identity.items():
    after = hashlib.sha256(path.read_bytes()).hexdigest()
    if before != after:
        raise SystemExit(f'Production changed during run: {path.relative_to(ROOT)}')
    print(f'SOURCE {path.relative_to(ROOT)} sha256={after}')
print('PASS source identity unchanged throughout both test runs; production sources were compiled directly, without rewriting.')
