using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public class PetalOverlay : PrecipBase
{
    private static Texture2D[][] sprites;
    private const int FrameMs = 200;

    private static void LoadSprites()
    {
        if (sprites != null)
            return;
        sprites = new Texture2D[6][];
        ResLoader loader = ResLoader.I;
        for (int i = 0; i < 6; i++)
        {
            sprites[i] = new Texture2D[4];
            string path = string.Format("res/weather/DH_TY_{0:0000}_01_01.gsa", i + 3);
            for (int frame = 0; frame < 4; frame++)
                sprites[i][frame] = loader != null ? loader.LoadGsaFrame(path, frame) : null;
        }
    }

    private void Start()
    {
        LoadSprites();
        gravity = 200;
        spawnRate = 2;
        fallBase = 2000;
        restFade = 2000;
        spriteCount = 6;
        Alloc(100);
    }

    protected override void Draw(P p, float sx, float sy)
    {
        Texture2D[] frames = sprites[p.sprite];
        if (frames == null)
            return;
        Texture2D texture = frames[(p.age / FrameMs) & 3];
        if (texture == null)
            return;
        GUI.color = new Color(1f, 1f, 1f, AlphaOf(p));
        TexScale.Draw(sx, sy, texture, .5f, .5f, 1f);
        GUI.color = Color.white;
    }
}
}
