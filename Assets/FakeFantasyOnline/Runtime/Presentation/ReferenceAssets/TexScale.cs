using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static class TexScale
{
    private static Dictionary<Texture2D, int> _of = new Dictionary<Texture2D, int>();
    private const int PurgeFloor = 512;
    private static int _purgeAt = PurgeFloor;

    public static void Register(Texture2D t, int scale)
    {
        if (t == null || scale < 2) return;
        if (_of.Count >= _purgeAt)
        {
            Purge();
            _purgeAt = System.Math.Max(PurgeFloor, _of.Count * 2);
        }
        _of[t] = scale;
    }

    private static void Purge()
    {
        Dictionary<Texture2D, int> live = new Dictionary<Texture2D, int>(_of.Count);
        foreach (KeyValuePair<Texture2D, int> pair in _of)
        {
            if (pair.Key != null) live[pair.Key] = pair.Value;
        }
        _of = live;
    }

    public static int Of(Texture2D t)
    {
        if (t == null) return 1;
        int scale;
        return _of.TryGetValue(t, out scale) ? scale : 1;
    }

    public static float W(Texture2D t) => t != null ? (float)t.width / Of(t) : 0f;
    public static float H(Texture2D t) => t != null ? (float)t.height / Of(t) : 0f;

    public static void Draw(float x, float y, Texture2D t,
        float pivotX = 0f, float pivotY = 0f, float extraScale = 1f)
    {
        if (t == null) return;
        float w = W(t) * extraScale;
        float h = H(t) * extraScale;
        GUI.DrawTexture(new Rect(x - w * pivotX, y - h * pivotY, w, h), t);
    }

    public static Sprite MakeSprite(Texture2D t, float basePpu, float pivotX = 0.5f, float pivotY = 0.5f)
    {
        if (t == null) return null;
        return Sprite.Create(t, new Rect(0f, 0f, t.width, t.height),
            new Vector2(pivotX, pivotY), basePpu * Of(t));
    }
}

}
