using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{
    // Official 1.5.8 token 060003d3. No current placement caller is established.
    private void PlaceTreeSplit(GsmMap.Obj o, Transform parent, float worldW, float worldH, float ppu)
    {
        if (!treeCache.TryGetValue(o.id, out var tree))
        {
            ResLoader.ObjSprite image = ResLoader.I != null
                ? ResLoader.I.LoadObj(o.id, o.xl, o.yl) : default;
            if (image.tex == null)
            {
                objCache[o.id] = null;
                treeCache[o.id] = (null, null, 0, 0, 0);
                return;
            }

            int width = image.tex.width;
            int height = image.tex.height;
            int scale = Mathf.Max(image.hdScale, 1);
            int trunkHeight = Mathf.Clamp(90 * scale, 1, height - 1);
            int canopyHeight = height - trunkHeight;
            int canopyOffset = canopyHeight / scale;
            float spritePpu = ppu * scale;
            Sprite trunk = Sprite.Create(image.tex, new Rect(0, 0, width, trunkHeight),
                new Vector2(0, 1), spritePpu);
            Sprite canopy = Sprite.Create(image.tex, new Rect(0, trunkHeight, width, canopyHeight),
                new Vector2(0, 1), spritePpu);
            tree = (trunk, canopy, image.nLeft, image.nTop, canopyOffset);
            treeCache[o.id] = tree;
            NoteMapObj(o.id);
        }

        if (tree.Item1 == null) return;

        // The native helper evaluates these names but constructs unnamed objects.
        _ = "tree" + o.id.ToString() + "_canopy";
        GameObject canopyObject = new GameObject();
        canopyObject.transform.parent = parent;
        SpriteRenderer canopyRenderer = canopyObject.AddComponent<SpriteRenderer>();
        canopyRenderer.sprite = tree.Item2;
        int order = o.sy - o.pz + o.yl;
        int top = o.sy + tree.Item4 - o.pz;
        float x = (o.sx + tree.Item3 + worldW * -0.5f) / ppu;
        float halfWorldHeight = worldH * 0.5f;
        canopyObject.transform.position = new Vector3(x, (halfWorldHeight - top) / ppu, 0f);
        canopyRenderer.sortingOrder = order + 300;

        _ = "tree" + o.id.ToString() + "_trunk";
        GameObject trunkObject = new GameObject();
        trunkObject.transform.parent = parent;
        SpriteRenderer trunkRenderer = trunkObject.AddComponent<SpriteRenderer>();
        trunkRenderer.sprite = tree.Item1;
        trunkObject.transform.position = new Vector3(
            x, (halfWorldHeight - (top + tree.Item5)) / ppu, 0f);
        trunkRenderer.sortingOrder = order;
    }
}

}
