using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class RepairDlg
{
    private readonly object mobilePickCancelOwner = new object();

    private void DrawMobilePickCancel()
    {
        if (MobileClientPresentation.Enabled && Picking && !SysMsgBox.Visible)
        {
            float scale = MobileClientPresentation.Scale;
            Rect safe = MobileClientPresentation.SafeAreaGui;
            Rect cancel = new Rect(safe.center.x - 90 * scale, safe.y + 8 * scale, 180 * scale, 44 * scale);
            WinZ.Raise(mobilePickCancelOwner);
            var pickFrame = WinZ.Frame(mobilePickCancelOwner, cancel, -100);
            GUI.depth = pickFrame.Item1;
            MobileClientPresentation.RegisterPanel(mobilePickCancelOwner, cancel);
            if (ArtworkSkin.Button(cancel, "取消修理选物", scale, enabled: !pickFrame.Item2 && !SysMsgBox.Visible))
            { CancelPick(); MobileClientPresentation.RemovePanel(mobilePickCancelOwner); WinZ.Forget(mobilePickCancelOwner); PlayerController.SuppressWorldClick(); }
            if (!pickFrame.Item2 && cancel.Contains(Event.current.mousePosition) &&
                (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseUp)) Event.current.Use();
        }
        else { MobileClientPresentation.RemovePanel(mobilePickCancelOwner); WinZ.Forget(mobilePickCancelOwner); }
    }
}
}
