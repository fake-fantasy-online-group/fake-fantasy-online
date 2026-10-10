using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class TaskDlg
{
    // Official 1.5.8 token 06000e84. Local coordinates to screen coordinates: the window origin is
    // winX/winY and every local value is scaled by s, including the size.
    public Rect L(float x, float y, float w, float h) =>
        new Rect(winX + x * s, winY + y * s, w * s, h * s);

    // Official 1.5.8 token 06000e85.
    public void Lbl(float x, float y, string t, Color c)
    {
        if (BitmapFont.Ready)
            BitmapFont.DrawOutline(winX + x * s, winY + y * s, t, c, s);
    }

    // Actual 1.6.0 token 06000e99. Capture the local right edge before iterating.
    // Explicit newlines advance one 14-unit line and reset the column; the measure and draw
    // calls each obtain their own character string, as in both native architectures.
    public void WrapLocal(float x, float y, float wPx, string text, Color c)
    {
        if (string.IsNullOrEmpty(text) || !BitmapFont.Ready) return;

        float startX = x * s;
        float rightEdge = (x + wPx) * s;
        float lineY = y * s;
        float columnX = startX;
        for (int index = 0; index < text.Length; index++)
        {
            char ch = text[index];
            if (ch == '\n')
            {
                lineY += s * 14f;
                columnX = startX;
                continue;
            }

            float advance = BitmapFont.Measure(ch.ToString(), s);
            if (rightEdge < columnX + advance)
            {
                lineY += s * 14f;
                columnX = startX;
            }

            BitmapFont.DrawOutline(columnX, lineY, ch.ToString(), c, s);
            columnX = advance + columnX;
        }
    }

    // Official 1.5.8 token 06000e8e. A 12-pixel glyph cell gives the per-line character budget;
    // the budget is clamped up to one so a very narrow width still counts one glyph per line, and
    // the count starts at one line. The column index advances only while the glyph is not a
    // newline and the next column still fits, otherwise it wraps to zero.
    public static int EstLines(string text, float wPx)
    {
        if (string.IsNullOrEmpty(text)) return 1;

        int perLine = (int)(wPx / 12f);
        if (perLine < 2) perLine = 1;

        int lines = 1;
        int column = 0;
        for (int index = 0; index < text.Length; index++)
        {
            char ch = text[index];
            if (ch == '\n' || perLine <= column + 1) lines++;

            int next = 0;
            if (ch != '\n' && column + 1 < perLine) next = column + 1;
            column = next;
        }

        return lines;
    }

    // Actual 1.6.0 token 06000e93. Publish the cached style after font/alignment initialization.
    // The normal color follows that assignment; font size is refreshed on every call.
    public GUIStyle EditStyle()
    {
        if (_edit == null)
        {
            _edit = new GUIStyle(GUIStyle.none)
            {
                font = NpcChatter.CnFont(),
                alignment = TextAnchor.MiddleLeft
            };
            _edit.normal.textColor = Color.white;
        }

        _edit.fontSize = (int)Mathf.Round(s * 12f);
        return _edit;
    }

    // Official 1.5.8 token 06000e88. A texture button tints to 1.25 when hovered; a fallback
    // button paints a solid hot/cold plate, then centres the outline label in the official WHITE.
    // Both shapes share the same click admission at the end.
    public bool Btn(Rect r, Texture2D tex, string fallback, Vector2 mp)
    {
        bool hot = r.Contains(mp);
        if (tex != null)
        {
            float tint = hot ? 1.25f : 1f;
            GUI.color = new Color(tint, tint, tint, 1f);
            GUI.DrawTexture(r, tex);
            GUI.color = Color.white;
        }
        else
        {
            GUI.color = hot
                ? new Color(0.55f, 0.42f, 0.18f, 0.95f)
                : new Color(0.3f, 0.26f, 0.16f, 0.88f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (BitmapFont.Ready) BitmapFont.CenterOutline(r, fallback, WHITE, s);
        }

        if (!hot || SysMsgBox.Visible) return false;
        if (Event.current.type != EventType.MouseDown) return false;
        if (Event.current.button != 0) return false;
        Event.current.Use();
        return true;
    }

    // Official 1.5.8 token 06000e89. The four-part Rect.Contains expansion gates the SysMsgBox
    // modal check, which in turn gates the left MouseDown admission.
    public bool Click(Rect r, Vector2 mp)
    {
        if (!r.Contains(mp)) return false;
        if (SysMsgBox.Visible) return false;
        if (Event.current.type != EventType.MouseDown) return false;
        if (Event.current.button != 0) return false;
        Event.current.Use();
        return true;
    }
}

}
