using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Mapped component recovery of the official 1.5.8 BlobShadow owner.
/// </summary>
public sealed class ReferenceWorldBlobShadow : MonoBehaviour
{
    public const float WidthFactor = 0.75f;
    public const int PlayerFloorPixels = 87;
    public const int MobFloorPixels = 69;
    public const float VerticalSquash = 0.65f;
    public const int SortingUnder = -11;
    public const float AlphaFactor = 0.2f;
    public const int TextureSize = 64;
    public const float CoreRadius = 0.75f;

    private SpriteRenderer source;
    private SpriteRenderer mountSource;
    private float diameterUnits;
    private float mountDiameterUnits;
    private Vector3 worldAnchor;
    private bool hasWorldAnchor;
    private SpriteRenderer shadowRenderer;

    public static bool Enabled => GameSettings.Shadow;
    // Compatibility access to the native global switch, not an independent actor visibility flag.
    public bool ShadowEnabled { get => Enabled; set => GameSettings.Shadow = value; }
    public SpriteRenderer Source => source;
    public SpriteRenderer MountSource => mountSource;
    public float DiameterUnits => diameterUnits;
    public float MountDiameterUnits => mountDiameterUnits;
    public SpriteRenderer ShadowRenderer => shadowRenderer;

    // Original BlobShadow fields mapped onto the existing renderer component.
    internal SpriteRenderer src
    {
        get => source;
        set => source = value;
    }
    internal float diameterU
    {
        get => diameterUnits;
        set => diameterUnits = value;
    }
    internal float mountDiameterU
    {
        get => mountDiameterUnits;
        set => mountDiameterUnits = value;
    }
    internal SpriteRenderer mountSrc
    {
        get => mountSource;
        set => mountSource = value;
    }

    public static ReferenceWorldBlobShadow Attach(
        Transform owner,
        SpriteRenderer sourceRenderer,
        float initialDiameterUnits)
    {
        if (owner == null)
        {
            return null;
        }

        GameObject shadowObject = new GameObject("BlobShadow");
        shadowObject.transform.SetParent(owner, false);
        ReferenceWorldBlobShadow shadow = shadowObject.AddComponent<ReferenceWorldBlobShadow>();
        shadow.source = sourceRenderer;
        shadow.diameterUnits = initialDiameterUnits;
        return shadow;
    }

    public static float DiameterPixels(int boundWidth, int floorPixels)
    {
        return DiameterPx(boundWidth, floorPixels);
    }

    // 0x060000c0, official 1.5.8.
    internal static float DiameterPx(int boundWidth, int floorPixels) =>
        Mathf.Max(Mathf.Max((float)boundWidth, 0f) * 0.75f, (float)floorPixels);

    public void SetSource(SpriteRenderer sourceRenderer)
    {
        source = sourceRenderer;
    }

    public void SetMountSource(SpriteRenderer sourceRenderer, float sourceDiameterUnits)
    {
        mountSource = sourceRenderer;
        mountDiameterUnits = sourceDiameterUnits;
    }

    public void SetDiameterPixels(int boundWidth, float pixelsPerUnit, int floorPixels)
    {
        SetDiameterPx(boundWidth, pixelsPerUnit, floorPixels);
    }

    // 0x060000c2, official 1.5.8. Ineligible input retains the current diameter.
    internal void SetDiameterPx(int boundWidth, float pixelsPerUnit, int floorPixels)
    {
        if (boundWidth > 0 && pixelsPerUnit > 0f)
        {
            diameterUnits = Mathf.Max((float)boundWidth * 0.75f, (float)floorPixels) / pixelsPerUnit;
        }
    }

    public void SetWorldAnchor(Vector3 value)
    {
        worldAnchor = value;
        hasWorldAnchor = true;
    }

    public void ClearWorldAnchor()
    {
        worldAnchor = Vector3.zero;
        hasWorldAnchor = false;
    }

    internal static Sprite BlobSpriteForAudit()
    {
        return Blob();
    }

    internal void RefreshForAudit()
    {
        LateUpdate();
    }

    private void Awake()
    {
        GameSettings.EnsureLoaded();
        shadowRenderer = gameObject.AddComponent<SpriteRenderer>();
        shadowRenderer.sprite = Blob();
        shadowRenderer.enabled = false;
    }

    private void LateUpdate()
    {
        SpriteRenderer selectedSource;
        float selectedDiameter;
        if (mountSource && mountSource.enabled && mountSource.sprite && mountDiameterUnits > 0f)
        {
            selectedSource = mountSource;
            selectedDiameter = mountDiameterUnits;
        }
        else
        {
            selectedSource = source;
            selectedDiameter = diameterUnits;
        }
        if (!GameSettings.Shadow || !shadowRenderer || !shadowRenderer.sprite || selectedDiameter <= 0.0001f ||
            !selectedSource || !selectedSource.enabled || !selectedSource.sprite ||
            !selectedSource.gameObject.activeInHierarchy)
        {
            if (shadowRenderer && shadowRenderer.enabled) shadowRenderer.enabled = false;
            return;
        }
        if (hasWorldAnchor) transform.position = worldAnchor;
        else transform.localPosition = Vector3.zero;
        Transform parent = transform.parent;
        float px = parent ? parent.lossyScale.x : 1f;
        float py = parent ? parent.lossyScale.y : 1f;
        float ax = Mathf.Abs(px), ay = Mathf.Abs(py);
        float dx = ax < 0.0001f ? 0.0001f : ax;
        float dy = ay < 0.0001f ? 0.0001f : ay;
        transform.localScale = new Vector3(selectedDiameter / dx,
            selectedDiameter * 0.65f / dy, 1f);
        shadowRenderer.sortingLayerID = selectedSource.sortingLayerID;
        int first = selectedSource.sortingOrder;
        int second = selectedSource.sortingOrder;
        int order = unchecked(first - 11);
        if (order < 0 && second >= 0) order = 0;
        shadowRenderer.sortingOrder = order;
        Color sourceColor = selectedSource.color;
        shadowRenderer.color = new Color(0f, 0f, 0f, sourceColor.a * 0.2f);
        if (!shadowRenderer.enabled) shadowRenderer.enabled = true;
    }

    private static Sprite Blob()
    {
        return BlobShadow.BlobForAdapter();
    }
}

}
