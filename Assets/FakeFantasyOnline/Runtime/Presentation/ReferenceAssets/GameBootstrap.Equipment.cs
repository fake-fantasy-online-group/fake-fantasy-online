using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameBootstrap
{
    // Original Toggle301/EnsureOpen305/OpenPanel306 create this independent scene root.
    // The borrower may acquire it unopened, without replaying those UI actions or Load.
    // Ordinary TransitionTo only destroys worldRoot. Final TeardownWorldUi's existing
    // scene-root purge destroys EquipDlg; there is no DDOL or invented reset hook here.
    internal static EquipDlg AcquireEquipmentForIndependentRuntime()
    {
        if (EquipDlg.I == null) EquipDlg.I = new GameObject("EquipDlg").AddComponent<EquipDlg>();
        return EquipDlg.I;
    }
}
}
