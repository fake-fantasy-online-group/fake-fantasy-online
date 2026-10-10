using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Official 1.5.8 BlobShadow, tokens 060000bf–060000c6.
public partial class BlobShadow : MonoBehaviour
{
    public const float WidthK = 0.75f;
    public const int RingPlayerPx = 87;
    public const int RingMobPx = 69;
    private const float Squash = 0.65f;
    private const int Under = -11;

    public SpriteRenderer src;
    public SpriteRenderer mountSrc;
    public float diameterU;
    public float mountDiameterU;
    public Vector3 worldAnchor;
    public bool hasWorldAnchor;
    private SpriteRenderer sr;

    private const float Alpha = 0.2f;
    private const int TEX = 64;
    private const float Core = 0.75f;
    private static Sprite _blob;

    public static bool Enabled => GameSettings.Shadow;

    public static float DiameterPx(int boundW, int floorPx) =>
        Mathf.Max(Mathf.Max((float)boundW, 0f) * WidthK, (float)floorPx);

    public static BlobShadow Attach(Transform owner, SpriteRenderer source, float diameter)
    {
        if (owner == null) return null;
        GameObject child = new GameObject("BlobShadow");
        child.transform.SetParent(owner, false);
        BlobShadow shadow = child.AddComponent<BlobShadow>();
        shadow.src = source;
        shadow.diameterU = diameter;
        return shadow;
    }

    public void SetDiameterPx(int boundW, float ppu, int floorPx)
    {
        if (boundW > 0 && ppu > 0f)
            diameterU = Mathf.Max((float)boundW * WidthK, (float)floorPx) / ppu;
    }

    private void Awake()
    {
        GameSettings.EnsureLoaded();
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = Blob();
        sr.enabled = false;
    }

    private void LateUpdate()
    {
        SpriteRenderer selectedSource;
        float selectedDiameter;
        if (mountSrc && mountSrc.enabled && mountSrc.sprite && mountDiameterU > 0f)
        {
            selectedSource = mountSrc;
            selectedDiameter = mountDiameterU;
        }
        else
        {
            selectedSource = src;
            selectedDiameter = diameterU;
        }

        if (!GameSettings.Shadow || !sr || !sr.sprite || selectedDiameter <= 0.0001f ||
            !selectedSource || !selectedSource.enabled || !selectedSource.sprite ||
            !selectedSource.gameObject.activeInHierarchy)
        {
            if (sr && sr.enabled) sr.enabled = false;
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
            selectedDiameter * Squash / dy, 1f);
        sr.sortingLayerID = selectedSource.sortingLayerID;
        int first = selectedSource.sortingOrder;
        int second = selectedSource.sortingOrder;
        int order = unchecked(first + Under);
        if (order < 0 && second >= 0) order = 0;
        sr.sortingOrder = order;
        Color sourceColor = selectedSource.color;
        sr.color = new Color(0f, 0f, 0f, sourceColor.a * Alpha);
        if (!sr.enabled) sr.enabled = true;
    }

    private static Sprite Blob()
    {
        if (_blob != null) return _blob;

        Texture2D texture = new Texture2D(TEX, TEX, TextureFormat.RGBA32, false);
        texture.name = "BlobShadowTex";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.HideAndDontSave;

        Color32[] pixels = new Color32[TEX * TEX];
        for (int y = 0; y < TEX; y++)
        {
            float normalizedY = (y - 31.5f) / 31.5f;
            float yy = normalizedY * normalizedY;
            for (int x = 0; x < TEX; x++)
            {
                float normalizedX = (x - 31.5f) / 31.5f;
                float distance = Mathf.Sqrt(yy + normalizedX * normalizedX);
                float core = Mathf.Clamp01((1f - distance) / Core);
                float smooth = core * core * (3f - (core + core));
                float alpha = smooth * 255f;
                pixels[y * TEX + x] = new Color32(
                    255, 255, 255, (byte)Math.Round((double)alpha));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        _blob = Sprite.Create(texture,
            new Rect(0f, 0f, TEX, TEX), new Vector2(0.5f, 0.5f), TEX);
        _blob.name = "BlobShadow";
        _blob.hideFlags = HideFlags.HideAndDontSave;
        return _blob;
    }
}

}
