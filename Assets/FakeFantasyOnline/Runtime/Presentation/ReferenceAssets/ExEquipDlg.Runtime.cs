using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal sealed partial class ExEquipDlg
{
    // Unity is the sole OnGUI driver. The root stays in the active scene, outside worldRoot;
    // no DontDestroyOnLoad, map reset or bespoke final cleanup is added. GameBootstrap's
    // already recovered final root purge destroys it, without clearing the static data.
    internal float WindowXForIndependentRuntime => winX;
    internal float WindowYForIndependentRuntime => winY;
    internal static int AtForIndependentRuntime(int index) => At(index);
    internal static IReadOnlyList<string> RaceNamesForIndependentRuntime => RaceNames;
    internal static IReadOnlyList<int> SlotPartsForIndependentRuntime =>
        ReferenceFirstWorldInspectedEquipmentDialog.EquipmentSlotParts;
    internal static IReadOnlyList<Vector2> SlotPositionsForIndependentRuntime() => SlotPositions();

    // Lossless projection at the successful receipt boundary, never from a frame/version poll.
    // Both host receive capabilities call this one bridge; Show still owns all publication.
    internal static void ReceiveForIndependentRuntime(WorldInspectedPlayerEquipmentState state)
    {
        var attrs = new int[state.Attributes.Count];
        for (int index = 0; index < attrs.Length; index++) attrs[index] = state.Attributes[index];
        var equip = new List<EquipDlg.EquippedItem>(state.Equipment.Count);
        foreach (WorldInspectedEquipmentItem row in state.Equipment)
            equip.Add(new EquipDlg.EquippedItem
            {
                slot = row.Slot, id = row.ItemId, name = row.Name, desc = row.Description,
                curDur = row.CurrentDurability, maxDur = row.MaximumDurability,
                avatar = row.Avatar, soulAvatar = row.SoulAvatar, quality = row.Quality,
                icon = NetClient.LoadItemIcon(row.Avatar > 0 ? row.Avatar : row.ItemId)
            });
        CharacterAppearanceRecord appearance = state.Appearance;
        Show(state.Name, state.Level, state.Race, state.Gender, state.Head, state.Hair,
            state.Title, state.Honor, appearance.Body, appearance.Cap, appearance.Backpack,
            appearance.RightWeapon, appearance.LeftWeapon, appearance.Face, attrs,
            state.MaximumAttack, state.MinimumAttack, state.Defense, state.MagicAttack,
            state.MagicDefense, state.Hit, equip);
    }

    // Legacy diagnostic projection only. There is no retained DTO or second version counter;
    // no product rendering, open/close, receipt or lifetime decision reads this observation.
    internal static WorldInspectedPlayerEquipmentState ObserveForIndependentRuntime()
    {
        if (I == null) return null;
        var equipment = new List<WorldInspectedEquipmentItem>(tgEquip.Count);
        foreach (EquipDlg.EquippedItem item in tgEquip)
            equipment.Add(new WorldInspectedEquipmentItem(item.slot, item.id, item.name, item.desc,
                item.curDur, item.maxDur, item.avatar, item.soulAvatar, item.quality));
        return new WorldInspectedPlayerEquipmentState(0, tgName, tgLevel, tgRace, tgGender,
            tgHead, tgHair, tgTitle, tgRep, new CharacterAppearanceRecord(tgApBody, tgApCap,
                tgApBackpack, tgApWeaponR, tgApWeaponL, tgApFace, 0, 0, 0), tgAttrs,
            tgMaxAtk, tgMinAtk, tgDef, tgMatk, tgMdef, tgHit, equipment);
    }
}

}
