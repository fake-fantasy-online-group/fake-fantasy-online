#!/usr/bin/env python3
"""Static wiring checks supplement executed model/panel contracts; not a Unity render test."""
from pathlib import Path
root = Path(__file__).resolve().parents[2]
runtime = root / 'Assets/FakeFantasyOnline/Runtime'
checks = 0

def contains(path, *snippets):
    global checks
    source = (runtime / path).read_text(encoding='utf-8')
    for snippet in snippets:
        assert snippet in source, f'{path}: missing {snippet}'
        checks += 1

contains('Bootstrap/NativeNetClientHost.cs', 'view.gameObject.AddComponent<EndgamePanel>()',
         'endgame.Bind(_gateway)', 'Unbind(_gateway)', '_gateway.TickEndgameForIndependentRuntime()')
contains('Protocol/PrivateServerGateway.cs', 'TryReceiveEndgamePacket(', 'ResetEndgameConnection()', 'endgameAccount =')
contains('Protocol/PrivateServerGateway.TitleResults.cs', 'endgameAccount =')
contains('Protocol/PrivateServerGateway.EnterWorldReceipt.cs', 'endgameCharacter =', 'persistentCharacterId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)', 'endgameWorldEntered = true')
contains('Protocol/PrivateServerGateway.WorldReturn.cs', 'ResetEndgameConnection()')
contains('Protocol/PrivateServerGateway.Endgame.cs', 'IEndgameClient', '_host.IsHostThread', '_worldEntryAdmission != null',
         'endgameRetryJournal.Remember', 'RestoreUncertainRequest', '_netClient.EquipItem(item.Slot)',
         '_netClient.UnequipItem(item.Slot)', 'row.ItemId == item.ItemID', 'current.UID != item.UID')
contains('Protocol/PrivateServerEndgameCodec.cs', '0x10d0', '0x80d0', 'new UTF8Encoding(false, true)')
contains('Presentation/ReferenceAssets/EndgamePanel.cs', 'NpcChatter.CnFont()', 'ArtworkSkin.Panel(', 'WinZ.Forget(this)',
         'KeyCode.F8', 'ClearSelection()', 'State?.InvalidateQuote()', 'client.RetryEndgame()',
         'State.Rift.RunID', 'PlayerController.SuppressWorldClick()')
contains('Presentation/ReferenceAssets/EndgamePanel.Items.cs', 'EquipEndgameItem(', 'UnequipEndgameItem(', 'NetClient.LoadItemIcon(')
contains('Presentation/ReferenceAssets/EndgamePanel.Runes.cs', '"socket.insert"', '"socket.remove"', 'State.ActiveFormations', 'rune.Stats')
contains('Presentation/ReferenceAssets/EndgamePanel.Cube.cs', 'chosen.Materials', 'wallet.Count', 'salvage.rune', 'socket.reorder',
         'rune.convert', 'new EndgameRequest("cube.execute", quote.Revision, quoteID: quote.QuoteID)')
contains('Presentation/ReferenceAssets/EndgamePanel.Rift.cs', '妖魔结界守卫', '试炼小仙', 'rift.Claimable', '"reward_pending"',
         '"interrupted"', '"completed"', '"closed"', '"rift.start"', '"rift.claim"', '"rift.leave"', 'DrawRiftMarkers', 'rift.Globes')
contains('Presentation/ReferenceAssets/InventoryDlg.OnGUI.cs', 'EndgamePanel.BagTooltip(')
contains('Presentation/ReferenceAssets/EquipDlg.OnGUI.cs', 'EndgamePanel.EquippedTooltip(')
for path in runtime.rglob('Endgame*.cs'):
    assert path.with_suffix('.cs.meta').is_file(), f'missing Unity metadata: {path}'
    checks += 1
for name in ('Foundation/BoundedJson.cs', 'Foundation/JsonData.cs', 'Protocol/PrivateServerEndgameCodec.cs', 'Protocol/PrivateServerGateway.Endgame.cs'):
    assert (runtime / (name + '.meta')).is_file(), f'missing Unity metadata: {name}'
    checks += 1
print(f'PASS {checks} endgame source/metadata integration contracts.')
