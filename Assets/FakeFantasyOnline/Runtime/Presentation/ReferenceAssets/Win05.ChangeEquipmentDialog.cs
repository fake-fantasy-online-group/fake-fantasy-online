using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Source translation of the official Win05 changeequipdlg branch.
/// </summary>
public partial class Win05
{
    public const string ChangeEquipmentDialogName = "changeequipdlg";
    public const string ChangeEquipmentBackgroundPath =
        "res/uires/equipdlg/changeequipdlgbk.gsa";
    public const string ChangeEquipmentCornerPath =
        "res/uires/waredlg/chgwearonekeybk.gsa";
    public const string ChangeEquipmentSetButtonPath =
        "res/uires/equipdlg/setchgwearbtn.gsa";
    public const string ChangeEquipmentApplyButtonPath =
        "res/uires/equipdlg/changeequipbtn.gsa";
    public const float ChangeEquipmentDialogWidth = 257f;
    public const float ChangeEquipmentDialogHeight = 329f;
    public const string ChangeEquipmentTitle = "换装方案";

    private Texture2D changeEquipmentCorner => UiFrame(ChangeEquipmentCornerPath, 0);
    private bool changeEquipmentDialogReady;

    public bool ChangeEquipmentDialogReady => changeEquipmentDialogReady;
    public bool ChangeEquipmentDialogVisible =>
        string.Equals(cur, ChangeEquipmentDialogName, StringComparison.Ordinal);

    private bool TryInitializeChangeEquipmentDialog()
    {
        changeEquipmentDialogReady = true;
        return true;
    }

    private void DisposeChangeEquipmentDialog()
    {
        changeEquipmentDialogReady = false;
    }
}

}
