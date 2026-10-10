using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static class EmoteText
{
    private static readonly Dictionary<int, Texture2D[]> cache = new Dictionary<int, Texture2D[]>();
    private static readonly Dictionary<(string, int), List<string>> wrapCache =
        new Dictionary<(string, int), List<string>>();

    public static List<string> Wrap(string text, float maxWidth, float s)
    {
        var key = (text ?? string.Empty, Mathf.RoundToInt(maxWidth / Mathf.Max(0.01f, s)));
        if (wrapCache.TryGetValue(key, out List<string> cached)) return cached;

        var rows = new List<string>();
        if (string.IsNullOrEmpty(text) || !BitmapFont.Ready)
        {
            rows.Add(text ?? string.Empty);
            return rows;
        }

        var line = new StringBuilder();
        float width = 0f;
        float emoteWidth = s * 17f;
        for (int i = 0; i < text.Length;)
        {
            bool emote = IsEmote(text, i, out int nn);
            int length = emote ? 4 : 1;
            float tokenWidth = emote ? emoteWidth : BitmapFont.Measure(text[i].ToString(), s);
            if (width + tokenWidth > maxWidth && line.Length > 0)
            {
                rows.Add(line.ToString());
                line.Clear();
                width = 0f;
            }
            line.Append(text, i, length);
            width = tokenWidth + width;
            i += length;
        }
        if (line.Length > 0 || rows.Count == 0) rows.Add(line.ToString());
        if (wrapCache.Count > 512) wrapCache.Clear();
        wrapCache[key] = rows;
        return rows;
    }

    private static Texture2D[] Frames(int nn)
    {
        if (cache.TryGetValue(nn, out Texture2D[] frames)) return frames;
        frames = null;
        if (ResLoader.I != null)
        {
            string path = "res/biaoqing/" + nn.ToString("D2") + ".gsa";
            int count = ResLoader.I.GsaFrameCount(path);
            if (count > 0)
            {
                frames = new Texture2D[count];
                for (int i = 0; i < count; i++) frames[i] = ResLoader.I.LoadGsaFrame(path, i);
            }
        }
        cache[nn] = frames;
        return frames;
    }

    private static bool IsEmote(string t, int i, out int nn)
    {
        nn = 0;
        if (unchecked(i + 3) >= t.Length || t[i] != '\\' || t[i + 1] != 'e' || !char.IsDigit(t[i + 2]) || !char.IsDigit(t[i + 3])) return false;
        nn = unchecked((t[i + 2] - '0') * 10 + t[i + 3] - '0');
        return unchecked((uint)(nn - 1)) < 96;
    }

    public static float Measure(string text, float s)
    {
        if (string.IsNullOrEmpty(text) || !BitmapFont.Ready) return 0f;
        float width = 0f;
        var sb = new StringBuilder();
        for (int i = 0; i < text.Length;)
        {
            if (IsEmote(text, i, out int nn))
            {
                if (sb.Length > 0) { width += BitmapFont.Measure(sb.ToString(), s); sb.Clear(); }
                width = 17f * s + width;
                i += 4;
            }
            else { sb.Append(text[i]); i++; }
        }
        if (sb.Length > 0) width += BitmapFont.Measure(sb.ToString(), s);
        return width;
    }

    public static void DrawColored(float x, float y, string text, Color col, float s, bool outline)
    {
        if (string.IsNullOrEmpty(text) || !BitmapFont.Ready) return;
        x = Mathf.Round(x); y = Mathf.Round(y);
        var sb = new StringBuilder();
        float emoteSize = 16f * s;
        void Flush()
        {
            if (sb.Length < 1) return;
            string run = sb.ToString();
            if (outline) BitmapFont.DrawOutline(x, y, run, col, s);
            else BitmapFont.Draw(x, y, run, col, s);
            x = x + BitmapFont.Measure(run, s);
            sb.Clear();
        }
        for (int i = 0; i < text.Length;)
        {
            if (IsEmote(text, i, out int nn))
            {
                Flush();
                Texture2D[] frames = Frames(nn);
                if (frames != null && frames.Length > 0)
                {
                    Texture2D frame = frames[unchecked((int)(Time.time / 0.2f)) % frames.Length];
                    if (ReferenceEquals(frame, null)) frame = frames[0];
                    if (frame != null) GUI.DrawTexture(new Rect(x, y + s * (-3f), emoteSize, emoteSize), frame);
                }
                x = x + (emoteSize + s);
                i += 4;
            }
            else { sb.Append(text[i]); i++; }
        }
        Flush();
    }

    public static void DrawOutline(float x, float y, string text, Color col, float s)
    {
        if (string.IsNullOrEmpty(text) || !BitmapFont.Ready) return;
        x = Mathf.Round(x); y = Mathf.Round(y);
        var sb = new StringBuilder();
        float emoteSize = 16f * s;
        void Flush()
        {
            if (sb.Length < 1) return;
            string run = sb.ToString();
            BitmapFont.DrawOutline(x, y, run, col, s);
            x = x + BitmapFont.Measure(run, s);
            sb.Clear();
        }
        for (int i = 0; i < text.Length;)
        {
            if (IsEmote(text, i, out int nn))
            {
                Flush();
                Texture2D[] frames = Frames(nn);
                if (frames != null && frames.Length > 0)
                {
                    Texture2D frame = frames[unchecked((int)(Time.time / 0.2f)) % frames.Length];
                    if (ReferenceEquals(frame, null)) frame = frames[0];
                    if (frame != null) GUI.DrawTexture(new Rect(x, y + s * (-3f), emoteSize, emoteSize), frame);
                }
                x = x + (emoteSize + s);
                i += 4;
            }
            else { sb.Append(text[i]); i++; }
        }
        Flush();
    }
}

}
