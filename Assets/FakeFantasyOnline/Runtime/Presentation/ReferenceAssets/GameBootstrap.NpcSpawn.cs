using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{
    // Official 1.5.8 token 060003d5.
    private GameObject SpawnNpcEntry(
        NpcEntry e, Transform parent, float worldW, float worldH, float ppu)
    {
        PlayerController.OctToStored(e.dir, out int storedDir, out bool flipX);
        ResLoader loader = ResLoader.I;
        DimgDecoder.Result decoded = default(DimgDecoder.Result);
        if (loader != null)
            decoded = loader.LoadNpcFull(e.sprite, storedDir);
        bool full = decoded.ok;
        Texture2D texture;
        if (full) texture = decoded.tex;
        else
        {
            loader = ResLoader.I;
            texture = loader != null ? loader.LoadNpcTex(e.sprite) : null;
        }
        if (texture == null) return null;

        if (!_scanOpaqueCache.TryGetValue(texture, out var fractions))
        {
            ScanOpaque(texture, out float cx, out float topf);
            fractions = (cx, topf);
            _scanOpaqueCache.Add(texture, fractions);
        }

        GameObject root = new GameObject(
            string.IsNullOrEmpty(e.name) ? e.sprite : e.name);
        root.transform.parent = parent;
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        npcSRs.Add(renderer);

        int scale = full ? Mathf.Max(decoded.scale, 1) : 1;
        Sprite sprite = Sprite.Create(texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0f, 1f), ppu * scale);
        renderer.sprite = sprite;
        if (flipX) renderer.flipX = true;

        int offsetX = full ? decoded.nLeft - decoded.boundCx : 0;
        int offsetY = full ? decoded.nTop - decoded.boundCy : 0;
        Vector3 position = new Vector3(
            (e.x + offsetX - worldW * 0.5f) / ppu,
            (worldH * 0.5f - (e.y + offsetY)) / ppu,
            0f);
        root.transform.position = position;

        Material material = IsoYCharMat();
        if (material != null)
        {
            renderer.sharedMaterial = material;
            renderer.sortingOrder = 0;
        }
        else
        {
            renderer.sortingOrder = (int)e.y;
        }

        Vector3 feet = new Vector3(
            (e.x - worldW * 0.5f) / ppu,
            (worldH * 0.5f - e.y) / ppu,
            0f);
        if (full)
        {
            int logicalWidth = texture.width / scale;
            if (logicalWidth < 1) logicalWidth = decoded.boundW;
            if (logicalWidth > 0)
            {
                float diameter = BlobShadow.DiameterPx(logicalWidth, BlobShadow.RingMobPx) / ppu;
                BlobShadow shadow = BlobShadow.Attach(root.transform, renderer, diameter);
                if (shadow != null)
                {
                    shadow.worldAnchor = feet;
                    shadow.hasWorldAnchor = true;
                }
            }
        }

        if (full && storedDir == 0 && !flipX &&
            decoded.frames / Mathf.Max(decoded.dirs, 1) > 1)
        {
            loader = ResLoader.I;
            if (loader != null)
            {
                string spriteName = e.sprite;
                float entryX = e.x, entryY = e.y;
                int boundCx = decoded.boundCx, boundCy = decoded.boundCy;
                FrameAnimator animator = root.AddComponent<FrameAnimator>();
                animator.frameMs = 100f;
                animator.loop = true;
                animator.pauseSecMin = 0.15f;
                animator.pauseSecMax = 1f;
                animator.loader = () =>
                {
                    ResLoader.ObjAnim animation = ResLoader.I.LoadNpcAnim(spriteName, ppu);
                    Sprite[] frames = animation.frames;
                    if (frames == null) return (null, null);
                    Vector3[] positions = new Vector3[frames.Length];
                    for (int i = 0; i < frames.Length; i++)
                    {
                        int frameX = animation.nLeft[i] - boundCx;
                        int frameY = animation.nTop[i] - boundCy;
                        positions[i] = new Vector3(
                            (entryX + frameX - worldW * 0.5f) / ppu,
                            (worldH * 0.5f - (entryY + frameY)) / ppu,
                            0f);
                    }
                    return (frames, positions);
                };
            }
        }

        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        Bounds bounds = renderer.sprite.bounds;
        float minimum = 100f / pixelsPerUnit;
        collider.size = new Vector2(
            Mathf.Max(bounds.size.x, minimum),
            Mathf.Max(bounds.size.y, minimum));
        collider.offset = new Vector2(bounds.center.x, bounds.center.y);

        NpcInteract interact = root.AddComponent<NpcInteract>();
        interact.npcName = e.name;
        interact.portrait = e.portrait;
        interact.label = e.label;
        interact.sell = e.sell;
        interact.trans = e.trans;
        interact.feetPos = feet;
        interact.hasFeetPos = true;

        NpcChatter chatter = root.AddComponent<NpcChatter>();
        chatter.npcName = e.name;
        float center = fractions.Item1 > 0f ? fractions.Item1 : 0.5f;
        chatter.headAnchor = new Vector3(
            position.x + texture.width / (ppu * scale) * center,
            position.y - texture.height / (ppu * scale) * fractions.Item2 + 0.05f,
            0f);
        return root;
    }
}

}
