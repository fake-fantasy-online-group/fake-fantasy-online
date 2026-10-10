using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{
    // Official 1.5.8 token 060003bf. Invocation belongs to BuildWorld's
    // still-unrecovered synchronous object/NPC population boundary.
    private void ComputeOccFloors()
    {
        for (int i = 0; i < occluders.Count; i++)
        {
            Occ occ = occluders[i];
            if (occ.sr == null) continue;

            occ.floor = occ.baseOrder;
            Bounds outer = occ.sr.bounds;
            float outerMinX = outer.center.x - outer.extents.x;
            float outerMaxX = outer.center.x + outer.extents.x;
            float outerMinY = outer.center.y - outer.extents.y;
            float outerMaxY = outer.center.y + outer.extents.y;

            for (int j = 0; j < npcSRs.Count; j++)
            {
                SpriteRenderer npc = npcSRs[j];
                if (npc == null) continue;
                int baseOrder = occ.baseOrder;
                if (!(npc.sortingOrder < baseOrder)) continue;

                Bounds inner = npc.bounds;
                float innerMinX = inner.center.x - inner.extents.x;
                float innerMaxX = inner.center.x + inner.extents.x;
                float innerMinY = inner.center.y - inner.extents.y;
                float innerMaxY = inner.center.y + inner.extents.y;
                if (outerMinX <= innerMaxX && outerMaxX >= innerMinX &&
                    outerMinY <= innerMaxY && outerMaxY >= innerMinY)
                {
                    occ.floor = Mathf.Min(occ.floor, unchecked(npc.sortingOrder - 1));
                }
            }
        }
    }
}

}
