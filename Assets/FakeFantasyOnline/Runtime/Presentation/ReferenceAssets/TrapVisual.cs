using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public class TrapVisual : MonoBehaviour
{
    private Sprite[] frames;
    private SpriteRenderer sr;
    private float timer;
    private int idx;
    private const float Fps = 6f;

    public void Load(int model, float ppu, int worldY)
    {
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingOrder = Mathf.Clamp(worldY, 0, 30000);
        string path = string.Format("avatar/npc/npc{0}01.gsn", model);
        byte[] bytes = ResLoader.I?.Pkg?.Read(path);
        if (bytes == null) bytes = ResLoader.Read24Res(path);
        if (bytes == null) return;
        DimgDecoder.Anim anim = DimgDecoder.DecodeAnim(bytes);
        if (!anim.ok || anim.f == null) return;
        var sprites = new List<Sprite>();
        foreach (DimgDecoder.FrameTex frame in anim.f)
        {
            if (!frame.tex) continue;
            TexScale.Register(frame.tex, anim.scale);
            sprites.Add(TexScale.MakeSprite(frame.tex, ppu, 0.5f, 0.12f));
        }
        frames = sprites.ToArray();
        if (frames.Length > 0) sr.sprite = frames[0];
    }

    private void Update()
    {
        if (frames == null || frames.Length < 2 || !sr) return;
        timer += Time.deltaTime;
        if (timer >= 1f / Fps)
        {
            timer = 0f;
            idx = unchecked(idx + 1) % frames.Length;
            sr.sprite = frames[idx];
        }
    }

    private void OnDestroy()
    {
        if (frames == null) return;
        var textures = new HashSet<Texture2D>();
        foreach (Sprite sprite in frames)
        {
            if (!sprite) continue;
            Texture2D texture = sprite.texture;
            Destroy(sprite);
            if (texture && textures.Add(texture)) Destroy(texture);
        }
        frames = null;
    }
}

}
