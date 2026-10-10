using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{
    // Official 1.5.8 token 060003c0. No original invocation is established;
    // retain this owner body without adding an Update or population-time call.
    private void UpdateStandOnReorder()
    {
        // The host pcRef wraps the original component. The existing accessor
        // directly reads it; reread where the original pcRef field is read.
        if (pcRef?.SourceControllerForIndependentRuntime == null || occluders.Count == 0)
            return;

        SpriteRenderer playerRenderer =
            pcRef.SourceControllerForIndependentRuntime.GetComponent<SpriteRenderer>();
        if (playerRenderer == null) return;

        int playerY = unchecked((int)pcRef.SourceControllerForIndependentRuntime.worldY);
        Bounds playerBounds = playerRenderer.bounds;
        float playerMaxX = playerBounds.center.x + playerBounds.extents.x;
        float playerMaxY = playerBounds.center.y + playerBounds.extents.y;
        float playerMinX = playerBounds.center.x - playerBounds.extents.x;
        float playerMinY = playerBounds.center.y - playerBounds.extents.y;

        for (int i = 0; i < occluders.Count; i++)
        {
            Occ occ = occluders[i];
            if (occ.sr == null) continue;

            int baseOrder = occ.baseOrder;
            int order = occ.floor;
            if (baseOrder > unchecked(playerY + 1))
            {
                Bounds bounds = occ.sr.bounds;
                float minX = bounds.center.x - bounds.extents.x;
                float minY = bounds.center.y - bounds.extents.y;
                float maxX = bounds.center.x + bounds.extents.x;
                float maxY = bounds.center.y + bounds.extents.y;
                if (minX <= playerMaxX && minY <= playerMaxY &&
                    maxX >= playerMinX && maxY >= playerMinY)
                {
                    order = Mathf.Min(order, playerY);
                }
            }

            if (occ.sr.sortingOrder != order)
                occ.sr.sortingOrder = order;
        }
    }
}

}
