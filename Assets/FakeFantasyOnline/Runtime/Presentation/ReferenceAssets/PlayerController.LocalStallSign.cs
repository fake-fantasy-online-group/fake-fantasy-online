using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class PlayerController
{
    private StallSign localSign;

    private void UpdateLocalStallSign()
    {
        if (StallConfigDlg.LocalStalling)
        {
            float head = avatar != null && avatar.headPx > 0 ? unchecked(avatar.headPx + 30) : 164f;
            float height = head / ppu;
            if (localSign == null)
            {
                var go = new GameObject("StallSign");
                go.transform.SetParent(transform, false);
                localSign = go.AddComponent<StallSign>();
                localSign.ppu = ppu;
                localSign.browseOwnerId = NetClient.SelfEntityId;
            }
            localSign.Tick(true, StallConfigDlg.LocalType, StallConfigDlg.LocalName, height);
        }
        else if (localSign != null) localSign.Tick(false, 0, null, 0f);
    }

    internal void UpdateLocalStallForIndependentRuntime()
    {
        if (StallConfigDlg.LocalStalling)
        {
            sitting = true;
            path.Clear();
            moveToTarget = false;
        }
        UpdateLocalStallSign();
    }
}
}
