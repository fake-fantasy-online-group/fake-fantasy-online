#!/usr/bin/env python3
"""Exercise helper CLI policy with a fake dotnet command. Never compiles or downloads software."""
from pathlib import Path
import os
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / 'scripts/build-desktop-updater.sh'
FAKE = '''#!/usr/bin/env python3
import json, os, pathlib, sys
if sys.argv[1:] == ['--list-sdks']:
 print(os.environ['FAKE_SDKS']); raise SystemExit(0)
assert sys.argv[1] == 'publish'
assert json.loads(pathlib.Path('global.json').read_text())['sdk']['version'] == os.environ['EXPECTED_SDK']
args=sys.argv
out=pathlib.Path(args[args.index('-o')+1]); out.mkdir(parents=True)
rid=args[args.index('-r')+1]
(out/('DesktopUpdater.exe' if rid=='win-x64' else 'DesktopUpdater')).write_text('synthetic never execute')
'''
class BuildHelperTests(unittest.TestCase):
    def run_script(self, platform, sdk='10.0.302 [fake]'):
        temp = tempfile.TemporaryDirectory(); self.addCleanup(temp.cleanup)
        root=Path(temp.name); executable=root/'dotnet'; executable.write_text(FAKE); executable.chmod(0o755)
        env=dict(os.environ,PATH=str(root)+os.pathsep+os.environ['PATH'],FAKE_SDKS=sdk,EXPECTED_SDK='10.0.302')
        output=root/'result'
        result=subprocess.run(['bash',str(SCRIPT),platform,str(output)],env=env,text=True,capture_output=True)
        return result,output
    def test_sdk10_targets_project_without_sdk8(self):
        result,output=self.run_script('windows')
        self.assertEqual(result.returncode,0,result.stderr)
        self.assertEqual(sorted(x.name for x in output.iterdir()),['DesktopUpdater.exe'])
    def test_both_mac_architectures(self):
        result,output=self.run_script('macos')
        self.assertEqual(result.returncode,0,result.stderr)
        self.assertEqual(sorted(x.name for x in output.iterdir()),['DesktopUpdater-osx-arm64','DesktopUpdater-osx-x64'])
        self.assertTrue(all(x.stat().st_mode & 0o111 for x in output.iterdir()))
    def test_preview_only_is_rejected(self):
        result,output=self.run_script('windows','10.0.100-preview.1 [fake]')
        self.assertNotEqual(result.returncode,0)
        self.assertFalse(output.exists())
    def test_old_sdk_is_rejected(self):
        result,output=self.run_script('windows','6.0.428 [fake]')
        self.assertNotEqual(result.returncode,0)
        self.assertFalse(output.exists())
if __name__=='__main__': unittest.main()
