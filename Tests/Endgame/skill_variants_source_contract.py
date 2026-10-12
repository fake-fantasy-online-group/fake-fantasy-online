#!/usr/bin/env python3
"""Guards on production cursor refresh wiring, supplemental to executed provider tests."""
from pathlib import Path
root = Path(__file__).resolve().parents[2]
source = (root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/SkillTargeting.Entry.cs').read_text()
update = source[source.index('private void Update()'):]
resolve = update.index('AuthoritativeProfessionSkill.TryResolve')
assert resolve < update.index('UpdateOutline(') < update.index('Input.GetMouseButtonDown(0)')
for fragment in ('if (refreshedMode != mode) { End(); return; }',
                 'rangePx = refreshedRange > 0f ? refreshedRange : float.PositiveInfinity;',
                 'radiusPx = refreshedRadius;', 'fanDeg = refreshedAngle;',
                 'rectLen = refreshedLength;', 'rectWid = refreshedWidth;', 'noCircle = refreshedTrap;'):
    assert fragment in update, fragment
print('PASS 8 targeting cursor refresh source guards (supplemental; no Unity frame execution).')
