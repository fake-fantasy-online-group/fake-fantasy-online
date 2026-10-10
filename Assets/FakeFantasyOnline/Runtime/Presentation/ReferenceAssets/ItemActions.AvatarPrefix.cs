using System;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static partial class ItemActions
{
    // Restored leading branches; false continues the remaining original Handle branches.
    internal static bool HandleAvatarPrefixForIndependentRuntime(int id, byte tab, int slot, bool cosmetic,
        string name, Func<bool> wardrobeVisible, Action<byte, int, byte> beginFusion)
    {
        bool isAvatar = SuitFxDB.IsAvatar(id);
        if (cosmetic || isAvatar)
        {
            if (wardrobeVisible())
                SysMsgBox.Show("将【" + name + "】放入魔法衣橱?\n(取出需消耗1个提取凭证)",
                    () => NetClient.I?.WardrobeStoreForIndependentRuntime(tab, slot), null);
            else beginFusion(tab, slot, 0);
            return true;
        }
        if (SuitFxDB.IsDragon(id))
        {
            beginFusion(tab, slot, 2);
            return true;
        }
        if (SuitFxDB.IsSoul(id))
        {
            beginFusion(tab, slot, 1);
            return true;
        }
        return false;
    }
}
}
