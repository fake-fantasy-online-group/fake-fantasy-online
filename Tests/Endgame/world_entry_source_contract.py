#!/usr/bin/env python3
"""Static NPC menu wiring guards, supplementing the executed panel entry contracts."""
from pathlib import Path
root = Path(__file__).resolve().parents[2]
source = (root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/DialogUI.cs').read_text()
checks = [
    'if (NpcName.Contains("巧匠") || NpcName.Contains("左慈"))',
    'AddCubeWorkbenchOption();',
    'if (NpcName.Contains("妖魔结界守卫") || NpcName.Contains("结界使"))',
    'AddRiftEntranceOption();',
    'Open(GameBootstrap.I?.Window?.NpcStateForIndependentRuntime(npc), npc);',
    'CaptureRiftEntrance(npc);',
    'module == EndgameModule.Rift ? EndgamePanel.TryOpenRiftFromWorld() :',
    '"符钥何用？", () => ShowSay(',
    '(NpcName.Contains("试炼小仙") || NpcName.Contains("试炼大仙"))',
    'AddTrialOption("领取试炼任务", "trialtask")',
    'AddTrialOption("交任务·领取试炼奖励", "trialclaim")',
    'AddTrialOption("进入妖魔结界(试炼)", "trial")',
    'Visible = false; LastOpenWasFurnace = true; ComposeDlg.Open(0); return true;',
    'Win05.OpenLifeLearn();',
]
for snippet in checks:
    assert snippet in source, snippet
assert '星篆 · 镶嵌取出' not in source and 'EndgameModule.Runes' not in source
assert 'CaptureCubeWorkbench(npc);' in source
cube = (root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/DialogUI.Cube.cs').read_text()
assert 'EndgamePanel.TryOpenCubeFromNpc' in cube and 'source.Script == "Nzhoutianding"' in cube
assert 'IsNpcStateCurrentForIndependentRuntime(source)' in cube and 'state.CubeInputsV1' in cube
print(f'PASS {len(checks) + 2} NPC menu wiring guards; native trial, life-skill and furnace paths retained.')

branch = source[source.index('if (NpcName.Contains("妖魔结界守卫")'):source.index('if (NpcName.Contains("试炼大仙")')]
assert 'OpenEndgameView' not in branch and 'WorldEntryAvailable' not in branch
entrance = (root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/DialogUI.RiftEntrance.cs').read_text()
assert '八仙使者' not in entrance and '妖魔结界守卫' not in entrance
assert 'source.DisplayName, source.Script' in entrance and 'IsNpcStateCurrentForIndependentRuntime(source)' in entrance
assert 'return pair.Value.State' in (root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/ReferenceFirstWorldWindowOwner.cs').read_text()
print('PASS server-owned menu identity and native source wiring guards')
