#!/usr/bin/env python3
"""Run production EFF alias/cache/parser/resource tests, optionally with a private corpus.

No assets are required by the portable suite. Private audit inputs are read in place;
all generated manifests and compiler outputs live in a deleted temporary directory.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import tempfile


def member(source, signature):
    """Extract these simple production methods verbatim, not a reimplementation."""
    start = source.index(signature)
    opening = source.index('{', start)
    position, depth = opening + 1, 1
    while depth:
        if source[position] == '{':
            depth += 1
        elif source[position] == '}':
            depth -= 1
        position += 1
    return source[start:position]


def private_cases(audit, originals, wrappers):
    rows = json.loads(audit.read_text())
    assert len(rows) == 186, 'Private audit must contain the complete 186-alias corpus'
    result = []
    for row in rows:
        source = originals / (row['source_effect'] + '.eff')
        data = source.read_bytes()
        assert hashlib.sha256(data).hexdigest() == row['source_sha256'], 'Original hash mismatch: ' + row['source_effect']
        kind = row['classification']
        assert kind in ('exact-original-reuse', 'filtered-single-sprite-subset'), kind
        magic = re.fullmatch(r'magic([0-9]+)', row['asset'])
        assert magic, row['asset']
        result.append({
            'alias': row['effect'],
            'original': row['source_effect'],
            'selectedMagicId': int(magic[1]) if kind == 'filtered-single-sprite-subset' else 0,
            'sourceText': data.decode('latin1'),
            'expectedText': (wrappers / (row['effect'] + '.eff')).read_bytes().decode('latin1'),
        })
    assert len({row['alias'] for row in result}) == 186, 'Duplicate private audit alias'
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet-root', type=Path, default=Path(os.environ.get('FFO_DOTNET_ROOT', '/tmp/ffo-dotnet')))
    parser.add_argument('--audit', type=Path, help='Optional private wrappers.json audit')
    parser.add_argument('--original-effects-dir', type=Path, help='Existing q21 effect directory, read only')
    parser.add_argument('--approved-wrappers-dir', type=Path, help='Existing approved LP EFF directory, read only')
    args = parser.parse_args()
    optional = (args.audit, args.original_effects_dir, args.approved_wrappers_dir)
    if any(optional) and not all(optional):
        parser.error('Pass all three private corpus options together')
    here = Path(__file__).resolve().parent
    root = here.parents[1]
    production = root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
    sources = [production / 'EffectPlayer.cs', production / 'EffectPlayer.LegendaryAliases.cs',
               here / 'NativeEffectReuseStubs.cs', here / 'NativeEffectReuseContract.cs']
    for source in sources:
        if not source.is_file():
            raise SystemExit('Missing source: ' + str(source))
    # The runtime switch is intentionally unassigned readonly bool, hence false.
    skill_sources = '\n'.join(p.read_text() for p in production.glob('SkillEffectDB*.cs'))
    assert re.search(r'public\s+static\s+readonly\s+bool\s+Use05Fx\s*;', skill_sources), 'Production Use05Fx must retain readonly false default'
    assert not re.search(r'\bUse05Fx\s*=(?!=)', skill_sources), 'Production Use05Fx gained an assignment; revisit package-priority tests'
    resolver = (production / 'ResLoader.cs').read_text()
    methods = '\n'.join(member(resolver, signature) for signature in (
        '    public byte[] ReadRes24First(', '    public static byte[] Read24Res('))
    dotnet = args.dotnet_root
    sdk = sorted((dotnet / 'sdk').glob('*/Roslyn/bincore/csc.dll'))[-1]
    refs = sorted((dotnet / 'packs/Microsoft.NETCore.App.Ref').glob('*/ref/net8.0'))[-1]
    version = sorted((dotnet / 'shared/Microsoft.NETCore.App').iterdir())[-1].name
    with tempfile.TemporaryDirectory(prefix='native-effect-reuse-') as temporary:
        out = Path(temporary)
        resolver_file = out / 'ProductionResourceResolvers.cs'
        resolver_file.write_text('using System; using System.IO;\nnamespace FakeFantasyOnline.Presentation.ReferenceAssets { internal partial class ResLoader {\n' + methods + '\n} }\n')
        sources.append(resolver_file)
        assembly = out / 'NativeEffectReuse.dll'
        compiler_args = ['-nologo', '-langversion:9.0', '-nostdlib+', '-target:exe',
                         '-nowarn:0169,0067,0414,0649', '-out:' + str(assembly)]
        compiler_args += ['-r:' + str(p) for p in refs.glob('*.dll')]
        compiler_args += list(map(str, sources))
        (out / 'NativeEffectReuse.runtimeconfig.json').write_text(json.dumps({
            'runtimeOptions': {'tfm': 'net8.0', 'framework': {'name': 'Microsoft.NETCore.App', 'version': version}}}))
        subprocess.run([str(dotnet / 'dotnet'), 'exec', str(sdk), *compiler_args], check=True)
        run_args = []
        if all(optional):
            manifest = out / 'private-corpus.json'
            manifest.write_text(json.dumps(private_cases(*optional)), encoding='utf8')
            run_args.append(str(manifest))
        subprocess.run([str(dotnet / 'dotnet'), 'exec', str(assembly), *run_args], check=True)
    print('PASS production Use05Fx readonly-false guard; production ReadRes24First/Read24Res executed with recording I/O boundaries')
    print('LIMIT: no Unity renderer/playback, real PKG decompression, or packaged build executed')


if __name__ == '__main__':
    main()
