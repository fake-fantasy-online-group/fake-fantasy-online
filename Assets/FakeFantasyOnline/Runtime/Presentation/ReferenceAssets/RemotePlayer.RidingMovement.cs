using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class RemotePlayer
{
    private void FollowRideAnchor()
    {
        if (rideAnchorId == NetClient.SelfEntityId)
        {
            PlayerController anchor = PlayerController.I;
            if (!anchor) return;
            worldX = anchor.worldX;
            worldY = anchor.worldY;
        }
        else
        {
            RemotePlayer anchor = CombatWorld.FindPlayer(rideAnchorId);
            if (!anchor || anchor == this) return;
            worldX = anchor.worldX;
            worldY = anchor.worldY;
        }
        pathX = null;
        pathY = null;
        pathIdx = 0;
        moving = false;
        transform.position = WorldToLocal(Mathf.Round(worldX), Mathf.Round(worldY));
    }

    private void StepRiding()
    {
        Vector2 before = new Vector2(worldX, worldY);
        if (moving && pathX != null)
        {
            float budget = moveSpeed * catchUp * Time.deltaTime;
            while (budget > 0f && pathIdx < pathX.Length)
            {
                float x = pathX[pathIdx], y = pathY[pathIdx];
                float dx = x - worldX, dy = y - worldY;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                if (distance <= budget || distance <= 0.01f)
                {
                    budget -= distance;
                    pathIdx++;
                    worldX = x;
                    worldY = y;
                }
                else
                {
                    worldX += budget * (dx / distance);
                    worldY += budget * (dy / distance);
                    break;
                }
            }
            if (pathIdx >= pathX.Length) moving = false;
        }
        transform.position = WorldToLocal(Mathf.Round(worldX), Mathf.Round(worldY));
        if (body) body.sortingOrder = SortFor(worldY);
        Vector2 delta = new Vector2(worldX, worldY) - before;
        if (delta.sqrMagnitude > 1f)
        {
            lastOct = (Mathf.RoundToInt(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg / 45f) + 8) % 8;
            curDir = dirByOct[lastOct];
            flip = flipByOct[lastOct];
        }
        if (CharData.nameMode >= 3 && !_f11Exempt)
        {
            if (srMount) srMount.enabled = false;
            if (srMountTop) srMountTop.enabled = false;
            if (srMountFx) srMountFx.enabled = false;
            if (mountRing && mountRing.gameObject.activeSelf) mountRing.gameObject.SetActive(false);
            return;
        }
        if (mountRing && !mountRing.gameObject.activeSelf) mountRing.gameObject.SetActive(true);
        ComposeRemoteMount(delta.sqrMagnitude > 1f);
    }

    internal void StepRidingForIndependentRuntime() => StepRiding();
    internal void FollowRideAnchorForIndependentRuntime()
    {
        if (rideAnchorId > 0) FollowRideAnchor();
    }
}

}
