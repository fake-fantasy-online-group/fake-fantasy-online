using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class EquipDlg
{
    // Reused native 030e/030f/0310/0312/0314/0315 bodies. No new helper acceptance claim.
    private void CenterPlain(
        Rect bounds,
        string value,
        Color color)
    {
        float left = bounds.x + (bounds.width - BitmapFont.Measure(value, s)) * 0.5f;
        BitmapFont.Draw(left, bounds.y, value, color, s);
    }

    private void Patch9(Rect rect)
    {
        Color saved = GUI.color;
        GUI.color = new Color(16f / 255f, 16f / 255f, 16f / 255f, 196f / 255f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = saved;
        if (pUL == null)
        {
            return;
        }

        float corner = 11f * s;
        float edge = 3f * s;
        GUI.DrawTexture(new Rect(rect.x, rect.y, corner, corner), pUL);
        GUI.DrawTexture(new Rect(rect.xMax - corner, rect.y, corner, corner), pUR);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - corner, corner, corner), pDL);
        GUI.DrawTexture(new Rect(rect.xMax - corner, rect.yMax - corner, corner, corner), pDR);
        GUI.DrawTexture(new Rect(rect.x + corner, rect.y, rect.width - (corner + corner), edge), pUM);
        GUI.DrawTexture(new Rect(rect.x + corner, rect.yMax - edge, rect.width - (corner + corner), edge), pDM);
        GUI.DrawTexture(new Rect(rect.x, rect.y + corner, edge, rect.height - (corner + corner)), pLM);
        GUI.DrawTexture(new Rect(rect.xMax - edge, rect.y + corner, edge, rect.height - (corner + corner)), pRM);
    }

    private void Border(Rect rect, Color color)
    {
        const float line = 1f;
        Color saved = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, line), Texture2D.whiteTexture);
        GUI.DrawTexture(
            new Rect(rect.x, rect.yMax - line, rect.width, line),
            Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, line, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(
            new Rect(rect.xMax - line, rect.y, line, rect.height),
            Texture2D.whiteTexture);
        GUI.color = saved;
    }

    private bool Spin(
        Rect rect,
        Texture2D[] frames,
        bool enabled,
        Event current)
    {
        if (frames == null || frames[0] == null)
        {
            return false;
        }

        if (enabled)
        {
            return Btn(rect, frames, current);
        }

        GUI.DrawTexture(
            rect,
            frames.Length > 3 && frames[3] != null ? frames[3] : frames[0]);
        return false;
    }

    private bool VTab(
        Rect rect,
        string label,
        bool selected,
        bool blue)
    {
        Texture2D texture = blue
            ? vtabBlue
            : selected ? vtab1 : vtab0;
        if (texture != null)
        {
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
        }

        if (BitmapFont.Ready)
        {
            Color color = blue
                ? new Color32(150,180,255,255)
                : selected ? new Color32(255,220,120,255) : new Color32(183,155,98,255);
            float textScale = Mathf.Max(1f, Mathf.Round(1.3f * s));
            float step = (rect.height - 8f * s) / label.Length;
            for (int index = 0; index < label.Length; index++)
            {
                BitmapFont.DrawOutline(
                    rect.x + (rect.width - textScale * 12) * 0.5f,
                    rect.y + 4f * s + step * index +
                        (step - textScale * 12) * 0.5f,
                    label[index].ToString(),
                    color,
                    textScale);
            }
        }

        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    private bool Btn(
        Rect rect,
        Texture2D[] frames,
        Event current)
    {
        if (frames == null || frames[0] == null)
        {
            return false;
        }

        int frame = rect.Contains(current.mousePosition)
            ? (Input.GetMouseButton(0) ? 2 : 1)
            : 0;
        GUI.DrawTexture(rect, frames[frame] ?? frames[0]);
        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }
}
}
