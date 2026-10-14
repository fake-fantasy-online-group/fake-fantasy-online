using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public partial class PlayerController
    {
        private bool TryMobileGroundPickup(Vector2 point)
        {
            int count = Physics2D.OverlapPointNonAlloc(point, _overlapBuf);
            GroundItemEntity nearest = null;
            float distance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                GroundItemEntity item = _overlapBuf[i].GetComponent<GroundItemEntity>();
                if (item == null) continue;
                float next = ((Vector2)_overlapBuf[i].bounds.center - point).sqrMagnitude;
                if (next < distance) { distance = next; nearest = item; }
            }
            if (nearest == null) return false;
            WalkToPickup(nearest.id, nearest.gx, nearest.gy);
            return true;
        }

    }
}
