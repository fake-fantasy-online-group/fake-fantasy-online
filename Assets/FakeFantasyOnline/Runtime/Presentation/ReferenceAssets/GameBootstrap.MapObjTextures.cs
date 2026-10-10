using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{
    // Official 1.5.8 token 060003cd. Feeds every map-object sprite to the
    // caller's sink: the plain object cache, then the sliced objects, then the
    // two sprites (front and back) of every cached tree.
    public void CollectMapObjTextures(Action<Texture2D> sink)
    {
        foreach (KeyValuePair<int, Sprite> entry in objCache)
        {
            if (entry.Value != null)
            {
                sink(entry.Value.texture);
            }
        }

        foreach (KeyValuePair<int, List<(Sprite, int, int)>> entry in sliceCache)
        {
            List<(Sprite, int, int)> slices = entry.Value;
            if (slices == null)
            {
                continue;
            }

            foreach ((Sprite sprite, int _, int _) in slices)
            {
                if (sprite != null)
                {
                    sink(sprite.texture);
                }
            }
        }

        foreach (KeyValuePair<int, (Sprite, Sprite, int, int, int)> entry in treeCache)
        {
            (Sprite front, Sprite back, int _, int _, int _) = entry.Value;
            if (front != null)
            {
                sink(front.texture);
            }

            if (back != null)
            {
                sink(back.texture);
            }
        }
    }
}

}
