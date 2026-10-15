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
    public static float Scale => Mathf.Max(.1f, Mathf.Min(SafeAreaGui.height / 540f, SafeAreaGui.width / 960f));
    private static Rect Centered(Vector2 c, float size) => new Rect(c.x-size*.5f,c.y-size*.5f,size,size);
    public static Rect JoystickRect => Centered(new Vector2(SafeAreaGui.x+100f*Scale,SafeAreaGui.yMax-110f*Scale),160f*Scale);
    public static Rect AttackRect => Centered(new Vector2(SafeAreaGui.xMax-68f*Scale,SafeAreaGui.yMax-68f*Scale),96f*Scale);
    private static readonly Vector2[] SkillOffsets = { new Vector2(-110,0), new Vector2(-104,-82), new Vector2(-36,-142), new Vector2(32,-102) };
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
    public static Rect MenuToggleRect => new Rect(MinimapRect.x-60f*Scale,SafeAreaGui.y+36f*Scale,48f*Scale,48f*Scale);
    internal static int MenuColumnsForWidth(float width, float scale, int count)
    {
        int columns=(int)((width+8f*scale)/(56f*scale));
        if (columns<1) columns=1;
        return columns<count ? columns : count;
    }
    private static float MenuAvailableWidth => Mathf.Max(48f*Scale,
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
            float width=(columns*56f-8f)*Scale;
            return new Rect(MenuToggleRect.x-8f*Scale-width,MenuToggleRect.y,
                width,(rows*56f-8f)*Scale);
        }
    }
    public static Rect MenuItemRect(int index)
    {
        Rect menu=MenuRect;
        int columns=MenuColumns;
        return new Rect(menu.x+(index%columns)*56f*Scale,menu.y+(index/columns)*56f*Scale,
            48f*Scale,48f*Scale);
    }
    public static Rect PageRect => new Rect(SafeAreaGui.xMax-200f*Scale,SafeAreaGui.yMax-254f*Scale,48f*Scale,48f*Scale);
    internal static int ActiveSkillButton = -1;
    public static Rect CancelRect
    {
        get
        {
            Vector2 origin=SkillRect(ActiveSkillButton>=0 && ActiveSkillButton<4 ? ActiveSkillButton : 0).center;
            float size=52f*Scale;
            Rect safe=SafeAreaGui;
            // Search nearby free space, nearest ring first; never obscure another control.
            for (int ring=1;ring<=8;ring++)
                for (int index=0;index<16;index++)
                {
                    float angle=(index*22.5f+180f)*Mathf.Deg2Rad;
                    Vector2 center=origin+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*(76f*ring*Scale);
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
        if (padded.Overlaps(AttackRect) || padded.Overlaps(PageRect) || padded.Overlaps(JoystickRect) ||
            padded.Overlaps(ChatRect) || padded.Overlaps(MinimapRect) || padded.Overlaps(AvatarRect) ||
            padded.Overlaps(MenuToggleRect) || (MenuExpanded && padded.Overlaps(MenuRect))) return true;
        for (int k=0;k<4;k++) if (padded.Overlaps(SkillRect(k))) return true;
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
            float height = (ChatExpanded ? 196f : 112f)*Scale;
            height=Mathf.Min(height,safe.height);
            float top=safe.yMax-height-12f*Scale;
            return new Rect(safe.center.x-width*.5f,Mathf.Max(safe.y,top),width,height);
        }
    }
    public static Vector2 GuiPoint(Vector2 screenPoint) => new Vector2(screenPoint.x,Screen.height-screenPoint.y);
    // While legacy windows are being migrated, any visible window owns the mobile
    // interaction layer. Never substitute the first finger's mouse position for a second touch.
    public static bool ModalOpen =>
        AccountSecDlg.Visible ||
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
        FamilyDlg.get_Visible() ||
        FamilyMailDlg.get_Visible() ||
        FamilyRelationDlg.get_Visible() ||
        FamilyStashDlg.Visible ||
        FigureDlg.Visible ||
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
        InventoryDlg.IsOpen ||
        JobCustomDlg.get_Visible() ||
        JobOfferDlg.get_Visible() ||
        JobRightDlg.get_Visible() ||
        MailConfigDlg.Visible ||
        MailDlg.Visible ||
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
        ShopDlg.Visible ||
        StallDlg.Visible ||
        StoreDlg.Visible ||
        SysConfigDlg.Visible ||
        SysMsgBox.Visible ||
        SysOptionDlg.Visible ||
        TaskDlg.Visible ||
        TeamConfigDlg.Visible ||
        TeamDlg.Visible ||
        TradeDlg.Visible ||
        TransportUI.VisibleS ||
        VoiceSetDlg.Visible ||
        WorldMapDlg.Visible ||
        (EquipDlg.I!=null && EquipDlg.I.IsOpen) || (Win05.I!=null && Win05.I.IsOpen) ||
        (UIManager.I!=null && UIManager.I.MobileWindowOpen);
    public static bool IsJoystickRegion(Vector2 screenPoint) => !ModalOpen && !DiagramOpen && !IsPanelPointer(screenPoint) && JoystickRect.Contains(GuiPoint(screenPoint));
    public static void RegisterPanel(object owner, Rect guiRect) { if (owner != null) panels[owner] = guiRect; }
    public static void RemovePanel(object owner) { if (owner != null) panels.Remove(owner); }
    public static bool IsPanelPointer(Vector2 screenPoint)
    {
        Vector2 point=GuiPoint(screenPoint);
        if (AddonUiHost.PointerOver(point)) return true;
        foreach (Rect panel in panels.Values) if (panel.Contains(point)) return true;
        return false;
    }
    public static bool IsPointerOverUi(Vector2 screenPoint)
    {
        if (!Enabled) return false;
        if (ModalOpen || DiagramOpen) return true;
        Vector2 point = GuiPoint(screenPoint);
        if (GameHud.I!=null && GameHud.I.showAvatar && AvatarRect.Contains(point)) return true;
        if (GameHud.I!=null && GameHud.I.showMinimap && MinimapRect.Contains(point)) return true;
        // Includes reserved thumb areas even before the first OnGUI event.
        if (JoystickRect.Contains(point) || AttackRect.Contains(point) || MenuToggleRect.Contains(point) || PageRect.Contains(point) || (ActiveSkillButton>=0 && CancelContains(point))) return true;
        if (GameHud.I != null && GameHud.I.showChat && ChatRect.Contains(point)) return true;
        if (MenuExpanded && MenuRect.Contains(point)) return true;
        for (int i=0;i<4;i++) if (SkillRect(i).Contains(point)) return true;
        return IsPanelPointer(screenPoint);
    }
}
}
