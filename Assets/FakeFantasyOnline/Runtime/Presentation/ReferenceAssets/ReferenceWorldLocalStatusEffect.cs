#if UNITY_EDITOR
// Historical 1.3.4 audit fixture. The 1.5.8 product calls EffectPlayer.SpawnOn.
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Source-owned local branch of CombatWorld.ShowEntityStatus and EffectPlayer.SpawnOn.
/// </summary>
/// <remarks>
/// The exact 衰弱 template retains its specialized clock. Other server-authored names enter the
/// generic EffectPlayer path instead of being discarded. Buff rows never enter this owner: the
/// official local body effect requires an explicit Application edge.
/// </remarks>
internal sealed class ReferenceWorldLocalStatusEffect : IDisposable
{
    public const string WeakenEffectName = "衰弱";
    public const string WeakenEffectPath = "effect/衰弱.eff";
    public const string WeakenMagicPath = "magic/magic/magic0102.gso";
    public const int WeakenMagicId = 102;
    public const int RequiredEffectBytes = 196;
    public const int RequiredMagicBytes = 12072;
    public const uint RequiredMagicVersion = 65537;
    public const int RequiredFrameCount = 8;
    public const float FrameMilliseconds = 100f;
    public const float LifetimeMilliseconds = 800f;
    public const int FrontSortingOffset = 20;

    private const string EffectSha256 =
        "0bc9ec2acf2af71d8b037e05555eeb278cfa252510229ddb921a4d8c40766f91";
    private const string MagicSha256 =
        "df178d9bf8a1dda0cb0bf131555cd5938035c5625b96d461ef0fd2ecf21cea4b";

    private readonly Transform target;
    private readonly SpriteRenderer targetRenderer;
    private ReferenceMagicFrameSet magicFrames;
    private Material material;
    private GameObject activeRoot;
    private SpriteRenderer activeRenderer;
    private float elapsedMilliseconds;
    private int frameIndex = -1;
    private bool disposed;

    private ReferenceWorldLocalStatusEffect(
        Transform target,
        SpriteRenderer targetRenderer,
        ReferenceMagicFrameSet magicFrames,
        Material material)
    {
        this.target = target;
        this.targetRenderer = targetRenderer;
        this.magicFrames = magicFrames;
        this.material = material;
        Ready = true;
    }

    public bool Ready { get; private set; }

    public bool Active => activeRoot != null;

    public int FrameIndex => frameIndex;

    public float ElapsedMilliseconds => elapsedMilliseconds;

    public long LastRevision { get; private set; }

    public SpriteRenderer Renderer => activeRenderer;

    public Transform ActiveRoot => activeRoot == null ? null : activeRoot.transform;

    public static bool TryCreate(
        ReferenceResourceLoader resources,
        Transform target,
        SpriteRenderer targetRenderer,
        out ReferenceWorldLocalStatusEffect owner)
    {
        owner = null;
        if (resources == null || target == null || targetRenderer == null)
        {
            return false;
        }

        byte[] effectBytes = resources.ReadRes(WeakenEffectPath);
        byte[] magicBytes = resources.ReadRes(WeakenMagicPath);
        if (effectBytes == null || effectBytes.Length != RequiredEffectBytes ||
            !string.Equals(Sha256Hex(effectBytes), EffectSha256, StringComparison.Ordinal) ||
            magicBytes == null || magicBytes.Length != RequiredMagicBytes ||
            !string.Equals(Sha256Hex(magicBytes), MagicSha256, StringComparison.Ordinal))
        {
            return false;
        }

        ReferenceMagicFrameSet frames = null;
        Material alpha = null;
        try
        {
            if (!ReferenceMagicFrameSet.TryCreateGso(
                    resources,
                    WeakenMagicId,
                    WeakenMagicPath,
                    RequiredMagicVersion,
                    24,
                    RequiredFrameCount,
                    out frames) ||
                frames == null)
            {
                frames?.Dispose();
                return false;
            }

            alpha = EffectPlayer.BorrowAlphaMaterial();
            if (alpha == null)
            {
                frames.Dispose();
                return false;
            }

            owner = new ReferenceWorldLocalStatusEffect(
                target,
                targetRenderer,
                frames,
                alpha);
            return true;
        }
        catch
        {
            frames?.Dispose();
            throw;
        }
    }

    public bool ApplyEdges(IReadOnlyList<WorldEntityStatusEffect> edges)
    {
        ThrowIfDisposed();
        if (!Ready || edges == null)
        {
            return false;
        }

        for (int index = 0; index < edges.Count; index++)
        {
            WorldEntityStatusEffect edge = edges[index];
            if (edge == null)
            {
                return false;
            }

            if (edge.Revision <= LastRevision)
            {
                continue;
            }

            if (edge.Revision < 1)
            {
                return false;
            }

            if (string.Equals(
                    edge.Effect,
                    WeakenEffectName,
                    StringComparison.Ordinal) &&
                !RestartWeakenEffect())
            {
                return false;
            }
            if (!string.IsNullOrEmpty(edge.Effect) &&
                !string.Equals(
                    edge.Effect,
                    WeakenEffectName,
                    StringComparison.Ordinal))
            {
                EffectPlayer player = EffectPlayer.PlayNamed(
                    target.position,
                    edge.Effect,
                    100f,
                    1,
                    -1,
                    false,
                    false);
                if (player)
                {
                    player.followTarget = target;
                    player.attachSort = targetRenderer;
                }
            }

            LastRevision = edge.Revision;
        }

        return true;
    }

    public bool Advance(float deltaTime)
    {
        ThrowIfDisposed();
        if (!Ready || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) ||
            deltaTime < 0f)
        {
            return false;
        }

        if (activeRoot == null)
        {
            return true;
        }

        elapsedMilliseconds += deltaTime * 1000f;
        if (elapsedMilliseconds >= LifetimeMilliseconds)
        {
            ReleaseActive();
            return true;
        }

        frameIndex = Mathf.FloorToInt(elapsedMilliseconds / FrameMilliseconds) %
                     RequiredFrameCount;
        activeRenderer.sprite = magicFrames.Frame(frameIndex);
        activeRenderer.sortingOrder = targetRenderer.sortingOrder + FrontSortingOffset;
        return activeRenderer.sprite != null;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        ReleaseActive();
        magicFrames?.Dispose();
        magicFrames = null;
        material = null;
        Ready = false;
    }

    private bool RestartWeakenEffect()
    {
        ReleaseActive();
        if (target == null || targetRenderer == null || magicFrames == null ||
            material == null)
        {
            return false;
        }

        activeRoot = new GameObject(WeakenEffectName);
        activeRoot.transform.SetParent(target, false);
        activeRoot.transform.localPosition = Vector3.zero;
        activeRoot.transform.localRotation = Quaternion.identity;
        activeRoot.transform.localScale = Vector3.one;
        activeRenderer = activeRoot.AddComponent<SpriteRenderer>();
        activeRenderer.sharedMaterial = material;
        activeRenderer.color = Color.white;
        activeRenderer.sortingOrder = targetRenderer.sortingOrder + FrontSortingOffset;
        elapsedMilliseconds = 0f;
        frameIndex = 0;
        activeRenderer.sprite = magicFrames.Frame(frameIndex);
        return activeRenderer.sprite != null;
    }

    private void ReleaseActive()
    {
        activeRenderer = null;
        DestroyOwned(activeRoot);
        activeRoot = null;
        elapsedMilliseconds = 0f;
        frameIndex = -1;
    }

    private static string Sha256Hex(byte[] value)
    {
        using (SHA256 hash = SHA256.Create())
        {
            return BitConverter.ToString(hash.ComputeHash(value))
                .Replace("-", string.Empty)
                .ToLowerInvariant();
        }
    }

    private static void DestroyOwned(UnityEngine.Object value)
    {
        if (value == null)
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
            throw new ObjectDisposedException(nameof(ReferenceWorldLocalStatusEffect));
        }
    }
}

}
#endif
