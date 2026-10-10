using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class WeatherFX : MonoBehaviour
{
    public enum WType { None, Rain, Petal }

    private static readonly Dictionary<int, WType> byMap;
    private static GameObject root;
    public static float Ppu;
    private static string lastMap;
    private static Transform lastRoot;
    private static float lastW;
    private static float lastH;
    private static float lastPpu;
    private static Texture2D dropTex;

    static WeatherFX()
    {
        byMap = new Dictionary<int, WType>
        {
            { 19, WType.Rain },
            { 37, WType.Petal }
        };
        Ppu = 100f;
    }

    private static int ParseMap(string m)
    {
        if (string.IsNullOrEmpty(m) || m.Length < 3)
            return -1;
        return int.TryParse(m.Substring(2), out int id) ? id : -1;
    }

    public static Texture2D DropTex()
    {
        if (dropTex != null)
            return dropTex;

        dropTex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        for (int y = 0; y < 4; y++)
        {
            dropTex.SetPixel(0, y, new Color(1f, 1f, 1f, .25f));
            dropTex.SetPixel(1, y, new Color(1f, 1f, 1f, .75f));
            dropTex.SetPixel(2, y, new Color(1f, 1f, 1f, .75f));
            dropTex.SetPixel(3, y, new Color(1f, 1f, 1f, .25f));
        }
        dropTex.Apply();
        dropTex.wrapMode = TextureWrapMode.Clamp;
        dropTex.filterMode = FilterMode.Bilinear;
        return dropTex;
    }
}
}
