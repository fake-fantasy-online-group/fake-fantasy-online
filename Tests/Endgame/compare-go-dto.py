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
cs = '\n'.join(p.read_text() for p in (root/'Assets/FakeFantasyOnline/Runtime/Application').glob('Endgame*.cs'))
classes = {}
for name, body in re.findall(r'public sealed class (Endgame\w+)(.*?)(?=public sealed class |\Z)', cs, re.S):
    classes[name] = body
count = 0
for name, body in re.findall(r'type (Endgame\w+) struct \{(.*?)\n\}', go, re.S):
    if not (name.endswith('View') or name == 'EndgameResponse'):
        continue
    assert name in classes, f'{name}: missing client class'
    client = classes[name]
    for typ, key in re.findall(r'\w+\s+(\S+)\s+`json:"([^",]+)(?:,[^"]*)?"`', body):
        expected = {'int32': ('Int',), 'bool': ('Bool',), 'string': ('String','ID'), 'map[string]string': ('StringMap',)}.get(typ)
        if typ.startswith('[]'):
            expected = ('List',)
        elif typ.startswith('*'):
            expected = ('Optional',)
        assert expected, f'{name}.{key}: unsupported Go type {typ}'
        assert any(re.search(r'EndgameData\.'+method+r'\(d, "'+key+r'"', client) for method in expected), f'{name}.{key}: Go {typ}, expected client {expected}'
        if typ.startswith('[]Endgame') or typ.startswith('*Endgame'):
            target = typ[2:] if typ.startswith('[]') else typ[1:]
            assert re.search(r'EndgameData\.(?:List|Optional)\(d, "'+key+r'"[^;]*new '+target+r'\(', client), f'{name}.{key}: wrong nested DTO'
        if typ == '[]int32':
            assert re.search(r'EndgameData\.List\(d, "'+key+r'"[^;]*EndgameData\.Integer\(', client), f'{name}.{key}: wrong integer array'
        if typ == '[]string':
            assert re.search(r'EndgameData\.List\(d, "'+key+r'"[^;]*EndgameData\.Text\(', client), f'{name}.{key}: wrong string array'
        count += 1
print(f'PASS {count} Go/client DTO field-type contracts (all response DTO fields).')
