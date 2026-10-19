using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public static partial class MobileClientPresentation
{
    internal static bool LayoutPanelsOpen
    {
        get
        {
            if (ModalOpen || DiagramOpen || PlayerController.WorkTipOpen || ChatUI.TypingNow || TouchScreenKeyboard.visible ||
                InventoryDlg.IsOpen || (EquipDlg.I!=null && EquipDlg.I.IsOpen) ||
                (Win05.I!=null && Win05.I.IsOpen) || (UIManager.I!=null && UIManager.I.MobileWindowOpen) ||
                TeamDlg.Visible || TradeDlg.Visible || StoreDlg.Visible || TaskDlg.Visible ||
                FigureDlg.Visible || MailDlg.Visible || ShopDlg.Visible || FamilyDlg.get_Visible() ||
                SysOptionDlg.Visible || SysConfigDlg.Visible || WinZ.AnyLayoutBlockingWindow) return true;
            foreach (var pair in panels)
            {
                if (pair.Key is InventoryDlg && !InventoryDlg.IsOpen) continue;
                if (pair.Key is EquipDlg equipment && !equipment.IsOpen) continue;
                if (pair.Key is Win05 window && !window.IsOpen) continue;
                if (pair.Value.width>0f && pair.Value.height>0f) return true;
            }
            return false;
        }
    }
}
}
