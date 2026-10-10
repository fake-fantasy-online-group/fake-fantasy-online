using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class PlayerController
{
    private int pendPickupId = -1;
    private float pendPickupX, pendPickupY, pendPickupExpire;
    internal int PendingPickupIdForIndependentRuntime => pendPickupId;

    public void WalkToPickup(int id, int gx, int gy)
    {
        pendPickupId = id;
        pendPickupX = gx;
        pendPickupY = gy;
        pendPickupExpire = Time.time + 15f;
        sitting = false;
        CombatWorld.StopAttack();
        SetPathTo(gx, gy);
    }
    public void CancelPickup() => pendPickupId = -1;

    internal int AdvancePickupForIndependentRuntime()
    {
        int requested = -1;
        if (pendPickupId >= 0)
        {
            if (Time.time > pendPickupExpire) pendPickupId = -1;
            else
            {
                float dx = pendPickupX - worldX;
                float dy = pendPickupY - worldY;
                if (dx * dx + dy * dy <= 2304f)
                {
                    if (NetClient.I != null)
                    {
                        int id = pendPickupId;
                        NetClient.I.PickItemForIndependentRuntime(id);
                        requested = id;
                    }
                    pendPickupId = -1;
                    moveToTarget = false;
                    path.Clear();
                }
            }
        }
        return requested;
    }
}
}
