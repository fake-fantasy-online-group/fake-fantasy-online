using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{
    private Texture2D nameImg;
    private float nameShownAt;

    internal void MapObjectsPlacedForIndependentRuntime(Transform mapRoot, float worldW, float worldH, float ppu)
    {
        worldRoot = mapRoot;
        ShowMapNameImage();
        WeatherFX.Apply(mapName, worldRoot, worldW, worldH, ppu);
    }

    private void ShowMapNameImage()
    {
        nameImg = null;
        if (ResLoader.I == null)
            return;
        MapDef.EnsureLoaded(ResLoader.I.Pkg);
        if (int.TryParse(mapName.Length > 2 ? mapName.Substring(2) : mapName, out int id) &&
            MapDef.TryGet(id, out MapDef.Entry descriptor) && !string.IsNullOrEmpty(descriptor.name))
        {
            Texture2D texture = ResLoader.I.LoadGsa("res/map/" + descriptor.name + ".gsa");
            if (texture != null)
            {
                nameImg = texture;
                nameShownAt = Time.time;
            }
        }
    }

    private void OnGUI()
    {
        if (fadeAlpha > 0f)
        {
            if (fadeTex == null)
            {
                fadeTex = new Texture2D(1, 1);
                fadeTex.SetPixel(0, 0, Color.black);
                fadeTex.Apply();
            }
            Color oldColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, fadeAlpha);
            GUI.depth = -1000;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), fadeTex);
            GUI.color = oldColor;
        }
        if (nameImg == null)
            return;
        float elapsed = Time.time - nameShownAt;
        float alpha = elapsed < nameHoldSec
            ? 1f
            : Mathf.Clamp01(1f - (elapsed - nameHoldSec) / nameFadeSec);
        if (alpha <= 0f)
        {
            nameImg = null;
            return;
        }
        float scale = Mathf.Min(1f, Screen.width * .5f / nameImg.width);
        float width = scale * nameImg.width;
        float height = scale * nameImg.height;
        float x = (Screen.width - width) * .5f;
        float y = Screen.height * .12f;
        Color previousColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.depth = -900;
        GUI.DrawTexture(new Rect(x, y, width, height), nameImg);
        GUI.color = previousColor;
    }
}
}
