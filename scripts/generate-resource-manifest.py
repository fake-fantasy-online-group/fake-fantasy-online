#!/usr/bin/env python3
"""Explicit resource-host operation. Never invoked by the resource-free player build."""
import argparse
import hashlib
from pathlib import Path, PurePosixPath
import re
import unicodedata

RESERVED_NAMES = {'CON', 'PRN', 'AUX', 'NUL'} | {prefix + str(index) for prefix in ('COM', 'LPT') for index in range(1, 10)}
PLATFORMS = ('android', 'windows', 'macos', 'linux')
PASSIVE_GAME_EXTENSIONS = {'.json', '.png', '.jpg', '.jpeg', '.bytes', '.txt', '.bin', '.ogg', '.wav'}


def destination(source):
    if source.startswith('ReferenceClient/packages/'):
        return 'data/' + source[len('ReferenceClient/packages/'):]
    if source.startswith('ReferenceClient/music/'):
        return 'Music/' + source[len('ReferenceClient/music/'):]
    return source


def passive_overlay(path):
    # These are data-only overrides read by ResLoader/EffectPlayer ahead of PKGs.
    # Keep the reviewed runtime directories explicit; never admit raw world trees
    # or executable/plugin extensions through a generic respatch exception.
    pieces = path.split('/')
    extension = PurePosixPath(path).suffix.lower()
    return ((len(pieces) == 4 and pieces[:3] == ['gameassets', 'respatch', 'effect'] and extension == '.eff') or
            (len(pieces) == 5 and pieces[:4] == ['gameassets', 'respatch', 'magic', 'magic'] and extension == '.gso') or
            (len(pieces) == 5 and pieces[:4] == ['gameassets', 'respatch', 'item', 'item'] and extension == '.gso'))


def allowed(path):
    pieces = PurePosixPath(path).parts
    if len(pieces) < 2 or any(p in ('', '.', '..') for p in pieces):
        return False
    extension = PurePosixPath(path).suffix.lower()
    return (passive_overlay(path) or (pieces[0] == 'data' and len(pieces) == 2 and extension == '.pkg') or
            (pieces[0] == 'Music' and len(pieces) == 2 and extension == '.ogg') or
            (pieces[0] in ('gameassets', 'ReferenceClient') and extension in PASSIVE_GAME_EXTENSIONS) or
            (len(pieces) >= 3 and pieces[0] in ('gameassets', 'ReferenceClient') and pieces[1] == 'font' and extension in ('.idx', '.wid')) or
            (pieces[0] in ('sndeffect', 'sndeffect24_pcm') and extension in ('.wav', '.ogg')))


def validate_path(path):
    # Match MobileResourceManifest.ValidateRelativePath and the remote installer's
    # cross-platform checks, including C1 controls and UTF-16 path length.
    pieces = path.split('/')
    if not path or path.startswith('/') or any(c in path for c in '\\:?#%<>"|*') or any(unicodedata.category(c) == 'Cc' for c in path):
        raise ValueError('Unsafe resource path: ' + path)
    if len(path.encode('utf-16-le')) // 2 > 1024 or len(pieces) > 64:
        raise ValueError('Resource path exceeds runtime length/depth limits: ' + path)
    for part in pieces:
        if part in ('', '.', '..') or part.endswith(('.', ' ')) or part.lower().endswith('.part'):
            raise ValueError('Unsafe resource path segment: ' + path)
        if part.split('.', 1)[0].upper() in RESERVED_NAMES:
            raise ValueError('Reserved resource filename: ' + path)


def generate(root, version, platform='android'):
    if platform not in PLATFORMS:
        raise ValueError('Unsupported client platform')
    root = Path(root).resolve(strict=True)
    if not root.is_dir():
        raise ValueError('Resource root must be a directory')
    if (len(version) > 64 or not re.fullmatch(r'(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)', version)
            or any(int(part) > 4294967295 for part in version.split('.'))):
        raise ValueError('Client version must be an explicit application SemVer')
    entries = []
    sources = set()
    destinations = set()
    source_spellings = {}
    destination_spellings = {}
    total_size = 0
    for file in sorted(root.rglob('*')):
        relative = file.relative_to(root).as_posix()
        # Plugins, endpoint configuration and Unity metadata are never remote game resources.
        if relative == 'mobile-dispatch.txt' or relative == 'mobile-resources.tsv' or relative.startswith('AddOns/') or relative == 'AddOns' or relative.lower().endswith('.meta'):
            continue
        if file.is_symlink():
            raise ValueError('Materialize symlinks before publishing resources: ' + relative)
        if file.is_dir():
            continue
        validate_path(relative)
        target = destination(relative)
        validate_path(target)
        if not allowed(target):
            raise ValueError('Unexpected or executable resource; review outside this passive-resource channel: ' + relative)
        for value, seen, spellings in [(relative, sources, source_spellings), (target, destinations, destination_spellings)]:
            key = value.lower()
            if key in seen or any(key.startswith(old + '/') or old.startswith(key + '/') for old in seen):
                raise ValueError('Case-insensitive path collision: ' + value)
            seen.add(key)
            pieces = value.split('/')
            for index in range(1, len(pieces) + 1):
                prefix = '/'.join(pieces[:index])
                previous = spellings.get(prefix.lower())
                if previous is not None and previous != prefix:
                    raise ValueError('Directory casing collision: ' + prefix)
                spellings[prefix.lower()] = prefix
        hasher = hashlib.sha256()
        size = 0
        with file.open('rb') as stream:
            for chunk in iter(lambda: stream.read(1024 * 1024), b''):
                size += len(chunk)
                hasher.update(chunk)
        if size == 0:
            raise ValueError('Empty resource is not supported: ' + relative)
        total_size += size
        if total_size > 64 * 1024 * 1024 * 1024 or len(entries) >= 100000:
            raise ValueError('Resource set exceeds runtime installation limits')
        entries.append(f'{relative}\t{target}\t{size}\t{hasher.hexdigest()}')
    if not entries:
        raise ValueError('Refusing to generate an empty resource manifest')
    text = 'FFO-MOBILE-1\n@client\t' + platform + '\t' + version + '\n' + '\n'.join(entries) + '\n'
    if len(text.encode('utf-8')) > 8 * 1024 * 1024:
        raise ValueError('Manifest exceeds runtime 8 MiB limit')
    return text


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('resource_root', type=Path, help='Materialized passive-resource directory to serve over HTTPS')
    parser.add_argument('--client-platform', choices=PLATFORMS, default='android', help='Exact target platform; defaults to android for existing callers')
    parser.add_argument('--client-version', required=True, help='Exact supported application version (PlayerSettings bundleVersion)')
    parser.add_argument('--output', type=Path, required=True, help='New manifest path; existing files are never overwritten')
    args = parser.parse_args()
    text = generate(args.resource_root, args.client_version, args.client_platform)
    with args.output.open('x', encoding='utf-8', newline='\n') as stream:
        stream.write(text)
    print('Created manifest; serve it at the resource root so relative source URLs resolve correctly. No upload or publishing was performed.')


if __name__ == '__main__':
    main()
