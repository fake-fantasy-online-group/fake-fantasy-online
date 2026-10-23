import importlib.util
import json
import os
from pathlib import Path
import stat
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch
import zipfile

ROOT = Path(__file__).resolve().parents[2]


def load(name, relative):
    spec = importlib.util.spec_from_file_location(name, ROOT / relative)
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


build = load('build', 'scripts/ci/build-resource-free.py')
pack = load('pack', 'scripts/ci/package-desktop-update.py')
apk = load('formal_apk', 'scripts/ci/verify-resource-free-apk.py')


class FormalBuildContract(unittest.TestCase):
    def test_local_test_endpoint_requires_explicit_opt_in(self):
        for host in ('localhost', '127.0.0.1', '[::1]'):
            value = 'http://' + host + ':8088/dispatch.json'
            self.assertEqual(build.validate_dispatch(value, True), value)
            with self.assertRaises(ValueError):
                build.validate_dispatch(value, False)
            with self.assertRaises(ValueError):
                build.main(['--platform', 'macos', '--version', '1.0.3', '--configuration', 'formal', '--development-http', '--dispatch-url', value])
        for value in ('http://0.0.0.0/d', 'http://[::]/d', 'http://sub.localhost/d', 'http://127.0.0.2/d', 'http://127.1/d', 'http://user:pass@localhost/d', 'http://localhost/d?token=x'):
            with self.subTest(value=value), self.assertRaises(ValueError):
                build.validate_dispatch(value, True)

    def test_explicit_formal_cli_and_endpoint_policy(self):
        args = build.parser().parse_args(['--platform', 'windows', '--version', '1.0.3', '--dispatch-url', 'https://example.com/dispatch.json'])
        self.assertEqual(args.configuration, 'formal')
        self.assertFalse(args.release)
        for endpoint in ('', 'https://user:secret@example.com/d', 'https://example.com/d?token=secret',
                         'https://example.com/d#x', 'https://localhost/d', 'https://127.0.0.1/d',
                         'https://0.0.0.0/d', 'http://example.com/d', ' https://example.com/d', 'https://example.com/\n'):
            with self.subTest(endpoint=endpoint), self.assertRaises(ValueError):
                build.validate_dispatch(endpoint)
        self.assertEqual(build.validate_dispatch('https://example.com/dispatch.json'), 'https://example.com/dispatch.json')
        self.assertEqual(build.validate_dispatch('http://192.168.1.2/d', True), 'http://192.168.1.2/d')
        with self.assertRaises(ValueError):
            build.validate_dispatch('http://8.8.8.8/d', True)

    def test_private_profile_legacy_inputs_and_no_implicit_migration(self):
        base = ['--platform', 'windows', '--version', '1.0.3', '--dispatch-url', 'https://current.example.test/d']
        with patch.dict(os.environ, {}, clear=True):
            self.assertEqual(build.legacy_dispatches(build.parser().parse_args(base)), [])
        with patch.dict(os.environ, {'FFO_RESOURCE_LEGACY_DISPATCH_URLS': 'https://old.example.test/d\r\nhttps://older.example.test/d'}):
            self.assertEqual(build.legacy_dispatches(build.parser().parse_args(base)),
                             ['https://old.example.test/d', 'https://older.example.test/d'])
            self.assertEqual(build.legacy_dispatches(build.parser().parse_args(base + ['--legacy-dispatch-url', 'https://explicit.example.test/d'])),
                             ['https://explicit.example.test/d'])
        for value in ('', ' https://old.example.test/d', 'https://user:secret@example.test/d',
                      'https://old.example.test/d?token=x', 'http://localhost/d', 'https://current.example.test/d'):
            with self.subTest(value=value), self.assertRaises(ValueError):
                build.legacy_dispatches(build.parser().parse_args(base + ['--legacy-dispatch-url', value]))
        for values in ('https://old.example.test/d', [1], ['https://old.example.test/d'] * 2,
                       ['https://old.example.test/' + str(i) for i in range(9)]):
            with self.subTest(values=values), self.assertRaises(ValueError):
                build.validate_legacy_dispatches(values, 'https://current.example.test/d')
        with self.assertRaises(ValueError):
            build.validate_legacy_dispatches(['https://old.example.test/d'], '')
        self.assertEqual(build.validate_legacy_dispatches(None, ''), [])

    def test_private_profile_reaches_unity_without_profile_files(self):
        with tempfile.TemporaryDirectory() as temporary:
            output = Path(temporary) / 'candidate.exe'
            captured = {}
            def fake_unity(command, cwd, env):
                captured.update(env)
                output.write_bytes(b'inert synthetic fixture, never executed')
                return type('Result', (), {'returncode': 0})()
            args = ['--platform', 'windows', '--version', '1.0.3', '--configuration', 'development',
                    '--dispatch-url', 'https://current.example.test/d', '--updater-directory', temporary,
                    '--output', str(output)]
            with patch.dict(os.environ, {'FFO_RESOURCE_LEGACY_DISPATCH_URLS': 'https://old.example.test/d'}), \
                    patch.object(build.shutil, 'which', return_value='/synthetic/Unity'), \
                    patch.object(build.subprocess, 'check_output', side_effect=['a' * 40 + '\n', '']), \
                    patch.object(build.subprocess, 'run', side_effect=fake_unity):
                self.assertEqual(build.main(args), 0)
            self.assertEqual(captured['FFO_RESOURCE_DISPATCH_URL'], 'https://current.example.test/d')
            self.assertEqual(captured['FFO_RESOURCE_LEGACY_DISPATCH_URLS'], 'https://old.example.test/d')
            self.assertFalse(json.loads(Path(str(output) + '.local-build.json').read_text())['productionReady'])

    def test_source_tag_release_gate_fails_before_editor(self):
        with self.assertRaisesRegex(ValueError, 'requires --configuration formal'):
            build.main(['--platform', 'android', '--version', '1.0.3', '--dispatch-url', 'https://example.com/d', '--release'])
        with self.assertRaisesRegex(ValueError, 'match ProjectSettings'):
            build.main(['--platform', 'android', '--version', '9.9.9', '--dispatch-url', 'https://example.com/d'])

    def test_android_formal_build_preserves_signing(self):
        source = (ROOT / 'Assets/FakeFantasyOnline/Editor/MobileClientBuild.cs').read_text()
        self.assertIn('if (development) PlayerSettings.Android.useCustomKeystore = false;', source)
        self.assertIn('profile.ValidateAndroidSigning', source)
        self.assertIn('development ? BuildOptions.Development : BuildOptions.None', source)
        for secret in ('keystorePass', 'keyaliasPass'):
            self.assertNotIn(secret, source)
            self.assertNotIn(secret, (ROOT / 'Assets/FakeFantasyOnline/Editor/ResourceFreeBuildProfile.cs').read_text())

    def test_desktop_formal_build_keeps_required_signing_pin(self):
        for platform, option in (('windows', '--windows-signer-sha256'), ('macos', '--mac-team-id')):
            with self.subTest(platform=platform), tempfile.TemporaryDirectory() as temporary, \
                    patch.object(build.subprocess, 'check_output', side_effect=['a' * 40 + '\n', '']), \
                    patch.object(build.subprocess, 'run') as run:
                with self.assertRaisesRegex(ValueError, option):
                    build.main(['--platform', platform, '--version', '1.0.3', '--configuration', 'formal',
                                '--dispatch-url', 'https://example.com/dispatch.json', '--updater-directory', temporary])
                run.assert_not_called()

    def fixture(self, parent, platform='windows', pinned=True):
        root = parent / ('fake-fantasy-online.app' if platform == 'macos' else 'fake-fantasy-online')
        root.mkdir()
        executable = 'Contents/MacOS/fake-fantasy-online' if platform == 'macos' else 'fake-fantasy-online.exe'
        player = root / executable
        player.parent.mkdir(parents=True, exist_ok=True)
        player.write_bytes(b'synthetic non-executable fixture')
        player.chmod(0o755)
        data = root / ('Contents/Resources/Data' if platform == 'macos' else 'fake-fantasy-online_Data')
        streaming = data / 'StreamingAssets'
        helpers = streaming / 'DesktopUpdater'
        helpers.mkdir(parents=True)
        for name in (['DesktopUpdater-osx-x64', 'DesktopUpdater-osx-arm64'] if platform == 'macos' else ['DesktopUpdater.exe']):
            file = helpers / name
            file.write_bytes(b'synthetic non-executable helper fixture')
            file.chmod(0o755)
        (streaming / 'resource-bootstrap.json').write_text(json.dumps({'schemaVersion': 1, 'mode': 'resource-free',
            'appVersion': '1.0.3', 'dispatchUrl': 'https://example.com/dispatch.json', 'developmentHttp': False}))
        marker = root / ('Contents/Resources/ffo-desktop-install.json' if platform == 'macos' else 'ffo-desktop-install.json')
        marker.write_text(json.dumps({'schemaVersion': 1, 'platform': platform, 'version': '1.0.3', 'installId': 'org.example.client',
            'executable': executable, 'macTeamId': 'ABCDEFGHIJ' if pinned and platform == 'macos' else '',
            'windowsSignerSha256': 'a' * 64 if pinned and platform == 'windows' else ''}))
        return root

    def test_actual_zip_roundtrip_and_no_signature_claim(self):
        for platform in ('windows', 'macos'):
            for pinned in (True, False):
                with self.subTest(platform=platform, pinned=pinned), tempfile.TemporaryDirectory() as temporary:
                    root = self.fixture(Path(temporary), platform, pinned)
                    output = Path(temporary) / 'candidate.zip'
                    receipt = pack.package(root, platform, output)
                    self.assertFalse(receipt['signingVerified'])
                    self.assertFalse(receipt['productionReady'])
                    self.assertEqual(len(receipt['sha256']), 64)
                    with zipfile.ZipFile(output) as archive:
                        self.assertTrue(all(name.startswith(root.name + '/') for name in archive.namelist()))
                        player = root.name + ('/Contents/MacOS/fake-fantasy-online' if platform == 'macos' else '/fake-fantasy-online.exe')
                        self.assertTrue(archive.getinfo(player).external_attr >> 16 & 0o111)
                        self.assertEqual(archive.read(player), b'synthetic non-executable fixture')
                        marker_name = ('Contents/Resources/' if platform == 'macos' else '') + 'ffo-desktop-install.json'
                        marker = json.loads(archive.read(root.name + '/' + marker_name))
                        self.assertEqual(marker['macTeamId'], 'ABCDEFGHIJ' if pinned and platform == 'macos' else '')
                        self.assertEqual(marker['windowsSignerSha256'], 'a' * 64 if pinned and platform == 'windows' else '')
                    self.assertFalse(list(output.parent.glob('*.part')))
                    with self.assertRaises(ValueError):
                        pack.package(root, platform, output)

    def test_signing_pins_are_explicit_empty_or_valid_strings_on_both_platforms(self):
        invalid_values = {
            'macTeamId': (None, False, 0, [], {}, ' ', 'ABCDEFGHI', 'ABCDEFGHIJK', 'abcdefghij', 'ABCDEFGHI-', 'ABCDEFGHIJ\n'),
            'windowsSignerSha256': (None, False, 0, [], {}, ' ', 'a' * 63, 'a' * 65, 'g' * 64, 'a' * 63 + '\n', ' a' * 32),
        }
        for platform in ('windows', 'macos'):
            with self.subTest(platform=platform), tempfile.TemporaryDirectory() as temporary:
                root = self.fixture(Path(temporary), platform, pinned=False)
                marker_path = root / (('Contents/Resources/' if platform == 'macos' else '') + 'ffo-desktop-install.json')
                valid = json.loads(marker_path.read_text())
                for field, values in invalid_values.items():
                    # A missing field must not silently become an unsigned policy.
                    missing = dict(valid)
                    del missing[field]
                    marker_path.write_text(json.dumps(missing))
                    with self.subTest(field=field, missing=True), self.assertRaisesRegex(ValueError, field):
                        pack.inspect_tree(root, platform)
                    for value in values:
                        marker_path.write_text(json.dumps(dict(valid, **{field: value})))
                        with self.subTest(field=field, value=value), self.assertRaisesRegex(ValueError, field):
                            pack.inspect_tree(root, platform)
                # Pin fields are not rewritten or normalized during packaging.
                valid.update(macTeamId='A1B2C3D4E5', windowsSignerSha256='aB' * 32)
                marker_path.write_text(json.dumps(valid))
                self.assertEqual(pack.inspect_tree(root, platform)[2], valid)

    def test_csharp_fixtures_include_signed_mode_and_unsigned_packages(self):
        with tempfile.TemporaryDirectory() as temporary:
            output = Path(temporary) / 'packaged'
            subprocess.run([sys.executable, str(ROOT / 'Tests/DesktopUpdater/make-package-fixtures.py'), str(output)], check=True)
            self.assertEqual(sorted(path.name for path in output.glob('*.zip')),
                             ['macos-unsigned.zip', 'macos.zip', 'windows-unsigned.zip', 'windows.zip'])
            for path in sorted(output.glob('*.zip')):
                with self.subTest(package=path.name), zipfile.ZipFile(path) as archive:
                    marker_name = next(name for name in archive.namelist() if name.endswith('/ffo-desktop-install.json'))
                    marker = json.loads(archive.read(marker_name))
                    self.assertEqual(bool(marker['macTeamId'] or marker['windowsSignerSha256']), '-unsigned' not in path.stem)
                    self.assertEqual(marker['platform'], path.stem.split('-')[0])

    def test_unsigned_package_keeps_identity_bootstrap_and_resource_checks(self):
        cases = (
            ('marker', 'schemaVersion', True), ('marker', 'platform', 'android'),
            ('marker', 'version', '1.0.3-beta'), ('marker', 'installId', 'invalid'),
            ('marker', 'executable', '../outside.exe'),
            ('bootstrap', 'appVersion', '9.9.9'), ('bootstrap', 'developmentHttp', True),
            ('bootstrap', 'dispatchUrl', 'http://example.com/dispatch.json'),
            ('bootstrap', 'dispatchUrl', 'https://user:secret@example.com/dispatch.json'),
            ('bootstrap', 'legacyDispatchUrls', ['http://old.example.com/dispatch.json']),
        )
        for platform in ('windows', 'macos'):
            for target, field, value in cases:
                with self.subTest(platform=platform, field=field, value=value), tempfile.TemporaryDirectory() as temporary:
                    root = self.fixture(Path(temporary), platform, pinned=False)
                    if target == 'marker':
                        path = root / (('Contents/Resources/' if platform == 'macos' else '') + 'ffo-desktop-install.json')
                    else:
                        path = root / ('Contents/Resources/Data' if platform == 'macos' else 'fake-fantasy-online_Data') / 'StreamingAssets/resource-bootstrap.json'
                    metadata = json.loads(path.read_text())
                    metadata[field] = value
                    path.write_text(json.dumps(metadata))
                    with self.assertRaises(ValueError):
                        pack.inspect_tree(root, platform)
            for location in ('streaming', 'private'):
                with self.subTest(platform=platform, location=location), tempfile.TemporaryDirectory() as temporary:
                    root = self.fixture(Path(temporary), platform, pinned=False)
                    data = root / ('Contents/Resources/Data' if platform == 'macos' else 'fake-fantasy-online_Data')
                    destination = data / 'StreamingAssets/private.pkg' if location == 'streaming' else root / '.env'
                    destination.write_text('synthetic, not user data')
                    with self.assertRaises(ValueError):
                        pack.inspect_tree(root, platform)

    def test_unsigned_macos_requires_player_and_helper_execute_permissions(self):
        for relative in ('Contents/MacOS/fake-fantasy-online',
                         'Contents/Resources/Data/StreamingAssets/DesktopUpdater/DesktopUpdater-osx-arm64',
                         'Contents/Resources/Data/StreamingAssets/DesktopUpdater/DesktopUpdater-osx-x64'):
            with self.subTest(relative=relative), tempfile.TemporaryDirectory() as temporary:
                root = self.fixture(Path(temporary), 'macos', pinned=False)
                (root / relative).chmod(0o644)
                with self.assertRaisesRegex(ValueError, 'must be executable'):
                    pack.inspect_tree(root, 'macos')

    def test_rejects_private_data_links_and_case_collisions(self):
        for name in ('.env', 'LocalConfig/user.json', 'SavedVariables/private.json', 'secret.pfx', 'CON.txt', 'file:stream'):
            with self.subTest(name=name), tempfile.TemporaryDirectory() as temporary:
                root = self.fixture(Path(temporary))
                file = root / name
                file.parent.mkdir(parents=True, exist_ok=True)
                file.write_text('synthetic')
                with self.assertRaises(ValueError):
                    pack.inspect_tree(root, 'windows')
        with tempfile.TemporaryDirectory() as temporary:
            root = self.fixture(Path(temporary))
            (root / 'linked').symlink_to(root / 'fake-fantasy-online.exe')
            with self.assertRaises(ValueError):
                pack.inspect_tree(root, 'windows')
            (root / 'linked').unlink()
            (root / 'FAKE-FANTASY-ONLINE.EXE').write_text('duplicate')
            with self.assertRaises(ValueError):
                pack.inspect_tree(root, 'windows')

    def test_no_game_resources_or_invalid_bootstrap_in_zip(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = self.fixture(Path(temporary))
            streaming = root / 'fake-fantasy-online_Data/StreamingAssets'
            (streaming / 'private.pkg').write_text('synthetic')
            with self.assertRaisesRegex(ValueError, 'Only updater binaries'):
                pack.inspect_tree(root, 'windows')
            (streaming / 'private.pkg').unlink()
            config = streaming / 'resource-bootstrap.json'
            value = json.loads(config.read_text())
            value['appVersion'] = '9.9.9'
            config.write_text(json.dumps(value))
            with self.assertRaises(ValueError):
                pack.inspect_tree(root, 'windows')

    def test_zip64_and_entry_limits_fail_closed(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = self.fixture(Path(temporary))
            with patch.object(pack, 'MAX_ENTRIES', 1), self.assertRaisesRegex(ValueError, 'limits'):
                pack.inspect_tree(root, 'windows')
            with patch.object(pack, 'MAX_CENTRAL_DIRECTORY', 1), self.assertRaisesRegex(ValueError, 'limits'):
                pack.inspect_tree(root, 'windows')
            # Sparse synthetic file tests the standard-ZIP size bound without allocating payload bytes.
            with (root / 'large.bin').open('wb') as file:
                file.truncate(2 * 1024**3)
            with self.assertRaisesRegex(ValueError, 'ZIP64'):
                pack.inspect_tree(root, 'windows')
            self.assertLessEqual(pack.MAX_ENTRIES, 65534)
            self.assertIn('allowZip64=False', (ROOT / 'scripts/ci/package-desktop-update.py').read_text())

    def test_unconfigured_development_bootstrap_stays_offline(self):
        for legacy in ([], None, ['https://old.example.test/d'], 'invalid-list'):
            with self.subTest(legacy=legacy), tempfile.TemporaryDirectory() as temporary:
                output = Path(temporary) / 'synthetic.apk'
                with zipfile.ZipFile(output, 'w') as archive:
                    archive.writestr('lib/arm64-v8a/libil2cpp.so', b'synthetic')
                    archive.writestr('lib/arm64-v8a/libunity.so', b'synthetic')
                    archive.writestr('assets/resource-bootstrap.json', json.dumps({'schemaVersion': 1, 'mode': 'resource-free',
                        'appVersion': '1.0.3', 'dispatchUrl': '', 'legacyDispatchUrls': legacy, 'developmentHttp': False}))
                if legacy is None or legacy == []:
                    self.assertEqual(apk.verify(output)['status'], 'development-candidate')
                else:
                    with self.assertRaises(ValueError):
                        apk.verify(output)

    def test_formal_apk_metadata_is_honest_and_dispatch_bound(self):
        with tempfile.TemporaryDirectory() as temporary:
            output = Path(temporary) / 'candidate.apk'
            with zipfile.ZipFile(output, 'w') as archive:
                archive.writestr('lib/arm64-v8a/libil2cpp.so', b'synthetic')
                archive.writestr('lib/arm64-v8a/libunity.so', b'synthetic')
                archive.writestr('assets/resource-bootstrap.json', json.dumps({'schemaVersion': 1, 'mode': 'resource-free',
                    'appVersion': '1.0.3', 'dispatchUrl': 'https://example.com/dispatch.json', 'legacyDispatchUrls': ['https://retired.example.test/d'], 'developmentHttp': False}))
            result = apk.verify(output, configuration='formal', expected_dispatch='https://example.com/dispatch.json',
                                expected_legacy_dispatches=['https://retired.example.test/d'])
            with self.assertRaisesRegex(ValueError, 'legacy dispatch list'):
                apk.verify(output, configuration='formal', expected_legacy_dispatches=[])
            self.assertEqual(result['status'], 'formal-candidate')
            self.assertFalse(result['acceptance']['signingCertificate'])
            with self.assertRaises(ValueError):
                apk.verify(output, configuration='formal', expected_dispatch='https://different.example.com/d')


if __name__ == '__main__':
    unittest.main()
