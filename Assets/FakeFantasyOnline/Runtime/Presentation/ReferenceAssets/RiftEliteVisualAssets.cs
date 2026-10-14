using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
/// <summary>Scene-owned native frames. No imported binaries, cache pin leaks or shared material edits.</summary>
internal sealed class RiftEliteVisualAssets : IDisposable
{
    internal sealed class Clip
    {
        internal Sprite[] Frames;
        internal float Width, Height, CenterX, CenterY;
        internal Vector2[] AlphaCenters, AlphaSizes;
        internal Vector2 AlphaCenter(int frame) => AlphaCenters[frame];
        internal Vector2 UV(int frame, float nx, float ny)
        {
            Sprite s = Frames[frame]; float factor = s.pixelsPerUnit / 100;
            Vector2 center = AlphaCenters[frame], size = AlphaSizes[frame];
            return new Vector2((s.pivot.x + (center.x + nx * size.x * .5f) * factor) / s.texture.width,
                (s.pivot.y + (center.y + ny * size.y * .5f) * factor) / s.texture.height);
        }
    }
    private readonly Dictionary<string, Clip> clips = new Dictionary<string, Clip>();
    internal Clip Magic(int id) => Load(string.Format("magic/magic/magic{0:D4}.gso", id));
    internal Clip Wall(int index) => Load("object/SK/SK_CLY_050" + (index + 1) + ".gso");
    private Clip Load(string path)
    {
        if (clips.TryGetValue(path, out Clip cached)) return cached;
        if (ResLoader.I == null) return null;
        byte[] bytes = ResLoader.I.ReadRes(path);
        if (bytes == null) return null; // A later resource mount may retry; no negative cache.
        DimgDecoder.Anim animation = DimgDecoder.DecodeAnim(bytes);
        if (!animation.ok || animation.f == null || animation.f.Length == 0) return null;
        var frames = new List<Sprite>(); var centers = new List<Vector2>(); var sizes = new List<Vector2>(); float largest = 0;
        var result = new Clip(); int scale = Math.Max(1, animation.scale);
        foreach (DimgDecoder.FrameTex frame in animation.f)
        {
            if (frame.tex == null) continue;
            var pivot = new Vector2((float)(animation.boundCx - frame.nLeft) * scale / frame.tex.width,
                1 - (float)(animation.boundCy - frame.nTop) * scale / frame.tex.height);
            Sprite sprite = Sprite.Create(frame.tex, new Rect(0, 0, frame.tex.width, frame.tex.height), pivot, scale * 100);
            frames.Add(sprite);
            Bounds(frame.tex.GetPixels32(), frame.tex.width, frame.tex.height, out int l, out int b, out int r, out int t);
            centers.Add(new Vector2(((l + r) * .5f - sprite.pivot.x) / scale, ((b + t) * .5f - sprite.pivot.y) / scale));
            sizes.Add(new Vector2((r - l) / (float)scale, (t - b) / (float)scale));
            frame.tex.wrapMode = TextureWrapMode.Clamp;
            float width = (r - l) / (float)scale;
            if (width > largest)
            {
                largest = width; result.Width = width; result.Height = (t - b) / (float)scale;
                result.CenterX = ((l + r) * .5f - sprite.pivot.x) / scale;
                result.CenterY = ((b + t) * .5f - sprite.pivot.y) / scale;
            }
        }
        if (frames.Count == 0) return null;
        result.Frames = frames.ToArray(); result.AlphaCenters = centers.ToArray(); result.AlphaSizes = sizes.ToArray(); clips.Add(path, result); return result;
    }
    private static void Bounds(Color32[] pixels, int width, int height, out int left, out int bottom, out int right, out int top)
    {
        left = width; bottom = height; right = top = 0;
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            if (pixels[y * width + x].a != 0)
            { left = Math.Min(left, x); bottom = Math.Min(bottom, y); right = Math.Max(right, x + 1); top = Math.Max(top, y + 1); }
        if (right == 0 || top == 0) { left = bottom = 0; right = top = 1; }
    }
    internal static Material TintMaterial(Color tint, bool additive)
    {
        var result = new Material(additive ? EffectPlayer.BorrowAdditiveMaterial() : EffectPlayer.BorrowAlphaMaterial());
        // Existing native shader's opt-in luminance tint. The borrowed material stays untouched.
        result.SetFloat("_ApprovedGlowB", 1); result.SetFloat("_TintMode", 1); result.SetColor("_Tint", tint);
        return result;
    }
    public void Dispose()
    {
        foreach (Clip clip in clips.Values) foreach (Sprite sprite in clip.Frames)
        { if (sprite != null) { UnityEngine.Object.Destroy(sprite.texture); UnityEngine.Object.Destroy(sprite); } }
        clips.Clear();
    }
}
}
