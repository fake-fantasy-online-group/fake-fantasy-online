#!/usr/bin/env python3
from pathlib import Path
root=Path(__file__).resolve().parents[2]
s=root/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
read=lambda p:(s/p).read_text()
cursor=read('CursorManager.cs'); pointer=read('PlayerController.PointerFrame.cs'); native=read('ReferenceFirstWorldWindowOwner.cs')
assert cursor.count('System.Math.Sign(unchecked(monster.level - CharData.level))')==1
for branch in (pointer,native):
 assert 'CursorManager.AttackKindForIndependentRuntime(' in branch
 assert 'Math.Sign(unchecked(attackableMonster.level - CharData.level))' not in branch
assert 'comparison > 0 ? "AttackH" : comparison == -1 ? "AttackL" : "AttackM"' in cursor
assert 'monster.NoAttack' in cursor and 'remote.HostileToLocal' in cursor
assert 'fetchCur ?? MobileCursorFrame("Fetch")' in cursor
key=read('PlayerController.PickupKey.cs')
assert 'PickupNearestForIndependentRuntime();' in key
assert key.count('GroundItems.NearestPickable(worldX, worldY, 600f)')==1
assert 'WalkToPickup(item.Item1, item.Item2, item.Item3)' in key
inp=read('GameHud.MobileInput.cs')
assert 'mobilePressed==8 && MobileClientPresentation.PickupRect.Contains(point)' in inp
assert 'PlayerController.I?.PickupNearestForIndependentRuntime();' in inp
assert 'MobileClientPresentation.PickupRect.Contains(point) || MobileClientPresentation.PageRect' in inp
assert 'if (cancel) SkillTargeting.CancelMobileHotSlot();' in inp
layout=read('MobileClientPresentation.cs')
assert 'padded.Overlaps(PickupRect)' in layout and 'PickupRect.Contains(point) || MenuToggleRect' in layout
hud=read('GameHud.Mobile.cs'); art=read('MobileHudArtwork.cs')
assert 'TargetSelect.CurrentSourceForIndependentRuntime' in hud and 'MobilePickupTexture' in hud
pickup_art=art.split("internal static void PickupFace",1)[1].split("internal static void SkillFace",1)[0]
assert 'Label(' not in pickup_art and '"拾取"' not in pickup_art and 'cursor, pressed' in pickup_art
assert 'hurttype0.gsa' not in art
print('PASS pickup/cursor source integration: shared desktop classification and pickup path; touch ownership, cancellation and UI exclusions retained.')
