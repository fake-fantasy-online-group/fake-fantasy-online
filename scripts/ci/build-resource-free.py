#!/usr/bin/env python3
"""Explicit local three-platform build entry. No login, SDK install, signing changes, tag, upload or deploy."""
import argparse
import importlib.util
import ipaddress
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
from urllib.parse import urlsplit

ROOT = Path(__file__).resolve().parents[2]


def validate_version(value):
    if not re.fullmatch(r'(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)', value) or any(int(x) > 4294967295 for x in value.split('.')):
        raise ValueError('Version must be a stable MAJOR.MINOR.PATCH with UInt32 components')
    return value


def validate_dispatch(value, development_http=False):
    if not value or value != value.strip() or len(value.encode('utf-8')) > 4096 or any(ord(c) < 32 or 127 <= ord(c) <= 159 for c in value) or '\\' in value:
        raise ValueError('Use a nonempty public credential-free dispatch URL')
    parsed = urlsplit(value)
    if not parsed.hostname or parsed.username is not None or parsed.password is not None or parsed.query or parsed.fragment or '?' in value or '#' in value or '%' in parsed.netloc:
        raise ValueError('Dispatch URL must not contain credentials, query, fragment or an escaped host')
    host = parsed.hostname.rstrip('.').lower()
    local_test = parsed.hostname.lower() in ('localhost', '127.0.0.1', '::1')
    if (host == 'localhost' or host.endswith('.localhost')) and not (development_http and local_test):
        raise ValueError('Loopback dispatch URLs are not distributable')
    if parsed.port is not None and not 1 <= parsed.port <= 65535:
        raise ValueError('Invalid dispatch port')
    try:
        address = ipaddress.ip_address(host)
    except ValueError:
        address = None
    if address and (address.is_unspecified or (address.is_loopback and not (development_http and local_test))):
        raise ValueError('Loopback dispatch URLs are not distributable')
    lan = address is not None and address.version == 4 and any(address in ipaddress.ip_network(net) for net in ('10.0.0.0/8', '172.16.0.0/12', '192.168.0.0/16'))
    if parsed.scheme != 'https' and not (development_http and parsed.scheme == 'http' and (lan or local_test)):
        raise ValueError('Dispatch URL requires HTTPS; explicit development HTTP permits private LAN IPv4 or local test loopback')
    return value


def validate_legacy_dispatches(values, dispatch_url, development_http=False):
    if values is None:
        values = []
    if not isinstance(values, list) or len(values) > 8:
        raise ValueError('Legacy dispatch migration accepts at most eight retired URLs')
    if values:
        validate_dispatch(dispatch_url, development_http)
    seen = {dispatch_url}
    for value in values:
        if not isinstance(value, str):
            raise ValueError('Legacy dispatch URLs must be strings')
        validate_dispatch(value, development_http)
        if value in seen:
            raise ValueError('Legacy dispatch URLs must be distinct from each other and the current default')
        seen.add(value)
    return values


def legacy_dispatches(args):
    if args.legacy_dispatch_url is not None:
        values = args.legacy_dispatch_url
    else:
        value = os.environ.get('FFO_RESOURCE_LEGACY_DISPATCH_URLS', '')
        values = value.replace('\r\n', '\n').split('\n') if value else []
    return validate_legacy_dispatches(values, args.dispatch_url, args.development_http)


def parser():
    result = argparse.ArgumentParser(description=__doc__)
    result.add_argument('--platform', required=True, choices=('android', 'windows', 'macos'))
    result.add_argument('--version', required=True, type=validate_version, help='Must match ProjectSettings bundleVersion; never changed by the build')
    result.add_argument('--dispatch-url', required=True, help='Public credential-free endpoint baked into resource-bootstrap.json')
    result.add_argument('--legacy-dispatch-url', action='append', help='Retired default to migrate to --dispatch-url; repeat for each. Defaults to newline-delimited FFO_RESOURCE_LEGACY_DISPATCH_URLS')
    result.add_argument('--configuration', choices=('formal', 'development'), default='formal')
    result.add_argument('--development-http', action='store_true')
    result.add_argument('--output', type=Path)
    result.add_argument('--updater-directory', type=Path, help='Desktop only: exact self-contained updater binaries, no local config')
    result.add_argument('--mac-team-id', help='Existing public Developer ID Team ID, not a signing credential')
    result.add_argument('--windows-signer-sha256', help='Existing public Authenticode certificate SHA-256')
    result.add_argument('--source-tag', help='Verify HEAD equals an existing vMAJOR.MINOR.PATCH source tag; never creates one')
    result.add_argument('--release', action='store_true', help='Require formal configuration and an exact existing source tag; still does not publish')
    return result


def main(argv=None):
    args = parser().parse_args(argv)
    if args.development_http and args.configuration != 'development':
        raise ValueError('HTTP opt-in is available only for explicit development builds')
    validate_dispatch(args.dispatch_url, args.development_http)
    legacy = legacy_dispatches(args)
    settings = (ROOT / 'ProjectSettings/ProjectSettings.asset').read_text()
    project_version = re.search(r'^  bundleVersion:\s*(\S+)\s*$', settings, re.M).group(1)
    if args.version != project_version:
        raise ValueError('Requested version must match ProjectSettings bundleVersion; no settings were changed')
    if args.release and (args.configuration != 'formal' or not args.source_tag):
        raise ValueError('Release packaging requires --configuration formal and --source-tag vMAJOR.MINOR.PATCH')
    head = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip()
    dirty = subprocess.check_output(['git', 'status', '--porcelain', '--untracked-files=normal'], cwd=ROOT, text=True)
    if args.source_tag:
        if args.source_tag != 'v' + args.version:
            raise ValueError('Source tag must be exactly v plus the requested stable version')
        tag = subprocess.check_output(['git', 'rev-parse', '--verify', 'refs/tags/' + args.source_tag + '^{commit}'], cwd=ROOT, text=True).strip()
        if head != tag:
            raise ValueError('HEAD does not match the explicitly selected source tag')
        # Byte-exact source verification; ignored local assets and local credentials are never enumerated.
        if dirty:
            raise ValueError('Source-tag packaging requires a clean tracked/source checkout; retain signing credentials outside tracked project files')
    if args.platform != 'android' and args.updater_directory is None:
        raise ValueError('Desktop packaging requires --updater-directory from the local self-contained helper build')
    if args.configuration == 'formal' and args.platform == 'macos' and not re.fullmatch(r'[A-Z0-9]{10}', args.mac_team_id or ''):
        raise ValueError('Formal macOS packaging requires the existing public --mac-team-id')
    if args.configuration == 'formal' and args.platform == 'windows' and not re.fullmatch(r'[0-9a-fA-F]{64}', args.windows_signer_sha256 or ''):
        raise ValueError('Formal Windows packaging requires the existing public --windows-signer-sha256')
    executable = shutil.which(os.environ.get('UNITY_EDITOR_BIN', 'Unity'))
    if executable is None:
        raise ValueError('Unity Editor not found; install/use the existing licensed Unity 6000.4.9f1 locally')
    if args.platform == 'macos' and sys.platform != 'darwin':
        raise ValueError('Build macOS Players on macOS for the native signing and validation toolchain')
    defaults = {'android': 'Build/ResourceFree/android/fake-fantasy-online.apk',
                'windows': 'Build/ResourceFree/windows/fake-fantasy-online/fake-fantasy-online.exe',
                'macos': 'Build/ResourceFree/macos/fake-fantasy-online.app'}
    output = (args.output or ROOT / defaults[args.platform]).resolve()
    if output.suffix.lower() != {'android': '.apk', 'windows': '.exe', 'macos': '.app'}[args.platform]:
        raise ValueError('Output extension does not match platform')
    if output.exists():
        raise ValueError('Use a fresh output location; an earlier artifact must not be mistaken for this build')
    env = dict(os.environ, FFO_BUILD_PLATFORM=args.platform, FFO_BUILD_VERSION=args.version,
               FFO_RESOURCE_DISPATCH_URL=args.dispatch_url, FFO_RESOURCE_LEGACY_DISPATCH_URLS='\n'.join(legacy), FFO_BUILD_CONFIGURATION=args.configuration,
               FFO_BUILD_DEVELOPMENT_HTTP='1' if args.development_http else '0', FFO_BUILD_OUTPUT=str(output))
    if args.platform == 'android':
        env['FAKE_FANTASY_ONLINE_ANDROID_BUILD_PATH'] = str(output)
    if args.updater_directory:
        env['FFO_DESKTOP_UPDATER_DIRECTORY'] = str(args.updater_directory.resolve())
    env['FFO_MAC_TEAM_ID'] = args.mac_team_id or ''
    env['FFO_WINDOWS_SIGNER_SHA256'] = args.windows_signer_sha256 or ''
    logs = ROOT / 'Build/ResourceFree/logs'
    logs.mkdir(parents=True, exist_ok=True)
    log = logs / (args.platform + '-unity-build.log')
    command = [executable, '-batchmode', '-quit', '-buildTarget', {'android': 'Android', 'windows': 'Win64', 'macos': 'OSXUniversal'}[args.platform],
               '-projectPath', str(ROOT), '-executeMethod', 'FakeFantasyOnline.Editor.CiClientBuild.BuildResourceFree', '-logFile', str(log)]
    result = subprocess.run(command, cwd=ROOT, env=env)
    if result.returncode:
        # The full local Unity log can contain credential/license details. Do not echo it.
        raise ValueError('Unity build failed; inspect the local Unity log privately. No successful artifact is claimed')
    if not output.exists():
        raise ValueError('Unity returned without the requested output; no successful artifact is claimed')
    if args.platform == 'android':
        spec = importlib.util.spec_from_file_location('apkcheck', ROOT / 'scripts/ci/verify-resource-free-apk.py')
        check = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(check)
        check.verify(output, configuration=args.configuration, expected_dispatch=args.dispatch_url, source_commit=head, expected_legacy_dispatches=legacy)
    receipt = {'schemaVersion': 1, 'status': 'local-build-candidate', 'appVersion': args.version,
               'platform': args.platform, 'configuration': args.configuration, 'sourceCommit': head,
               'sourceDirty': bool(dirty),
               'verifiedSourceTag': args.source_tag, 'dispatchUrl': args.dispatch_url,
               'signingVerified': False, 'runtimeAccepted': False, 'productionReady': False}
    Path(str(output) + '.local-build.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print('Local ' + args.configuration + ' build output: ' + str(output))
    print('Actual signing/identity, device startup, resource delivery and update acceptance remain release gates. Nothing was published.')
    return 0


if __name__ == '__main__':
    try:
        raise SystemExit(main())
    except (ValueError, OSError, subprocess.CalledProcessError) as error:
        raise SystemExit(str(error))
