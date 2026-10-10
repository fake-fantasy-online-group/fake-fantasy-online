using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public class RainOverlay : PrecipBase
{
    private Texture2D tex;
    private readonly Color tint = new Color(.78f, .86f, 1f, .5f);

    private void Start()
    {
        tex = WeatherFX.DropTex();
        gravity = 900;
        spawnRate = 100;
        fallBase = 800;
        restFade = 0;
        Alloc(1000);
    }

    protected override void Draw(P p, float sx, float sy)
    {
        float height = Mathf.Clamp(p.factor * gravity * .014f, 8f, 22f);
        GUI.color = tint;
        GUI.DrawTexture(new Rect(sx, sy - height, 2f, height), tex);
        GUI.color = Color.white;
    }
}
}
