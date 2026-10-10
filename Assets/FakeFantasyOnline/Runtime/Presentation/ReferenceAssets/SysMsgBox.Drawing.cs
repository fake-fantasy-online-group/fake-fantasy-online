using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class SysMsgBox
{
    private void OnGUI()
    {
        if (!shown) return;
        EnsureAssets();
        float sc = UIScale.Factor;
        Event e = Event.current;
        Color saved = GUI.color;
        float natW = bg != null ? TexScale.W(bg) : BG_W;
        float natH = bg != null ? TexScale.H(bg) : BG_H;
        List<string> lines = WrapLines(msg, (natW - 68f) * sc, sc);
        float wanted = (float)lines.Count * LINE_H;
        wanted = wanted + TEXT_TOP;
        wanted = wanted + TEXT_GAP;
        wanted = wanted + BTN_H;
        wanted = wanted + BTN_MY;
        float logicalH = Mathf.Max(natH, wanted);
        logicalH = Mathf.Min(logicalH, (float)Screen.height / Mathf.Max(0.01f, sc) - 8f);
        float width = natW * sc;
        float height = logicalH * sc;
        float x = UIScale.SnapX(((float)Screen.width - width) * 0.5f);
        float y = UIScale.SnapY(((float)Screen.height - height) * 0.5f);
        Rect frame = new Rect(x, y, width, height);
        if (bg != null)
        {
            DrawFrame(frame, bg, natH, sc);
        }
        else
        {
            GUI.color = new Color(26f / 255f, 27f / 255f, 22f / 255f, 0.92f);
            UIScale.SnapDraw(frame, Texture2D.whiteTexture);
            GUI.color = saved;
        }
        if (BitmapFont.Ready && lines.Count > 0)
        {
            float lineY = y + TEXT_TOP * sc;
            foreach (string line in lines)
            {
                BitmapFont.DrawOutline(x + 34f * sc, lineY, line,
                    new Color(235f / 255f, 225f / 255f, 180f / 255f, 1f), sc);
                lineY = lineY + LINE_H * sc;
            }
        }
        float buttonY = y + ((logicalH - BTN_H) - BTN_MY) * sc;
        Rect ok = new Rect(x + BTN_MX * sc, buttonY, BTN_W * sc, BTN_H * sc);
        Rect cancel = new Rect(x + ((natW - BTN_W) - BTN_MX) * sc, buttonY, BTN_W * sc, BTN_H * sc);
        Texture2D okTex = ok.Contains(e.mousePosition) ? (okH ?? okN) : okN;
        Texture2D caTex = cancel.Contains(e.mousePosition) ? (caH ?? caN) : caN;
        if (okTex != null) GUI.DrawTexture(ok, okTex);
        if (caTex != null) GUI.DrawTexture(cancel, caTex);
        if (e.type == EventType.MouseDown)
        {
            if (e.button == 1)
            {
                PlayerController.SuppressWorldClick();
                Fire(onCancel);
                e.Use();
                return;
            }
            if (e.button == 0)
            {
                PlayerController.SuppressWorldClick();
                if (ok.Contains(e.mousePosition)) Fire(onOk);
                else if (cancel.Contains(e.mousePosition)) Fire(onCancel);
                e.Use();
            }
        }
        else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            Fire(onCancel);
            e.Use();
        }
    }

    private static List<string> WrapLines(string text, float maxWidth, float sc)
    {
        var lines = new List<string>();
        if (string.IsNullOrEmpty(text) || !BitmapFont.Ready) return lines;
        foreach (string paragraph in text.Split('\n', StringSplitOptions.None))
        {
            var sb = new StringBuilder();
            foreach (char ch in paragraph)
            {
                if (sb.Length > 0 && BitmapFont.Measure(sb.ToString() + ch.ToString(), sc) > maxWidth)
                {
                    lines.Add(sb.ToString());
                    sb.Clear();
                }
                sb.Append(ch);
            }
            lines.Add(sb.ToString());
        }
        return lines;
    }

    private static void DrawFrame(Rect r, Texture2D tex, float natH, float sc)
    {
        if (r.height <= natH * sc + 0.5f)
        {
            UIScale.SnapDraw(r, tex);
            return;
        }
        float cap = FRAME_CAP * sc;
        float both = cap + cap;
        if (r.height <= both + 1f)
        {
            UIScale.SnapDraw(r, tex);
            return;
        }
        float uvCap = FRAME_CAP / natH;
        GUI.DrawTextureWithTexCoords(new Rect(r.x, r.y, r.width, cap), tex,
            new Rect(0f, 1f - uvCap, 1f, uvCap));
        GUI.DrawTextureWithTexCoords(new Rect(r.x, r.y + cap, r.width, r.height - both), tex,
            new Rect(0f, uvCap, 1f, 1f - (uvCap + uvCap)));
        GUI.DrawTextureWithTexCoords(new Rect(r.x, (r.y + r.height) - cap, r.width, cap), tex,
            new Rect(0f, 0f, 1f, uvCap));
    }
}

}
