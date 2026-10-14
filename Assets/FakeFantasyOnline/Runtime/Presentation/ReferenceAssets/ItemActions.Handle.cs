using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static partial class ItemActions
{
    // Bounds the hosted inventory connection; Handle remains the sole confirmation owner.
    internal static bool IsPetFeedOrGrowthForIndependentRuntime(int id) =>
        unchecked((uint)(id - 0x6237)) < 4 || id == 0x18066;

    internal static bool IsPetResetForIndependentRuntime(int id) => id == 0xc04 || id == 0x62b6;

    // Official 1.5.8 0x0600054a. Branch order is shared by inventory and hotbar callers.
    public static bool Handle(int id, byte tab, int slot, bool bound, bool cosmetic, string name,
        Action onEquipTab = null)
    {
        if (EndgamePanel.HandleCubeItemForIndependentRuntime(id, tab, slot)) return true;
        bool avatar = SuitFxDB.IsAvatar(id);
        if (cosmetic || avatar)
        {
            if (Win05.IsShowing("magicstoredlg"))
                SysMsgBox.Show("将【" + name + "】放入魔法衣橱?\n(取出需消耗1个提取凭证)",
                    () => NetClient.I?.WardrobeStoreForIndependentRuntime(tab, slot), null);
            else AvatarFuse.Begin(tab, slot, 0);
            return true;
        }
        if (SuitFxDB.IsDragon(id)) { AvatarFuse.Begin(tab, slot, 2); return true; }
        if (SuitFxDB.IsSoul(id)) { AvatarFuse.Begin(tab, slot, 1); return true; }
        if (id == 0x3125 || id == 0x6311 || id == 0x182ba || id == 0x182bb ||
            id == 0x182bc || id == 0x182cf)
        {
            ToolTarget.Begin(id);
            return true;
        }
        if (id == 0x18036) { EquipFuseDlg.Open(); return true; }
        if (id == 0x182cd || id == 0x182ce || unchecked((uint)(id - 0x182d0)) < 8)
        {
            AttrWashDlg.Open();
            return true;
        }
        if (id == 0x350c)
        {
            SysMsgBox.Show("【魔法衣橱提取凭证】不能直接双击。\n请打开魔法衣橱, 在要取出的外形上【右键】, 即可取出(每次消耗1个)。", () => { }, null);
            return true;
        }
        if (PetForgeIds.IsTalisman(id) || PetForgeIds.IsBook(id) || PetForgeIds.IsPot(id))
        {
            int kind = PetForgeIds.IsPot(id) ? 2 : PetForgeIds.IsTalisman(id) ? 0 : 1;
            PetForgeDlg.Open(kind, tab, slot, id, name, NetClient.LoadItemIcon(id));
            return true;
        }
        if (id == 0xd2f) { StallConfigDlg.Open(0); return true; }
        if (id == 0xd30) { StallConfigDlg.Open(1); return true; }
        if (id == 0x180bb)
        {
            if (ShopDlg.Visible) return true;
            ShopDlg.OpenSellOnly();
            // WBD-144: official ShopDlg.OnGUI calls RepairDlg.BeginPick directly, so the sell-only
            // shop draws the three repair buttons. This branch also feeds the hotbar use path
            // (GameHud.UseHotSlot 0600043c), which never passed the explicit repair callback the
            // inventory WindowPrefix path supplies; without this binding the first sell-only shop
            // of a session leaves beginRepairSelection null and clicking 修理/特殊修理 raises a
            // NullReferenceException inside ShopDlg.OnGUI instead of starting the official pick.
            ShopDlg.I.BindRepairSelectionForIndependentRuntime(RepairDlg.BeginPick);
            NetClient.I?.UseItem(id, tab, slot);
            GameHud.StartItemCd(id);
            return true;
        }
        if (HandlePortableStorage(id, tab, slot)) return true;
        if (HandleEquipmentTab(tab, onEquipTab)) return true;
        if (unchecked((uint)(id - 0x6237)) < 4 && NetClient.I != null)
        {
            string pet = string.IsNullOrEmpty(CharData.petName) ? "当前出战的宠物" :
                string.Format("【{0}】(Lv.{1})", CharData.petName, CharData.petLevel);
            SysMsgBox.Show("确定把【" + name + "】喂给 " + pet +
                " 吗?\n它是 100% 必定领悟符, 喂下去立刻生效、无法撤回, 也不能改喂别的宠物。",
                () => { NetClient.I?.UseItem(id, tab, slot); GameHud.StartItemCd(id); }, null);
            return true;
        }
        if (HandlePetResetForIndependentRuntime(id, tab, slot, name)) return true;
        if (id == 0x18066 && NetClient.I != null)
        {
            string pet = string.IsNullOrEmpty(CharData.petName) ? "当前出战的宠物" : "【" + CharData.petName + "】";
            string growth = string.IsNullOrEmpty(CharData.petPrename) ? "普通" : CharData.petPrename.Replace("的", "");
            SysMsgBox.Show("用【" + name + "】重随 " + pet + " 的成长型吗?\n当前成长型: " + growth +
                "\n\n· 会随机换成【另一个】成长型(也可能是没有成长型的「普通」)\n· 等级、已学技能、成长档【都不变】\n· 属性点会全部退回【未分配】, 需要你重新分配",
                () => { NetClient.I?.UseItem(id, tab, slot); GameHud.StartItemCd(id); }, null);
            return true;
        }
        bool petBindItem = NetClient.IsPetBindItem(id);
        if (bound && petBindItem && !CharData.petBound && NetClient.I != null)
        {
            SysMsgBox.Show("此道具已绑定, 使用后宠物也将【绑定】(绑定后不可交易、不可解除绑定), 是否继续?",
                () => NetClient.I?.UseItem(id, tab, slot), null);
            return true;
        }
        return false;
    }

    // Shared 0600054a storage branch for the full owner and hosted inventory window prefix.
    private static bool HandlePortableStorage(int id, byte tab, int slot)
    {
        if (id != 0x180ba) return false;
        if (StoreDlg.Visible) return true;
        StoreDlg.OpenNow();
        NetClient.I?.UseItem(id, tab, slot);
        GameHud.StartItemCd(id);
        return true;
    }

    private static bool HandleEquipmentTab(byte tab, Action onEquipTab)
    {
        if (onEquipTab != null && tab == 2) { onEquipTab(); return true; }
        return false;
    }

    // The hosted inventory reuses this bounded 0600054a branch without replacing its other
    // activation paths. Its original equipment callback still wins before reset confirmation.
    internal static bool HandlePetResetForIndependentRuntime(int id, byte tab, int slot, string name,
        Action onEquipTab = null)
    {
        if (!IsPetResetForIndependentRuntime(id)) return false;
        if (HandleEquipmentTab(tab, onEquipTab)) return true;
        if (NetClient.I != null)
        {
            string itemName = string.IsNullOrEmpty(name) ?
                (id == 0x62b6 ? "高级易筋洗髓丸" : "易筋洗髓丸") : name;
            SysMsgBox.Show("使用【" + itemName +
                "】会让【出战宠物】:\n· 等级回到 1 级\n· 已学技能全部清空\n· 属性加点全部归零" +
                (id == 0x62b6 ? "\n· 成长型随机重置" : "") +
                "\n\n★以上都无法恢复。确定要洗炼吗?",
                () => NetClient.I?.UseItem(id, tab, slot), null);
            return true;
        }
        return false;
    }
}
}
