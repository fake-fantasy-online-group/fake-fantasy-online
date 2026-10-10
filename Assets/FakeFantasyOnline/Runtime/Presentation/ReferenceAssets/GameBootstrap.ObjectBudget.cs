using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{

    internal void TouchMapObjectForIndependentRuntime(int id) => _mapObjBudget.Touch(id);
    internal void NoteMapObjectForIndependentRuntime(int id) => NoteMapObj(id);

    public string MapObjBudgetStat() => _mapObjBudget.Stat();

    private void NoteMapObj(int id)
    {
        if (_mapObjNoted.Contains(id)) return;
        _mapObjNoted.Add(id);
        _mapObjBudget.Note(id, ResLoader.I != null ? ResLoader.I.ObjSrcBytes(id) : 0L);
    }

    public int EvictMapObjectsAtSafePoint()
    {
        if (ResLoader.I != null)
            foreach (int id in _mapObjNoted)
                _mapObjBudget.UpdateBytes(id, ResLoader.I.ObjSrcBytes(id));
        return _mapObjBudget.EvictAtSafePoint(DropObjArt);
    }

    private void DropObjArt(int id)
    {
        if (objCache.TryGetValue(id, out Sprite sprite))
        {
            if (sprite != null) Destroy(sprite);
            objCache.Remove(id);
        }
        if (sliceCache.TryGetValue(id, out List<(Sprite, int, int)> slices))
        {
            if (slices != null)
                foreach (var slice in slices)
                    if (slice.Item1 != null) Destroy(slice.Item1);
            sliceCache.Remove(id);
        }
        if (treeCache.TryGetValue(id, out var tree))
        {
            if (tree.Item1 != null) Destroy(tree.Item1);
            if (tree.Item2 != null) Destroy(tree.Item2);
            treeCache.Remove(id);
        }
        if (ResLoader.I != null) ResLoader.I.DropObj(id);
        _mapObjNoted.Remove(id);
    }
}

}
