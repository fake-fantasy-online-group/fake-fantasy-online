#!/usr/bin/env python3
"""Static NPC menu wiring guards, supplementing the executed panel entry contracts."""
from pathlib import Path
root = Path(__file__).resolve().parents[2]
source = (root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/DialogUI.cs').read_text()
checks = [
    'if (NpcName.Contains("巧匠") || NpcName.Contains("左慈"))',
    '"周天鼎 · 问鼎炼化", () => OpenEndgameView(EndgameModule.Cube)',
    'if (NpcName.Contains("妖魔结界守卫") || NpcName.Contains("结界使"))',
    '"演劫洞天 · 查看符钥与开境", () => OpenEndgameView(EndgameModule.Rift)',
    'module == EndgameModule.Rift ? EndgamePanel.TryOpenRiftFromWorld() :',
    'module == EndgameModule.Runes ? EndgamePanel.TryOpenRunesFromWorld() : EndgamePanel.TryOpenCubeFromWorld()',
    '"星篆 · 镶嵌取出", () => OpenEndgameView(EndgameModule.Runes)',
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
assert source.count('if (EndgamePanel.WorldEntryAvailable)') == 2
assert 'if (EndgamePanel.WorldEntryAvailable &&' in source
print(f'PASS {len(checks) + 2} NPC menu wiring guards; native trial, life-skill and furnace paths retained.')
