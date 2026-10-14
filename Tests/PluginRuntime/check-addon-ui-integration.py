#!/usr/bin/env python3
"""Source-wiring regressions; complements actual UI state tests with Unity stubs."""
from pathlib import Path
import re
root = Path(__file__).resolve().parents[2]
reference = root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
checks = 0

def check(value, label):
    global checks
    assert value, label
    checks += 1

ui = (reference / 'AddonUiHost.cs').read_text()
hit_body = ui.split('public static bool MouseOver()', 1)[1].split('public static IReadOnlyDictionary', 1)[0]
check('panel.Visible && host.ResolveRect(panel).Contains(pointer)' in hit_body, 'only visible panels block using current geometry')
check('host.isActiveAndEnabled' in hit_body and 'host.disposed' in hit_body, 'inactive/disposed hosts cannot block world')
check('GameHud' not in hit_body and 'OverAnyUI' not in hit_body, 'no recursive addon/HUD pointer gate')
hud = (reference / 'GameHud.WindowHits.cs').read_text()
check(re.search(r'OverUI\s*=>\s*AddonUiHost\.MouseOver\(\)\s*\|\|', hud), 'independent collider gate includes addons')
player = (reference / 'PlayerController.OverAnyUI.cs').read_text()
check('if (AddonUiHost.MouseOver()) return true;' in player, 'player movement/NPC gate includes addons')
for source, side_effect in [('MonsterEntity.Click.cs', 'CombatWorld.SetTarget'),
                            ('RemotePlayer.Selection.cs', 'CombatWorld.SetPvpTarget'),
                            ('GroundItemEntity.Click.cs', 'WalkToPickup')]:
    text = (reference / source).read_text()
    check(text.index('GameHud.OverUI') < text.index(side_effect), source + ' gates before world action')
pointer = (reference / 'PlayerController.PointerFrame.cs').read_text()
check(': OverAnyUI();' in pointer, 'desktop pointer loop retains augmented gate')
check('MobileClientPresentation.IsPointerOverUi(pointer.Position)' in pointer, 'touch pointer loop uses owned screen position gate')
check('owner.OverAnyUI()' in (reference / 'ReferenceFirstWorldWindowOwner.cs').read_text(), 'wrapper pointer loop uses augmented gate')
bootstrap = (root / 'Assets/FakeFantasyOnline/Runtime/Bootstrap/PluginBootstrap.cs').read_text()
check('new PluginGameApi(() => view != null ? view.CurrentSessionForPlugins : null)' in bootstrap, 'session resolved at call time')
check(bootstrap.index('server?.Dispose()', bootstrap.index('public void Shutdown')) < bootstrap.index('game?.Dispose()') < bootstrap.index('ui?.Dispose()'), 'shutdown stops transport then game then UI')
check('ParseArguments(Environment.GetCommandLineArgs());' in bootstrap and 'if (optIn && validPort) SetBridge(true, agentPort);' in bootstrap, 'CLI bridge needs explicit opt in')
print(f'PASS {checks} addon/bootstrap source integration assertions (not runtime/PlayMode)')
