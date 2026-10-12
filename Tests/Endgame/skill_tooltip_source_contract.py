#!/usr/bin/env python3
"""Source guards complement executed native skill layout/render tests.

The managed harness does not compile the entire Unity window. These guards pin its
actual dispatch, starter/life exclusions and separate native fallback. They are not
Unity execution or screenshot evidence.
"""
from pathlib import Path
import re
import hashlib

ROOT = Path(__file__).resolve().parents[2]
PRESENTATION = ROOT / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
growth = (PRESENTATION / 'Win05.GrowthSkills.cs').read_text()
dialog = (PRESENTATION / 'Win05.SkillDialog.cs').read_text()
compose = (PRESENTATION / 'Win05.SkillTipText.cs').read_text()
native = (PRESENTATION / 'UITip.cs').read_text()
stubs = (ROOT / 'Tests/Endgame/NativeSkillTooltipStubs.cs').read_text()
runner = (ROOT / 'Tests/Endgame/run-skill-tooltip-contract.sh').read_text()
scroll = (PRESENTATION / 'UITip.SkillScroll.cs').read_text()
hotbar_source = (PRESENTATION / 'ReferenceFirstWorldHotbar.cs').read_text()
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
check('rank.EffectiveRank' in growth and not re.search(r'(?:int|byte|ushort|long|float|double)\.Parse\(rank\.', growth),
      'Exact effective-rank strings must not be narrowed before display')
check('AppendGrowthLevelBlock(rows, rank.NextEffectLines, null, "下一等级", false)' in growth and 'rank.NextChangeLines' not in growth,
      'Original next-level block must repeat full prose/details and never select changed-only summaries')
check('rank.Contributions' not in growth,
      'Skill card must not dump equipment grant/debug contribution records as training effects')
check('rank.TrainingSynergies' in growth and 'synergy.PerPointValue' in growth and 'synergy.InvestedHardPoints' in growth,
      'Training section must display typed source hard-points and per-point rate')
check('CultureInfo.InvariantCulture' in growth,
      'Per-point percent formatting must remain stable across device locales')
check(all(label not in growth for label in ['投入点数:', '装备与状态:', '授予等级:', '投入1点后:', '其余效果不变']),
      'Original skill body must not be replaced or surrounded by diagnostic rank/summary rows')
check('您现在的剩余技能点数为' in growth and '您现在还无法升级这个技能' in growth,
      'Original orange footer wording must be preserved')
check(growth.index('AppendGrowthSynergies(rows, rank);') > growth.index('双击或拖曳至快捷栏以使用这个技能'),
      'Incoming synergy is appended after the complete original card, never replaces its body')
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
# Golden bodies from pre-restoration HEAD4a341045. Skill-only overflow must not
# alter native equipment/comparison wrapping, position, frame, palette or drawing.
for signature, digest in {
    'private static void Render(': 'dbe3150e2ac10ff87d0d9b70a2fb05a630e7c55baef0f6a5b8d0b715c2990970',
    'private static (List<(string, Color, bool)>, float, float) Layout(': '69ceba72f2b201aef9da40ab545161202371a4e60f863d67c4d872663f09846f',
    'private static void DrawBox(': 'c960fb43954bb6cabf124168d78a9d013b732ce8273e31ba5ecf972f239241af',
}.items():
    check(hashlib.sha256(method(native, signature).encode()).hexdigest() == digest,
          'Original equipment/shared native render body must remain byte-identical: ' + signature)
check('if (reqIsSkill) RenderSkill(reqText, reqS, reqMouse);' in native and 'reqIsSkill = false;' in native,
      'Overflow must be explicit skill-only opt-in; ordinary Show keeps native renderer')
check('if (layout.Item3 <= Screen.height)' in scroll and 'Render(text, null, scale, mouse);' in scroll,
      'Fitting skill cards retain original render path, scale and dimensions')
check('layout.Item1.GetRange(skillFirstRow, count)' in scroll and '滚轮查看 ' in scroll,
      'Oversized skills expose complete row scrolling with a visible native affordance')
check('if (UsesGrowthSkill(hoveredInfo.id)) UITip.ShowSkill(text, s);' in dialog and
      'UITip.ScrollSkill(BuildGrowthSkillTip(scrollInfo, true), s, current.delta.y)' in dialog,
      'Only hovered growth-skill overflow intercepts tree scrolling')
check('if (growthSkill) UITip.ShowSkill(HoverTooltip, scale);' in hotbar_source and
      'GameHud.hotKind[slotIndex] != 1 && GameHud.hotKind[slotIndex] != 2' in hotbar_source,
      'Hotbar items and pets remain outside skill overflow path')
check('UITip.SkillScroll.cs' in runner, 'Managed renderer harness must compile actual skill overflow source')
print(f'PASS {checks} native skill dispatch, source-isolation and starter/life compatibility guards')
