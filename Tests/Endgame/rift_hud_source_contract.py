"""Keep the native target render/hit paths on the same compact rift offset."""
from pathlib import Path
p = Path(__file__).resolve().parents[2] / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
target = (p / 'ReferenceFirstWorldTargetHud.cs').read_text()
hit = (p / 'GameHud.BasicHit.cs').read_text()
for token in ('108f + EndgamePanel.RiftTargetHudOffset', '113f + EndgamePanel.RiftTargetHudOffset',
              '122f + EndgamePanel.RiftTargetHudOffset', '130f + EndgamePanel.RiftTargetHudOffset',
              '108f + 37f + EndgamePanel.RiftTargetHudOffset'):
    assert token in target, token
assert 'float top = (108f + EndgamePanel.RiftTargetHudOffset) * scale;' in target
assert '(108f + EndgamePanel.RiftTargetHudOffset) * scale' in hit
hud = (p / 'EndgamePanel.Rift.cs').read_text().split('private void DrawRiftHud(', 1)[1].split('private void DrawRiftMarkers(', 1)[0]
assert 'Screen.width * .5f' not in hud and 'ArtworkSkin.Button' not in hud and 'DrawRiftWindowArtwork' not in hud
art = (p / 'EndgamePanel.RiftArtwork.cs').read_text()
assert 'res/uires/gameui/intonatebar.gsa' in art and 'res/uires/gameui/intonatebarbk.gsa' in art
assert 'elapsed > progress' in art and 'GUI.color = color;' in art
print('PASS rift HUD native-target offset/render/hit/source wiring (11 checks)')
