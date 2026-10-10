using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{

    // Official 1.5.8 token 060003e1.
    private void ClearMapObjectCaches()
    {
        _mapObjBudget.Reset();
        _mapObjNoted.Clear();
        foreach (Sprite sprite in objCache.Values)
            if (sprite != null) Destroy(sprite);
        objCache.Clear();
        foreach (List<(Sprite, int, int)> slices in sliceCache.Values)
            if (slices != null)
                foreach (var slice in slices)
                    if (slice.Item1 != null) Destroy(slice.Item1);
        sliceCache.Clear();
        foreach (var tree in treeCache.Values)
        {
            if (tree.Item1 != null) Destroy(tree.Item1);
            if (tree.Item2 != null) Destroy(tree.Item2);
        }
        treeCache.Clear();
        _scanOpaqueCache.Clear();
        if (ResLoader.I != null) ResLoader.I.ClearObjCaches();
    }
}

}
