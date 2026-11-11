"""Debug-only login-page server switch: compile-time gating, build flag and artifact check."""
import importlib.util
from pathlib import Path
import re
import tempfile
import unittest
import zipfile

ROOT = Path(__file__).resolve().parents[2]
RUNTIME = ROOT / 'Assets/FakeFantasyOnline/Runtime'
DEFINE = 'FFO_DEBUG_SERVER_SWITCH'


def build_module():
    spec = importlib.util.spec_from_file_location('build_resource_free', ROOT / 'scripts/ci/build-resource-free.py')
    loaded = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(loaded)
    return loaded


build = build_module()
BASE = ['--platform', 'macos', '--version', '1.0.6', '--dispatch-url', 'https://example.test/dispatch.json']


class DebugServerSwitchContract(unittest.TestCase):
    def test_runtime_code_is_compiled_only_with_define(self):
        for relative in ('Protocol/DebugServerSwitchStore.cs', 'Presentation/ReferenceAssets/TitleFlow.DebugServerSwitch.cs'):
            lines = (RUNTIME / relative).read_text(encoding='utf-8').strip().splitlines()
            self.assertEqual(lines[0], '#if ' + DEFINE, relative)
            self.assertEqual(lines[-1], '#endif', relative)
        # Every mention of the switch, in any runtime file, sits inside an #if FFO_DEBUG_SERVER_SWITCH region.
        mentions = 0
        for path in RUNTIME.rglob('*.cs'):
            stack = []
            for number, line in enumerate(path.read_text(encoding='utf-8').splitlines(), 1):
                stripped = line.strip()
                if stripped.startswith('#if'):
                    stack.append(stripped)
                elif stripped.startswith('#endif'):
                    stack.pop()
                elif 'DebugServerSwitch' in line:
                    mentions += 1
                    self.assertIn('#if ' + DEFINE, stack, '%s:%d' % (path, number))
        self.assertGreater(mentions, 4)

    def test_define_is_per_build_never_project_settings(self):
        self.assertNotIn(DEFINE, (ROOT / 'ProjectSettings/ProjectSettings.asset').read_text(encoding='utf-8'))
        for relative in ('Assets/FakeFantasyOnline/Editor/DesktopResourceFreeBuild.cs', 'Assets/FakeFantasyOnline/Editor/MobileClientBuild.cs'):
            source = (ROOT / relative).read_text(encoding='utf-8')
            self.assertIn('extraScriptingDefines =', source)
            self.assertNotIn('SetScriptingDefineSymbols', source)

    def test_switch_has_no_private_hosts(self):
        for relative in ('Protocol/DebugServerSwitchStore.cs', 'Presentation/ReferenceAssets/TitleFlow.DebugServerSwitch.cs'):
            source = (RUNTIME / relative).read_text(encoding='utf-8')
            hosts = set(re.findall(r'\b\d{1,3}(?:\.\d{1,3}){3}\b', source))
            self.assertLessEqual(hosts, {'127.0.0.1'}, relative)
            self.assertNotRegex(source, r'https?://')

    def test_flag_rejected_outside_development(self):
        for extra in (['--debug-server-switch'], ['--debug-server-switch', '--configuration', 'formal'],
                      ['--debug-server-switch', '--configuration', 'development', '--release']):
            with self.assertRaisesRegex(ValueError, 'debug-server-switch'):
                build.main(BASE + extra)

    def test_artifact_check(self):
        with tempfile.TemporaryDirectory() as temp:
            app = Path(temp) / 'client.app'
            managed = app / 'Contents/Resources/Data/Managed'
            managed.mkdir(parents=True)
            (managed / 'FakeFantasyOnline.Presentation.dll').write_bytes(b'MZ...TitleFlow...')
            self.assertEqual(build.verify_debug_server_switch(app, 'macos', False), [])
            with self.assertRaises(ValueError):
                build.verify_debug_server_switch(app, 'macos', True)
            (managed / 'FakeFantasyOnline.Protocol.dll').write_bytes(b'MZ...DebugServerSwitchStore...')
            self.assertEqual(len(build.verify_debug_server_switch(app, 'macos', True)), 1)
            with self.assertRaises(ValueError):
                build.verify_debug_server_switch(app, 'macos', False)
            with self.assertRaises(ValueError):
                build.verify_debug_server_switch(Path(temp) / 'empty.app', 'macos', False)
            exe = Path(temp) / 'win/fake.exe'
            (exe.parent / 'fake_Data/Managed').mkdir(parents=True)
            (exe.parent / 'fake_Data/Managed/FakeFantasyOnline.Protocol.dll').write_bytes(b'plain')
            self.assertEqual(build.verify_debug_server_switch(exe, 'windows', False), [])
            apk = Path(temp) / 'client.apk'
            with zipfile.ZipFile(apk, 'w') as archive:
                archive.writestr('assets/bin/Data/Managed/Metadata/global-metadata.dat', b'..DebugServerSwitchBindings..')
            self.assertEqual(len(build.verify_debug_server_switch(apk, 'android', True)), 1)
            with self.assertRaises(ValueError):
                build.verify_debug_server_switch(apk, 'android', False)


if __name__ == '__main__':
    unittest.main()
