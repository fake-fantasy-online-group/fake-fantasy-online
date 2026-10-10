using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static class HeadHpBar
{
    public const float SPPU = 100f;
    public const float BaseOffPlayer = 29f;
    public const float BaseOffMonster = 17f;
    private const string ResPath = "res/effect/hpbar.gsa";

    private static bool _show;
    private static bool initDone;
    private static Texture2D[] tex;
    private static bool tried;
    private static Sprite groove;
    private static Sprite[][] fills;
    private static Material mat;
    private static int hdScale = 1;

    public static bool Show
    {
        get { EnsureInit(); return _show; }
        set { EnsureInit(); _show = value; }
    }

    private static void EnsureInit()
    {
        if (initDone) return;
        initDone = true;
        GameSettings.EnsureLoaded();
        _show = GameSettings.HeadHpBarOn;
    }

    public static int W { get; private set; }
    public static int H { get; private set; }

    public static void Toggle()
    {
        Show = !Show;
        string message = Show
            ? "头顶血条: 开 (Insert 关闭)"
            : "头顶血条: 关 (Insert 开启; 也可在 O 系统设置里勾选保存)";
        ChatUI.Add("系统", message, new Color(150f / 255f, 215f / 255f, 1f, 1f));
    }

    public static bool Ready
    {
        get { Ensure(); return tex != null; }
    }

    private static void Ensure()
    {
        if (tried) return;
        ResLoader loader = ResLoader.I;
        if (!loader) return;
        tried = true;
        Texture2D[] frames = new Texture2D[4];
        frames[0] = loader.LoadGsaFrameA(ResPath, 0);
        if (!frames[0]) return;
        frames[1] = loader.LoadGsaFrameA(ResPath, 1);
        if (!frames[1]) return;
        frames[2] = loader.LoadGsaFrameA(ResPath, 2);
        if (!frames[2]) return;
        frames[3] = loader.LoadGsaFrameA(ResPath, 3);
        if (!frames[3]) return;

        tex = frames;
        hdScale = TexScale.Of(frames[0]);
        W = frames[0].width / hdScale;
        H = frames[0].height / hdScale;
        groove = Sprite.Create(frames[0], new Rect(0f, 0f, frames[0].width, frames[0].height),
            Vector2.zero, SPPU * hdScale);
        fills = new Sprite[3][];
        fills[0] = new Sprite[W + 1];
        fills[1] = new Sprite[W + 1];
        fills[2] = new Sprite[W + 1];
    }

    public static int Pct(int hp, int maxHp)
    {
        if (maxHp <= 0) return 0;
        return (int)((long)Mathf.Clamp(hp, 0, maxHp) * 100L / maxHp);
    }

    public static int ColorIdx(int pct) => Mathf.Min(pct / 33, 2);
    public static int FillW(int pct) => Mathf.Clamp(unchecked(W * pct) / 100, 0, W);

    public static Sprite Groove()
    {
        Ensure();
        return groove;
    }

    public static Sprite Fill(int idx, int width)
    {
        Ensure();
        if (width <= 0 || tex == null) return null;
        idx = Mathf.Clamp(idx, 0, 2);
        width = Mathf.Min(width, W);
        Sprite[] row = fills[idx];
        if (!row[width])
        {
            row[width] = Sprite.Create(tex[idx + 1],
                new Rect(0f, 0f, unchecked(width * hdScale), unchecked(H * hdScale)),
                Vector2.zero, SPPU * hdScale);
        }
        return row[width];
    }

    public static Material Mat()
    {
        if (!mat)
        {
            Shader shader = Shader.Find("Overlay/NameTag");
            if (shader) mat = new Material(shader);
        }
        return mat;
    }

    internal static int TextureScaleForIndependentRuntime => hdScale;
}

}
