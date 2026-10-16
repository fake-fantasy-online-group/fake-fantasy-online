#!/usr/bin/env python3
"""Compile real map rebuild methods and doll binding against deterministic map substitutes.
Run with the same FFO_MONO/FFO_CSC/FFO_FRAMEWORK_DIR as run-map-transition-contract.py.
--source accepts a baseline or candidate GameBootstrap.cs without editing production.
"""
from pathlib import Path
import os, tempfile
root = Path(os.environ.get('FFO_SOURCE_ROOT', Path(__file__).resolve().parents[2]))
tests = root / 'Tests/WorldEntry'
source = root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
original = Path(__file__).with_name('MapTransitionContract.cs').read_text()
original = original.replace('Builds.Add(projected);', 'Builds.Add(projected); AppearanceRepro.Bind(projected.Appearance);')
original = original.replace('WorldEntryRecord CaptureNativeWorldEntryForIndependentRuntime() => entry;', 'WorldEntryRecord CaptureNativeWorldEntryForIndependentRuntime() => new WorldEntryRecord { Appearance = new CharacterAppearanceRecord { Body = CharData.apBody, RightWeapon = CharData.apWeaponR } };')
doll = (source / 'ReferenceWorldLocalPlayerDoll.cs').read_text()
assert 'BindBaseAppearanceForIndependentRuntime(character.Appearance);' in doll
start = doll.index('    private static void BindBaseAppearanceForIndependentRuntime(')
binding = doll[start:doll.index(';', start)+1].replace('private static void BindBaseAppearanceForIndependentRuntime', 'public static void Bind')
extra = Path(__file__).with_name('EquipmentAppearanceTransferContract.cs').read_text()
extra = extra.replace('using FakeFantasyOnline.Presentation.ReferenceAssets;', '')
original += '\n'+extra+'\nnamespace FakeFantasyOnline.Presentation.ReferenceAssets { public static partial class AppearanceRepro {\n'+binding+'\n} }\n'
runner_path = tests / 'run-map-transition-contract.py'
runner = runner_path.read_text()
with tempfile.TemporaryDirectory(prefix='equipment-appearance-contract-') as directory:
    stub = Path(directory) / 'EquipmentStubs.cs'
    stub.write_text(original)
    runner = runner.replace("stub=Path(__file__).with_name('MapTransitionContract.cs')", 'stub=Path('+repr(str(stub))+')')
    runner = runner.replace('command += [str(production)', "command += ['-main:FakeFantasyOnline.Presentation.ReferenceAssets.AppearanceRepro',str(production)")
    exec(compile(runner, str(runner_path), 'exec'), {'__file__': str(runner_path), '__name__': '__main__'})
