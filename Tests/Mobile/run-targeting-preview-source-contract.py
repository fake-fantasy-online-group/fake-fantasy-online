#!/usr/bin/env python3
"""Wiring assertions complement the executable production geometry contract."""
from pathlib import Path
root = Path(__file__).resolve().parents[2]
p = root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
entry = (p / 'SkillTargeting.Entry.cs').read_text()
begin = entry.split('public static void Begin(', 1)[1].split('public static void CastImmediate(', 1)[0]
assert begin.index('I.Update();') < begin.index('I.Show(true);')
assert 'MobileClientPresentation.Enabled && MobileHotSlotActive' in begin
update = entry.split('private void Update()', 1)[1]
assert update.index('MobileAimPosition(mobileAimDelta)') < update.index('ScreenToWorldPoint(ClientInput.AimPosition)')
mobile = (p / 'SkillTargeting.Mobile.cs').read_text()
assert 'SetSkillAim(MobileAimPosition(Vector2.zero), false, false)' in mobile
assert 'mobileAimDelta = delta;' in mobile
assert '(!dragged && !GameSettings.SmartCast)' in mobile
assert 'TryMobileSkillTarget(skill, effectiveRange, true' in mobile
assert 'RequestGroundCast' not in mobile and 'RequestSkillOn' not in mobile
print('PASS mobile preview lifecycle wiring and non-chasing quick-tap source contracts')
