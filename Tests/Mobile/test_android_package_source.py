#!/usr/bin/env python3
"""Static security/lifecycle contract. These assertions do not simulate an Android device."""
from pathlib import Path
import re
import copy
import importlib.util
import json
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
PLUGIN = ROOT / 'Assets/Plugins/Android/ClientPackageInstaller.androidlib'
JAVA = (PLUGIN / 'src/main/java/org/fakefantasyonline/updates/ClientPackageInstaller.java').read_text()
POLICY = (PLUGIN / 'src/main/java/org/fakefantasyonline/updates/PackagePolicy.java').read_text()
CS = (ROOT / 'Assets/FakeFantasyOnline/Runtime/Bootstrap/AndroidClientPackageInstaller.cs').read_text()
_spec = importlib.util.spec_from_file_location('unity_dependencies', ROOT / 'Tests/Mobile/AndroidPackageInstaller/validate_unity_dependencies.py')
UNITY_DEPS = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(UNITY_DEPS)

def body(name):
    match = re.search(r'(?m)^    (?:public |private )?static [\w\[\]]+ ' + name + r'\(', JAVA)
    if not match: raise AssertionError('missing method ' + name)
    start = match.start()
    start = JAVA.index('{', start)
    depth = 1
    for i in range(start + 1, len(JAVA)):
        depth += (JAVA[i] == '{') - (JAVA[i] == '}')
        if depth == 0:
            return JAVA[start:i+1]
    raise AssertionError(name)

class AndroidPackageSourceContract(unittest.TestCase):
    def test_actual_project_enables_jni_module(self):
        UNITY_DEPS.validate_project(ROOT)

    def test_missing_or_invalid_jni_project_wiring_fails_closed(self):
        manifest = json.loads((ROOT / 'Packages/manifest.json').read_text())
        lock = json.loads((ROOT / 'Packages/packages-lock.json').read_text())
        assembly = json.loads((ROOT / 'Assets/FakeFantasyOnline/Runtime/Bootstrap/FakeFantasyOnline.Bootstrap.asmdef').read_text())
        for failure in ('manifest_missing', 'lock_missing', 'lock_registry', 'lock_version', 'engine_disabled', 'android_excluded'):
            m, l, a = copy.deepcopy((manifest, lock, assembly))
            if failure == 'manifest_missing': del m['dependencies'][UNITY_DEPS.JNI_PACKAGE]
            if failure == 'lock_missing': del l['dependencies'][UNITY_DEPS.JNI_PACKAGE]
            if failure == 'lock_registry': l['dependencies'][UNITY_DEPS.JNI_PACKAGE]['source'] = 'registry'
            if failure == 'lock_version': l['dependencies'][UNITY_DEPS.JNI_PACKAGE]['version'] = '2.0.0'
            if failure == 'engine_disabled': a['noEngineReferences'] = True
            if failure == 'android_excluded': a['excludePlatforms'] = ['Android']
            with self.subTest(failure=failure), self.assertRaises(ValueError):
                UNITY_DEPS.validate(m, l, a)

    def test_no_provider_or_exported_component(self):
        manifest = ET.parse(PLUGIN / 'src/main/AndroidManifest.xml').getroot()
        android = '{http://schemas.android.com/apk/res/android}'
        self.assertEqual([x.get(android + 'name') for x in manifest.findall('uses-permission')], ['android.permission.REQUEST_INSTALL_PACKAGES'])
        self.assertFalse(manifest.findall('.//provider'))
        self.assertEqual(manifest.find('.//receiver').get(android + 'exported'), 'false')
        self.assertFalse(manifest.findall('.//intent-filter'))
    def test_exact_current_signers_and_monotone_version(self):
        self.assertIn('GET_SIGNING_CERTIFICATES', JAVA)
        self.assertIn('GET_SIGNATURES', JAVA)
        self.assertIn('getApkContentsSigners()', JAVA)
        self.assertNotIn('getSigningCertificateHistory()', JAVA)
        self.assertIn('incomingCode <= installedCode', POLICY)
        self.assertIn('signerIdentity(installedSigners).equals(signerIdentity(incomingSigners))', POLICY)
        self.assertIn('expectedVersion.equals(incomingVersion)', POLICY)
    def test_reverify_private_bytes_and_session(self):
        self.assertIn('app.getCacheDir()', body('prepareWorker'))
        self.assertIn('File.createTempFile', JAVA)
        self.assertIn('transfer(current, input, output', body('prepareWorker'))
        self.assertIn('session.openRead("base.apk")', body('stage'))
        self.assertLess(body('stage').index('session.openRead'), body('stage').index('session.commit'))
        self.assertEqual(body('stage').count('validate(source, version)'), 2)
        self.assertIn('count, digest.digest()', body('transfer'))
    def test_explicit_mutable_session_scoped_callback(self):
        stage = body('stage')
        self.assertIn('new Intent(app, InstallStatusReceiver.class)', stage)
        self.assertIn('PendingIntent.FLAG_MUTABLE', stage)
        self.assertNotIn('FLAG_ONE_SHOT', JAVA)
        self.assertIn('USER_ACTION_REQUIRED', stage)
        self.assertNotIn('USER_ACTION_NOT_REQUIRED', JAVA)
        self.assertLess(stage.index('record()'), stage.index('session.openWrite'))
        self.assertGreaterEqual(stage.count('record()'), 2)
        self.assertIn('installer.getMySessions()', body('cleanupOrphanedSessions'))
        receive = body('receive')
        self.assertIn('intent.getData()', receive)
        self.assertIn('EXTRA_SESSION_ID', receive)
        self.assertIn('observedOutcome != outcomeEpoch', receive)
    def test_no_silent_or_background_permission_grant(self):
        self.assertIn('isDeviceOwnerApp', body('requireInteractiveInstaller'))
        self.assertIn('isProfileOwnerApp', body('requireInteractiveInstaller'))
        self.assertIn('Manifest.permission.INSTALL_PACKAGES', body('requireInteractiveInstaller'))
        self.assertNotIn('startActivity', body('poll'))
        self.assertNotIn('startActivity', body('receive'))
        self.assertIn('ACTION_MANAGE_UNKNOWN_APP_SOURCES', body('openPermissionSettings'))
        self.assertIn('ACTION_SECURITY_SETTINGS', body('openPermissionSettings'))
        self.assertIn('canRequestPackageInstalls()', JAVA)
        self.assertNotRegex(JAVA, r'(grantRuntimePermission|setPermissionGrantState|Settings\.\w+\.put)')
    def test_cancel_and_failure_are_not_install_success(self):
        self.assertIn('STATUS_FAILURE_ABORTED', body('receive'))
        self.assertIn('finish("Cancelled"', body('receive'))
        self.assertIn('STATUS_FAILURE_BLOCKED', body('receive'))
        self.assertIn('if ("AwaitingResult".equals(state))', body('cancel'))
        self.assertIn('abandon(sessionId)', body('cancel'))
        self.assertIn('++generation', body('cancel'))
        self.assertNotIn('Succeeded', body('stage'))
        self.assertIn('successObserved', body('initialize'))
        self.assertIn('putBoolean("successObserved", true).commit()', body('receive'))
        self.assertIn('isExpectedInstalled()', body('receive'))
        self.assertIn('Interrupted', body('initialize'))
    def test_off_main_thread_and_platform_fallback(self):
        self.assertGreaterEqual(JAVA.count('WORKER.execute('), 4)
        self.assertIn('activity.runOnUiThread', body('openSystemConfirmation'))
        self.assertIn('#if UNITY_ANDROID && !UNITY_EDITOR', CS)
        self.assertIn('AndroidClientPackageInstallState.Unsupported', CS)
        self.assertIn('catch (Exception exception)', CS)
        self.assertIn('Debug.LogWarning', CS)
        self.assertIn('consumerProguardFiles', (PLUGIN / 'build.gradle').read_text())
        self.assertIn('ClientPackageInstaller { public static *; }', (PLUGIN / 'consumer-rules.pro').read_text())
    def test_no_newer_java_runtime_helpers(self):
        self.assertNotIn('String.join(', POLICY)
        self.assertNotIn('HexFormat', POLICY)

if __name__ == '__main__': unittest.main()
