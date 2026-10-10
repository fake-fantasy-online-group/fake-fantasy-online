using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// HornArt retains the restored animation loader/draw machinery. 1.6.0 adds the
// Inner geometry query (060004ff) and the authored text bounds in each skin.
public static class HornArt
{
    public struct Layer
    {
        public int gso;
        public bool additive;
    }

    public struct Skin
    {
        public string name;
        public int intervalMs;
        public int ax;
        public int ay;
        public int tx;
        public int ty;
        public int tw;
        public int th;
        public Layer[] layers;
    }

    public static readonly Skin[] Skins;
    private static readonly Dictionary<int, DimgDecoder.Anim> _anims;

    private static Layer A(int g) { return new Layer { gso = g, additive = false }; }
    private static Layer G(int g) { return new Layer { gso = g, additive = true }; }

    // Official 1.6.0 060004ff. Invalid/unconfigured skins leave every output at zero.
    public static bool Inner(int skin, out int tx, out int ty, out int tw, out int th)
    {
        tx = ty = tw = th = 0;
        if (skin < 1 || skin >= Skins.Length) return false;
        Skin selected = Skins[skin];
        if (selected.tw < 1 || selected.th < 1) return false;
        tx = selected.tx;
        ty = selected.ty;
        tw = selected.tw;
        th = selected.th;
        return true;
    }

    private static DimgDecoder.Anim Load(int gso)
    {
        if (_anims.TryGetValue(gso, out DimgDecoder.Anim anim)) return anim;
        anim = default;
        ResLoader loader = ResLoader.I;
        if (loader != null)
        {
            try
            {
                byte[] data = loader.ReadRes(string.Format("magic/magic/magic{0:D4}.gso", gso));
                if (data != null) anim = DimgDecoder.DecodeAnim(data);
            }
            catch
            {
                anim = default;
            }
        }
        _anims[gso] = anim;
        return anim;
    }

    public static void Draw(int skin, Rect banner, float s, float elapsedMs)
    {
        if (skin <= 0 || skin >= Skins.Length) return;
        if (Event.current.type != EventType.Repaint) return;
        Skin selected = Skins[skin];
        int tick = (int)(elapsedMs / Mathf.Max(1, selected.intervalMs));
        foreach (Layer layer in selected.layers)
        {
            DimgDecoder.Anim anim = Load(layer.gso);
            if (!anim.ok || anim.f == null || anim.f.Length == 0) continue;
            int frames = anim.frames / Mathf.Max(1, anim.dirs);
            int count = frames > 0 ? Mathf.Min(frames, anim.f.Length) : anim.f.Length;
            DimgDecoder.FrameTex frame = anim.f[tick % count];
            if (frame.tex == null) continue;
            int scale = Mathf.Max(1, anim.scale);
            Rect rect = new Rect(
                (banner.x + selected.ax * s) - unchecked(anim.boundCx - frame.nLeft) * s,
                (banner.y + selected.ay * s) - unchecked(anim.boundCy - frame.nTop) * s,
                s * ((float)frame.tex.width / scale),
                s * ((float)frame.tex.height / scale));
            if (layer.additive)
                Graphics.DrawTexture(rect, frame.tex, EffectPlayer.BorrowAdditiveMaterial());
            else
                GUI.DrawTexture(rect, frame.tex);
        }
    }

    // Official 1.6.0 06000502: populate all twelve slots before publishing Skins,
    // then allocate the animation cache. Slot zero remains the default Skin.
    static HornArt()
    {
        var skins = new Skin[12];
        skins[1] = new Skin { name = "黄金龙", intervalMs = 100, ax = 216, ay = 65, tx = 79, ty = 6, tw = 268, th = 64, layers = new[] { A(7349), G(7348) } };
        skins[2] = new Skin { name = "黄金凤", intervalMs = 100, ax = 199, ay = 76, tx = 40, ty = 13, tw = 315, th = 62, layers = new[] { A(8040), G(8039) } };
        skins[3] = new Skin { name = "决斗之王", intervalMs = 100, ax = 208, ay = 65, tx = 31, ty = 11, tw = 248, th = 64, layers = new[] { A(7275) } };
        skins[4] = new Skin { name = "恶魔王子", intervalMs = 120, ax = 199, ay = 76, tx = 34, ty = 25, tw = 370, th = 50, layers = new[] { A(7479), G(7478) } };
        skins[5] = new Skin { name = "精灵仙子", intervalMs = 120, ax = 204, ay = 76, tx = 24, ty = 24, tw = 374, th = 64, layers = new[] { A(7481), G(7480) } };
        skins[6] = new Skin { name = "财源广进", intervalMs = 100, ax = 210, ay = 76, tx = 35, ty = 25, tw = 289, th = 50, layers = new[] { A(7651) } };
        skins[7] = new Skin { name = "年年有余", intervalMs = 100, ax = 218, ay = 96, tx = 80, ty = 27, tw = 310, th = 56, layers = new[] { A(7653), G(7652) } };
        skins[8] = new Skin { name = "张灯结彩", intervalMs = 100, ax = 218, ay = 88, tx = 34, ty = 25, tw = 369, th = 50, layers = new[] { A(7657), G(7656) } };
        skins[9] = new Skin { name = "招财进宝", intervalMs = 100, ax = 218, ay = 66, tx = 47, ty = 26, tw = 353, th = 49, layers = new[] { A(7655), G(7654) } };
        skins[10] = new Skin { name = "白富美", intervalMs = 100, ax = 218, ay = 66, tx = 53, ty = 0, tw = 327, th = 60, layers = new[] { A(7753), G(7754) } };
        skins[11] = new Skin { name = "高富帅", intervalMs = 100, ax = 218, ay = 66, tx = 30, ty = 0, tw = 372, th = 56, layers = new[] { A(7752), G(7754) } };
        Skins = skins;
        _anims = new Dictionary<int, DimgDecoder.Anim>();
    }
}

}
