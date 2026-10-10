using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal sealed class ReferenceWorldDealingMarkState
{
    public ReferenceWorldDealingMarkState(
        Transform target,
        float headHeight,
        GameObject root,
        SpriteRenderer renderer)
    {
        Target = target;
        HeadHeight = headHeight;
        Root = root;
        Renderer = renderer;
    }

    public Transform Target { get; }

    public float HeadHeight { get; }

    public GameObject Root { get; }

    public SpriteRenderer Renderer { get; }

    public int Frame { get; set; }

    public float Elapsed { get; set; }
}

/// <summary>
/// Source translation of the locked official 1.3.4 DealingMark owner.
/// </summary>
public sealed class ReferenceWorldDealingMarks : IDisposable
{
    public const string ResourcePath = "res/effect/dealing.gsa";
    public const string ObjectName = "DealingMark";
    public const int ExpectedFrameCount = 4;
    public const int SortingOrder = 30000;
    public const float FrameSeconds = 0.1f;
    public const float PixelsPerUnit = 100f;
    public const float HeadOffset = 0.35f;
    public const float HeadHeightFallback = 1.4f;

    private readonly ReferenceResourceLoader resources;
    private readonly List<ReferenceWorldDealingMarkState> live =
        new List<ReferenceWorldDealingMarkState>();
    private Sprite[] frames;
    private bool triedFrames;
    private int partnerEntityId;
    private bool disposed;

    private ReferenceWorldDealingMarks(ReferenceResourceLoader resources)
    {
        this.resources = resources;
        Ready = true;
    }

    public bool Ready { get; private set; }

    public int Count => live.Count;

    public int PartnerEntityId => partnerEntityId;

    public static bool TryCreate(
        ReferenceResourceLoader resources,
        out ReferenceWorldDealingMarks owner)
    {
        owner = null;
        if (resources == null)
        {
            return false;
        }

        owner = new ReferenceWorldDealingMarks(resources);
        return true;
    }

    public static float ResolveHeadHeight(SpriteRenderer renderer)
    {
        Sprite sprite = renderer == null ? null : renderer.sprite;
        if (!sprite || !(sprite.pixelsPerUnit > 0f))
        {
            return HeadHeightFallback;
        }

        float height = sprite.rect.height;
        return height > 0f ? height / sprite.pixelsPerUnit : HeadHeightFallback;
    }

    public bool Synchronize(
        bool dialogVisible,
        int otherEntityId,
        Transform localTarget,
        SpriteRenderer localRenderer,
        Transform remoteTarget,
        SpriteRenderer remoteRenderer,
        float deltaTime)
    {
        ThrowIfDisposed();
        if (!Ready || deltaTime < 0f || float.IsNaN(deltaTime) ||
            float.IsInfinity(deltaTime))
        {
            return false;
        }

        if (!dialogVisible)
        {
            Clear();
            partnerEntityId = 0;
            return true;
        }

        if (partnerEntityId != otherEntityId)
        {
            Clear();
            partnerEntityId = otherEntityId;
        }

        Attach(localTarget, ResolveHeadHeight(localRenderer));
        Attach(remoteTarget, ResolveHeadHeight(remoteRenderer));
        Advance(deltaTime);
        return true;
    }

    public void Clear()
    {
        ThrowIfDisposed();
        for (int index = 0; index < live.Count; index++)
        {
            DestroyOwned(live[index].Root);
        }

        live.Clear();
    }

    public Transform TargetAt(int index)
    {
        return index >= 0 && index < live.Count ? live[index].Target : null;
    }

    public int FrameAt(int index)
    {
        return index >= 0 && index < live.Count ? live[index].Frame : -1;
    }

    public Vector3 PositionAt(int index)
    {
        return index >= 0 && index < live.Count && live[index].Root
            ? live[index].Root.transform.position
            : new Vector3(float.NaN, float.NaN, float.NaN);
    }

    public int SortingOrderAt(int index)
    {
        return index >= 0 && index < live.Count && live[index].Renderer
            ? live[index].Renderer.sortingOrder
            : int.MinValue;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        Clear();
        disposed = true;
        Ready = false;
        partnerEntityId = 0;
        DestroySprites(frames);
        frames = null;
    }

    private void Attach(Transform target, float headHeight)
    {
        if (!target)
        {
            return;
        }

        if (!EnsureFrames())
        {
            return;
        }

        for (int index = 0; index < live.Count; index++)
        {
            if (live[index].Target == target)
            {
                return;
            }
        }

        GameObject root = new GameObject(ObjectName);
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = SortingOrder;
        renderer.sprite = frames[0];
        live.Add(new ReferenceWorldDealingMarkState(
            target,
            headHeight,
            root,
            renderer));
    }

    private bool EnsureFrames()
    {
        if (frames != null)
        {
            return true;
        }

        if (triedFrames)
        {
            return false;
        }

        triedFrames = true;
        if (resources.GsaFrameCount(ResourcePath) != ExpectedFrameCount)
        {
            return false;
        }

        Sprite[] loaded = new Sprite[ExpectedFrameCount];
        try
        {
            for (int index = 0; index < loaded.Length; index++)
            {
                Texture2D texture = resources.LoadGsaFrame2021First(ResourcePath, index);
                // The GSA canvas is 32x39, while the official direct-frame decode crops each
                // nontransparent frame to its exact 32x32 texture before TexScale.MakeSprite.
                if (!texture || texture.width != 32 || texture.height != 32)
                {
                    DestroySprites(loaded);
                    return false;
                }

                loaded[index] = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    PixelsPerUnit);
                if (!loaded[index])
                {
                    DestroySprites(loaded);
                    return false;
                }

                loaded[index].name = ObjectName + index;
            }

            frames = loaded;
            return true;
        }
        catch
        {
            DestroySprites(loaded);
            return false;
        }
    }

    private void Advance(float deltaTime)
    {
        for (int index = live.Count - 1; index >= 0; index--)
        {
            ReferenceWorldDealingMarkState mark = live[index];
            if (!mark.Target)
            {
                DestroyOwned(mark.Root);
                live.RemoveAt(index);
                continue;
            }

            Vector3 position = mark.Target.position;
            position.y += mark.HeadHeight + HeadOffset;
            mark.Root.transform.position = position;
            mark.Elapsed += deltaTime;
            if (mark.Elapsed >= FrameSeconds)
            {
                mark.Elapsed -= FrameSeconds;
                mark.Frame = (mark.Frame + 1) % frames.Length;
                mark.Renderer.sprite = frames[mark.Frame];
            }
        }
    }

    private static void DestroySprites(Sprite[] values)
    {
        if (values == null)
        {
            return;
        }

        for (int index = 0; index < values.Length; index++)
        {
            DestroyOwned(values[index]);
            values[index] = null;
        }
    }

    private static void DestroyOwned(UnityEngine.Object value)
    {
        if (!value)
        {
            return;
        }

        if (UnityEngine.Application.isPlaying)
        {
            UnityEngine.Object.Destroy(value);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(value);
        }
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(ReferenceWorldDealingMarks));
        }
    }
}

}
