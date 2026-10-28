namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static partial class ItemActions
{
    internal static bool HandleWindowPrefixForIndependentRuntime(int id, byte tab, int slot, string name, System.Action<byte> beginRepairSelection)
    {
        if (EndgamePanel.HandlePowerItemForIndependentRuntime(id, tab, slot)) return true;
        if (EndgamePanel.HandleSpiritItemForIndependentRuntime(id, tab, slot)) return true;
        if (EndgamePanel.HandleCubeItemForIndependentRuntime(id, tab, slot)) return true;
        if (id == 0x350c)
        {
            SysMsgBox.Show("【魔法衣橱提取凭证】不能直接双击。\n请打开魔法衣橱, 在要取出的外形上【右键】, 即可取出(每次消耗1个)。", () => { }, null);
            return true;
        }
        if (PetForgeIds.IsTalisman(id) || PetForgeIds.IsBook(id) || PetForgeIds.IsPot(id))
        {
            int kind = PetForgeIds.IsPot(id) ? 2 : PetForgeIds.IsTalisman(id) ? 0 : 1;
            var icon = NetClient.LoadItemIcon(id);
            PetForgeDlg.Open(kind, tab, slot, id, name, icon);
            return true;
        }
        if (id == 0xd2f)
        {
            StallConfigDlg.Open(0);
            return true;
        }
        if (id == 0xd30)
        {
            StallConfigDlg.Open(1);
            return true;
        }
        if (id == 0x180bb)
        {
            if (ShopDlg.Visible) return true;
            ShopDlg.OpenSellOnly();
            ShopDlg.I.BindRepairSelectionForIndependentRuntime(beginRepairSelection);
            NetClient.I?.UseItem(id, tab, slot);
            GameHud.StartItemCd(id);
            return true;
        }
        return HandlePortableStorage(id, tab, slot);
    }
}
}
