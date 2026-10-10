using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>Observation labels only; UiEsc owns the complete native decision order.</summary>
public enum ReferenceFirstWorldEscapeAction
{
    None = 0,
    InputMenu = 1,
    ChatCopyOverlay = 2,
    SystemMessageBox = 3,
    NpcDialog = 4,
    PlayerActionMenu = 5,
    TradeDialog = 52,
    DealDialog = 6,
    InventoryDialog = 7,
    LocalEquipmentDialog = 8,
    InspectedEquipmentDialog = 9,
    SkillDialog = 10,
    GossipDialog = 11,
    WorldMapDialog = 12,
    LocalMapDiagram = 13,
    SkillTargeting = 14,
    SystemSettings = 15,
    SystemMenu = 16,
    SecurityCodePrompt = 17,
    AccountSecurity = 18,
    PetForge = 19,
    StallConfig = 20,
    StallDetail = 21,
    AvatarFusion = 22,
    AttributeWash = 23,
    RepairSelection = 24,
    RepairDialog = 25,
    ShopPicker = 26,
    NativeShop = 27,
    GroceryStatus = 28,
    PetAwakening = 29,
    HairSalon = 30,
    HelpWin = 31,
    ComposeDialog = 40,
    SculpDialog = 41,
    MoneyInput = 42,
    ArenaResult = 43,
    ArenaEsc = 53,
    TaskDialog = 44,
    StoreDialog = 45,
    GarageDialog = 46,
    PasswordDialog = 47,
    MailDialog = 48,
    MailConfigDialog = 49,
    MailResistDialog = 50,
    SocialDialog = 51,
    StallDialog = 54,
    QueryDialog = 55,
    SelectPopupDialog = 56,
    FigureDialog = 57,
    ReportDialog = 58,
    InventorySplit = 59,
    ToolTarget = 60,
    ColorDialog = 61,
    FaceDialog = 62,
    FindModeDialog = 63,
    ArenaDraft = 64,
    ArenaDeath = 65,
    TransportDialog = 66,
    FamilyMail = 67,
    FamilyDialog = 68,
    FamilyCreateDialog = 69,
    FamilyDetailDialog = 70,
    FamilyRelationDialog = 71,
    TeamDialog = 72,
    RackShopDialog = 73,
    GroceryConfig = 74,
    GroceryDetail = 75,
    JobCustomDialog = 76,
    JobOfferDialog = 77,
    JobRightDialog = 78,
    FriendInfoDialog = 79,
    MasterInfoDialog = 80,
    TeamConfigDialog = 81,
    FriendConfigDialog = 82,
    MasterListDialog = 83,
    VoiceSetDialog = 84,
}


/// <summary>
/// Decision-free access to the two windows that still have source wrapper owners, and to the
/// existing GameHud diagram byte. No visibility snapshot, priority list or close switch lives here.
/// </summary>
public static class ReferenceFirstWorldEscapeDispatch
{
    private static Func<ReferenceFirstWorldNpcDialog> dialogOwner;
    private static Func<ReferenceFirstWorldInspectedEquipmentDialog> inspectedEquipmentOwner;

    // A caller records the completed slot after its action; this observation never chooses a slot.
    internal static ReferenceFirstWorldEscapeAction LastAction { get; set; }

    internal static Action BindOwnersForIndependentRuntime(
        Func<ReferenceFirstWorldNpcDialog> dialog,
        Func<ReferenceFirstWorldInspectedEquipmentDialog> inspectedEquipment)
    {
        dialogOwner = dialog;
        inspectedEquipmentOwner = inspectedEquipment;
        return () =>
        {
            // Releasing a superseded world must not detach a newer world's live borrowers.
            if (!ReferenceEquals(dialogOwner, dialog) ||
                !ReferenceEquals(inspectedEquipmentOwner, inspectedEquipment)) return;
            dialogOwner = null;
            inspectedEquipmentOwner = null;
        };
    }

    // Original DialogUI.get_VisibleS / CloseS (06000220 / 06000221). Each accessor reads the
    // current native singleton; the close never reuses a receiver from the visibility test.
    internal static bool DialogVisible => DialogUI.VisibleS;

    internal static void CloseDialog()
    {
        DialogUI.CloseS();
    }

    // Original ExEquipDlg.get_IsOpen / CloseNow (06000332 / 06000333) read the live native
    // singleton even while a map borrower is absent during TransitionTo's yields.
    internal static bool InspectedEquipmentVisible => ExEquipDlg.IsOpen;

    internal static void CloseInspectedEquipment()
    {
        ExEquipDlg.CloseNow();
    }

    // Existing GameHud static accessors 06000452 / 06000453, borrowing the same single byte
    // already used by ReferenceFirstWorldMinimapHud. No diagram is created or loaded here.
    internal static bool DiagramOpen => GameHud.I != null && GameHud.I.diagramOpen;
    internal static void CloseDiagram()
    {
        if (GameHud.I != null) GameHud.I.diagramOpen = false;
    }
}

}
