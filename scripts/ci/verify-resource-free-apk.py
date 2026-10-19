#!/usr/bin/env python3
"""Structural check only: not an Android installation or runtime acceptance test."""
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import zipfile

ROOT = Path(__file__).resolve().parents[2]


def setting(name):
    text = (ROOT / 'ProjectSettings/ProjectSettings.asset').read_text()
    match = re.search(r'^  ' + re.escape(name) + r':\s*(\S+)\s*$', text, re.M)
    if not match:
        raise ValueError('Missing PlayerSettings field: ' + name)
    return match.group(1)


def verify(apk):
    apk = Path(apk)
    app_version = setting('bundleVersion')
    with zipfile.ZipFile(apk) as archive:
        names = archive.namelist()
        if len(set(names)) != len(names):
            raise ValueError('APK contains duplicate entries')
        for name in names:
            if name.startswith('assets/') and not name.endswith('/'):
                if not (name == 'assets/resource-bootstrap.json' or name.startswith('assets/bin/Data/')):
                    raise ValueError('Unexpected embedded Android asset: ' + name)
            if name.startswith('lib/') and not name.startswith('lib/arm64-v8a/') and not name.endswith('/'):
                raise ValueError('Unexpected native ABI: ' + name)
        for library in ['lib/arm64-v8a/libil2cpp.so', 'lib/arm64-v8a/libunity.so']:
            if archive.getinfo(library).file_size == 0:
                raise ValueError('Empty native library: ' + library)
        marker = archive.getinfo('assets/resource-bootstrap.json')
        if marker.file_size > 16384:
            raise ValueError('Oversized bootstrap config')
        config = json.loads(archive.read(marker).decode('utf-8-sig'))
        if config.get('schemaVersion') != 1 or config.get('mode') != 'resource-free':
            raise ValueError('Invalid resource-free bootstrap config')
        if config.get('appVersion') != app_version:
            raise ValueError('APK bootstrap version differs from the single application version source')
    digest = hashlib.sha256()
    with apk.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            digest.update(chunk)
    commit = os.environ.get('GITHUB_SHA') or subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip()
    if not re.fullmatch(r'[a-fA-F0-9]{40}', commit):
        raise ValueError('Source commit must be an immutable 40-character Git SHA')
    protocol = re.search(r'public const string ClientVersion = "([^"]+)";', (ROOT / 'Assets/FakeFantasyOnline/Runtime/Protocol/NetClient.cs').read_text()).group(1)
    metadata = {
        'schemaVersion': 1,
        'status': 'development-candidate',
        'appVersion': app_version,
        'sourceCommit': commit,
        'unityVersion': '6000.4.9f1',
        'target': 'Android',
        'verifiedNativeAbi': 'arm64-v8a',
        'verifiedIl2CppLibrary': True,
        'configuredAndroidVersionCode': int(setting('AndroidBundleVersionCode')),
        'configuredApplicationId': 'org.fakefantasyonline.resourcefreecandidate',
        'signing': 'Unity development default; certificate not verified by this script',
        'loginProtocolMarker': protocol,
        'serverCompatibility': 'not verified; application version does not migrate login protocol',
        'resourceMode': 'resource-free',
        'bytes': apk.stat().st_size,
        'sha256': digest.hexdigest(),
        'acceptance': {'apkStructure': True, 'deviceLaunch': False, 'resourceDownload': False, 'productionReady': False},
    }
    Path(str(apk) + '.build.json').write_text(json.dumps(metadata, ensure_ascii=False, indent=2) + '\n')
    Path(str(apk) + '.sha256').write_text(digest.hexdigest() + '  ' + apk.name + '\n')
    print('PASS resource-free APK structure, ARM64 IL2CPP presence, bootstrap version, and SHA-256. Runtime acceptance remains required.')
    return metadata


if __name__ == '__main__':
    if len(sys.argv) != 2:
        raise SystemExit('Usage: verify-resource-free-apk.py path/to/candidate.apk')
    verify(sys.argv[1])
