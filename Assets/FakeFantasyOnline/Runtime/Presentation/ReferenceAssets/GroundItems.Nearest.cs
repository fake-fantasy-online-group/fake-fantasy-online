using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class GroundItems
{
    public static (int, int, int) NearestPickable(float px, float py, float range = 0f)
    {
        if (I == null) return (-1, 0, 0);
        int id = -1, x = 0, y = 0;
        float best = float.MaxValue;
        foreach (var pair in I.items)
        {
            Item item = pair.Value;
            if (item.go == null) continue;
            if (!item.owned && !(Time.time >= item.lockEnd)) continue;
            GroundItemEntity entity = item.go.GetComponent<GroundItemEntity>();
            if (entity == null) continue;
            float dx = entity.gx - px;
            float dy = entity.gy - py;
            float distance = dx * dx + dy * dy;
            if (distance > range * range && range > 0f) continue;
            if (distance < best)
            {
                id = pair.Key;
                x = entity.gx;
                y = entity.gy;
                best = distance;
            }
        }
        return (id, x, y);
    }
}
}
