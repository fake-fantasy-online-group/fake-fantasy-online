using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{
    // Borrow the current receiver field at the original parent-read point.
    internal Transform WorldRootForMapConstructionForIndependentRuntime => worldRoot;

    // Admission branch of official PlaceMapObjects (060003e6). The source map
    // parser owns objsOk; the compatibility map only exposes that field.
    internal static bool MapObjectsAdmittedForIndependentRuntime(ReferenceGsmMap map) =>
        map != null && map.ObjectsOk;

    // Tail of PlaceMapObjects after static and embedded children are placed.
    internal void RegisterMapObjectOccludersForIndependentRuntime(Transform mapObjectsRoot)
    {
        if (mapObjectsRoot == null) return;
        for (int i = 0; i < mapObjectsRoot.childCount; i++)
        {
            SpriteRenderer renderer = mapObjectsRoot.GetChild(i).GetComponent<SpriteRenderer>();
            if (renderer == null || renderer.sortingOrder < 0) continue;
            Occ occ = new Occ
            {
                sr = renderer,
                baseOrder = renderer.sortingOrder,
                floor = renderer.sortingOrder
            };
            occluders.Add(occ);
        }
    }
}

}
