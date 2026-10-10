namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameHud
{
    // The original table is initialized in native order in GameHud.Initialization.cs.

    internal static int ControlButtonCountForIndependentRuntime => CtrlBtns.Length;

    internal static (string Name, float X, string Shortcut, string Label)
        ControlButtonForIndependentRuntime(int index) => CtrlBtns[index];

    // 06000414 control-loop click branch. Dispatch at the button, before the next
    // descriptor is drawn; the host observes the action but must not invoke it again.
    internal static void ActivateControlButtonForIndependentRuntime(int index)
    {
        var button = CtrlBtns[index];
        if (button.Item1 == "statusbtn") EquipDlg.Toggle();
        else if (button.Item1 == "warebtn") InventoryDlg.Toggle();
        else if (button.Item1 == "skillbtn") Win05.Toggle(Win05.SkillDialogName);
        else if (button.Item1 == "teambtn") TeamDlg.Toggle();
        else if (button.Item1 == "familybtn")
        {
            if (string.IsNullOrEmpty(SocialClient.FamilyName)) FamilyDlg.Toggle();
            else FamilyDetailDlg.Toggle();
        }
        else if (button.Item1 == "taskbtn") TaskDlg.Toggle();
        else if (button.Item1 == "emailbtn") MailDlg.Toggle();
        else if (button.Item1 == "optionbtn") SysConfigDlg.Toggle();
        else if (button.Item1 == "rackbtn") RackShopDlg.Toggle();
        else if (button.Item4 != null && UIManager.I != null) UIManager.I.Toggle(button.Item4);
    }
}

}
