#!/usr/bin/env python3
"""Compare every Go response DTO field against the client's explicit typed JSON read.
Usage: python3 compare-go-dto.py /path/to/server/internal/domain/endgame_contract.go
This is source-level schema validation, separate from production encoder fixtures.
"""
import re
import sys
from pathlib import Path
root = Path(__file__).resolve().parents[2]
go = Path(sys.argv[1]).read_text()
for file in ('skill_progression_v5.go', 'equipment_views_v5.go'):
    companion = Path(sys.argv[1]).with_name(file)
    if companion.exists(): go += '\n' + companion.read_text()
renames = {'SkillRankView': 'EndgameSkillRankView', 'SkillRankBonus': 'EndgameSkillContribution',
    'SkillTrainingSynergyView': 'EndgameTrainingSynergyView',
    'EndgameCombatStatsV5': 'EndgameCombatStatsView', 'EndgameElementStatsV5': 'EndgameElementCombatView',
    'EquipmentAuraView': 'EndgameEquipmentAuraView', 'EquipmentProcReadinessView': 'EndgameProcReadinessView'}
cs = '\n'.join(p.read_text() for p in (root/'Assets/FakeFantasyOnline/Runtime/Application').glob('Endgame*.cs'))
classes = {}
for name, body in re.findall(r'public sealed class (Endgame\w+)(.*?)(?=public sealed class |\Z)', cs, re.S):
    classes[name] = body
count = 0
for name, body in re.findall(r'type (\w+) struct \{(.*?)\n\}', go, re.S):
    if not (name.endswith('View') or name == 'EndgameResponse' or name in renames):
        continue
    name = renames.get(name, name)
    assert name in classes, f'{name}: missing client class'
    client = classes[name]
    for typ, key, options in re.findall(r'\w+\s+(\S+)\s+`json:"([^",]+)([^"]*)"`', body):
        if key == "-": continue  # Go explicitly excludes this internal field from the wire.
        expected = {'int32': ('Int',), 'SkillID': ('Int',), 'EntityID': ('UInt',), 'int64': ('Long',), 'bool': ('Bool',), 'string': ('String','ID'), 'map[string]string': ('StringMap',)}.get(typ)
        if ',string' in options: expected = ('String', 'ID')
        if typ.startswith('[]'):
            expected = ('List',)
        elif typ.startswith('*'):
            expected = ('Optional',)
        assert expected, f'{name}.{key}: unsupported Go type {typ}'
        assert any(re.search(r'EndgameData\.'+method+r'\(d, "'+key+r'"', client) for method in expected), f'{name}.{key}: Go {typ}, expected client {expected}'
        target = typ[2:] if typ.startswith('[]') else typ[1:] if typ.startswith('*') else ''
        if target.startswith('Endgame') or target in renames:
            target = renames.get(target, target)
            assert re.search(r'EndgameData\.(?:List|Optional)\(d, "'+key+r'"[^;]*new '+target+r'\(', client), f'{name}.{key}: wrong nested DTO'
        if typ == '[]int32':
            assert re.search(r'EndgameData\.List\(d, "'+key+r'"[^;]*EndgameData\.Integer\(', client), f'{name}.{key}: wrong integer array'
        if typ == '[]string':
            assert re.search(r'EndgameData\.List\(d, "'+key+r'"[^;]*EndgameData\.Text\(', client), f'{name}.{key}: wrong string array'
        count += 1
print(f'PASS {count} Go/client DTO field-type contracts (all response DTO fields).')
