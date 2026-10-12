#!/usr/bin/env python3
"""Compile the actual Win05 icon methods with deterministic GUI draw-recording stubs.
No Unity process, external artwork, or graphics device is used.
"""
from pathlib import Path
import os
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/Win05.SkillDialog.cs'


def method(source, signature):
    start = source.index(signature)
    opening = source.index('{', start)
    depth = 1
    index = opening + 1
    while depth:
        if source[index] == '{':
            depth += 1
        elif source[index] == '}':
            depth -= 1
        index += 1
    return source[start:index]


if __name__ == '__main__':
    mono = Path(os.environ['FFO_MONO_ROOT']) / 'bin/mono'
    compiler = Path(os.environ['FFO_CSC'])
    framework = Path(os.environ['FFO_FRAMEWORK_DIR'])
    source = SOURCE.read_text()
    ranks = (ROOT / 'Assets/FakeFantasyOnline/Runtime/Application/EndgameSkills.cs').read_text()
    assert 'CanCast != (EffectiveRank != "0")' in ranks, 'CanCast must remain a learned-rank flag, including passives'
    assert 'bool canUse = UsesGrowthSkill(slot.id) ? rank?.CanCast == true : currentLevel > 0;' in source, 'Icon state must use learned rank, not active-cast admission'
    assert 'DrawSkillIcon(iconRect, icon, canUse, tree != null);' in source, 'Production call must pass actual tree availability'
    assert source.index('if (tree)\n        {\n            GUI.DrawTexture(') < source.index('DrawSkillIcon(iconRect'), 'Authored tree must precede node overlays'
    assert 'if (canUse && BitmapFont.Ready)' in source, 'Learned level display must stay enabled'
    assert 'if (CanShowUpgrade(slot.id))' in source, 'Upgrade behavior must remain independent of inactive artwork'
    methods = '\n'.join(method(source, signature) for signature in (
        'private static Rect Fit(', 'private static void DrawSkillIcon('))
    with tempfile.TemporaryDirectory(prefix='skill-icon-contract-') as output:
        output = Path(output)
        production = output / 'Win05.IconMethods.cs'
        production.write_text('using UnityEngine;\nnamespace FakeFantasyOnline.Presentation.ReferenceAssets {\n'
                              'public partial class Win05 {\n' + methods + '\n}}\n')
        exe = output / 'SkillIconContract.exe'
        subprocess.run([str(mono), str(compiler), '-nologo', '-noconfig', '-nostdlib+',
                        '-langversion:9.0', '-out:' + str(exe)]
                       + ['-r:' + str(framework / name) for name in ('mscorlib.dll', 'System.dll', 'System.Core.dll')]
                       + [str(production), str(ROOT / 'Tests/UiArtwork/SkillIconContract.cs')], check=True)
        subprocess.run([str(mono), str(exe)], check=True)
