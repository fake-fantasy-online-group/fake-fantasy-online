using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{
    private float bWorldW;
    private float bWorldH;
    private float bPpu;

    // Official 1.5.8 token 060003e7. No activation caller is established.
    // The original ignores parent and uses worldRoot for its new bridge group.
    private void PlaceCentralBridges(Transform parent, float worldW, float worldH, float ppu)
    {
        if (ResLoader.I == null) return;

        var bridges = new (int id, float x, float y, float scale)[]
        {
            (0x012c0066, 5835f, 5152f, 0.7f),
            (0x012c0067, 6472f, 5126f, 0.7f),
            (0x012c0066, 6497f, 5403f, 1f),
            (0x012c0067, 5852f, 5426f, 1f)
        };

        redBridgeGOs.Clear();
        redBridgeIds.Clear();
        bWorldW = worldW;
        bWorldH = worldH;
        bPpu = ppu;
        var walk = new List<Rect>();
        Transform bridgeRoot = new GameObject("RedBridges").transform;
        if (worldRoot != null) bridgeRoot.parent = worldRoot;

        for (int i = 0; i < bridges.Length; i++)
        {
            var bridge = bridges[i];
            ResLoader.ObjSprite image = ResLoader.I.LoadObj(bridge.id, 0, 0);
            Texture2D texture = image.tex;
            if (texture == null) continue;

            int textureHeight = texture.height;
            int textureWidth = texture.width;
            Sprite sprite = Sprite.Create(texture,
                new Rect(0f, 0f, textureWidth, textureHeight),
                new Vector2(0.5f, 0.5f), ppu * Mathf.Max(image.hdScale, 1));
            var go = new GameObject("redbridge" + i.ToString() + "_id" + bridge.id.ToString());
            go.transform.parent = bridgeRoot;
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            go.transform.position = new Vector3(
                (bridge.x - worldW * 0.5f) / ppu,
                (worldH * 0.5f - bridge.y) / ppu, 0f);
            go.transform.localScale = new Vector3(bridge.scale, bridge.scale, 1f);
            renderer.sortingOrder = unchecked(-6000 + i);
            redBridgeGOs.Add(go);
            redBridgeIds.Add(bridge.id);

            int walkTextureWidth = texture.width;
            int walkTextureHeight = texture.height;
            float width = bridge.scale * walkTextureWidth * 0.6f;
            float height = bridge.scale * walkTextureHeight;
            walk.Add(new Rect(bridge.x - width * 0.5f, bridge.y - height * 0.5f,
                width, height));
        }

        // pcRef is represented by the existing Doll adapter; keep the original
        // Unity null check and bridgeWalk store on its actual PlayerController.
        PlayerController controller = pcRef?.SourceControllerForIndependentRuntime;
        if (controller != null) controller.bridgeWalk = walk;
    }
}

}
