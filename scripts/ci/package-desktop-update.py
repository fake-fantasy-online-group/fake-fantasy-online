#!/usr/bin/env python3
"""Package a fresh desktop Player tree into the updater's strict ZIP contract. Never signs or publishes."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import stat
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parents[2]
# The helper deliberately rejects ZIP64. Keep Python's conservative standard-ZIP
# file/offset limit too; resource-free Player archives should be far smaller.
MAX_ENTRIES, MAX_EXPANDED, MAX_COMPRESSED = 65534, 16 * 1024**3, 2 * 1024**3 - 1
MAX_CENTRAL_DIRECTORY = 64 * 1024**2
spec = importlib.util.spec_from_file_location('build_inputs', Path(__file__).with_name('build-resource-free.py'))
inputs = importlib.util.module_from_spec(spec)
spec.loader.exec_module(inputs)


def relative(value):
    if not value or len(value.encode('utf-16-le')) // 2 > 512 or value.startswith('/') or '\\' in value:
        raise ValueError('Invalid archive relative path')
    for part in value.split('/'):
        if not part or part in ('.', '..') or part.endswith(('.', ' ')) or any(ord(c) < 32 or 127 <= ord(c) <= 159 or c in ':*?"<>|' for c in part):
            raise ValueError('Invalid archive path component')
        stem = part.split('.')[0].upper()
        if stem in ('CON', 'PRN', 'AUX', 'NUL') or re.fullmatch(r'(COM|LPT)[0-9]', stem):
            raise ValueError('Reserved archive path component')
        if part.lower() in ('.ds_store', '__macosx', 'localconfig', 'playerdata', 'savedvariables', '.git', '.env') or part.lower().startswith('.env.') or Path(part).suffix.lower() in ('.keystore', '.jks', '.p12', '.pfx', '.pem', '.key'):
            raise ValueError('Local user data, signing material or housekeeping files must not enter a client package')


def no_links(path):
    for item in [path, *path.parents]:
        if item.is_symlink():
            raise ValueError('Linked source/output paths are not supported')


def object_file(path):
    if path.stat().st_size > 16384:
        raise ValueError('Oversized package metadata')
    def unique(pairs):
        result = {}
        for key, value in pairs:
            if key in result:
                raise ValueError('Duplicate metadata field')
            result[key] = value
        return result
    value = json.loads(path.read_text(encoding='utf-8-sig'), object_pairs_hook=unique)
    if not isinstance(value, dict):
        raise ValueError('Expected package metadata object')
    return value


def inspect_tree(source, platform):
    source = Path(source).absolute()
    no_links(source)
    if not source.is_dir() or (platform == 'macos' and source.suffix != '.app'):
        raise ValueError('Source must be the dedicated Windows application directory or macOS .app')
    relative(source.name)
    files, seen, size, central_size = [], set(), 0, 0
    for base, directories, names in os.walk(source, followlinks=False):
        for name in sorted(directories + names):
            path = Path(base) / name
            info = path.lstat()
            name_in_zip = source.name + '/' + path.relative_to(source).as_posix()
            relative(name_in_zip)
            if not (stat.S_ISREG(info.st_mode) or stat.S_ISDIR(info.st_mode)):
                raise ValueError('Archive accepts only regular files/directories; flatten approved links before signing')
            if name_in_zip.casefold() in seen:
                raise ValueError('Case-colliding archive paths')
            seen.add(name_in_zip.casefold())
            central_size += 46 + len(name_in_zip.encode('utf-8')) + (1 if stat.S_ISDIR(info.st_mode) else 0)
            if stat.S_ISREG(info.st_mode):
                if info.st_size > MAX_COMPRESSED:
                    raise ValueError('Individual file exceeds standard ZIP limit; ZIP64 is not supported')
                size += info.st_size
            files.append((path, name_in_zip, info))
            if len(files) > MAX_ENTRIES or size > MAX_EXPANDED or central_size > MAX_CENTRAL_DIRECTORY:
                raise ValueError('Archive content exceeds updater limits')
    marker_root = source if platform == 'windows' else source / 'Contents/Resources'
    marker = object_file(marker_root / 'ffo-desktop-install.json')
    if type(marker.get('schemaVersion')) is not int or marker.get('schemaVersion') != 1 or marker.get('platform') != platform:
        raise ValueError('Invalid desktop install marker')
    inputs.validate_version(marker.get('version', ''))
    app_id = marker.get('installId', '')
    if not isinstance(app_id, str) or len(app_id) > 200 or not re.fullmatch(r'[A-Za-z][A-Za-z0-9_-]*(\.[A-Za-z][A-Za-z0-9_-]*)+', app_id):
        raise ValueError('Invalid stable application identifier')
    executable = marker.get('executable', '')
    relative(executable)
    if platform == 'windows' and ('/' in executable or not executable.endswith('.exe')):
        raise ValueError('Windows executable must be at the dedicated application root')
    if platform == 'macos' and (not executable.startswith('Contents/MacOS/') or '/' in executable[len('Contents/MacOS/'):]):
        raise ValueError('macOS executable must be directly under Contents/MacOS')
    if not (source / executable).is_file():
        raise ValueError('Player executable missing')
    data = source / (Path(executable).stem + '_Data') if platform == 'windows' else source / 'Contents/Resources/Data'
    streaming = data / 'StreamingAssets'
    if sorted(p.name for p in streaming.iterdir()) != ['DesktopUpdater', 'resource-bootstrap.json']:
        raise ValueError('Only updater binaries and bootstrap configuration belong in resource-free StreamingAssets')
    config = object_file(streaming / 'resource-bootstrap.json')
    if type(config.get('schemaVersion')) is not int or config.get('schemaVersion') != 1 or config.get('mode') != 'resource-free' or config.get('appVersion') != marker['version'] or config.get('developmentHttp'):
        raise ValueError('Desktop update package requires matching stable resource-free HTTPS bootstrap')
    inputs.validate_dispatch(config.get('dispatchUrl', ''))
    inputs.validate_legacy_dispatches(config.get('legacyDispatchUrls', []), config['dispatchUrl'])
    helpers = ['DesktopUpdater.exe'] if platform == 'windows' else ['DesktopUpdater-osx-arm64', 'DesktopUpdater-osx-x64']
    helper_root = streaming / 'DesktopUpdater'
    if sorted(p.name for p in helper_root.iterdir()) != helpers or any(not (helper_root / name).is_file() or (helper_root / name).stat().st_size == 0 for name in helpers):
        raise ValueError('Incorrect self-contained updater payload')
    if platform == 'macos':
        if not re.fullmatch(r'[A-Z0-9]{10}', marker.get('macTeamId', '')):
            raise ValueError('Existing Developer ID Team ID is required')
        if any(not path.stat().st_mode & 0o111 for path in [source / executable, *[helper_root / name for name in helpers]]):
            raise ValueError('Player and updater binaries must be executable before signing/packaging')
    elif not re.fullmatch(r'[0-9a-fA-F]{64}', marker.get('windowsSignerSha256', '')):
        raise ValueError('Existing Windows signer SHA-256 is required')
    return source, files, marker


def package(source, platform, output):
    if platform not in ('windows', 'macos'):
        raise ValueError('Platform must be windows or macos')
    source, files, marker = inspect_tree(source, platform)
    output = Path(output).absolute()
    no_links(output)
    if output.suffix.lower() != '.zip' or source == output or source in output.parents or output.exists():
        raise ValueError('Choose a new ZIP output outside the application tree')
    output.parent.mkdir(parents=True, exist_ok=True)
    temporary = None
    try:
        descriptor, temporary = tempfile.mkstemp(prefix=output.name + '.', suffix='.part', dir=output.parent)
        os.close(descriptor)
        with zipfile.ZipFile(temporary, 'w', compression=zipfile.ZIP_DEFLATED, allowZip64=False) as archive:
            for path, name, before in files:
                after = path.lstat()
                if (after.st_ino, after.st_dev, after.st_mode, after.st_size, after.st_mtime_ns) != (before.st_ino, before.st_dev, before.st_mode, before.st_size, before.st_mtime_ns):
                    raise ValueError('Build tree changed during packaging; retry from immutable signed output')
                archive.write(path, name + ('/' if stat.S_ISDIR(before.st_mode) else ''))
        if Path(temporary).stat().st_size > MAX_COMPRESSED:
            raise ValueError('Compressed archive exceeds updater limit')
        # Refuse overwrite even if another process created the output during compression.
        os.link(temporary, output)
    finally:
        if temporary is not None:
            Path(temporary).unlink(missing_ok=True)
    digest = hashlib.sha256()
    with output.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            digest.update(chunk)
    receipt = {'schemaVersion': 1, 'status': 'structurally-validated-candidate', 'platform': platform,
               'version': marker['version'], 'installId': marker['installId'], 'bytes': output.stat().st_size,
               'sha256': digest.hexdigest(), 'signingVerified': False, 'productionReady': False}
    Path(str(output) + '.build.json').write_text(json.dumps(receipt, indent=2) + '\n')
    Path(str(output) + '.sha256').write_text(digest.hexdigest() + '  ' + output.name + '\n')
    return receipt


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path)
    parser.add_argument('--platform', required=True, choices=('windows', 'macos'))
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    try:
        result = package(args.source, args.platform, args.output)
    except (ValueError, OSError, KeyError, TypeError, zipfile.LargeZipFile) as error:
        raise SystemExit(str(error))
    print('ZIP structure and SHA-256 verified; OS signature/notarization and actual update acceptance remain required.')
