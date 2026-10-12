using System;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal enum HudWindowSlot
{
    WorldMap, Chat, ChatCopy, Equip, ExEquip, Inventory, Skills, Dialog,
    Horn, Duel, UiInput, Mail, Task, Family, FamilyCreate, FamilyDetail, FamilyRelation,
    Team, PetArousal, RackShop, Trade, Deal, PlayerMenu, Store, Garage, Passwd,
    FriendInfo, MasterInfo, TeamConfig, FriendConfig, MailConfig, MailResist,
    JobCustom, JobOffer, JobRight, GroceryConfig, GroceryDetail, Compose, Sculp,
    Query, SelectPopup, HairSalon, Figure, Help, Report, VoiceSet, MoneyInput,
    Color, Face, FindMode, Rename, AccountSec, MasterList, Gossip, SystemInfo, TeamBar, Difficulty,
    Count
}
public partial class GameHud
{
    // Product host bindings are not part of the native GameHud constructor. Allocate the
    // callback table at its existing first binding, after native field/base initialization.
    private Func<bool>[] windowHits;
    internal void BindWindowHitForIndependentRuntime(HudWindowSlot slot, Func<bool> hit)
    {
        if (windowHits == null) windowHits = new Func<bool>[(int)HudWindowSlot.Count];
        windowHits[(int)slot] = hit;
    }
    private bool WindowHit(HudWindowSlot slot) => windowHits != null &&
        (windowHits[(int)slot]?.Invoke() ?? false);

    // Collider OnMouseDown handlers use this gate independently of PlayerController.Update.
    public static bool OverUI => AddonUiHost.MouseOver() || (I != null && I.MouseOverHud());
    public bool MouseOverHud()
    {
        float scale = uiScale;
        if (!(scale > 0f)) scale = UIScale.Factor;
        float width = Screen.width;
        Vector2 point = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        if (WindowHit(HudWindowSlot.WorldMap)) return true;
        if (MouseOverAvatarForIndependentRuntime(point, scale)) return true;
        if (HotbarContainsForIndependentRuntime(point, width, Screen.height, scale,
                GameSettings.HotbarBottom, showHotbar, hotbarExpanded)) return true;
        if (MouseOverMenuForIndependentRuntime(point, width, Screen.height, scale)) return true;
        if (MouseOverMinimapForIndependentRuntime(point, width, scale)) return true;
        if (showChat && ChatUI.I != null && ChatUI.I.MouseOver()) return true;
        if (ChatCopyOverlay.MouseOver() || EquipDlg.I != null && EquipDlg.I.MouseOver() ||
            ExEquipDlg.I != null && ExEquipDlg.I.MouseOver() || InventoryDlg.I != null && InventoryDlg.I.MouseOver() ||
            WindowHit(HudWindowSlot.Skills) || DialogUI.I != null && DialogUI.I.MouseOverBox()) return true;
        if (TransportUI.VisibleS) return true;
        if (ShopDlg.I != null && ShopDlg.I.MouseOver()) return true;
        if (SysMsgBox.Visible || WindowHit(HudWindowSlot.Horn) || WindowHit(HudWindowSlot.Duel) ||
            UiInput.MouseOver() || RepairDlg.Visible || WindowHit(HudWindowSlot.Mail)) return true;
        if (WindowHit(HudWindowSlot.Task) || WindowHit(HudWindowSlot.Family) ||
            WindowHit(HudWindowSlot.FamilyCreate) || WindowHit(HudWindowSlot.FamilyDetail) ||
            WindowHit(HudWindowSlot.FamilyRelation) || SysConfigDlg.MouseOver() || SysOptionDlg.MouseOver()) return true;
        if (WindowHit(HudWindowSlot.Team) || WindowHit(HudWindowSlot.PetArousal) ||
            WindowHit(HudWindowSlot.RackShop) || WindowHit(HudWindowSlot.Trade) ||
            DealDlg.MouseOver() || PlayerActionMenu.Visible) return true;
        if (MouseOverTargetForIndependentRuntime()) return true;
        if (WindowHit(HudWindowSlot.Store) || WindowHit(HudWindowSlot.Garage) || WindowHit(HudWindowSlot.Passwd) ||
            WindowHit(HudWindowSlot.FriendInfo) || WindowHit(HudWindowSlot.MasterInfo) ||
            WindowHit(HudWindowSlot.TeamConfig) || WindowHit(HudWindowSlot.FriendConfig) ||
            WindowHit(HudWindowSlot.MailConfig) || WindowHit(HudWindowSlot.MailResist) ||
            WindowHit(HudWindowSlot.JobCustom) || WindowHit(HudWindowSlot.JobOffer) || WindowHit(HudWindowSlot.JobRight)) return true;
        if (GroceryStatusDlg.MouseOver() || WindowHit(HudWindowSlot.GroceryConfig) || WindowHit(HudWindowSlot.GroceryDetail) ||
            StallConfigDlg.MouseOver() || StallDetailDlg.MouseOver()) return true;
        // Official GameHud.MouseOverHud (06000417) tests FigureDlg.MouseOver at 0x43c084,
        // HelpWinDlg.MouseOver (06000a8c) at 0x43c0a8 and ReportDlg.MouseOver at 0x43c0b4, so the
        // recovered help browser answers the same slot the source graph already reserved as Help.
        if (WindowHit(HudWindowSlot.Compose) || WindowHit(HudWindowSlot.Sculp) || WindowHit(HudWindowSlot.Query) ||
            WindowHit(HudWindowSlot.SelectPopup) || WindowHit(HudWindowSlot.HairSalon) || WindowHit(HudWindowSlot.Figure) ||
            WindowHit(HudWindowSlot.Help) || HelpWinDlg.MouseOver() || WindowHit(HudWindowSlot.Report) || WindowHit(HudWindowSlot.VoiceSet) ||
            WindowHit(HudWindowSlot.MoneyInput) || WindowHit(HudWindowSlot.Color) || WindowHit(HudWindowSlot.Face) ||
            WindowHit(HudWindowSlot.FindMode) || WindowHit(HudWindowSlot.Rename) || WindowHit(HudWindowSlot.AccountSec) ||
            SecCodeAskDlg.MouseOver() || WindowHit(HudWindowSlot.MasterList) || GossipDlg.MouseOver() ||
            WindowHit(HudWindowSlot.SystemInfo) || WindowHit(HudWindowSlot.TeamBar) || WindowHit(HudWindowSlot.Difficulty)) return true;
        return false;
    }
}
}
