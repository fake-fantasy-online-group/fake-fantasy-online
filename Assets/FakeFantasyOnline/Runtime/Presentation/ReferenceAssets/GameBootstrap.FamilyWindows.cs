using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameBootstrap
{
    // Final world release only. ConfigureWorld preserves receipt-created windows on first
    // admission; ordinary maps release their driver without destroying these DDOL roots.
    private static void ReleaseFamilyWindowsForIndependentRuntime()
    {
        FamilyDlg.BindGuiDriverForIndependentRuntime(null);
        if (FamilyDlg.I != null)
        {
            // The original DDOL component stays live until Unity completes destruction.
            Object.Destroy(FamilyDlg.I.gameObject);
        }
        if (FamilyDetailDlg.I != null)
            Object.Destroy(FamilyDetailDlg.I.gameObject);
        if (JobRightDlg.I != null)
        {
            // The original DDOL component stays live until Unity completes destruction.
            Object.Destroy(JobRightDlg.I.gameObject);
        }
        if (JobCustomDlg.I != null)
        {
            // Keep the original component live until Unity completes deferred destruction.
            Object.Destroy(JobCustomDlg.I.gameObject);
        }
        // No evidence in this module authorizes clearing the SocialClient family statics.
    }
}
}
