#!/usr/bin/env python3
"""Source/asset contracts only. This is deliberately not reported as a C# compile or Unity render."""
import argparse, hashlib, importlib.util, json, math
from pathlib import Path
root = Path(__file__).resolve().parents[2]
source = root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
plan = (source / 'RiftEliteVisualPlan.cs').read_text()
world = (source / 'RiftEliteWorldEffect.cs').read_text()
actor = (source / 'RiftEliteActorVisual.cs').read_text()
assets = (source / 'RiftEliteVisualAssets.cs').read_text()
panel = (source / 'EndgamePanel.RiftEliteVisuals.cs').read_text()
checks = 0
def check(value, message):
    global checks
    assert value, message
    checks += 1
for text in [world, actor, assets, panel]:
    for forbidden in ['GUI.', 'whiteTexture', 'PlayNamed(', 'NextScale =', 'DealDamage', 'ApplyStatus', 'SendEndgame', 'StartCoroutine']:
        check(forbidden not in text, 'Forbidden overlay/combat/global-effect path: ' + forbidden)
for resource in [4149,420,216,4040,147,43,230,4145,4147]:
    check(f'assets.Magic({resource})' in world, f'Missing approved native source {resource}')
for source_id, count in [(201,14),(80,6),(85,5)]:
    check(f', {source_id}, {count},' in actor, f'Missing native actor effect {source_id}')
check('SK_CLY_050' in assets and 'index + 1' in assets, 'All three original wall resources must be addressed')
check('Camera.main' in world and 'camera.transform.right' in world and 'i < 128' in world, 'Real camera projection required')
check('State.CanUseRiftHazards(GameBootstrap.CurMapId, GameBootstrap.I.WorldRootForMapConstructionForIndependentRuntime)' in panel,
      'Bind to actual map root and scene context, not mapID alone')
check('State.RiftHazards.CurrentHazards' in panel and 'rift.Impacts' in panel, 'Authoritative active hazards/confirmed impact receipts required')
check('impact.AtMS' in panel and 'hit.AtMS' in world, 'Late impacts must use server age')
check('aura.GoldenUntilMS > serverNow' in panel and 'aura.CloneOf' in panel, 'Do not infer gold/clone state from translated names')
check('if (disposed) return;' in world and 'if (root != null) root.SetActive(false);' in world, 'Disposal must survive destroyed native roots')
check('trail[i].transform.position = trailPosition[i]' in actor and 'new Vector3(1, 1, 1)' in actor, 'Trails must stay at past world positions without doubling actor scale')
check('new Material(' in assets and 'Destroy(tintMaterial)' in actor, 'Tint materials must be owned/released')
check('RiftElite*.cs' in (root/'Tests/Endgame/run-ui-contract.sh').read_text(), 'Production rendering must be in managed UI compilation inputs')
for path in source.glob('RiftElite*.cs'):
    check(Path(str(path)+'.meta').is_file(), 'Missing Unity metadata '+path.name)
manifest = json.loads((root/'Tests/Endgame/Fixtures/elite-native-artwork.json').read_text())
rows = {row['resource']: row for row in manifest['resources']}
check(rows['magic/magic/magic4149.gso']['peak']['visibleWidth'] == 256, 'Frozen lava core alpha diameter')
check(rows['magic/magic/magic0420.gso']['peak']['visibleWidth'] == 150, 'Frozen lava burst alpha diameter')
check(rows['magic/magic/magic0420.gso']['peak']['centerFromPivotX'] == -2, 'Keep native burst pivot correction')
max_error = 0.0
# Independent numeric evaluation of map-circle support and native-alpha sizing; not C# execution.
for radius in [120,140,220]:
    for zoom in [.5,1,2]:
        for degrees, squash in [(0,1),(37,1),(23,.55),(71,.67)]:
            angle=math.radians(degrees); a=math.cos(angle); b=math.sin(angle)*squash
            span=2*radius*math.hypot(a,b)*zoom
            projected=[radius*(a*math.cos(i*math.tau/128)+b*math.sin(i*math.tau/128))*zoom for i in range(128)]
            measured=max(projected)-min(projected)
            error=abs(measured-span);max_error=max(max_error,error)
            check(error < 1, 'Projected circle sampling exceeds one pixel')
            core=measured*.30/256; burst=measured/150
            check(abs(core*256-measured*.30)<1e-8 and abs(burst*150-measured)<1e-8, 'Native peak diameters must share one authoritative radius')
            for sample in rows.values():
                if 'magic0420' in sample['resource']:
                    cx=sample['peak']['centerFromPivotX']
                    check(abs((-cx*burst)+cx*burst)<1e-8, 'Native alpha center must cancel authored pivot offset')
parser=argparse.ArgumentParser();parser.add_argument('--asset-root',type=Path);parser.add_argument('--decoder',type=Path)
args=parser.parse_args()
if args.asset_root:
    check(args.decoder is not None and args.decoder.is_file(), 'Provide the independent native decoder for pixel checks')
    spec=importlib.util.spec_from_file_location('native_decoder',args.decoder);decoder=importlib.util.module_from_spec(spec);spec.loader.exec_module(decoder)
    for row in rows.values():
        path=args.asset_root/row['resource'];data=path.read_bytes()
        check(hashlib.sha256(data).hexdigest()==row['sha256'], 'Native resource changed: '+row['resource'])
        decoded=decoder.decode(path)
        check(len(decoded['images'])==row['frames'] and decoded['dirs']==1, 'Native frame count/direction mismatch')
        widths=[(im.getchannel('A').getbbox()[2]-im.getchannel('A').getbbox()[0])/decoded['scale'] for im in decoded['images']]
        check(max(widths)==row['peak']['visibleWidth'], 'Native alpha peak width mismatch')
print(f'PASS {checks} elite visual source/native-pixel/numeric contracts; max circle sampling error {max_error:.4f}px. This source/pixel check does not compile C# or launch Unity.')
