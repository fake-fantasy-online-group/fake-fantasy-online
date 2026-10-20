using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameHud
{
    // Character-local, explicit choices. Positive up IDs are pet skills; negative up IDs
    // are saddle items. Down is always a pet skill. Never borrow ordinary hotbar slots.
    private const string MobileMountUpPreference = "mobile_fixed_mount_up_v1_";
    private const string MobileMountDownPreference = "mobile_fixed_mount_down_v1_";

    internal static bool CanBindMobileMountSkill(bool dismount, CharData.PetSkill skill) =>
        MobileShortcutEligibility.Pet(skill.id, skill.level, skill.active) &&
        (dismount || skill.name == "坐骑");

    private static CharData.PetSkill MobileBoundPetSkill(int id)
    {
        if (CharData.petSkills != null)
            foreach (CharData.PetSkill skill in CharData.petSkills)
                if (skill.id == id) return skill;
        return default(CharData.PetSkill);
    }

    internal static bool BindMobileMountSkill(bool dismount, int expectedId)
    {
        CharData.PetSkill live = MobileBoundPetSkill(expectedId);
        if (!CanBindMobileMountSkill(dismount, live) ||
            string.IsNullOrEmpty(CharData.PrefSuffix) || MobileFixedAdmission().Length != 0) return false;
        CharData.SetPref(dismount ? MobileMountDownPreference : MobileMountUpPreference, live.id);
        MobileFixedFeedback("【" + live.name + "】已设为" + (dismount ? "下坐骑" : "上坐骑"));
        return true;
    }

    internal static bool CanBindMobileSaddle(int tab, InventoryDlg.Item item) =>
        tab >= 0 && tab < 4 && tab != 2 && item.id > 0 && item.count > 0 && !item.pet.HasValue &&
        !item.cosmetic && MobileSaddleCatalog.Contains(item.id);

    internal static bool BindMobileSaddle(int tab, int slot, int expectedId)
    {
        InventoryDlg.Item live = InventoryDlg.ItemAt(tab, slot);
        if (live.id != expectedId || !CanBindMobileSaddle(tab, live) || live.locked || live.blocked != 0 ||
            DealDlg.IsOffered((byte)tab, slot) || string.IsNullOrEmpty(CharData.PrefSuffix) ||
            MobileFixedAdmission().Length != 0) return false;
        CharData.SetPref(MobileMountUpPreference, -live.id);
        MobileFixedFeedback("【" + live.name + "】已设为上坐骑");
        return true;
    }

    private static int MobileMountBinding(bool mounted) =>
        CharData.GetPref(mounted ? MobileMountDownPreference : MobileMountUpPreference, 0);

    private static MobileFixedActionState MobileMountState()
    {
        bool mounted = CharData.riding || CharData.rideAnchorId != 0;
        string label = mounted ? "下坐骑" : "上坐骑";
        int binding = MobileMountBinding(mounted);
        string reason = MobileFixedAdmission();
        if (mobileMountRequestPending && (mobileMountRequestCharacter != CharData.PrefSuffix ||
            mobileMountRequestRiding != CharData.riding || mobileMountRequestAnchor != CharData.rideAnchorId ||
            Time.unscaledTime - mobileMountRequestTime >= 2.5f)) mobileMountRequestPending = false;
        if (reason.Length == 0 && mobileMountRequestPending) reason = "等待坐骑状态更新";
        if (reason.Length == 0 && binding == 0)
            reason = mounted ? "尚未配置下坐骑，请长按已学宠物主动技能设置" :
                "尚未配置上坐骑，请长按鞍具或宠物坐骑技能设置";

        Texture2D icon = null;
        float cooldown = 0f;
        int itemId = 0;
        if (!mounted && binding < 0 && binding != int.MinValue)
        {
            itemId = -binding;
            icon = NetClient.LoadItemIcon(itemId);
            var found = InventoryDlg.FindItem(itemId);
            InventoryDlg.Item item = found.Item1 ? InventoryDlg.ItemAt(found.Item2, found.Item3) : default;
            if (reason.Length == 0)
            {
                if (!found.Item1 || !CanBindMobileSaddle(found.Item2, item))
                    reason = "已配置的上骑鞍具不在背包或不可用，请补充或重新设置";
                else if (item.locked || item.blocked != 0 || DealDlg.IsOffered(found.Item2, found.Item3))
                    reason = "已配置的鞍具已锁定或暂不可使用";
                else if (CharData.petModel <= 0) reason = "请先出战适用此鞍具的宠物";
            }
            // petRidable does not describe bag saddle eligibility. The existing UseItem
            // server route owns species/partner/level validation; never infer it from icons.
        }
        else if (binding != 0)
        {
            CharData.PetSkill skill = MobileBoundPetSkill(binding);
            if (ResLoader.I != null && skill.icon > 0)
                icon = ResLoader.I.LoadTex("item/item/item" + skill.icon + ".gso", 0);
            cooldown = skill.id > 0 ? CdFrac(skill.id) : 0f;
            if (reason.Length == 0)
            {
                if (CharData.petModel <= 0) reason = "请先出战已学此技能的宠物";
                else if (!CanBindMobileMountSkill(mounted, skill))
                    reason = "当前宠物不能使用已配置的" + label + "技能，请重新设置";
                else if (!mounted && !CharData.petRidable) reason = "当前出战宠物不可骑乘";
                else if (cooldown > 0f) reason = "已配置的宠物技能正在冷却中";
            }
        }
        return new MobileFixedActionState(label, icon, reason.Length == 0, 0, cooldown, itemId, reason, mounted);
    }

    private static void UseMobileMountBinding(MobileFixedActionState state)
    {
        // Dispatch exactly the selected action. Server rules still apply to passengers,
        // incompatible pets and saddle combinations; no automatic dismount/fallback.
        if (state.ItemId > 0)
        {
            var found = InventoryDlg.FindItem(state.ItemId);
            if (!found.Item1) return;
            NetClient.I.UseItem(state.ItemId, found.Item2, found.Item3);
        }
        else NetClient.I.PetUseSkillForIndependentRuntime(MobileMountBinding(state.Active));
        mobileMountRequestPending = true;
        mobileMountRequestTime = Time.unscaledTime;
        mobileMountRequestRiding = CharData.riding;
        mobileMountRequestAnchor = CharData.rideAnchorId;
        mobileMountRequestCharacter = CharData.PrefSuffix;
    }
}
}
