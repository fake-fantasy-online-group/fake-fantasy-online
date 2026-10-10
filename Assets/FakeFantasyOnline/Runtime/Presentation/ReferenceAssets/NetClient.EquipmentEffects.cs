using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal partial class NetClient
{
    private Action<Func<int>> writeEquipmentEffects;
    internal void BindEquipmentEffectsWriterForIndependentRuntime(Action<Func<int>> writer) => writeEquipmentEffects = writer;
    public void SendEquipFx() => writeEquipmentEffects(CharData.EquipFxHideMask);

    private IReadOnlyList<WorldInventoryEquippedItemState> equipmentPublicationForIndependentRuntime;

    internal void ApplyEquipmentPublicationForIndependentRuntime(IReadOnlyList<WorldInventoryEquippedItemState> items)
    {
        if (ReferenceEquals(equipmentPublicationForIndependentRuntime, items)) return;
        equipmentPublicationForIndependentRuntime = items;
        CharData.LoadEquipFx();
        NetClient.I?.SendEquipFx();
        CharData.soulFxPart.Clear();
        foreach (WorldInventoryEquippedItemState item in items)
        {
            foreach (int id in new[] { item.ItemId, item.SoulAvatar, item.Avatar })
            {
                if (id <= 0) continue;
                string effect = SuitFxDB.SoulTFX(id);
                if (!string.IsNullOrEmpty(effect)) CharData.soulFxPart[effect] = item.Slot;
            }
        }
        if (PlayerController.I) PlayerController.I.RefreshSoulFx();
        EnduranceHud.UpdateFromEquipped(items);
        RebuildEquippedTransmogForIndependentRuntime(items);
    }
}

}
