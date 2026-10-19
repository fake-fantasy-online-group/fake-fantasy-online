#!/usr/bin/env python3
"""Validate project wiring before compiling JNI with recovered SDK references.

Passing C# directly to Roslyn with AndroidJNIModule.dll is insufficient: Unity only
supplies that assembly when the corresponding built-in package is enabled.
This check reads the actual project manifest, lock and owning assembly definition.
It does not run Unity's package resolver or claim Player-build acceptance.
"""
import json
from pathlib import Path
import sys

JNI_PACKAGE = 'com.unity.modules.androidjni'


def validate(manifest, lock, assembly):
    version = manifest.get('dependencies', {}).get(JNI_PACKAGE)
    if version != '1.0.0':
        raise ValueError('Packages/manifest.json must enable com.unity.modules.androidjni 1.0.0 for AndroidJavaClass.')
    resolved = lock.get('dependencies', {}).get(JNI_PACKAGE, {})
    if resolved.get('version') != version or resolved.get('source') != 'builtin' or resolved.get('depth') != 0:
        raise ValueError('Packages/packages-lock.json must resolve com.unity.modules.androidjni as a direct built-in module.')
    if assembly.get('noEngineReferences', False):
        raise ValueError('Bootstrap asmdef disables the UnityEngine references needed by the JNI bridge.')
    included, excluded = assembly.get('includePlatforms', []), assembly.get('excludePlatforms', [])
    if (included and 'Android' not in included) or 'Android' in excluded:
        raise ValueError('Bootstrap asmdef must include the Android player.')


def validate_project(root):
    def read(relative):
        return json.loads((root / relative).read_text())
    validate(read('Packages/manifest.json'), read('Packages/packages-lock.json'),
             read('Assets/FakeFantasyOnline/Runtime/Bootstrap/FakeFantasyOnline.Bootstrap.asmdef'))


if __name__ == '__main__':
    root = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else Path(__file__).resolve().parents[3]
    try:
        validate_project(root)
    except (ValueError, OSError) as error:
        raise SystemExit('FAIL Unity JNI project dependency: ' + str(error))
    print('PASS Unity JNI project manifest, built-in lock and Bootstrap assembly wiring.')
