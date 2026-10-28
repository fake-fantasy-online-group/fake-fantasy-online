#!/usr/bin/env python3
"""Integration guards; optional local PKG index checks references without copying assets."""
from pathlib import Path
import json, os
root = Path(__file__).resolve().parents[2]
ui = root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
def read(name): return (ui / name).read_text()
assert 'CaptureSpiritWorkbench(npc)' in read('DialogUI.cs') and 'AddSpiritWorkbenchOptions()' in read('DialogUI.cs')
assert 'IsNpcStateCurrentForIndependentRuntime(source)' in read('DialogUI.Spirit.cs')
assert 'SpiritWorkbenchOpen' in read('InventoryDlg.OnGUI.cs') and 'SpiritWorkbenchOpen' in read('InventoryDlg.Mobile.cs')
for name in ('ItemActions.Handle.cs', 'ItemActions.WindowPrefix.cs'):
    assert 'HandleSpiritItemForIndependentRuntime(id, tab, slot)' in read(name)
assert read('ItemActions.Handle.cs').index('HandleSpiritItemForIndependentRuntime') < read('ItemActions.Handle.cs').index('bool avatar')
assert read('AvatarFuse.Targets.cs').count('ApplySpiritItem(') == 2
panel = read('EndgamePanel.Spirit.cs')
assert 'State.PendingRequest.Op == "spirit." + operation' in panel and 'client.RetryEndgame()' in panel
assert 'spiritUseRevision != state.Revision' in panel and 'kind != Current.spiritUseKind' in panel
assert 'Level == 15' not in panel and 'v.CanProcess' in panel
assert 'ringsoul' not in panel.lower()
assert 'new EndgameRequest("spirit.preview"' in panel and 'spiritMaterial: spiritMaterial' in panel
art = read('EndgamePanel.SpiritArtwork.cs')
assert 'res/uires/' in art and 'LoadImage' not in art and 'Resources.Load' not in art
index = os.environ.get('FFO_PKG_INDEX')
if index:
    paths = {r['name'].lower().removeprefix('res/') for r in json.loads(Path(index).read_text())}
    for key in ('glovesoul','shoering','wingsoul','capsoul','neckring','facesoul','ClothSoul','HonorSoul','WeaponSoul'):
        small = key in ('glovesoul','shoering','wingsoul'); modern = key in ('ClothSoul','HonorSoul','WeaponSoul')
        backgrounds = [key+x for x in ('Relieve','Process','Reset')] if modern else ('dearchive','process','reset') if key in ('wingsoul','facesoul') else ('jiefengbk','jiagongbk','guiyuanbk')
        button_dir = 'shoering' if small else key if modern else 'RingSoul'
        buttons = ('jiefeng','jiagong','huitui','guiyuan','qingkong') if small else ('RelieveBtn','ProcessBtn','BackBtn','ResetBtn','ClearBtn')
        for directory, files in ((key, backgrounds),(button_dir, buttons)):
            for name in files: assert f'uires/{directory}/{name}.gsa'.lower() in paths
    print('PASS 72 original background/button PKG references; no assets copied')
print('PASS spirit native NPC/inventory/targeting/lifecycle source integration guards')
