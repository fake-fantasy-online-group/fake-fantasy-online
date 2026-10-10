using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class PlayerController
{
    internal void BrowseOwnStallAtForIndependentRuntime(Vector2 point)
    {
        int count = Physics2D.OverlapPointNonAlloc(point, _overlapBuf);
        for (int i = 0; i < count; i++)
        {
            StallSignClick sign = _overlapBuf[i].GetComponent<StallSignClick>();
            if (sign != null && sign.ownerId == NetClient.SelfEntityId)
            {
                NetClient.I?.StallBrowseForIndependentRuntime(NetClient.SelfEntityId);
                break;
            }
        }
    }
    internal StallSignClick PickStallSignForIndependentRuntime(Vector2 point, bool npcSelected)
    {
        int count = Physics2D.OverlapPointNonAlloc(point, _overlapBuf);
        StallSignClick sign = null;
        for (int i = 0; i < count; i++)
        {
            sign = _overlapBuf[i].GetComponent<StallSignClick>();
            if (sign != null) break;
        }
        return npcSelected ? null : sign;
    }
    internal void BrowseStallForIndependentRuntime(StallSignClick sign)
    {
        if (NetClient.I != null) NetClient.I.StallBrowseForIndependentRuntime(sign.ownerId);
        moveToTarget = false;
        followMode = false;
        path.Clear();
    }
}
}
