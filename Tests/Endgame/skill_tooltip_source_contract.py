#!/usr/bin/env python3
"""Source guards complement executed native skill layout/render tests.

The managed harness does not compile the entire Unity window. These guards pin its
actual dispatch, starter/life exclusions and separate native fallback. They are not
Unity execution or screenshot evidence.
"""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
PRESENTATION = ROOT / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
growth = (PRESENTATION / 'Win05.GrowthSkills.cs').read_text()
dialog = (PRESENTATION / 'Win05.SkillDialog.cs').read_text()
compose = (PRESENTATION / 'Win05.SkillTipText.cs').read_text()
native = (PRESENTATION / 'UITip.cs').read_text()
stubs = (ROOT / 'Tests/Endgame/NativeSkillTooltipStubs.cs').read_text()
runner = (ROOT / 'Tests/Endgame/run-skill-tooltip-contract.sh').read_text()
checks = 0


def check(condition, message):
    global checks
    checks += 1
    assert condition, message


def method(source, signature):
    start = source.index(signature)
    opening = source.index('{', start)
    # Ignore strings/comments while balancing the C# body, leaving offsets intact.
    tokens = re.compile(r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|[{}]')
    depth = 0
    for token in tokens.finditer(source, opening):
        if token.group() == '{':
            depth += 1
        elif token.group() == '}':
            depth -= 1
            if depth == 0:
                return source[opening:token.end()]
    raise AssertionError('Unclosed method: ' + signature)


hotbar = method(dialog, 'public static string SkillTipText(')
card = method(dialog, 'private string BuildSkillTip(')
upgrade = method(dialog, 'public bool CanShowUpgrade(')
life = method(dialog, 'private void DrawLifeSkills(')
check('if (UsesGrowthSkill(info.id)) return BuildGrowthSkillTip(info, false);' in hotbar,
      'Actual hotbar must route growth skills through current-only native composition')
check('if (UsesGrowthSkill(info.id)) return BuildGrowthSkillTip(info, true);' in card,
      'Actual skill card must route growth skills through full native composition')
check('if (UsesGrowthSkill(skillId)) return EndgamePanel.GrowthState.CanInvestSkill(skillId);' in upgrade,
      'Actual plus button must use authoritative hard-point/pending eligibility')
check('EndgameSkillRankView.IsProfessionSkill(id)' in growth,
      'Frozen profession whitelist must retain starter and life native paths')
for section, name in [(hotbar, 'hotbar'), (card, 'skill card')]:
    check('AppendLevelBlock(rows, info, Mathf.Max(1, currentLevel),' in section and 'AppendDmgBonusRow(rows, info);' in section,
          f'Existing non-growth {name} keeps native level/effect construction')
    check('return ComposeTip(rows);' in section,
          f'Existing non-growth {name} keeps native composition')
check('CharData.skills.TryGetValue(skillId, out int currentLevel)' in upgrade and 'currentLevel >= info.maxlv' in upgrade,
      'Non-growth upgrade preserves native skill/rank cap')
check('lifeData.TryGetValue' in life and 'lifeCanLearn.Contains' in life and 'UITip.Show(tip, s);' in life,
      'Life skills keep their existing native data, learning and tooltip route')
check('BuildGrowthSkillTip' not in life and 'GrowthState' not in life and 'Endgame' not in life,
      'Life skill tooltip must not import V5 profession progression')
check('return ComposeTip(rows);' in growth and 'ReferenceFirstWorldUiTip' not in growth,
      'Growth card must use original native ComposeTip rather than diagnostic layout')
check('AppendGrowthRequirement' in growth and 'NextRequiredLevel' in growth,
      'Next investment requirement must stay a separate server-authoritative native row')
check('rank.NextEffectiveRank' in growth and not re.search(r'(?:int|byte|ushort|long|float|double)\.Parse\(rank\.', growth),
      'Exact effective-rank strings must not be narrowed before display')
check('rank.NextChangeLines.Count > 0 ? rank.NextChangeLines : rank.NextEffectLines' in growth,
      'Native preview uses supplied semantic changes with full-next legacy fallback')
check('rank.Contributions' not in growth,
      'Skill card must not dump equipment grant/debug contribution records as training effects')
check('rank.TrainingSynergies' in growth and 'synergy.PerPointValue' in growth and 'synergy.InvestedHardPoints' in growth,
      'Training section must display typed source hard-points and per-point rate')
check('CultureInfo.InvariantCulture' in growth,
      'Per-point percent formatting must remain stable across device locales')
check('if (rank.SoftRank != "0")' in growth and 'if (rank.GrantRank != "0")' in growth,
      'Zero soft/granted ranks must not add debug-like zero rows')
check('new string(\' \', padding)' in compose and 'width = Mathf.Min(width, 32)' in compose,
      'Native ComposeTip space-centering and width contract must remain intact')
check('private static (List<(string, Color, bool)>, float, float) Layout(' in native and 'private static void Render(' in native,
      'Test must target actual private native layout/render methods without public test hooks')
for forbidden in ['ComposeTip(', ' Layout(', ' Render(', 'AppendGrowthEffectLines(', 'AppendGrowthSynergies(']:
    check(forbidden not in stubs, 'Test substitutes must not reimplement production behavior: ' + forbidden)
for path in ['Win05.GrowthSkills.cs', 'Win05.SkillTipText.cs', 'UITip.cs', 'UIScale.cs', 'Foundation/{JsonData,BoundedJson}.cs', 'Application/Endgame*.cs']:
    check(path in runner, 'Runner must compile actual production source: ' + path)
check('skill-tooltip-all149-v5.json' in runner and 'skill-tooltip-high-equipped-v5.json' in runner,
      'Default runner must retain both complete catalog and real high-equipped fixture coverage')
check('[[ -f "$fixture" ]] ||' in runner,
      'Missing required fixtures must fail instead of silently reducing coverage')
print(f'PASS {checks} native skill dispatch, source-isolation and starter/life compatibility guards')
