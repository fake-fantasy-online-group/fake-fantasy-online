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
    'desktop keeps default startup': 'InitializeClient(PrivateServerOptions.LocalDefault());' in bootstrap,
}
for name, ok in checks.items():
    assert ok, name
print(f'PASS mobile startup source contracts ({len(checks)} checks; not runtime rendering)')
