#!/usr/bin/env python3
"""Supplement executed diagnostic layouts with native UITip palette/source parity guards."""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[2]
sources = root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
native = (sources / 'UITip.cs').read_text()
diagnostic = (sources / 'ReferenceFirstWorldUiTip.cs').read_text()
checks = 0


def check(condition, message):
    global checks
    checks += 1
    assert condition, message


def palette(source):
    return source.split('public static Color32 QualityColor(', 1)[1].split('private static Color LineColor', 1)[0].split('public static Color LineColor', 1)[0]


def entry(source, label):
    match = re.search(r'\b' + label + r':\s*return new Color32\((\d+),\s*(\d+),\s*(\d+),\s*(\d+)\)', palette(source))
    return tuple(map(int, match.groups())) if match else None


for label, expected in [('case 1', (64, 224, 96, 255)),
                        ('case 2', (82, 150, 252, 255)),
                        ('case 3', (210, 90, 255, 255)),
                        ('case 5', (183, 155, 98, 255)),
                        ('default', (247, 245, 245, 255))]:
    check(entry(native, label) == entry(diagnostic, label) == expected,
          'Native and diagnostic tooltip palette mismatch: ' + label)

check("if (line.Length >= 2 && line[0] == '\\u0003')" in native,
      'Native parser must consume explicit quality zero/five markers')
check("line[1] >= '0' && line[1] <= '5'" in diagnostic,
      'Diagnostic parser must accept the supported quality range zero through five')
check(entry(diagnostic, 'case 4') == (255, 180, 40, 255),
      'Existing diagnostic ordinary quality four palette must stay unchanged')
check('case 4: return GameSettings.ColorWeak ? new Color32(255, 180, 40, 255) : new Color32(238, 222, 70, 255);' in native,
      'Native ordinary rare quality must preserve its existing color-weak behavior')
for source in [native, diagnostic]:
    check('new Color32(255, 136, 48, 255)' in source,
          'Orange direct effect color remains available')

print(f'PASS {checks} native/diagnostic tooltip palette and parser source guards')
