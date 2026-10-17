using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
/// <summary>One mobile presentation switch and layout, independent of desktop HUD geometry.</summary>
public static class MobileClientPresentation
{
    // Explicit opt-in simulation; a desktop installation stays unchanged by default.
    public static bool Simulate { get; set; }
    public static bool Enabled => UnityEngine.Application.isMobilePlatform || Simulate;
    public static bool MenuExpanded { get; set; }
    public static bool ChatExpanded { get; set; }
    private static readonly Dictionary<object, Rect> panels = new Dictionary<object, Rect>();
    public static Rect SafeAreaGui
    {
        get
        {
            Rect area = Screen.safeArea;
            if (area.width <= 0f || area.height <= 0f) area = new Rect(0, 0, Screen.width, Screen.height);
            return new Rect(area.x, Screen.height - area.yMax, area.width, area.height);
        }
    }
    // The persisted “界面大小” preference is shared with desktop settings, but
    // mobile uses continuous scaling rather than the desktop integer rounding.
    // Fit the full HUD at the upper end: never enlarge touch regions past safe edges.
    public static float Scale => ResolveScale(SafeAreaGui.width, SafeAreaGui.height, UIScale.User);
    internal static float ResolveScale(float width, float height, float user)
    {
        float automatic = Mathf.Min(height / 540f, width / 960f);
        float preference = Mathf.Max(.6f, Mathf.Min(2f, user));
        float fit = Mathf.Min(height / 520f, width / 940f);
        return Mathf.Max(.001f, Mathf.Min(automatic * preference, fit));
    }
    // Past the fit limit, increase content within the existing touch targets.
    // This keeps the upper half of the preference useful without control overlap.
    internal static float Readability => Mathf.Max(0f, Mathf.Min(1f, UIScale.User - 1f));
    internal static float TextSizeMultiplier => (16f + 4f * Readability) / 12f;
    internal static float SkillIconPadding => 6f - 4f * Readability;
    internal static float ChatPreviewHeight => 44f + 10f * TextSizeMultiplier + 54f * TextSizeMultiplier + 1f;
    private static Rect Centered(Vector2 c, float size) => new Rect(c.x-size*.5f,c.y-size*.5f,size,size);
    public static Rect JoystickRect => Centered(new Vector2(SafeAreaGui.x+100f*Scale,SafeAreaGui.yMax-110f*Scale),160f*Scale);
    public static Rect AttackRect => Centered(new Vector2(SafeAreaGui.xMax-100f*Scale,SafeAreaGui.yMax-110f*Scale),96f*Scale);
    // Fits beside the lower attack edge with a 2px gap; attack/joystick centers remain symmetric.
    public static Rect PickupRect => Centered(AttackRect.center+new Vector2(74f,64f)*Scale,48f*Scale);
    private static readonly Vector2[] SkillOffsets = { new Vector2(-108,32), new Vector2(-116,-40), new Vector2(-116,-116), new Vector2(-40,-156), new Vector2(36,-156), new Vector2(64,-80) };
    public static Rect SkillRect(int index) => Centered(AttackRect.center+SkillOffsets[index]*Scale,64f*Scale);
    // The avatar has no extra horizontal gutter beyond the device's real safe area.
    public static Rect AvatarRect => new Rect(SafeAreaGui.x,SafeAreaGui.y,171f*Scale,155f*Scale);
    public static int MinimapViewportWidth => (int)(SafeAreaGui.width-5f*Scale);
    public static Rect MinimapRect => new Rect(SafeAreaGui.x+MinimapViewportWidth-179f*Scale,
        SafeAreaGui.y,184f*Scale,173f*Scale);
    public static Rect MapActionRect(int index)
    {
        // Native artwork positions: plus (w-29,57), minus (w-24,83),
        // world (w-39,30), toggle (w-20,0). Widen inward for thumbs,
        // keeping vertical lanes disjoint and every face inside its own hit region.
        float right=SafeAreaGui.x+MinimapViewportWidth;
        if(index==0) return new Rect(right-42f*Scale,SafeAreaGui.y+57f*Scale,44f*Scale,26f*Scale);
        if(index==1) return new Rect(right-42f*Scale,SafeAreaGui.y+83f*Scale,44f*Scale,26f*Scale);
        if(index==2) return new Rect(right-44f*Scale,SafeAreaGui.y+30f*Scale,44f*Scale,26f*Scale);
        return new Rect(right-40f*Scale,SafeAreaGui.y,40f*Scale,26f*Scale);
    }
    public static bool DiagramOpen => GameHud.I!=null && GameHud.I.diagramOpen;
    // One compact switch and the original nine entries share the top safe-area band.
    // Neither the avatar's 171px band nor the compact map is covered by an entry.
    public static Rect MenuToggleRect => new Rect(MinimapRect.x-68f*Scale,SafeAreaGui.y+36f*Scale,56f*Scale,56f*Scale);
    internal static int MenuColumnsForWidth(float width, float scale, int count)
    {
        int columns=(int)((width+8f*scale)/(64f*scale));
        if (columns<1) columns=1;
        return columns<count ? columns : count;
    }
    private static float MenuAvailableWidth => Mathf.Max(56f*Scale,
        MenuToggleRect.x-8f*Scale-(SafeAreaGui.x+183f*Scale));
    public static int MenuColumns => MenuColumnsForWidth(MenuAvailableWidth,Scale,
        GameHud.ControlButtonCountForIndependentRuntime);
    public static Rect MenuRect
    {
        get
        {
            int count=GameHud.ControlButtonCountForIndependentRuntime;
            int columns=MenuColumns;
            int rows=(count+columns-1)/columns;
            float width=(columns*64f-8f)*Scale;
            return new Rect(MenuToggleRect.x-8f*Scale-width,MenuToggleRect.y,
                width,(rows*64f-8f)*Scale);
        }
    }
    public static Rect MenuItemRect(int index)
    {
        Rect menu=MenuRect;
        int columns=MenuColumns;
        return new Rect(menu.x+(index%columns)*64f*Scale,menu.y+(index/columns)*64f*Scale,
            56f*Scale,56f*Scale);
    }
    public static Rect PageRect => new Rect(SafeAreaGui.xMax-308f*Scale,SafeAreaGui.yMax-280f*Scale,48f*Scale,48f*Scale);
    internal static bool GroupWheelOpen;
    internal static float GroupWheelRadius => 72f*Scale;
    internal static float GroupWheelDeadZone => 24f*Scale;
    internal static Vector2 GroupWheelCenter
    {
        get
        {
            Rect safe=SafeAreaGui;
            float padding=GroupWheelRadius+24f*Scale;
            return new Vector2(
                MobileGroupWheel.ClampCenter(PageRect.center.x,safe.x,safe.width,padding),
                MobileGroupWheel.ClampCenter(PageRect.center.y,safe.y,safe.height,padding));
        }
    }
    internal static bool GroupWheelContains(Vector2 point) => (GroupWheelOpen || GameHud.MobileGroupWheelOpening || GameHud.MobileGroupWheelClosing) &&
        (point-GroupWheelCenter).sqrMagnitude <= (GroupWheelRadius+24f*Scale)*(GroupWheelRadius+24f*Scale);
    internal static int ActiveSkillButton = -1;
    public static Rect CancelRect
    {
        get
        {
            Vector2 origin=SkillRect(ActiveSkillButton>=0 && ActiveSkillButton<6 ? ActiveSkillButton : 0).center;
            float size=52f*Scale;
            Rect safe=SafeAreaGui;
            // Search nearby free space, at least 128 logical pixels away; never obscure another control.
            for (int ring=1;ring<=8;ring++)
                for (int index=0;index<16;index++)
                {
                    float angle=(index*22.5f+180f)*Mathf.Deg2Rad;
                    Vector2 center=origin+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*((128f+24f*(ring-1))*Scale);
                    Rect candidate=Centered(center,size);
                    if (candidate.x<safe.x || candidate.y<safe.y || candidate.xMax>safe.xMax || candidate.yMax>safe.yMax) continue;
                    if (CancelOverlapsControl(candidate)) continue;
                    return candidate;
                }
            return Centered(safe.center,size);
        }
    }
    private static bool CancelOverlapsControl(Rect rect)
    {
        // Include a small gap, so a cancel release cannot also hit an adjacent button.
        Rect padded=new Rect(rect.x-6f*Scale,rect.y-6f*Scale,rect.width+12f*Scale,rect.height+12f*Scale);
        if (padded.Overlaps(AttackRect) || padded.Overlaps(PickupRect) || padded.Overlaps(PageRect) || padded.Overlaps(JoystickRect) ||
            padded.Overlaps(ChatRect) || padded.Overlaps(MinimapRect) || padded.Overlaps(AvatarRect) ||
            padded.Overlaps(MenuToggleRect) || (MenuExpanded && padded.Overlaps(MenuRect))) return true;
        for (int k=0;k<6;k++) if (padded.Overlaps(SkillRect(k))) return true;
        return false;
    }
    public static bool CancelContains(Vector2 point)
    {
        if (ActiveSkillButton<0) return false;
        Rect rect=CancelRect;
        return (point-rect.center).sqrMagnitude<=rect.width*rect.width*.25f;
    }
    public static Rect ChatRect
    {
        get
        {
            Rect safe = SafeAreaGui;
            if (TouchScreenKeyboard.visible && TouchScreenKeyboard.area.height>0f)
                safe.height=Mathf.Max(1f,safe.height-TouchScreenKeyboard.area.height);
            float width = Mathf.Min(360f*Scale,safe.width*.38f);
            float height = (ChatExpanded ? 196f : ChatPreviewHeight)*Scale;
            height=Mathf.Min(height,safe.height);
            float top=safe.yMax-height-12f*Scale;
            return new Rect(safe.center.x-width*.5f,Mathf.Max(safe.y,top),width,height);
        }
    }
    public static Vector2 GuiPoint(Vector2 screenPoint) => new Vector2(screenPoint.x,Screen.height-screenPoint.y);
    // Unported legacy dialogs retain conservative modal ownership. Migrated normal
    // panels own only their rectangles, independently for each physical pointer.
    public static bool ModalOpen =>
        AccountSecDlg.Visible ||
        InventoryDlg.SplitOpen ||
        MailDlg.ComposeVisible ||
        (ShopDlg.Visible && ShopDlg.Picking) ||
        ArenaDeathDlg.Visible ||
        ArenaDraftDlg.Visible ||
        ArenaEscDlg.Visible ||
        AttrWashDlg.Visible ||
        ChatCopyOverlay.Visible ||
        ColorDlg.Visible ||
        ComposeDlg.Visible ||
        DealDlg.Visible ||
        DialogUI.VisibleS ||
        DifficultyBox.Visible ||
        DuelDlg.Visible ||
        EquipFuseDlg.Visible ||
        ExEquipDlg.IsOpen ||
        FaceDlg.Visible ||
        FamilyCreateDlg.get_Visible() ||
        FamilyDetailDlg.get_Visible() ||
        FamilyMailDlg.get_Visible() ||
        FamilyRelationDlg.get_Visible() ||
        FamilyStashDlg.Visible ||
        FindModeDlg.Visible ||
        FriendConfigDlg.Visible ||
        FriendInfoDlg.Visible ||
        GarageDlg.Visible ||
        GossipDlg.Visible ||
        GroceryConfigDlg.Visible ||
        GroceryDetailDlg.Visible ||
        GroceryStatusDlg.Visible ||
        HelpWinDlg.Visible ||
        HornDialog.Visible ||
        JobCustomDlg.get_Visible() ||
        JobOfferDlg.get_Visible() ||
        JobRightDlg.get_Visible() ||
        MailConfigDlg.Visible ||
        MailResistDlg.Visible ||
        MasterInfoDlg.Visible ||
        MasterListDlg.Visible ||
        MoneyInputDlg.Visible ||
        PasswdDlg.Visible ||
        PetArousalDlg.Visible ||
        PetForgeDlg.Visible ||
        PlayerActionMenu.Visible ||
        QueryDlg.Visible ||
        RackShopDlg.Visible ||
        RenameDlg.Visible ||
        RepairDlg.Visible ||
        ReportDlg.Visible ||
        ReviveDlg.Visible ||
        SculpDlg.Visible ||
        SecCodeAskDlg.Visible ||
        SelectPopupDlg.Visible ||
        StallDlg.Visible ||
        SysMsgBox.Visible ||
        TeamConfigDlg.Visible ||
        TransportUI.VisibleS ||
        VoiceSetDlg.Visible ||
        WorldMapDlg.Visible;
    public static bool IsJoystickRegion(Vector2 screenPoint) => !ModalOpen && !IsPanelPointer(screenPoint) && JoystickRect.Contains(GuiPoint(screenPoint));
    public static void RegisterPanel(object owner, Rect guiRect) { if (owner != null) panels[owner] = guiRect; }
    public static void RemovePanel(object owner) { if (owner != null) panels.Remove(owner); }
    public static bool IsPanelPointer(Vector2 screenPoint)
    {
        Vector2 point=GuiPoint(screenPoint);
        if (AddonUiHost.PointerOver(point)) return true;
        if (TeamDlg.MobileContainsPointer(point)) return true;
        if (TradeDlg.MobileContainsPointer(point)) return true;
        if (StoreDlg.MobileContainsPointer(point)) return true;
        if (TaskDlg.MobileContainsPointer(point)) return true;
        if (FigureDlg.MobileContainsPointer(point)) return true;
        if (MailDlg.MobileContainsPointer(point)) return true;
        if (ShopDlg.MobileContainsPointer(point)) return true;
        if (FamilyDlg.MobileContainsPointer(point)) return true;
        if (SysOptionDlg.MobileContainsPointer(point)) return true;
        if (SysConfigDlg.MobileContainsPointer(point)) return true;
        if (DiagramOpen && !MinimapRect.Contains(point) && GameHud.MobileDiagramRect.Contains(point)) return true;
        if (UIManager.I != null && UIManager.I.MobileWindowContains(screenPoint)) return true;
        if (WinZ.AnyWindowAt(point) || UITip.MobileContainsPointer(point)) return true;
        if (InventoryDlg.I != null && InventoryDlg.I.MobileOverlayContains(point)) return true;
        if (EquipDlg.I != null && EquipDlg.I.MobileOverlayContains(point)) return true;
        if (Win05.I != null && Win05.I.MobileOverlayContains(point)) return true;
        foreach (var panel in panels)
        {
            if (panel.Key is InventoryDlg && !InventoryDlg.IsOpen) continue;
            if (panel.Key is EquipDlg equipment && !equipment.IsOpen) continue;
            if (panel.Key is Win05 window && !window.IsOpen) continue;
            if (panel.Value.Contains(point)) return true;
        }
        return false;
    }
    public static bool IsPointerOverUi(Vector2 screenPoint)
    {
        if (!Enabled) return false;
        if (ModalOpen) return true;
        Vector2 point = GuiPoint(screenPoint);
        if (GameHud.I!=null && GameHud.I.showAvatar && AvatarRect.Contains(point)) return true;
        if (GameHud.I!=null && GameHud.I.showMinimap && MinimapRect.Contains(point)) return true;
        // Includes reserved thumb areas even before the first OnGUI event.
        if (GroupWheelContains(point) || JoystickRect.Contains(point) || AttackRect.Contains(point) || PickupRect.Contains(point) || MenuToggleRect.Contains(point) || PageRect.Contains(point) || (ActiveSkillButton>=0 && CancelContains(point))) return true;
        if (GameHud.I != null && GameHud.I.showChat && ChatRect.Contains(point)) return true;
        if (MenuExpanded && MenuRect.Contains(point)) return true;
        for (int i=0;i<6;i++) if (SkillRect(i).Contains(point)) return true;
        return IsPanelPointer(screenPoint);
    }
}
}
