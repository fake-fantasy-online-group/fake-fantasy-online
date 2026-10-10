using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class RemotePlayer
{
    private StallSign stallSign;

    private void UpdateStallSign()
    {
        long stamp = LoadTrace.On ? LoadTrace.SegStamp() : 0;
        try
        {
            try
            {
                if (_isStallOwner)
                {
                    var info = StallDlg.Owners[id];
                    float y = avatar != null && avatar.headPx > 0
                        ? unchecked(avatar.headPx + 30) / ppu : 1.6f;
                    if (!stallSign)
                    {
                        GameObject root = new GameObject("StallSign");
                        root.transform.SetParent(transform, false);
                        stallSign = root.AddComponent<StallSign>();
                        stallSign.ppu = ppu;
                        stallSign.mat = tagMat;
                        stallSign.browseOwnerId = id;
                    }
                    stallSign.Tick(true, info.Item1, info.Item2, y);
                }
                else if (stallSign) stallSign.Tick(false, 0, null, 0f);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[StallSign] remote tick failed: " + ex.ToString());
            }
        }
        finally
        {
            if (LoadTrace.On) LoadTrace.AddSeg(LoadTrace.Seg.玩_名牌血条, stamp);
        }
    }
}

}
