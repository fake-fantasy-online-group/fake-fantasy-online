import contextlib
import importlib.util
import io
import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest
import zipfile

ROOT = Path(__file__).resolve().parents[2]


def module(path, name):
    spec = importlib.util.spec_from_file_location(name, ROOT / path)
    loaded = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(loaded)
    return loaded


manifest = module('scripts/generate-resource-manifest.py', 'manifest')
apkcheck = module('scripts/ci/verify-resource-free-apk.py', 'apkcheck')


class BuildContract(unittest.TestCase):
    def test_application_version_single_source(self):
        self.assertEqual(apkcheck.setting('bundleVersion'), '1.0.0')
        desktop = (ROOT / 'Assets/FakeFantasyOnline/Editor/ClientBuild.cs').read_text()
        self.assertNotIn('PlayerSettings.bundleVersion =', desktop)
        mobile = (ROOT / 'Assets/FakeFantasyOnline/Editor/MobileClientBuild.cs').read_text()
        self.assertIn('appVersion = PlayerSettings.bundleVersion', mobile)
        self.assertIn('AndroidArchitecture.ARM64', mobile)
        self.assertIn('ScriptingImplementation.IL2CPP', mobile)
        self.assertIn('ClientBuild.PrepareClientScene(resourceFree)', mobile)
        self.assertIn('if (!resourceFree) ConfigureBranding();', desktop)
        self.assertIn('mode != "offline" && mode != "bundled" && mode != "resource-free"', mobile)
        free_branch = mobile.split('if (resourceFree)', 1)[1].split('\n            else\n', 1)[0]
        self.assertNotIn('GenerateManifest(', free_branch)
        self.assertNotIn('Directory.GetFiles(', free_branch)
        self.assertEqual(apkcheck.setting('AndroidBundleVersionCode'), '1')

    def test_manual_only_credential_boundary(self):
        workflow = (ROOT / '.github/workflows/android-resource-free.yml').read_text()
        self.assertIn('workflow_dispatch:', workflow)
        self.assertNotIn('pull_request:', workflow)
        self.assertNotIn('pull_request_target:', workflow)
        self.assertIn('github.event.repository.default_branch', workflow)
        self.assertIn('contents: read', workflow)
        self.assertNotIn('contents: write', workflow)
        self.assertIn('persist-credentials: false', workflow)
        self.assertIn('unityci/editor@sha256:', workflow)
        self.assertIn("|| 'personal-pending'", workflow)
        self.assertNotIn('create-release', workflow)
        static = (ROOT / '.github/workflows/client-static.yml').read_text()
        self.assertIn('pull_request:', static)
        self.assertNotIn('secrets.', static)
        self.assertNotIn('pull_request_target:', static)

    def test_missing_license_blocks_without_exposing_values(self):
        for mode, license_text, expected in [('', '', 1), ('personal', 'test-only-not-a-license', 1), ('pro-ulf', '', 1), ('pro-ulf', 'test-only-not-a-license', 0)]:
            env = dict(os.environ, UNITY_CI_LICENSE_MODE=mode, UNITY_LICENSE=license_text)
            env.pop('GITHUB_STEP_SUMMARY', None)
            result = subprocess.run(['bash', str(ROOT / 'scripts/ci/license-preflight.sh')], env=env, capture_output=True, text=True)
            self.assertEqual(result.returncode, expected)
            self.assertNotIn('test-only-not-a-license', result.stdout + result.stderr)

    def test_no_resource_tree_enumeration_in_stage(self):
        stage = (ROOT / 'Assets/FakeFantasyOnline/Editor/ResourceFreeBuildStage.cs').read_text()
        self.assertNotIn('Directory.GetFiles', stage)
        self.assertNotIn('SearchOption.AllDirectories', stage)
        self.assertNotIn('Directory.Delete', stage)

    def test_manifest_hash_layout_and_excluded_code(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            for name, content in [('ReferenceClient/packages/a.pkg', b'package'), ('ReferenceClient/music/M001.ogg', b'music'), ('ReferenceClient/font/gbk12.idx', b'font-index'), ('ReferenceClient/font/gbk12.wid', b'font-width'), ('ReferenceClient/creation-textures/hero.png', b'passive-fixture'), ('AddOns/example/main.lua', b'untrusted'), ('mobile-dispatch.txt', b'endpoint')]:
                file = root / name
                file.parent.mkdir(parents=True, exist_ok=True)
                file.write_bytes(content)
            text = manifest.generate(root, '1.0.0')
            self.assertTrue(text.startswith('FFO-MOBILE-1\n@client\tandroid\t1.0.0\n'))
            self.assertIn('ReferenceClient/packages/a.pkg\tdata/a.pkg\t7\t', text)
            self.assertIn('ReferenceClient/font/gbk12.idx\tReferenceClient/font/gbk12.idx\t10\t', text)
            self.assertIn('ReferenceClient/creation-textures/hero.png', text)
            self.assertNotIn('AddOns', text)
            self.assertNotIn('mobile-dispatch', text)
            (root / 'malware.dll').write_bytes(b'not executable test data')
            with self.assertRaises(ValueError):
                manifest.generate(root, '1.0.0')

    def test_manifest_targets_and_stable_versions(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp) / 'resources'
            (root / 'data').mkdir(parents=True)
            (root / 'data' / 'a.pkg').write_bytes(b'synthetic resource')
            for platform in ('android', 'windows', 'macos', 'linux'):
                self.assertTrue(manifest.generate(root, '1.0.0', platform).startswith(
                    'FFO-MOBILE-1\n@client\t' + platform + '\t1.0.0\n'))
            for platform in ('ios', 'any', '', 'Windows'):
                with self.assertRaises(ValueError):
                    manifest.generate(root, '1.0.0', platform)
            for version in ('01.0.0', '1.0.0-rc.1', '1.0.0+build', '1.0', '4294967296.0.0'):
                with self.assertRaises(ValueError):
                    manifest.generate(root, version)
            self.assertIn('@client\tlinux\t4294967295.0.0', manifest.generate(root, '4294967295.0.0', 'linux'))
            output = Path(temp) / 'windows.tsv'
            result = subprocess.run([os.sys.executable, str(ROOT / 'scripts/generate-resource-manifest.py'),
                str(root), '--client-version', '1.0.0', '--client-platform', 'windows', '--output', str(output)],
                text=True, capture_output=True)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn('@client\twindows\t1.0.0', output.read_text())

    def test_manifest_rejects_case_collision_symlinks_and_empty(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / 'data').mkdir()
            for name in ['a.pkg', 'A.pkg']:
                (root / 'data' / name).write_bytes(b'x')
            with self.assertRaises(ValueError):
                manifest.generate(root, '1.0.0')
            (root / 'data/A.pkg').unlink()
            (root / 'data/link.pkg').symlink_to(root / 'data/a.pkg')
            with self.assertRaises(ValueError):
                manifest.generate(root, '1.0.0')
            (root / 'data/link.pkg').unlink()
            (root / 'data/a.pkg').write_bytes(b'')
            with self.assertRaises(ValueError):
                manifest.generate(root, '1.0.0')

    def test_manifest_rejects_directory_case_drift(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            for path in ['gameassets/Tables/a.json', 'gameassets/tables/b.json']:
                file = root / path
                file.parent.mkdir(parents=True, exist_ok=True)
                file.write_text('{}')
            with self.assertRaises(ValueError):
                manifest.generate(root, '1.0.0')

    def test_manifest_rejects_runtime_reserved_names_and_controls(self):
        paths = ['data/CON.pkg', 'gameassets/nul.json', 'gameassets/AUX/file.json',
                 'gameassets/PrN.bin', 'data/COM1.pkg', 'gameassets/LPT9.txt']
        paths += ['gameassets/bad' + char + '.json' for char in '<>"|*\x7f\x85\x9f']
        for path in paths:
            with self.subTest(path=path), self.assertRaises(ValueError):
                manifest.validate_path(path)
        for path in ['data/console.pkg', 'gameassets/COM0.json', 'gameassets/lpt10.txt',
                     'ReferenceClient/font/gbk12.idx', 'gameassets/中文.json']:
            with self.subTest(path=path):
                manifest.validate_path(path)
        # C# counts UTF-16 code units rather than Python's Unicode code points.
        with self.assertRaises(ValueError):
            manifest.validate_path('gameassets/' + '😀' * 510 + '.json')

    def test_manifest_generation_enforces_reserved_directory_segments(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            file = root / 'gameassets/CON/data.json'
            file.parent.mkdir(parents=True)
            file.write_text('{}')
            with self.assertRaisesRegex(ValueError, 'Reserved resource filename'):
                manifest.generate(root, '1.0.0')

    def make_apk(self, path, extra=None, version='1.0.0'):
        with zipfile.ZipFile(path, 'w') as z:
            # Deliberately synthetic fixtures; no assertion of valid Android packaging.
            z.writestr('lib/arm64-v8a/libil2cpp.so', b'synthetic library fixture')
            z.writestr('lib/arm64-v8a/libunity.so', b'synthetic library fixture')
            z.writestr('assets/resource-bootstrap.json', json.dumps({'schemaVersion': 1, 'mode': 'resource-free', 'appVersion': version}))
            if extra:
                z.writestr(extra, b'x')

    def test_apk_structure_checker_and_metadata(self):
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / 'candidate.apk'
            self.make_apk(path)
            with contextlib.redirect_stdout(io.StringIO()):
                result = apkcheck.verify(path)
            self.assertEqual(result['status'], 'development-candidate')
            self.assertFalse(result['acceptance']['productionReady'])
            self.assertEqual(len(result['sha256']), 64)
            self.assertTrue(Path(str(path) + '.build.json').exists())
            for extra in ['assets/data/private.pkg', 'assets/AddOns/main.lua', 'lib/armeabi-v7a/libil2cpp.so']:
                self.make_apk(path, extra=extra)
                with self.assertRaises(ValueError):
                    apkcheck.verify(path)
            self.make_apk(path, version='9.9.9')
            with self.assertRaises(ValueError):
                apkcheck.verify(path)


if __name__ == '__main__':
    unittest.main()
