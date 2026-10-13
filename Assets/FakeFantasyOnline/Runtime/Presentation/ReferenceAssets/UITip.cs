using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal static partial class UITip
{
    private static Texture2D pUL, pUM, pUR, pLM, pRM, pDL, pDM, pDR;
    private static bool tried;
    private static string reqText, reqCmp;
    private static float reqS;
    private static Vector2 reqMouse;
    private static UITipRenderer renderer;

    public static void Show(string text, float s)
    {
        if (string.IsNullOrEmpty(text)) return;
        reqText = text;
        reqCmp = null;
        reqIsSkill = false;
        reqS = s;
        reqMouse = Event.current != null ? Event.current.mousePosition :
            new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        if (renderer == null) renderer = new GameObject("UITipRenderer").AddComponent<UITipRenderer>();
    }

    public static void ShowCompare(string text, string cmpText, float s)
    {
        Show(text, s);
        if (reqText == text) reqCmp = cmpText;
    }

    // Decision-free bridge for hosted requesters. All requests use the original static
    // fields and the same automatic UITipRenderer; explicit-position callers only supply
    // the pointer already captured by their existing host API.
    internal static string RequestedTextForIndependentRuntime => reqText;
    internal static string RequestedComparisonForIndependentRuntime => reqCmp;
    internal static float RequestedScaleForIndependentRuntime => reqS;
    internal static Vector2 RequestedMouseForIndependentRuntime => reqMouse;
    internal static void ShowAtForIndependentRuntime(string text, float scale, Vector2 mouse)
    {
        Show(text, scale);
        if (reqText == text) reqMouse = mouse;
    }
    internal static void ShowCompareAtForIndependentRuntime(string text, string comparison, float scale, Vector2 mouse)
    {
        ShowCompare(text, comparison, scale);
        if (reqText == text) reqMouse = mouse;
    }

    private sealed class UITipRenderer : MonoBehaviour
    {
        private void OnGUI()
        {
            GUI.depth = 1;
            if (reqText != null)
            {
                if (reqIsSkill) RenderSkill(reqText, reqS, reqMouse);
                else
                {
                    ResetSkillScroll();
                    Render(reqText, reqCmp, reqS, reqMouse);
                }
                if (Event.current.type == EventType.Repaint)
                {
                    reqText = null;
                    reqCmp = null;
                    reqIsSkill = false;
                }
            }
            else if (Event.current.type == EventType.Repaint) ResetSkillScroll();
        }
    }

    private static void Ensure()
    {
        if (pUL != null || tried) return;
        ResLoader rl = ResLoader.I;
        if (rl == null) return;
        tried = true;
        pUL = rl.LoadUI("ui/equipdlg05/pul.png");
        pUM = rl.LoadUI("ui/equipdlg05/pum.png");
        pUR = rl.LoadUI("ui/equipdlg05/pur.png");
        pLM = rl.LoadUI("ui/equipdlg05/plm.png");
        pRM = rl.LoadUI("ui/equipdlg05/prm.png");
        pDL = rl.LoadUI("ui/equipdlg05/pdl.png");
        pDM = rl.LoadUI("ui/equipdlg05/pdm.png");
        pDR = rl.LoadUI("ui/equipdlg05/pdr.png");
        if (pUL == null) tried = false;
    }

    private static void Render(string text, string cmp, float s, Vector2 mp)
    {
        if (!BitmapFont.Ready) return;
        Ensure();
        var main = Layout(text, s);
        float x = mp.x + 16f * s;
        float y = mp.y + 16f * s;
        if (x + main.Item2 > Screen.width) x = mp.x - main.Item2 - 4f * s;
        if (y + main.Item3 > Screen.height) y = mp.y - main.Item3 - 4f * s;
        x = Mathf.Max(0f, x);
        y = Mathf.Max(0f, y);
        DrawBox(new Rect(x, y, main.Item2, main.Item3), main.Item1, s);
        if (!string.IsNullOrEmpty(cmp))
        {
            var other = Layout(cmp, s);
            float cx = x - other.Item2 - (s + s);
            if (cx < 0f) cx = x + main.Item2 + (s + s);
            if (cx + other.Item2 > Screen.width) cx = Mathf.Max(0f, Screen.width - other.Item2);
            float cy = y;
            if (cy + other.Item3 > Screen.height) cy = Mathf.Max(0f, Screen.height - other.Item3);
            DrawBox(new Rect(cx, cy, other.Item2, other.Item3), other.Item1, s);
        }
    }

    private static (List<(string, Color, bool)>, float, float) Layout(string text, float s)
    {
        string[] lines = text.Replace("\\n", "\n").Split('\n');
        var rows = new List<(string, Color, bool)>();
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            bool center = line.Length > 0 && line[0] == '\u0007';
            if (center) line = line.Substring(1);
            Color color;
            if (line.Length >= 2 && line[0] == '\u0003')
            {
                color = QualityColor(line[1] - '0');
                line = line.Substring(2);
            }
            else if (line.Length > 0 && line[0] == '\u0001')
            { color = new Color32(120, 220, 120, 255); line = line.Substring(1); }
            else if (line.Length > 0 && line[0] == '\u0002')
            { color = new Color32(128, 128, 128, 255); line = line.Substring(1); }
            else if (line.Length > 0 && line[0] == '\u0004')
            { color = new Color32(255, 80, 80, 255); line = line.Substring(1); }
            else if (line.Length > 0 && line[0] == '\u0005')
            { color = new Color32(86, 196, 255, 255); line = line.Substring(1); }
            else if (line.Length > 0 && line[0] == '\u0006')
            { color = new Color32(255, 136, 48, 255); line = line.Substring(1); }
            else color = LineColor(line, i);
            foreach (string wrapped in WrapByWidth(line, 150f * s, s))
            {
                rows.Add((wrapped, color, center));
            }
        }
        float width = 0f;
        foreach (var row in rows) width = Mathf.Max(width, BitmapFont.Measure(row.Item1, s));
        float pad = s * 6f;
        return (rows, pad + pad + width, pad + pad + (s * 16f) * rows.Count + s * -4f);
    }

    private static List<string> WrapByWidth(string line, float maxW, float s)
    {
        var lines = new List<string>();
        if (string.IsNullOrEmpty(line)) { lines.Add(""); return lines; }
        if (maxW <= 0f || BitmapFont.Measure(line, s) <= maxW) { lines.Add(line); return lines; }
        var current = new StringBuilder();
        foreach (char c in line)
        {
            if (current.Length > 0 && BitmapFont.Measure(current.ToString() + c, s) > maxW)
            { lines.Add(current.ToString()); current.Clear(); }
            current.Append(c);
        }
        if (current.Length > 0) lines.Add(current.ToString());
        return lines;
    }

    public static Color32 QualityColor(int q)
    {
        switch (q)
        {
            case 1: return new Color32(64, 224, 96, 255);
            case 2: return new Color32(82, 150, 252, 255);
            case 3: return new Color32(210, 90, 255, 255);
            case 5: return new Color32(183, 155, 98, 255); // Endgame dark gold; ordinary rare quality stays yellow.
            case 4: return GameSettings.ColorWeak ? new Color32(255, 180, 40, 255) : new Color32(238, 222, 70, 255);
            default: return new Color32(247, 245, 245, 255);
        }
    }

    private static Color LineColor(string line, int idx)
    {
        if (idx == 0) return new Color32(255, 220, 120, 255);
        if (line.StartsWith("套装")) return new Color32(96, 160, 255, 255);
        if (line.Contains(" +") || line.Contains(" -")) return new Color32(96, 230, 230, 255);
        return Color.white;
    }

    private static void DrawBox(Rect r, List<(string, Color, bool)> rows, float s)
    {
        Color old = GUI.color;
        GUI.color = new Color32(16, 16, 16, 196);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
        if (pUL != null)
        {
            float edge = 3f * s;
            // Official 1.5.8 06000fc5: the local function the compiler emits for the four-argument
            // Rect helper declared inside DrawBox. Every texture-coordinate rect below is a call to
            // it, exactly as the native body does (the helper is inlined on both architectures, so
            // DrawBox carries no surviving call instruction for it).
            Rect TC(float u0, float v0, float du, float dv) => new Rect(u0, v0, du, dv);
            GUI.DrawTextureWithTexCoords(new Rect(r.x, r.y, edge, edge), pUL, TC(0f, 8f / 11f, 3f / 11f, 3f / 11f));
            GUI.DrawTextureWithTexCoords(new Rect(r.xMax - edge, r.y, edge, edge), pUR, TC(8f / 11f, 8f / 11f, 3f / 11f, 3f / 11f));
            GUI.DrawTextureWithTexCoords(new Rect(r.x, r.yMax - edge, edge, edge), pDL, TC(0f, 0f, 3f / 11f, 3f / 11f));
            GUI.DrawTextureWithTexCoords(new Rect(r.xMax - edge, r.yMax - edge, edge, edge), pDR, TC(8f / 11f, 0f, 3f / 11f, 3f / 11f));
            GUI.DrawTextureWithTexCoords(new Rect(r.x + edge, r.y, r.width - (edge + edge), edge), pUM, TC(0f, 4f / 7f, 1f, 3f / 7f));
            GUI.DrawTextureWithTexCoords(new Rect(r.x + edge, r.yMax - edge, r.width - (edge + edge), edge), pDM, TC(0f, 0f, 1f, 3f / 7f));
            GUI.DrawTextureWithTexCoords(new Rect(r.x, r.y + edge, edge, r.height - (edge + edge)), pLM, TC(0f, 0f, 1f, 1f));
            GUI.DrawTextureWithTexCoords(new Rect(r.xMax - edge, r.y + edge, edge, r.height - (edge + edge)), pRM, TC(0f, 0f, 1f, 1f));
        }
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            float x = row.Item3 ? r.x + (r.width - BitmapFont.Measure(row.Item1, s)) * 0.5f : r.x + 6f * s;
            BitmapFont.Draw(x, r.y + 6f * s + s * 16f * i, row.Item1, row.Item2, s);
        }
    }
}
}
