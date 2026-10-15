using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class SkillTargeting
{
    // Official 1.5.8 06000d1a: preserve PNG-first lookup and the bounded GSA fallback.
    private void LoadCircle()
    {
        var frames = new List<Sprite>();
        // Retain the loading list for partial-load teardown without a constructor allocation.
        ownedAimerSprites = frames;
        for (int index = 0; index < 16; index++)
        {
            Texture2D texture = ResLoader.I != null
                ? ResLoader.I.LoadUI("effect/fanwei_" + index + ".png") : null;
            if (texture == null && ResLoader.I != null && index < 10)
                texture = ResLoader.I.LoadGsaFrame("res/effect/fanwei.gsa", index);
            if (texture == null) break;

            if (index == 0)
            {
                aimerTexW = texture.width;
                aimerHdScale = Mathf.Max(TexScale.Of(texture), 1);
            }
            Sprite sprite = Sprite.Create(texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), aimerHdScale * 100f);
            frames.Add(sprite);
        }
        aimerFrames = frames.ToArray();
        aimerSR = MakeSR("Fanwei", aimerFrames.Length > 0 ? aimerFrames[0] : null,
            Color.white, AimerSortingOrder);
    }

    // Official 06000d1b: sprite/tint/order stores do not change the enabled state.
    private SpriteRenderer MakeSR(string name, Sprite sprite, Color tint, int order)
    {
        var owner = new GameObject(name);
        owner.transform.SetParent(transform, false);
        var renderer = owner.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = tint;
        renderer.sortingOrder = order;
        return renderer;
    }

    // Official 06000d17.
    private void MakeArrow()
    {
        arrowFrames = EffectPlayer.GetMagicFramesAll(9006);
        var owner = new GameObject("DirArrow");
        owner.transform.SetParent(transform, false);
        arrowSR = owner.AddComponent<SpriteRenderer>();
        arrowSR.sortingOrder = TargetingSortingOrder;
        arrowSR.enabled = false;
    }

    // Official 06000d18. Keep the created material reference for independent-owner teardown.
    private void MakeSelLine()
    {
        var owner = new GameObject("DirOutline");
        owner.transform.SetParent(transform, false);
        selLine = owner.AddComponent<LineRenderer>();
        selLine.useWorldSpace = true;
        selLine.loop = true;
        selLine.textureMode = LineTextureMode.Stretch;
        selLine.numCapVertices = 1;
        selLine.numCornerVertices = 2;
        selLine.receiveShadows = false;
        selLine.shadowCastingMode = ShadowCastingMode.Off;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            selLineMaterial = new Material(shader) { name = "DirOutline" };
            selLine.material = selLineMaterial;
        }
        selLine.sortingOrder = TargetingSortingOrder;
        selLine.enabled = false;
    }

    // Official 06000d1c: the outline's visibility does not depend on arrow-frame availability.
    private void Show(bool on)
    {
        bool mobilePoint = MobileClientPresentation.Enabled && MobileHotSlotActive &&
            (mode == Aim.Circle || mode == Aim.Single || mode == Aim.Trap);
        bool circle = on && (mobilePoint || (mode == Aim.Circle && !noCircle)) &&
            aimerFrames != null && aimerFrames.Length > 0;
        if (aimerSR != null) aimerSR.enabled = circle;
        bool direction = on && mode == Aim.Direction;
        if (arrowSR != null)
            arrowSR.enabled = direction && arrowFrames != null && arrowFrames.Length > 0;
        if (selLine != null) selLine.enabled = direction;
    }
}
}
