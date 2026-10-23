#!/usr/bin/env python3
"""Derive a private parser fixture from original ov_skillshow rows; never fetch assets.

The BIN is not the published compact JSON. This deterministic projection retains the
original effect IDs and delays and maps the four schema phase groups to the native
SkillCaster suffixes 40/20/30/50. Position fields are metadata, never phase suffixes.
"""
import argparse, hashlib, json, pathlib, re, struct
parser = argparse.ArgumentParser()
parser.add_argument('--bin', required=True)
parser.add_argument('--script', required=True)
parser.add_argument('--output-dir', required=True)
a = parser.parse_args()
source, script, out = pathlib.Path(a.bin), pathlib.Path(a.script), pathlib.Path(a.output_dir)
raw = source.read_bytes()
magic, version, size, count = struct.unpack_from('<4I', raw)
assert (magic, version, size) == (0x5566, 1, 100)
assert len(raw) == 16 + size * count
wanted = {10319: (47, 550), 10409: (40, 370)}
rows, shows = [], []
script_text = script.read_bytes().decode('gbk')
for index in range(count):
    row = raw[16+index*size:16+(index+1)*size]
    skill, = struct.unpack_from('<i', row)
    if skill not in wanted:
        continue
    values = struct.unpack_from('<16i', row, 36)
    phases = []
    evidence = []
    for suffix, offset in [(40, 3), (20, 6), (30, 9), (50, 12)]:
        effect, position, delay = values[offset:offset+3]
        if effect <= 0:
            continue
        phases.append({'s': suffix, 'e': effect, 'd': delay})
        evidence.append({'phase': suffix, 'effect': effect, 'position': position,
                         'delayMS': delay, 'asset': f'TFX{effect:03d}_{suffix}'})
    expected, delay = wanted[skill]
    assert phases == [{'s': 20, 'e': expected, 'd': delay}, {'s': 30, 'e': expected, 'd': 0}]
    assert not re.search(r'function\s+Skill'+str(skill)+r'\s*\(', script_text)
    shows.append({'id': skill, 'phy': values[0], 'ph': phases})
    rows.append({'skill': skill, 'row': index, 'name': row[4:36].split(b'\0')[0].decode('gbk'),
                 'rowSHA256': hashlib.sha256(row).hexdigest(), 'phases': evidence,
                 'customSkillScript': False})
assert len(shows) == len(wanted), 'Missing or duplicate original rows'
out.mkdir(parents=True, exist_ok=True)
(out/'native-child-skillshow-fixture.json').write_text(json.dumps({'shows': shows}, ensure_ascii=False, indent=2)+'\n')
report = {'source': str(source), 'sourceSHA256': hashlib.sha256(raw).hexdigest(),
          'header': [magic, version, size, count], 'script': str(script),
          'scriptSHA256': hashlib.sha256(script.read_bytes()).hexdigest(), 'rows': rows,
          'scope': 'Original BIN-derived fixture for real SkillEffectDB parser; not the published compact JSON. No Unity rendering or native sound-table validation.'}
(out/'native-child-skillshow-fixture-provenance.json').write_text(json.dumps(report, ensure_ascii=False, indent=2)+'\n')
print(out/'native-child-skillshow-fixture.json')
