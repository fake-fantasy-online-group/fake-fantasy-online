using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class MonsterEntity
{
    public struct StatusIcon
    {
        public int status;
        public int icon;
        public bool good;
        public int remainMs;
    }

    private List<StatusIcon> sicons;
    private float[] siExpire;
    private int siSig;
    private GameObject siGo;
    private SpriteRenderer siSr;
    private static Material nameTagMat;

    public void SetStatusIcons(List<StatusIcon> list)
    {
        sicons = list;
        if (sicons != null && sicons.Count > 0)
        {
            siExpire = new float[sicons.Count];
            for (int i = 0; i < sicons.Count; i++)
                siExpire[i] = sicons[i].remainMs < 0 ? -1f
                    : Time.time + Mathf.Max(0.1f, sicons[i].remainMs / 1000f);
        }
        else siExpire = null;
        ApplyStatusIcons();
    }

    private void ApplyStatusIcons()
    {
        var visible = new List<StatusIcon>(8);
        if (sicons != null)
        {
            for (int i = 0; i < sicons.Count && visible.Count < 8; i++)
                if (siExpire == null || siExpire[i] < 0f || Time.time < siExpire[i])
                    visible.Add(sicons[i]);
        }
        int signature = 17;
        foreach (StatusIcon row in visible) signature = unchecked(signature * 31 + row.status);
        signature = visible.Count == 0 ? 0 : unchecked(signature * 31 + visible.Count);
        if (signature == siSig && ((siGo == null) == (visible.Count == 0))) return;
        siSig = signature;
        if (visible.Count == 0)
        {
            if (siGo) siGo.SetActive(false);
            return;
        }

        Texture2D texture = new Texture2D(visible.Count * 17 - 1, 16, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[texture.width * texture.height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(0, 0, 0, 0);
        for (int i = 0; i < visible.Count; i++)
        {
            int x0 = i * 17;
            Color32 border = visible[i].good ? new Color32(70, 210, 90, 255)
                : new Color32(225, 70, 70, 255);
            for (int n = 0; n < 16; n++)
            {
                pixels[x0 + n] = border;
                pixels[15 * texture.width + x0 + n] = border;
                pixels[n * texture.width + x0] = border;
                pixels[n * texture.width + x0 + 15] = border;
            }
            Texture2D icon = GameHud.BuffIcon(visible[i].icon);
            if (!icon) continue;
            for (int y = 0; y < 14; y++)
            {
                float v = (y + 0.5f) / 14f;
                for (int x = 0; x < 14; x++)
                {
                    Color color = icon.GetPixelBilinear((x + 0.5f) / 14f, v);
                    if (color.a <= 0.02f) continue;
                    pixels[(y + 1) * texture.width + x0 + x + 1] = (Color32)color;
                }
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        if (!siGo)
        {
            siGo = new GameObject("MonStatusIcons");
            siGo.transform.SetParent(transform, false);
            siSr = siGo.AddComponent<SpriteRenderer>();
            siSr.sortingOrder = 32000;
            if (!nameTagMat)
            {
                Shader shader = Shader.Find("Overlay/NameTag");
                if (shader) nameTagMat = new Material(shader);
            }
            if (nameTagMat) siSr.sharedMaterial = nameTagMat;
        }
        if (!siGo.activeSelf) siGo.SetActive(true);
        Sprite old = siSr.sprite;
        siSr.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 1f), ppu);
        if (old)
        {
            Texture2D previousTexture = old.texture;
            Object.Destroy(old);
            if (previousTexture) Object.Destroy(previousTexture);
        }
    }

    private GameObject nameTagGo;

    private void TickStatusIcons()
    {
        if (sicons == null || sicons.Count == 0) return;
        for (int i = 0; i < sicons.Count; i++)
        {
            if (siExpire[i] < 0f) continue;
            if (Time.time >= siExpire[i])
            {
                ApplyStatusIcons();
                return;
            }
        }
    }

    private void PlaceStatusIcons()
    {
        if (!siGo) return;
        if (!nameTagGo || !nameTagGo.activeSelf || dying)
        {
            if (siGo.activeSelf) siGo.SetActive(false);
            return;
        }
        if (siSig != 0 && !siGo.activeSelf) siGo.SetActive(true);
        if (siGo.activeSelf) PlayerNameTag.PlaceSharp(siGo.transform, headTopU + 0.15f, ppu);
    }

    internal void PlaceStatusIconsForIndependentRuntime() => PlaceStatusIcons();
    internal void ReleaseStatusIconsForIndependentRuntime()
    {
        if (siSr && siSr.sprite)
        {
            Texture2D texture = siSr.sprite.texture;
            Object.Destroy(siSr.sprite);
            if (texture) Object.Destroy(texture);
        }
    }

}

}
