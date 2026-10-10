using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// ZoneOverlay initially restored from 1.5.8 (tokens 060010a2-060010b0). Draws the poison ring
// of a custom map: four stretched "ZoneFill" sprites cover everything outside the
// safe circle, a radial "ZoneMask" sprite paints the inner gradient and a
// "ZoneNextRing" band previews the next phase circle. Every threshold, tint,
// sort order, rectangle and scale divisor follows the frozen dual-architecture
// evidence. Bounded 1.6.0 direct-owner corrections use tokens 060010ae-060010bc;
// historical citations below retain their original version:
// Build/ReferenceReverseGhidra-1.5.8/evidence/zoneoverlay-v1/zoneoverlay-evidence.json
public class ZoneOverlay : MonoBehaviour
{
    public static ZoneOverlay I;

    public static bool active;

    public static float cx;

    public static float cy;

    public static float r;

    public static float nextCx;

    public static float nextCy;

    public static float nextR;

    public static byte phase;

    public static int phaseSec;

    public static int ring;

    public static int rings;

    public static int alive;

    public static int dmgPerSec;

    public static bool outside;

    public static bool canSurrender;

    public static int cardsLeft;

    public static int deaths;

    public static string blockWhy = "";

    public static int faShenPx;

    public static float recvTime;

    public static int recvMapId;

    private const float StaleSec = 6f;

    private const int SortOrder = 32500;

    private SpriteRenderer maskSr;

    private SpriteRenderer nextSr;

    private SpriteRenderer[] fill = new SpriteRenderer[4];

    private static Texture2D _maskTex;

    private static Texture2D _ringTex;

    private static Texture2D _dotTex;

    // Baked from .cctor: DAT_0111d4d0..ec of the x86_64 slice.
    private static readonly Color PoisonTint = new Color(0.42f, 0.1f, 0.5f, 0.42f);

    private static readonly Color NextTint = new Color(0.85f, 0.95f, 1f, 0.5f);

    // 060010a2: stale packets and map switches hide the overlay.
    public static bool Showing
    {
        get
        {
            if (active && Time.realtimeSinceStartup - recvTime < StaleSec
                && recvMapId == GameBootstrap.CurMapId)
            {
                return CustomMaps.IsCustom(GameBootstrap.CurMapId);
            }
            return false;
        }
    }

    // 060010a3: negative phaseSec means "no countdown"; elapsed is floored.
    public static int PhaseSecNow
    {
        get
        {
            if (phaseSec < 0)
            {
                return -1;
            }
            int elapsed = Mathf.FloorToInt(Time.realtimeSinceStartup - recvTime);
            return Mathf.Max(0, phaseSec - elapsed);
        }
    }

    // 060010a4: server snapshot of the zone; creates the host object lazily.
    public static void Apply(bool act, int ccx, int ccy, int rr, int ncx, int ncy, int nr,
        byte ph, int psec, int rg, int rgs, int alv, bool outs, int dmg,
        bool canSurr = false, int cards = 0, int dea = 0, string why = "", int fashen = 0)
    {
        canSurrender = canSurr;
        cardsLeft = cards;
        deaths = dea;
        blockWhy = why ?? "";
        faShenPx = fashen;
        active = act;
        cx = ccx;
        cy = ccy;
        r = rr;
        nextCx = ncx;
        nextCy = ncy;
        nextR = nr;
        phase = ph;
        phaseSec = psec;
        ring = rg;
        rings = rgs;
        alive = alv;
        outside = outs;
        dmgPerSec = dmg;
        recvTime = Time.realtimeSinceStartup;
        recvMapId = GameBootstrap.CurMapId;
        if (I == null && act)
        {
            GameObject host = new GameObject("ZoneOverlay");
            host.AddComponent<ZoneOverlay>();
        }
    }

    // 060010a5
    public static void Clear()
    {
        active = false;
    }

    // 060010a6: 1x1 white texture used as the stretchable fill source.
    public static Texture2D Dot
    {
        get
        {
            if (_dotTex != null)
            {
                return _dotTex;
            }
            _dotTex = new Texture2D(1, 1, TextureFormat.RGBA32, false, false);
            _dotTex.SetPixel(0, 0, Color.white);
            _dotTex.Apply();
            return _dotTex;
        }
    }

    // 060010a7
    private void Awake()
    {
        I = this;
        maskSr = MakeSprite("ZoneMask", MaskTex(), PoisonTint, SortOrder);
        nextSr = MakeSprite("ZoneNextRing", RingTex(), NextTint, SortOrder + 1);
        for (int i = 0; i < 4; i++)
        {
            fill[i] = MakeSprite("ZoneFill" + i.ToString(), Dot, PoisonTint, SortOrder);
        }
    }

    // 060010a8: child GameObject carrying one tinted, 100 PPU sprite.
    private SpriteRenderer MakeSprite(string name, Texture2D tex, Color tint, int order)
    {
        GameObject child = new GameObject(name);
        child.transform.parent = transform;
        SpriteRenderer sr = child.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), 100f);
        sr.color = tint;
        sr.sortingOrder = order;
        return sr;
    }

    // 060010a9: radial alpha mask - clear inside 0.965, ramp to 1.0, solid to
    // 1.03, alpha 200 outside.
    private static Texture2D MaskTex()
    {
        if (_maskTex != null)
        {
            return _maskTex;
        }
        Color32[] cols = new Color32[65536];
        for (int y = 0; y < 256; y++)
        {
            float dy = y - 127.5f;
            for (int x = 0; x < 256; x++)
            {
                float dx = x - 127.5f;
                float d = Mathf.Sqrt(dy * dy + dx * dx) / 127.5f;
                byte a;
                if (d < 0.965f)
                {
                    a = 0;
                }
                else if (d < 1f)
                {
                    float t = Mathf.Clamp01((d - 0.965f) / (1f - 0.965f));
                    a = (byte)(t * 255f);
                }
                else if (d < 1.03f)
                {
                    a = 255;
                }
                else
                {
                    a = 200;
                }
                cols[y * 256 + x] = new Color32(255, 255, 255, a);
            }
        }
        Texture2D texture = new Texture2D(256, 256, TextureFormat.RGBA32, false, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        _maskTex = texture;
        _maskTex.SetPixels32(cols, 0);
        _maskTex.Apply(false, false);
        return _maskTex;
    }

    // 060010aa: hollow band between 0.955 and 1.0 of the circle radius.
    private static Texture2D RingTex()
    {
        if (_ringTex != null)
        {
            return _ringTex;
        }
        Color32[] cols = new Color32[65536];
        for (int y = 0; y < 256; y++)
        {
            float dy = y - 127.5f;
            for (int x = 0; x < 256; x++)
            {
                float dx = x - 127.5f;
                float d = Mathf.Sqrt(dy * dy + dx * dx) / 127.5f;
                cols[y * 256 + x] = (d > 0.955f && d <= 1f)
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(255, 255, 255, 0);
            }
        }
        Texture2D texture = new Texture2D(256, 256, TextureFormat.RGBA32, false, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        _ringTex = texture;
        _ringTex.SetPixels32(cols, 0);
        _ringTex.Apply(false, false);
        return _ringTex;
    }

    // 060010ab: world pixels to Unity world units.
    private static Vector3 W2U(float wx, float wy)
    {
        return new Vector3(
            (wx - GameBootstrap.CurWorldW * 0.5f) / GameBootstrap.CurPpu,
            (GameBootstrap.CurWorldH * 0.5f - wy) / GameBootstrap.CurPpu, 0f);
    }

    // 060010ac
    private void LateUpdate()
    {
        bool show = Showing && GameBootstrap.CurPpu > 0f && GameBootstrap.CurWorldW > 0f;
        if (maskSr != null)
        {
            maskSr.enabled = show;
        }
        if (nextSr != null)
        {
            nextSr.enabled = show && nextR > 1f && Mathf.Abs(nextR - r) > 1f;
        }
        for (int i = 0; i < 4; i++)
        {
            if (fill[i] != null)
            {
                fill[i].enabled = show;
            }
        }
        if (!show)
        {
            return;
        }
        float ppu = GameBootstrap.CurPpu;
        float worldW = GameBootstrap.CurWorldW;
        float worldH = GameBootstrap.CurWorldH;
        float radius = r;
        Bounds maskBounds = maskSr.sprite.bounds;
        maskSr.transform.position = W2U(cx, cy);
        float maskSpan = (radius + radius) / ppu;
        maskSr.transform.localScale = new Vector3(
            maskSpan / maskBounds.size.x, maskSpan / maskBounds.size.y, 1f);
        if (nextSr.enabled)
        {
            float nextRadius = nextR;
            Bounds nextBounds = nextSr.sprite.bounds;
            nextSr.transform.position = W2U(nextCx, nextCy);
            float nextSpan = (nextRadius + nextRadius) / ppu;
            nextSr.transform.localScale = new Vector3(
                nextSpan / nextBounds.size.x, nextSpan / nextBounds.size.y, 1f);
        }
        // Four rectangles: everything outside the safe circle's bounding box,
        // extended by 2000px so the edges never show a gap.
        float centerX = cx;
        float centerY = cy;
        radius = r;
        SetFill(fill[0], -2000f, -2000f, worldW + 2000f, centerY - radius);
        SetFill(fill[1], -2000f, centerY + radius, worldW + 2000f, worldH + 2000f);
        SetFill(fill[2], -2000f, centerY - radius, centerX - radius, centerY + radius);
        SetFill(fill[3], centerX + radius, centerY - radius, worldW + 2000f, centerY + radius);
    }

    // 060010ad: stretches one dot sprite over a world-pixel rectangle.
    private void SetFill(SpriteRenderer sr, float wx0, float wy0, float wx1, float wy1)
    {
        if (sr == null)
        {
            return;
        }
        float dx = wx1 - wx0;
        float dy = wy1 - wy0;
        bool on = !(dx <= 0.5f || dy <= 0.5f);
        if (!on)
        {
            sr.enabled = false;
            return;
        }
        sr.enabled = true;
        Bounds b = sr.sprite.bounds;
        sr.transform.position = W2U(wx0 + dx * 0.5f, wy0 + dy * 0.5f);
        sr.transform.localScale = new Vector3(
            dx / GameBootstrap.CurPpu / b.size.x,
            dy / GameBootstrap.CurPpu / b.size.y, 1f);
    }

    // 060010ae
    internal static void CirclePoints(float ccx, float ccy, float rr, int n, Vector2[] outp)
    {
        if (n <= 0)
        {
            return;
        }
        for (int i = 0; i < n && i < outp.Length; i++)
        {
            float ang = i * Mathf.PI * 2f / n;
            outp[i] = new Vector2(ccx + Mathf.Cos(ang) * rr, ccy + Mathf.Sin(ang) * rr);
        }
    }
}
}
