using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Exact geometry and state selection recovered from RemotePlayer.UpdateStallSign/StallSign.Tick.
/// </summary>
internal static class ReferenceWorldStallSignMath
{
    public const float PixelsPerUnit = 100f;
    public const float FallbackHeadTopY = 1.6f;
    public const float SittingTopOffsetPixels = 30f;
    public const float TextWrapPixels = 110f;
    public const float ColliderWidthPixels = 118f;
    public const float ColliderHeightPixels = 69f;
    public const float ColliderOffsetXPixels = -2f;
    public const float ColliderOffsetYPixels = 43.5f;
    public const float TextOffsetXPixels = -2f;
    public const float TextOffsetYPixels = 40f;

    public static bool IsPurchase(byte stallType)
    {
        return stallType == 1;
    }

    public static float HeadTopY(int sittingTopPixels)
    {
        return sittingTopPixels < 1
            ? FallbackHeadTopY
            : (sittingTopPixels + SittingTopOffsetPixels) / PixelsPerUnit;
    }
}

/// <summary>
/// Source-owned sale/purchase bubble sprites shared by every remote stall sign.
/// </summary>
internal sealed class ReferenceWorldStallSignAssets : IDisposable
{
    public const string SaleBubblePath = "res/effect/paopao_maichu.gsa";
    public const string PurchaseBubblePath = "res/effect/paopao_shougou.gsa";
    public const int BubbleWidthPixels = 141;
    public const int BubbleHeightPixels = 91;

    private Sprite saleBubble;
    private Sprite purchaseBubble;
    private bool disposed;

    private ReferenceWorldStallSignAssets(
        Texture2D saleTexture,
        Texture2D purchaseTexture,
        Sprite saleBubble,
        Sprite purchaseBubble)
    {
        SaleTexture = saleTexture;
        PurchaseTexture = purchaseTexture;
        this.saleBubble = saleBubble;
        this.purchaseBubble = purchaseBubble;
    }

    public Texture2D SaleTexture { get; }
    public Texture2D PurchaseTexture { get; }
    public Sprite SaleBubble => saleBubble;
    public Sprite PurchaseBubble => purchaseBubble;

    public static bool TryCreate(
        ReferenceResourceLoader resources,
        out ReferenceWorldStallSignAssets assets)
    {
        assets = null;
        if (resources == null)
        {
            return false;
        }

        Texture2D sale = resources.LoadGsa2021First(SaleBubblePath);
        Texture2D purchase = resources.LoadGsa2021First(PurchaseBubblePath);
        if (!HasExactGeometry(sale) || !HasExactGeometry(purchase))
        {
            return false;
        }

        Sprite saleSprite = null;
        Sprite purchaseSprite = null;
        try
        {
            saleSprite = MakeBubbleSprite(sale, "RecoveredRemoteStallSaleBubble");
            purchaseSprite = MakeBubbleSprite(
                purchase,
                "RecoveredRemoteStallPurchaseBubble");
            if (!saleSprite || !purchaseSprite)
            {
                return false;
            }

            assets = new ReferenceWorldStallSignAssets(
                sale,
                purchase,
                saleSprite,
                purchaseSprite);
            saleSprite = null;
            purchaseSprite = null;
            return true;
        }
        finally
        {
            DestroyOwned(saleSprite);
            DestroyOwned(purchaseSprite);
        }
    }

    public Sprite BubbleFor(byte stallType)
    {
        ThrowIfDisposed();
        return ReferenceWorldStallSignMath.IsPurchase(stallType)
            ? purchaseBubble
            : saleBubble;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        DestroyOwned(saleBubble);
        DestroyOwned(purchaseBubble);
        saleBubble = null;
        purchaseBubble = null;
    }

    private static bool HasExactGeometry(Texture2D texture)
    {
        return texture &&
               GsaDecoder.Scale(texture) == 1 &&
               GsaDecoder.W(texture) == BubbleWidthPixels &&
               GsaDecoder.H(texture) == BubbleHeightPixels;
    }

    private static Sprite MakeBubbleSprite(Texture2D texture, string name)
    {
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0f),
            GsaDecoder.Scale(texture) * ReferenceWorldStallSignMath.PixelsPerUnit,
            0,
            SpriteMeshType.FullRect);
        if (sprite)
        {
            sprite.name = name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
        }

        return sprite;
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(ReferenceWorldStallSignAssets));
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
}

/// <summary>
/// Disposable source translation of StallSign.Tick for one authoritative remote stall owner.
/// </summary>
internal sealed class ReferenceWorldStallSignView : IDisposable
{
    public const int BubbleSortingOrder = 32001;
    public const int TextSortingOrder = 32002;

    private readonly ReferenceWorldRemoteFeedbackAssets feedback;
    private readonly int browseOwnerId;
    private GameObject root;
    private SpriteRenderer bubbleRenderer;
    private BoxCollider2D pickBox;
    private GameObject textRoot;
    private SpriteRenderer textRenderer;
    private Sprite textSprite;
    private Texture2D textTexture;
    private int lastType = -2;
    private string lastName;
    private bool disposed;

    public ReferenceWorldStallSignView(
        ReferenceWorldRemoteFeedbackAssets feedback,
        int browseOwnerId)
    {
        this.feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
        this.browseOwnerId = browseOwnerId;
    }

    public GameObject Root => root;
    public SpriteRenderer BubbleRenderer => bubbleRenderer;
    public BoxCollider2D PickBox => pickBox;
    public SpriteRenderer TextRenderer => textRenderer;
    public Texture2D TextTexture => textTexture;
    public int BrowseOwnerId => browseOwnerId;
    public int LastType => lastType;
    public string LastName => lastName;

    public bool TryTick(
        Transform owner,
        bool active,
        byte stallType,
        string displayName,
        float headTopY)
    {
        ThrowIfDisposed();
        if (!active)
        {
            if (root && root.activeSelf)
            {
                root.SetActive(false);
            }

            return true;
        }

        if (owner == null || !IsFinite(headTopY))
        {
            return false;
        }

        ReferenceWorldStallSignAssets stallAssets = feedback.StallSign;
        if (stallAssets == null)
        {
            return false;
        }

        if (!root && !Build(owner))
        {
            return false;
        }

        if (!root.activeSelf)
        {
            root.SetActive(true);
        }

        if (lastType != stallType || bubbleRenderer.sprite == null)
        {
            Sprite bubble = stallAssets.BubbleFor(stallType);
            if (!bubble)
            {
                return false;
            }

            bubbleRenderer.sprite = bubble;
            lastType = stallType;
        }

        root.transform.localPosition = new Vector3(0f, headTopY, 0f);
        string resolvedName = displayName ?? string.Empty;
        if (lastName != resolvedName || !textRoot)
        {
            if (!TryRebuildText(resolvedName))
            {
                return false;
            }

            lastName = resolvedName;
        }

        return true;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        DestroyOwned(root);
        DestroyOwned(textSprite);
        DestroyOwned(textTexture);
        root = null;
        bubbleRenderer = null;
        pickBox = null;
        textRoot = null;
        textRenderer = null;
        textSprite = null;
        textTexture = null;
    }

    private bool Build(Transform owner)
    {
        root = new GameObject("StallSignBubble");
        root.transform.SetParent(owner, false);
        bubbleRenderer = root.AddComponent<SpriteRenderer>();
        bubbleRenderer.sortingOrder = BubbleSortingOrder;
        Material material = feedback.NameTagMaterial;
        if (material)
        {
            bubbleRenderer.sharedMaterial = material;
        }

        if (browseOwnerId > 0)
        {
            pickBox = root.AddComponent<BoxCollider2D>();
            pickBox.size = new Vector2(
                ReferenceWorldStallSignMath.ColliderWidthPixels /
                    ReferenceWorldStallSignMath.PixelsPerUnit,
                ReferenceWorldStallSignMath.ColliderHeightPixels /
                    ReferenceWorldStallSignMath.PixelsPerUnit);
            pickBox.offset = new Vector2(
                ReferenceWorldStallSignMath.ColliderOffsetXPixels /
                    ReferenceWorldStallSignMath.PixelsPerUnit,
                ReferenceWorldStallSignMath.ColliderOffsetYPixels /
                    ReferenceWorldStallSignMath.PixelsPerUnit);
        }

        return true;
    }

    private bool TryRebuildText(string displayName)
    {
        if (string.IsNullOrEmpty(displayName))
        {
            if (textRoot && textRoot.activeSelf)
            {
                textRoot.SetActive(false);
            }

            DestroyOwned(textSprite);
            DestroyOwned(textTexture);
            textSprite = null;
            textTexture = null;
            if (textRenderer)
            {
                textRenderer.sprite = null;
            }

            return true;
        }

        string[] wrapped = feedback.Font.Wrap(
            displayName,
            ReferenceWorldStallSignMath.TextWrapPixels,
            1f);
        string primary = displayName;
        string[] topLines = null;
        if (wrapped.Length >= 2)
        {
            topLines = new[] { wrapped[0] };
            primary = feedback.Font.Elide(
                string.Join(string.Empty, wrapped, 1, wrapped.Length - 1),
                ReferenceWorldStallSignMath.TextWrapPixels,
                1f);
        }

        Texture2D nextTexture = feedback.Font.BakeNameTag(
            primary,
            string.Empty,
            Color.white,
            Color.white,
            1,
            topLines,
            Color.white,
            null,
            null,
            null);
        if (!nextTexture)
        {
            return false;
        }

        Sprite nextSprite = Sprite.Create(
            nextTexture,
            new Rect(0f, 0f, nextTexture.width, nextTexture.height),
            new Vector2(0.5f, 0.5f),
            ReferenceWorldStallSignMath.PixelsPerUnit,
            0,
            SpriteMeshType.FullRect);
        if (!nextSprite)
        {
            DestroyOwned(nextTexture);
            return false;
        }

        nextSprite.name = "RecoveredRemoteStallSignTextSprite";
        nextSprite.hideFlags = HideFlags.HideAndDontSave;
        if (!textRoot)
        {
            textRoot = new GameObject("StallSignText");
            textRoot.transform.SetParent(root.transform, false);
            textRoot.transform.localPosition = new Vector3(
                ReferenceWorldStallSignMath.TextOffsetXPixels /
                    ReferenceWorldStallSignMath.PixelsPerUnit,
                ReferenceWorldStallSignMath.TextOffsetYPixels /
                    ReferenceWorldStallSignMath.PixelsPerUnit,
                0f);
            textRenderer = textRoot.AddComponent<SpriteRenderer>();
            textRenderer.sortingOrder = TextSortingOrder;
            Material material = feedback.NameTagMaterial;
            if (material)
            {
                textRenderer.sharedMaterial = material;
            }
        }
        else if (!textRoot.activeSelf)
        {
            textRoot.SetActive(true);
        }

        Sprite oldSprite = textSprite;
        Texture2D oldTexture = textTexture;
        textSprite = nextSprite;
        textTexture = nextTexture;
        textRenderer.sprite = textSprite;
        DestroyOwned(oldSprite);
        DestroyOwned(oldTexture);
        return true;
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(ReferenceWorldStallSignView));
        }
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
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
}

}
