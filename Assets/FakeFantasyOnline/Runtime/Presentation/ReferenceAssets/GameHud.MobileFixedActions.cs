using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameHud
{
    private static bool mobileMountRequestPending;
    private static float mobileMountRequestTime;
    private static bool mobileMountRequestRiding;
    private static int mobileMountRequestAnchor;
    private static string mobileMountRequestCharacter;
    private static Texture2D mobileSittingIcon;
    private static bool mobileSittingIconLoaded;

    private static Texture2D MobileSittingIcon()
    {
        if (!mobileSittingIconLoaded)
        {
            mobileSittingIcon = Resources.Load<Texture2D>("FakeFantasyOnline/MobileControls/Sit");
            mobileSittingIconLoaded = true;
        }
        return mobileSittingIcon;
    }
    // Separate character-local preferences; neither these keys nor their buttons address hotSkill.
    private static string MobilePotionPreference(MobileFixedAction action) =>
        action == MobileFixedAction.HealthPotion ? "mobile_fixed_hp_v1_" : "mobile_fixed_mp_v1_";

    internal static bool CanBindMobilePotion(MobileFixedAction action, int tab, InventoryDlg.Item item) =>
        tab >= 0 && tab < 2 && item.id > 0 && item.count > 0 && !item.pet.HasValue &&
        !item.cosmetic && MobilePotionEligibility.Allows(action, item.desc);

    internal static bool BindMobilePotion(MobileFixedAction action, int tab, int slot, int expectedId)
    {
        InventoryDlg.Item live = InventoryDlg.ItemAt(tab, slot);
        if (live.id != expectedId || !CanBindMobilePotion(action, tab, live) || live.locked ||
            DealDlg.Visible || SocialClient.TradeActive || DealDlg.IsOffered((byte)tab, slot) || InventoryDlg.HasHeldAny ||
            EquipFuseDlg.Picking || ToolTarget.Active || PetForgeDlg.Picking || AttrWashDlg.Picking ||
            AvatarFuse.Active || RepairDlg.Picking || string.IsNullOrEmpty(CharData.PrefSuffix)) return false;
        CharData.SetPref(MobilePotionPreference(action), live.id);
        MobileFixedFeedback((action == MobileFixedAction.HealthPotion ? "红药" : "蓝药") + "已设为【" + live.name + "】");
        return true;
    }

    private static int MobilePotionItemId(MobileFixedAction action)
    {
        int configured = CharData.GetPref(MobilePotionPreference(action), 0);
        // An explicit choice remains selected when empty. Never silently substitute another item.
        if (configured > 0) return configured;
        for (int slot = 0; slot < 24; slot++)
        {
            if (hotKind[slot] != 2 || hotSkill[slot] <= 0) continue;
            var found = InventoryDlg.FindItem(hotSkill[slot]);
            if (found.Item1 && CanBindMobilePotion(action, found.Item2,
                InventoryDlg.ItemAt(found.Item2, found.Item3))) return hotSkill[slot];
        }
        return 0;
    }

    internal static MobileFixedActionState MobileFixedState(MobileFixedAction action)
    {
        if (action == MobileFixedAction.Mount) return MobileMountState();
        if (action == MobileFixedAction.Sit)
        {
            PlayerController player = PlayerController.I;
            bool sitting = player != null && player.SittingForIndependentRuntime;
            string sitReason = MobileFixedAdmission();
            if (sitReason.Length == 0) sitReason = player.MobileSittingUnavailableReason;
            return new MobileFixedActionState(sitting ? "起立" : "坐下", MobileSittingIcon(),
                sitReason.Length == 0, 0, 0f, 0, sitReason, sitting);
        }
        if (action != MobileFixedAction.HealthPotion && action != MobileFixedAction.ManaPotion)
            return new MobileFixedActionState("", null, false, 0, 0f, 0, "无效操作");
        string label = action == MobileFixedAction.HealthPotion ? "红药" : "蓝药";
        int id = MobilePotionItemId(action);
        int count = id > 0 ? InventoryDlg.ItemCount(id) : 0;
        float cooldown = id > 0 ? CdFrac(id) : 0f;
        Texture2D icon = id > 0 ? NetClient.LoadItemIcon(id) : null;
        string reason = MobileFixedAdmission();
        if (reason.Length == 0 && id == 0) reason = "尚未配置" + label + "，请在背包长按恢复药品，选择“设为" + label + "”";
        if (reason.Length == 0 && count <= 0) reason = "已配置的" + label + "已用完，请补充或在背包重新设置";
        if (reason.Length == 0)
        {
            var found = InventoryDlg.FindItem(id);
            InventoryDlg.Item item = InventoryDlg.ItemAt(found.Item2, found.Item3);
            if (!found.Item1 || !CanBindMobilePotion(action, found.Item2, item)) reason = "此物品当前不是可用的" + label + "，请在背包重新设置";
            else if (item.locked || item.blocked != 0 || DealDlg.IsOffered(found.Item2, found.Item3)) reason = "该药品已锁定或暂不可使用";
            else if (cooldown > 0f) reason = "该药品正在冷却中";
        }
        return new MobileFixedActionState(label, icon, reason.Length == 0, count, cooldown, id, reason);
    }

    private static CharData.PetSkill MobileMountSkill()
    {
        if (CharData.petSkills != null)
            foreach (CharData.PetSkill skill in CharData.petSkills)
                // Match the server's isMountPetSkill classification, never infer from a model.
                if (skill.name == "坐骑" && skill.level > 0 && skill.active) return skill;
        return default(CharData.PetSkill);
    }

    private static MobileFixedActionState MobileMountState()
    {
        bool mounted = CharData.riding || CharData.rideAnchorId != 0;
        CharData.PetSkill skill = MobileMountSkill();
        Texture2D icon = null;
        if (ResLoader.I != null)
        {
            if (mounted && CharData.mountModel > 0)
                icon = ResLoader.I.LoadGsa("res/peticon/M" + CharData.mountModel + ".gsa");
            else if (skill.icon > 0)
                icon = ResLoader.I.LoadTex("item/item/item" + skill.icon + ".gso", 0);
        }
        string reason = MobileFixedAdmission();
        float cooldown = !mounted && skill.id > 0 ? CdFrac(skill.id) : 0f;
        if (mobileMountRequestPending && (mobileMountRequestCharacter != CharData.PrefSuffix ||
            mobileMountRequestRiding != CharData.riding || mobileMountRequestAnchor != CharData.rideAnchorId ||
            Time.unscaledTime - mobileMountRequestTime >= 2.5f)) mobileMountRequestPending = false;
        if (reason.Length == 0 && mobileMountRequestPending) reason = "等待坐骑状态更新";
        if (reason.Length == 0 && !mounted)
        {
            if (CharData.petModel <= 0) reason = "请先出战可骑乘的宠物";
            else if (!CharData.petRidable) reason = "当前出战宠物不可骑乘";
            else if (skill.id <= 0) reason = "当前宠物未学坐骑技能，请使用鞍具上骑";
            else if (cooldown > 0f) reason = "坐骑技能正在冷却中";
        }
        return new MobileFixedActionState(mounted ? "下坐骑" : "上坐骑", icon,
            reason.Length == 0, 0, cooldown, 0, reason, mounted);
    }

    private static string MobileFixedAdmission()
    {
        if (MobileClientPresentation.GameplayInputBlocked) return "请先完成界面布局编辑";
        if (NetClient.I == null || !NetClient.I.ConnectedForIndependentRuntime) return "未连接服务器";
        if (PlayerController.I == null || PlayerController.I.IsDead) return "当前状态无法使用";
        if (DealDlg.Visible || SocialClient.TradeActive || InventoryDlg.HasHeldAny || EquipFuseDlg.Picking || ToolTarget.Active ||
            PetForgeDlg.Picking || AttrWashDlg.Picking || AvatarFuse.Active || RepairDlg.Picking)
            return "请先结束当前物品操作";
        return string.Empty;
    }

    // Call only after a completed short tap. The layout gesture owns holds/cancel and never calls here.
    internal static void UseMobileFixedAction(MobileFixedAction action)
    {
        MobileFixedActionState state = MobileFixedState(action);
        if (!state.Available) { MobileFixedFeedback(state.Reason); return; }
        if (action == MobileFixedAction.Sit)
        {
            // The same native toggle as R; the existing UpdateSitting carrier sends once.
            PlayerController.I.TryToggleMobileSitting();
            return;
        }
        if (action == MobileFixedAction.Mount)
        {
            if (CharData.rideAnchorId != 0) NetClient.I.RideTogetherForIndependentRuntime(0);
            else if (CharData.riding) NetClient.I.PetShowForIndependentRuntime(true);
            else NetClient.I.PetUseSkillForIndependentRuntime(MobileMountSkill().id);
            mobileMountRequestPending = true;
            mobileMountRequestTime = Time.unscaledTime;
            mobileMountRequestRiding = CharData.riding;
            mobileMountRequestAnchor = CharData.rideAnchorId;
            mobileMountRequestCharacter = CharData.PrefSuffix;
            // Riding, pet selection and active-skill state change only on the server's receipt.
            return;
        }
        var item = InventoryDlg.FindItem(state.ItemId);
        if (!item.Item1) return;
        if (ItemActions.Handle(state.ItemId, item.Item2, item.Item3, item.Item4, item.Item5, item.Item6, null)) return;
        NetClient.I.UseItem(state.ItemId, item.Item2, item.Item3);
        StartItemCd(state.ItemId);
    }

    private static void MobileFixedFeedback(string message)
    {
        if (!string.IsNullOrEmpty(message)) ChatUI.Add("系统", message, new Color(1f, .75f, .35f, 1f));
    }
}
}
