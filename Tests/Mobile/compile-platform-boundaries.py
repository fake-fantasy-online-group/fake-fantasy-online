#!/usr/bin/env python3
"""Compile Bootstrap and Editor against recovered Unity APIs, never execute Unity/build an APK.
Run engine-free compilation and compile-current-presentation.py first.
"""
import os
from pathlib import Path
import subprocess

root = Path(__file__).resolve().parents[2]
deps = Path(os.environ['FFO_COMPILE_DIR'])
presentation = Path(os.environ.get('FFO_PRESENTATION_COMPILE_DIR', str(deps.parent / 'dotnet-presentation-compile')))
out = deps.parent / 'mobile-boundary-compile'
out.mkdir(exist_ok=True)
settings = []
for line in (presentation / 'current-presentation.rsp').read_text().splitlines():
    if line.startswith(('-out:', '-refout:', '-analyzer:', '/additionalfile:', '-additionalfile:')) or line.strip('"').endswith('.cs'):
        continue
    settings.append(line)
for path in deps.glob('*.dll'):
    entry = '-r:"' + str(path) + '"'
    if entry not in settings:
        settings.append(entry)
settings.append('-r:"' + str(presentation / 'FakeFantasyOnline.Presentation.dll') + '"')
for name, folder in [('Bootstrap', 'Runtime/Bootstrap'), ('Editor', 'Editor')]:
    sources = sorted((root / 'Assets/FakeFantasyOnline' / folder).glob('*.cs'))
    response = out / (name + '.rsp')
    response.write_text('\n'.join(settings + ['-out:"' + str(out / ('FakeFantasyOnline.' + name + '.dll')) + '"'] + ['"' + str(p) + '"' for p in sources]) + '\n')
    result = subprocess.run([os.environ['FFO_DOTNET'], 'exec', os.environ['FFO_DOTNET_CSC'], '/nostdlib', '/noconfig', '@' + str(response)], cwd=root, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    (out / (name + '.log')).write_text(result.stdout)
    print(result.stdout, end='')
    if result.returncode:
        raise SystemExit(result.returncode)
    print('PASS complete current ' + name + ' source compilation against recovered Unity APIs (no Unity execution).')
