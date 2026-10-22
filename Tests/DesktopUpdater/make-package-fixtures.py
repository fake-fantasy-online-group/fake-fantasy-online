#!/usr/bin/env python3
"""Inert Players with signed-mode or empty pins for packager->updater tests only."""
import importlib.util
import json
from pathlib import Path
import sys
root = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('packager', root / 'scripts/ci/package-desktop-update.py')
packager = importlib.util.module_from_spec(spec)
spec.loader.exec_module(packager)


def make_fixture(output, platform, pinned):
    fixture_name = platform if pinned else platform + '-unsigned'
    app = output / fixture_name / ('fake-fantasy-online.app' if platform == 'macos' else 'fake-fantasy-online')
    data = app / ('Contents/Resources/Data' if platform == 'macos' else 'fake-fantasy-online_Data')
    streaming = data / 'StreamingAssets'
    helpers = streaming / 'DesktopUpdater'
    helpers.mkdir(parents=True)
    marker_root = app / 'Contents/Resources' if platform == 'macos' else app
    executable = 'Contents/MacOS/fake-fantasy-online' if platform == 'macos' else 'fake-fantasy-online.exe'
    (app / executable).parent.mkdir(parents=True, exist_ok=True)
    (app / executable).write_text('Synthetic unsigned executable; never execute.\n')
    (app / executable).chmod(0o755)
    for name in (['DesktopUpdater-osx-arm64', 'DesktopUpdater-osx-x64'] if platform == 'macos' else ['DesktopUpdater.exe']):
        (helpers / name).write_text('Synthetic unsigned helper; never execute.\n')
        (helpers / name).chmod(0o755)
    marker = dict(schemaVersion=1, platform=platform, version='1.0.1', installId='org.example.client', executable=executable,
                  macTeamId='ABCDEFGHIJ' if pinned and platform == 'macos' else '',
                  windowsSignerSha256='a'*64 if pinned and platform == 'windows' else '')
    (marker_root / 'ffo-desktop-install.json').write_text(json.dumps(marker))
    (streaming / 'resource-bootstrap.json').write_text(json.dumps(dict(schemaVersion=1, mode='resource-free', appVersion='1.0.1', dispatchUrl='https://example.test/dispatch.json', developmentHttp=False)))
    packager.package(app, platform, output / (fixture_name + '.zip'))


if __name__ == '__main__':
    output = Path(sys.argv[1])
    for platform in ('windows', 'macos'):
        for pinned in (True, False):
            make_fixture(output, platform, pinned)
