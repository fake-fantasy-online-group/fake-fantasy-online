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
         'KeyCode.Escape', 'EndgameModule.Cube', 'EndgameModule.Rift', 'ClearSelection()', 'State?.InvalidateQuote()', 'client.RetryEndgame()',
         'State.Rift.RunID', 'PlayerController.SuppressWorldClick()')
contains('Presentation/ReferenceAssets/EndgamePanel.Items.cs', 'NetClient.LoadItemIcon(', 'MatchesNative(item)', 'bagOnly')
contains('Presentation/ReferenceAssets/EndgamePanel.Runes.cs', 'DrawRuneBank', 'State.ActiveFormations', 'OwnedItemTooltip(rune)')
for filename in ('EndgamePanel.cs', 'EndgamePanel.Runes.cs', 'EndgamePanel.World.cs', 'DialogUI.cs'):
    source = (runtime / 'Presentation/ReferenceAssets' / filename).read_text(encoding='utf-8')
    for removed in ('EndgameModule.Runes', 'TryOpenRunesFromWorld', 'DrawRunes', '星篆 · 镶嵌取出'):
        assert removed not in source, f'{filename}: duplicate rune workbench {removed}'
        checks += 1
contains('Presentation/ReferenceAssets/SocketUI.State.cs', 'CanInlayHole(holeIdx)', 'InlayForIndependentRuntime(TargetKind, TgtTab, TgtSlot, (byte)holeIdx, runeTab, runeBagSlot)')
contains('Presentation/ReferenceAssets/SculpDlg.Window.cs', 'TryDropRune((byte)InventoryDlg.HeldTab, InventoryDlg.HeldBagSlot, i)', 'SocketUI.CanInlayHole(i)')
assert 'endgame.socket.remove' not in (runtime / 'Presentation/ReferenceAssets/PluginGameApi.Endgame.Actions.cs').read_text()
checks += 1
contains('Presentation/ReferenceAssets/EndgamePanel.Cube.cs', 'State.Materials', 'cubeInputs: CubeInputs()', 'SyncCubeInputs()',
         'new EndgameRequest("cube.execute", quote.Revision, quoteID: quote.QuoteID, npcEntityID: cubeNpcEntityID)')
contains('Presentation/ReferenceAssets/EndgamePanel.Rift.cs', 'DrawRiftTierSelector(rift)', 'DrawRiftActions(rift, selectedTierValid)', 'rift.Claimable', '"reward_pending"',
         '"interrupted"', '"completed"', '"closed"', '"rift.start"', '"rift.claim"', '"rift.leave"', 'DrawRiftMarkers', 'rift.Globes', 'rift.Transitioning', 'RiftDepth', 'aura.Affixes')
contains('Presentation/ReferenceAssets/EndgamePanel.Rift.cs', 'NewbieTipBox.PushText(notice)', 'if (!transition && !floorTransition) return;')
rift_ui = (runtime / 'Presentation/ReferenceAssets/EndgamePanel.Rift.cs').read_text(encoding='utf-8')
assert '"rift.advance"' not in rift_ui and 'AdvanceRift(' not in rift_ui, 'Only physical gate entry may initiate a UI floor transfer'
checks += 1
for path in ('EndgamePanel.cs', 'EndgamePanel.Rift.cs'):
    source = (runtime / 'Presentation/ReferenceAssets' / path).read_text(encoding='utf-8')
    assert 'DrawRiftAnnouncement' not in source and 'announcementUntil' not in source, 'Rift notices must use the native queue without a separate announcement renderer or lifetime'
    checks += 1
contains('Presentation/ReferenceAssets/EndgamePanel.RiftTelegraphs.cs', 'DrawRiftArea', 'DrawRiftRing', 'DrawRiftLine', 'TelegraphRemainingMS', 'tell.InnerRadius', 'tell.RemainingMS', 'GUI.matrix = matrix', 'Texture2D.whiteTexture')
contains('Presentation/ReferenceAssets/InventoryDlg.OnGUI.cs', 'EndgamePanel.BagTooltip(')
contains('Presentation/ReferenceAssets/InventoryDlg.OnGUI.cs', 'EndgamePanel.EquippedTooltip(equipped, TooltipContent.Fmt(')
contains('Presentation/ReferenceAssets/UITip.cs', 'case 5: return new Color32(183, 155, 98, 255)', 'case 4: return GameSettings.ColorWeak', 'BitmapFont.Measure', 'BitmapFont.Draw')
contains('Presentation/ReferenceAssets/EndgamePanel.Tooltips.cs', 'TooltipContent.QualityName(text, 5)', 'lines.InsertRange(anchor, attributes)', '同类宿阵优先级未满足')
contains('Presentation/ReferenceAssets/EndgamePanel.Tooltips.cs', 'private static string Orange(string text) => "\\u0006" + text;',
         'EquipmentAttributeText(PlainLine(lines[i]))', '魔法装备获取几率提升', 'IsAdditionalAttributeLine',
         'lines.InsertRange(anchor + attributes.Count, effects)', '!alreadyOverlaid', '!lines.Contains(composition)')
tooltip_overlay = (runtime / 'Presentation/ReferenceAssets/EndgamePanel.Tooltips.cs').read_text(encoding='utf-8')
assert tooltip_overlay.index('lines.InsertRange(anchor + attributes.Count, effects)') < tooltip_overlay.index('AddDescription(lines, formation.Description)'), 'Effects close the attribute block before authored lore'
checks += 1
contains('Presentation/ReferenceAssets/EquipDlg.OnGUI.cs', 'EndgamePanel.EquippedTooltip(')
for path in runtime.rglob('Endgame*.cs'):
    assert path.with_suffix('.cs.meta').is_file(), f'missing Unity metadata: {path}'
    checks += 1
for name in ('Foundation/BoundedJson.cs', 'Foundation/JsonData.cs', 'Protocol/PrivateServerEndgameCodec.cs', 'Protocol/PrivateServerGateway.Endgame.cs'):
    assert (runtime / (name + '.meta')).is_file(), f'missing Unity metadata: {name}'
    checks += 1
assert 'KeyCode.F8' not in (runtime / 'Presentation/ReferenceAssets/EndgamePanel.cs').read_text()
assert 'string[] names' not in (runtime / 'Presentation/ReferenceAssets/EndgamePanel.cs').read_text()
contains('Protocol/PrivateServerGateway.Endgame.cs', 'Endgame.CanInvestSkill(skillID)', 'new EndgameRequest("skill.upgrade", Endgame.Revision, skillID: skillID)', 'await SendAsync(')
contains('Protocol/PrivateServerGateway.cs', 'if (Endgame.GrowthV5 && EndgameSkillRankView.IsProfessionSkill(skillId)) return UpgradeGrowthSkillAsync(skillId, cancellationToken);')
contains('Presentation/ReferenceAssets/Win05.SkillDialog.cs', 'if (UsesGrowthSkill(skillId)) return EndgamePanel.GrowthState.CanInvestSkill(skillId);', 'rank.EffectiveRank', 'BuildGrowthSkillTip(info, true)', 'DisplayedSkillPoints', 'EndgamePanel.RetryGrowthRequest()', 'EndgamePanel.RefreshGrowthSkills()')
contains('Presentation/ReferenceAssets/Win05.GrowthSkills.cs', 'rank.CurrentEffectLines', 'rank.NextEffectLines', 'rank.HardRank', 'rank.EffectiveRank', 'AppendGrowthLevelBlock', '您现在的剩余技能点数为')
contains('Presentation/ReferenceAssets/PluginGameApi.Observations.cs', 'case "endgame.skills":', '"metadataAvailable"', 'row["ranks"] = rank == null ? null : ObserveEndgameSkill(rank)')
contains('Presentation/ReferenceAssets/PluginGameApi.Endgame.Actions.cs', '"endgame.skill.upgrade"', 'state.CanInvestSkill(skill)', 'EndgameWireID(entry.Request.RequestId), floor, skill')
contains('Application/EndgameClientState.Snapshots.cs', 'snapshotCombatStatsVersion == CombatStatsVersion', 'PublishCombatStats(snapshotCombatStats, snapshotRevision)', 'CompareRevision(response.Revision, CombatStatsRevision)')
contains('Application/EndgameClientState.cs', 'response.Kind == "combat.stats" && !ApplyCombatStats(response)')
contains('Presentation/ReferenceAssets/PluginGameApi.Endgame.Observations.cs', 'data["combatStats"] = ObserveEndgameCombatStats(state.CombatStats)', '"procReadiness"', '"equipmentAuras"')
contains('Presentation/ReferenceAssets/PluginGameApi.cs', 'endgame?.Endgame.CombatStatsVersion')
print(f'PASS {checks} endgame source/metadata integration contracts.')
