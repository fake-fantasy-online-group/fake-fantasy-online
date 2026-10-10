using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class NpcChatter : MonoBehaviour
{
    [Serializable] private class Line { public string role; public string text; }
    [Serializable] private class ChatterData { public Line[] lines; }
    [Serializable] private class Greet { public int id; public string text; public string role; }
    [Serializable] private class GreetData { public Greet[] list; }
    private static bool loaded;
    private static Dictionary<string, List<string>> byRole;
    private static List<string> all;
    private static List<string> ovGreets;
    private static Dictionary<string, List<string>> ovByRole;
    private static Font font;
    private static GUIStyle box;

    public static Font CnFont() { EnsureData(); return font; }

    private static void EnsureData()
    {
        if (loaded) return;
        loaded = true;
        byRole = new Dictionary<string, List<string>>();
        all = new List<string>();
        string text = GameTables.LoadText("npc_chatter");
        if (text != null)
        {
            ChatterData data = JsonUtility.FromJson<ChatterData>(text);
            if (data != null && data.lines != null)
                foreach (Line line in data.lines)
                {
                    if (string.IsNullOrEmpty(line.text)) continue;
                    all.Add(line.text);
                    if (!byRole.TryGetValue(line.role, out List<string> list))
                    { list = new List<string>(); byRole[line.role] = list; }
                    list.Add(line.text);
                }
        }
        ovGreets = new List<string>();
        ovByRole = new Dictionary<string, List<string>>();
        text = GameTables.LoadText("npc_greet");
        if (text != null)
        {
            GreetData data = JsonUtility.FromJson<GreetData>(text);
            if (data != null && data.list != null)
                foreach (Greet row in data.list)
                {
                    if (string.IsNullOrEmpty(row.text)) continue;
                    ovGreets.Add(row.text);
                    if (string.IsNullOrEmpty(row.role)) continue;
                    if (!ovByRole.TryGetValue(row.role, out List<string> list))
                    { list = new List<string>(); ovByRole[row.role] = list; }
                    list.Add(row.text);
                }
        }
        string[] preferred = { "SimSun", "NSimSun", "新宋体", "宋体", "Songti SC", "STSong", "Songti", "PingFang SC", "Hiragino Sans GB", "Heiti SC", "Microsoft YaHei", "Arial Unicode MS" };
        string[] installed = Font.GetOSInstalledFontNames() ?? new string[0];
        var set = new HashSet<string>(installed);
        string chosen = null;
        foreach (string candidate in preferred)
            if (set.Contains(candidate)) { chosen = candidate; break; }
        if (chosen == null)
            foreach (string candidate in installed)
                if (candidate != null && (candidate.Contains("Song") || candidate.Contains("宋") || candidate.Contains("Ping") || candidate.Contains("Hei") || candidate.Contains("黑") || candidate.Contains("Hiragino") || candidate.Contains("CJK") || candidate.Contains("GB") || candidate.Contains("Han")))
                { chosen = candidate; break; }
        font = chosen != null ? Font.CreateDynamicFontFromOSFont(chosen, 16) : null;
    }

    private static Texture2D[] BubblePieces(string theme)
    {
        if (bubbleCache.TryGetValue(theme, out Texture2D[] pieces)) return pieces;
        pieces = new Texture2D[4];
        ResLoader loader = ResLoader.I;
        if (loader != null)
        {
            pieces[0] = loader.LoadGsa("res/npcicon/" + theme + "_left.gsa");
            pieces[1] = loader.LoadGsa("res/npcicon/" + theme + "_middle.gsa");
            pieces[2] = loader.LoadGsa("res/npcicon/" + theme + "_right.gsa");
            pieces[3] = loader.LoadGsa("res/npcicon/" + theme + "_zhiyin.gsa");
        }
        bubbleCache[theme] = pieces;
        return pieces;
    }

    public static void DrawBubble(float cx, float anchorY, string text, string theme)
    {
        if (string.IsNullOrEmpty(text)) return;
        Texture2D[] pieces = BubblePieces(theme);
        Texture2D left = pieces[0], middle = pieces[1], right = pieces[2], tail = pieces[3];
        if (left == null || middle == null || right == null) return;
        float s = UIScale.Factor;
        float lw = GsaDecoder.W(left), mw = GsaDecoder.W(middle), rw = GsaDecoder.W(right), h = GsaDecoder.H(left);
        float tw = tail != null ? GsaDecoder.W(tail) : 11f;
        float th = tail != null ? GsaDecoder.H(tail) : 15f;
        float textWidth = BitmapFont.Ready ? EmoteText.Measure(text, s) : text.Length * 7f * s;
        float midWidth = mw * s;
        float denominator = midWidth < 1f ? 1f : midWidth;
        int tiles = Mathf.Max(1, Mathf.CeilToInt(textWidth / denominator));
        float L = lw * s, R = rw * s, H = h * s, TH = th * s, TW = tw * s, padding = 5f * s;
        float W = L + midWidth * tiles + R;
        float x = UIScale.SnapX(cx - W * 0.5f);
        float y = UIScale.SnapY(anchorY - 2f - TH + padding - H);
        GUI.DrawTexture(new Rect(x, y, L, H), left);
        for (int i = 0; i < tiles; i++) GUI.DrawTexture(new Rect(x + L + midWidth * i, y, midWidth, H), middle);
        GUI.DrawTexture(new Rect(x + L + midWidth * tiles, y, R, H), right);
        if (tail != null) GUI.DrawTexture(new Rect(UIScale.SnapX(cx - TW * 0.5f), y + H - padding, TW, TH), tail);
        if (BitmapFont.Ready)
        {
            EmoteText.DrawColored(x + Mathf.Round((W - textWidth) * 0.5f), y + Mathf.Round((H - 12f * s) * 0.5f), text, Color.black, s, false);
            return;
        }
        if (box == null)
        {
            var style = new GUIStyle();
            style.font = font;
            style.fontSize = Mathf.RoundToInt(21.599998474121094f);
            style.alignment = TextAnchor.MiddleCenter;
            style.wordWrap = false;
            box = style;
            box.normal.textColor = Color.black;
        }
        GUI.Label(new Rect(x, y, W, H), text, box);
    }
}

}
