#!/usr/bin/env python3
"""Transfer verified development resources to an explicitly selected, authorized Android device.
Obtain --remote-root from the game's FFO_MOBILE_RESOURCE_ROOT log line; do not guess it.
Does not install APKs, enable debugging, change device permissions, or contact a server.
"""
import argparse
import hashlib
from pathlib import Path
import re
import subprocess


def digest(path):
    h = hashlib.sha256()
    with path.open('rb') as f:
        for block in iter(lambda: f.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


def safe_relative(value):
    parts = value.split('/')
    if not value or any(x in value for x in '\\:\t\r\n\0?#%') or any(
            x in ('', '.', '..') or x.endswith(('.', ' ')) or x.lower().endswith('.part') for x in parts):
        raise ValueError('Unsafe resource path')
    return value


def entries(manifest):
    raw = manifest.read_bytes()
    if len(raw) > 8 * 1024 * 1024:
        raise ValueError('Manifest too large')
    lines = raw.decode('utf-8').splitlines()
    if not lines or lines[0] != 'FFO-MOBILE-OFFLINE-1':
        raise ValueError('Expected APK-produced offline manifest')
    rows, destinations = [], set()
    for line in lines[1:]:
        source, target, length, sha = line.split('\t')
        safe_relative(source); safe_relative(target)
        if target.lower() in destinations or not length.isdecimal() or not re.fullmatch('[0-9a-f]{64}', sha):
            raise ValueError('Invalid or duplicate manifest entry')
        destinations.add(target.lower())
        rows.append((source, target, int(length), sha))
    if not rows or len(rows) > 100000:
        raise ValueError('Empty or excessive manifest')
    return hashlib.sha256(raw).hexdigest(), rows


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--adb', default='adb')
    p.add_argument('--device', required=True, help='Explicit serial from adb devices')
    p.add_argument('--manifest', type=Path, required=True, help='Exact .apk.resources.tsv output')
    p.add_argument('--source', type=Path, required=True, help='Original Assets/StreamingAssets directory')
    p.add_argument('--remote-root', required=True, help='Exact runtime FFO_MOBILE_RESOURCE_ROOT value')
    a = p.parse_args()
    version, rows = entries(a.manifest)
    # Restrict all shell arguments and app scope, while accommodating actual runtime storage roots.
    if not re.fullmatch(r'/storage/(?:emulated/[0-9]+|self/primary)/Android/data/[A-Za-z0-9_.]+/files/MobileResources/' + version, a.remote_root):
        raise ValueError('Remote root must be the runtime app-scoped path for this exact manifest version')
    if not re.fullmatch(r'[A-Za-z0-9_.:/-]+', a.device):
        raise ValueError('Invalid device serial')
    adb = [a.adb, '-s', a.device]
    def run(*args):
        return subprocess.run(adb + list(args), check=True, text=True, stdout=subprocess.PIPE).stdout.strip()
    if run('get-state') != 'device':
        raise RuntimeError('Explicit device not connected and authorized')
    # Validate all host bytes before touching device data. Source links are intentional in this project.
    for source, _, length, sha in rows:
        f = a.source / source
        if not f.is_file() or f.stat().st_size != length or digest(f) != sha:
            raise ValueError('Host resource mismatch: ' + source)
    for i, (source, target, _, sha) in enumerate(rows):
        remote = a.remote_root + '/' + target
        # POSIX single-quote escaping: file names are manifest-validated but may contain spaces.
        quote = lambda s: "'" + s.replace("'", "'\\''") + "'"
        parent = remote.rsplit('/', 1)[0]
        run('shell', 'mkdir -p ' + quote(parent))
        run('push', str(a.source / source), remote + '.part')
        actual = run('shell', 'toybox sha256sum ' + quote(remote + '.part')).split()[0]
        if actual != sha:
            raise RuntimeError('Device checksum mismatch; partial file retained: ' + target)
        run('shell', 'mv ' + quote(remote + '.part') + ' ' + quote(remote))
        print(f'{i + 1}/{len(rows)} verified: {target}', flush=True)
    print('Transfer verified. Restart the game; the APK independently verifies every file before activation.')


if __name__ == '__main__':
    main()
