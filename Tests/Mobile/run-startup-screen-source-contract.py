#!/usr/bin/env python3
"""Guard startup ownership and measured progress; device rendering is separate acceptance."""
from pathlib import Path
root = Path(__file__).resolve().parents[2]
installer = (root / 'Assets/FakeFantasyOnline/Runtime/Bootstrap/MobileResourceInstaller.cs').read_text()
bootstrap = (root / 'Assets/FakeFantasyOnline/Runtime/Bootstrap/ClientBootstrap.cs').read_text()
checks = {
    'initial camera explicitly clears framebuffer': 'clearCamera.clearFlags = CameraClearFlags.SolidColor;' in installer,
    'clear camera does not render game geometry': 'clearCamera.cullingMask = 0;' in installer,
    'opaque asset independent fallback': 'Texture2D.whiteTexture' in installer and 'Screen.width, Screen.height' in installer,
    'file work starts after initial frame': installer.index('yield return null;') < installer.index('Directory.CreateDirectory(storage)'),
    'hash progress is atomic': 'preceding + bytes' in installer and 'Interlocked.Read(ref completedBytes)' in installer,
    'copy progress comes from transferred bytes': 'request.downloadedBytes' in installer,
    'unknown client startup has no percentage': 'Preparing the title screen...", 0)' in installer,
    'screen survives resource verification': 'Destroy(installer)' not in bootstrap,
    'title completion releases screen': bootstrap.index('_view.BindTitleBootstrapForIndependentRuntime(title);') < bootstrap.index('startup.Complete();'),
    'release waits for render': 'yield return new WaitForEndOfFrame();' in installer,
    'failure keeps screen visible': 'if (!failed) StartCoroutine' in installer,
    'GUI state is restored': 'GUI.matrix = oldMatrix' in installer and 'GUI.color = oldColor' in installer,
    'notches and landscape use safe area': 'Rect safe = Screen.safeArea;' in installer,
    'all platforms pass update gate': 'StartCoroutine(InitializeWithUpdateGate());' in bootstrap and 'InitializeClient(PrivateServerOptions.LocalDefault());' not in bootstrap,
    'only one editable address': installer.count('GUI.TextField(') == 1 and 'FFO.ResourceManifestUrl' not in installer,
    'development local mode explicit': 'allowBundled = Debug.isDebugBuild || UnityEngine.Application.isEditor;' in installer,
    'required upgrade blocks entering': 'updatePlan != null && !updatePlan.Required' in installer,
    'dispatch precedes manifest': installer.index('ReadDispatch(selectedDispatch') < installer.index('installer.Install(plan.ResourceManifestUrl'),
    'pause cancels actual work': 'remoteCancellation.Cancel();' in installer,
    'no remote execution': 'Assembly.Load' not in installer and 'Process.Start' not in installer,
    'atomic endpoint persistence': 'ClientUpdateAddressStore.Save(' in installer,
    'first run opens configuration': 'configurationOpen = configurationRequired;' in installer,
    'configured start requests check': 'if (!configurationRequired) downloadRequested = true;' in installer,
    'config confirmation owns address mutation': 'dispatchUrl = selected;' in installer and 'draftDispatchUrl = GUI.TextField(' in installer,
    'source-owned icon tooltips': 'repairIcon, "修复资源"' in installer and 'configureIcon, "配置更新地址"' in installer,
    'no details control': '查看详情' not in installer and 'showDetails' not in installer,
    'errors use safe summary and retain full log': 'failed ? conciseError : status' in installer and '"Remote resource setup stopped: " + exception' in installer,
    'speed measures network only': 'phase == "Downloading resources"' in installer and 'SpeedText(downloadBytesPerSecond)' in installer,
    'root activation requires user entry': installer.index('if (enterRequested)') < installer.index('ClientResourcePaths.Activate(verifiedRoot);'),
}
for name, ok in checks.items():
    assert ok, name
print(f'PASS unified startup source contracts ({len(checks)} checks; not runtime rendering)')

# Match source geometry across safe areas; these are geometry checks, not Unity rendering.
import re
layout = (root / 'Assets/FakeFantasyOnline/Runtime/Foundation/ClientUpdateLayout.cs').read_text()
v = {k: float(n) for k, n in re.findall(r'(\w+)\s*=\s*(\d+)', layout)}
for w, h in [(800, 600), (960, 540), (1280, 720), (1920, 1080), (640, 320)]:
    for configure in [False, True]:
        ph = v['ConfigHeight'] if configure else v['Height']
        scale = min(1, (w - 32) / v['Width'], (h - v['Bottom'] - 16) / ph)
        x = (w - v['Width'] * scale) / 2
        y = h - (ph + v['Bottom']) * scale
        assert x >= 0 and y >= 0 and x + v['Width'] * scale <= w and y + ph * scale <= h
        assert v['Padding'] + v['ProgressWidth'] < v['Width'] - v['Padding'] - v['SpeedWidth']
        assert v['RepairX'] + v['IconSize'] < (v['Width'] - 244) / 2
        assert (v['Width'] + 244) / 2 < v['ConfigureX']
        assert v['ConfirmY'] + v['ButtonHeight'] <= v['ConfigHeight']
print('PASS unified startup geometry (10 viewport/config combinations; icons and rate do not overlap)')
