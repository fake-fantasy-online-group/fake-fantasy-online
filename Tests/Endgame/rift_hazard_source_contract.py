#!/usr/bin/env python3
"""Static wiring and actual-Go-fixture checks. This does NOT execute/compile C# or Unity."""
from pathlib import Path
import json, math, struct, sys
root=Path(__file__).resolve().parents[2]
a=root/'Assets/FakeFantasyOnline/Runtime/Application'
p=root/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
f=root/'Tests/Endgame/Fixtures'
checks=0

def check(value, text):
    global checks
    assert value, text
    checks+=1

def canonical_snapshot(data):
    data=json.loads(json.dumps(data))
    rift=data['rift']
    for key,identity in [('auras','entityId'),('hazards','id'),('impacts','id'),('members','charId')]:
        if key in rift:rift[key].sort(key=lambda row:row[identity])
    return data

def contains(file, *snippets):
    text=file.read_text()
    for snippet in snippets:check(snippet in text, f'{file.name}: missing {snippet}')

raw=(f/'elite-wire-v6.json').read_bytes()
wire=bytes.fromhex((f/'elite-wire-v6.wire.hex').read_text())
check(len(wire)>6, 'real Go wire fixture exists')
check(struct.unpack('<HHH',wire[:6])==(0x80d0,1,len(wire)-6), 'unchanged opcode/schema/uint16 JSON length')
check(json.loads(raw)==json.loads(wire[6:]), 'actual Go JSON and binary fixtures are the same view')
check(len(wire)-6<=60*1024, 'wire remains within old 60KiB envelope')
meta_raw=(f/'elite-wire-v6-metadata.json').read_bytes()
meta_wire=bytes.fromhex((f/'elite-wire-v6-metadata.wire.hex').read_text())
check(struct.unpack('<HHH',meta_wire[:6])==(0x80d0,1,len(meta_wire)-6), 'metadata uses same binary envelope')
check(json.loads(meta_raw)==json.loads(meta_wire[6:]), 'actual metadata JSON and binary fixture agree')
metadata=json.loads(meta_raw)['rift']
check(sum(bool(a.get('cloneOf')) for a in metadata['auras'])==3, 'actual Go clone owner metadata')
check(sum(a.get('goldenUntilMS',0)>metadata['serverNowMS'] for a in metadata['auras'])==1, 'actual Go golden deadline metadata')
check(len(metadata['impacts'])==2 and all(i['expiresMS']-i['atMS']==700 and i['atMS']<=metadata['serverNowMS']<i['expiresMS'] for i in metadata['impacts']), 'actual resolved blast receipt TTLs')
oracle=json.loads((f/'elite-wall-geometry-v6.json').read_text())
check(oracle['clearance']==12 and len(oracle['cases'])==20, 'actual Go slab fixture: 20 axis/rotated/fractional/swept cases')
check(any(row['blocked'] for row in oracle['cases']) and any(not row['blocked'] for row in oracle['cases']), 'geometry fixture exercises blocked and clear paths')
r=json.loads(raw)['rift']
check(r['mapId']>0 and r['serverNowMS']>0, 'authoritative native map and server sample present')
mechanisms={2:'death_lava',3:'lightning_orb',4:'ice_orb',6:'poison_pool',7:'walls'}
check({h['mechanism'] for h in r['hazards']}==set(mechanisms.values()), 'Go fixture exercises all persistent mechanisms')
ids=set();walls=set()
for h in r['hazards']:
    check(h['id'] not in ids, 'hazard IDs unique');ids.add(h['id'])
    check(h['sourceId'].isdigit() and 0<int(h['sourceId'])<=2**32-1, 'source is native uint32 decimal')
    check(mechanisms[h['abilityId']]==h['mechanism'], 'stable ability ID agrees with mechanism')
    check(0<h['expiresMS']-h['bornMS']<=60000, 'bounded duration')
    check(h['bornMS']<=r['serverNowMS']<h['expiresMS'], 'live effect at authoritative sample')
    check(h['remainingMS']==h['expiresMS']-r['serverNowMS'], 'remaining/deadline use same server sample')
    for w in h.get('walls',[]):
        check(w['id'] not in walls, 'wall IDs unique');walls.add(w['id'])
        check(all(math.isfinite(w[k]) and abs(w[k])<=1e7 for k in ['x','y','x2','y2']), 'finite geometry bounds')
        check(0<w['width']<=1024 and 0<math.hypot(w['x2']-w['x'],w['y2']-w['y'])<=2048, 'server/client rectangle limits')
check(len(walls)==3, 'complete three-wall group')
contains(a/'EndgameRift.cs','EndgameData.Long(d, "goldenUntilMS")','EndgameData.String(d, "cloneOf", 20)','EndgameData.List(d, "impacts", 40','EndgameData.List(d, "hazards", 56','EndgameData.Int(d, "mapId")','EndgameData.Long(d, "serverNowMS")','hazard.RemainingMS != hazard.ExpiresMS - ServerNowMS','active > 24 || death > 32')
contains(a/'EndgameRiftHazards.cs','EndgameData.Number(d, "x")','ActorClearance = 12','Math.Abs(delta) < 1e-12','return lo <= hi','length / 2 + ActorClearance','wall.Width / 2 + ActorClearance')
contains(a/'EndgameRiftHazardState.cs','Math.Max(ServerTimeMS(localNow), next.ServerNowMS)','pair.Value.Retired = true','hazard.ExpiresMS > serverNow','next.FloorIndex < Snapshot.FloorIndex','new ReadOnlyCollection<EndgameRiftHazardView>(active)')
contains(a/'EndgameClientState.RiftHazards.cs','riftHazardContextEpoch <= riftHazardWorldEpoch','view.FloorIndex == riftHazardContextFloor','view.RunID == riftHazardContextRun','view.ServerNowMS >= riftHazardContextTime','RiftHazards.SnapshotCurrent','time <= riftHazardRejectedContextBarrier','Math.Max(riftHazardContextTime, RiftHazards.Snapshot == null ? 0 : RiftHazards.Snapshot.ServerNowMS)')
contains(a/'EndgameClientState.cs','Stopwatch.GetTimestamp()','RiftHazards.Apply(response.Rift, MonotonicMS)','ResetRiftHazardContext()')
contains(p/'PlayerController.Navigation.cs','RiftCellWalkable(cx, cy)','RiftCellEdgeWalkable(x, y, nx, ny)','RiftCellEdgeWalkable(x0, y0, x1, y1)','RiftPathWalkable(worldX, worldY, tx, ty)','RiftPathWalkable(from.x, from.y, tx, ty)')
contains(p/'PlayerController.Displacement.cs','ReplanRiftPathIfChanged()','MovementSegmentWalkable(worldX + dx, worldY + dy)','MovementSegmentWalkable(worldX + dx, worldY)','MovementSegmentWalkable(worldX, worldY + dy)')
contains(p/'PlayerController.RiftCollision.cs','state.CanUseRiftHazards(GameBootstrap.CurMapId, root)','!world.Switching','SetPathTo(riftPathGoalX, riftPathGoalY)')
contains(p/'GameBootstrap.cs','EndgamePanel.WorldState?.NoteRiftSceneChange();','EndgamePanel.WorldState?.NoteRiftSceneChangeRejected(worldRoot, backCurMap);')
mask=(p/'PlayerController.Mask.cs').read_text()
check('Rift' not in mask, 'static projectile LOS is unchanged')
displacement=(p/'PlayerController.Displacement.cs').read_text()
static_collision=displacement[displacement.index('private bool IsWalkable'):displacement.index('internal readonly struct DisplacementObservation')]
check('Rift' not in static_collision, 'static terrain/bridge query is unchanged')
for file in [a/'EndgameRiftHazards.cs',a/'EndgameRiftHazardState.cs',a/'EndgameClientState.RiftHazards.cs',p/'PlayerController.RiftCollision.cs']:
    check(file.with_suffix('.cs.meta').is_file(), f'{file.name} has Unity metadata')
if len(sys.argv)>1:
    server=Path(sys.argv[1])
    check(canonical_snapshot(json.loads((server/'server/docs/endgame/elite-wire-v6.json').read_bytes()))==canonical_snapshot(json.loads(raw)), 'fixture equals current production Go test output')
    check(canonical_snapshot(json.loads((server/'server/docs/endgame/elite-wire-v6-metadata.json').read_bytes()))==canonical_snapshot(json.loads(meta_raw)), 'metadata fixture equals current production Go test output')
    contains(server/'server/internal/game/scene/endgame_rift_state.go','result["runId"]','result["floorIndex"]','result["mapId"]','result["serverNowMS"]')
print(f'PASS {checks} static hazard wiring / actual Go fixture checks; C# execution and Unity rendering NOT covered.')
