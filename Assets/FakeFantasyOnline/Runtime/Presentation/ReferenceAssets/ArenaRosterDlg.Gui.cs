using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class ArenaRosterDlg
{
    private const float PW = 420f;
    private const float HEAD = 34f;
    private const float ROWH = 20f;
    private const float PADB = 12f;

    // Official 1.6.0 0600007d: exclude duel arenas before reading the held key/typing state.
    public static bool Showing =>
        CustomMaps.IsCustom(GameBootstrap.CurMapId) &&
        !CustomMaps.IsDuelArena(GameBootstrap.CurMapId) &&
        Input.GetKey(KeyCode.U) &&
        !ChatUI.TypingNow &&
        !Win05.GmTyping &&
        GUIUtility.keyboardControl == 0;

    // Official 0600007d..80.
    private static float S => UIScale.Factor;
    private static float Font => Mathf.Max(1f, Mathf.Floor(UIScale.Factor));
    private static Rect Snap(Rect r) => UIScale.Snap(r);

    private static void TX(float x, float y, string t, Color c, float sc)
    {
        BitmapFont.DrawOutline(Mathf.Round(x), Mathf.Round(y), t, c, sc);
    }

    // Official 06000081. Keep each float operation in the native order.
    private static Rect PanelRect()
    {
        float s = UIScale.Factor;
        int rowCount = Mathf.Max(1, _rows.Count);
        int screenWidth = Screen.width;
        int screenHeight = Screen.height;
        float width = s * PW;
        float x = (screenWidth - width) * 0.5f;
        float top = Mathf.Max(s * ROWH, screenHeight * 0.18f);
        float height = rowCount * ROWH;
        height += HEAD;
        height += PADB;
        height *= s;
        return UIScale.Snap(new Rect(x, top, width, height));
    }

    // Official 06000083: char iteration is over UTF-16 code units, with no word wrapping.
    private static List<string> WrapDesc(string t, float f, float maxW)
    {
        var lines = new List<string>();
        if (string.IsNullOrEmpty(t)) return lines;

        var current = new StringBuilder();
        for (int i = 0; i < t.Length; i++)
        {
            char c = t[i];
            if (c == '\n')
            {
                lines.Add(current.ToString());
                current.Length = 0;
                continue;
            }

            current.Append(c);
            if (BitmapFont.Measure(current.ToString(), f) > maxW)
            {
                current.Length = current.Length - 1;
                lines.Add(current.ToString());
                current.Length = 0;
                current.Append(c);
            }
        }

        if (current.Length > 0) lines.Add(current.ToString());
        if (lines.Count > 8) lines.RemoveRange(8, lines.Count - 8);
        return lines;
    }

    // Official 06000082. Unity invokes this on the persistent component created by Ensure.
    private void OnGUI()
    {
        if (!Showing) return;

        float s = S;
        float font = Font;
        float border = Mathf.Max(1f, Mathf.Floor(s));
        Texture2D dot = ZoneOverlay.Dot;
        Color originalColor = GUI.color;
        Rect panel = PanelRect();

        GUI.color = new Color(0.1f, 0.08f, 0.14f, 0.97f);
        GUI.DrawTexture(panel, dot);
        GUI.color = new Color(183f / 255f, 155f / 255f, 98f / 255f, 0.85f);
        GUI.DrawTexture(Snap(new Rect(panel.x, panel.y, panel.width, border)), dot);
        GUI.DrawTexture(Snap(new Rect(panel.x, panel.y + panel.height - border,
            panel.width, border)), dot);
        GUI.DrawTexture(Snap(new Rect(panel.x, panel.y, border, panel.height)), dot);
        GUI.DrawTexture(Snap(new Rect(panel.x + panel.width - border, panel.y,
            border, panel.height)), dot);
        GUI.color = Color.white;

        float titleX = panel.x + 12f * s;
        float titleY = panel.y + 9f * s;
        TX(titleX, titleY, "全场天赋", new Color32(255, 226, 138, 255), font);
        string status = _rows.Count > 0
            ? string.Format("{0} 人 · 松开 U 收起", _rows.Count)
            : "松开 U 收起";
        float statusWidth = BitmapFont.Measure(status, font);
        TX(panel.x + panel.width - statusWidth - 12f * s, titleY, status,
            new Color32(150, 150, 162, 255), font);

        GUI.color = new Color(183f / 255f, 155f / 255f, 98f / 255f, 0.3f);
        GUI.DrawTexture(Snap(new Rect(panel.x + 10f * s, panel.y + 27f * s,
            panel.width - 20f * s, border)), dot);
        GUI.color = Color.white;

        if (_rows.Count == 0)
        {
            TX(titleX, panel.y + 34f * s, "还没有人选卡",
                new Color32(150, 150, 162, 255), font);
        }
        else
        {
            float mouseX = Input.mousePosition.x;
            float mouseY = Screen.height - Input.mousePosition.y;
            float rowY = panel.y + HEAD * s;
            float rowStep = ROWH * s;
            float chipStartX = panel.x + 150f * s;
            float chipRightLimit = panel.x + panel.width - 10f * s;
            float chipPadding = 8f * s;
            float chipGap = 4f * s;
            float chipHeight = 16f * s;
            string hoveredName = null;
            string hoveredDescription = null;
            Rect hoveredChip = default;

            foreach (Row row in _rows)
            {
                bool local = row.entityId == NetClient.SelfEntityId;
                if (local)
                {
                    GUI.color = new Color(0.24f, 0.2f, 0.1f, 0.85f);
                    GUI.DrawTexture(Snap(new Rect(panel.x + border, rowY - 2f * s,
                        panel.width - 2f * border, rowStep)), dot);
                    GUI.color = Color.white;
                }

                Color32 nameColor = local
                    ? new Color32(255, 238, 180, 255)
                    : new Color32(215, 200, 235, 255);
                if (!row.alive) nameColor = new Color32(120, 120, 126, 255);
                TX(titleX, rowY, row.name, nameColor, font);

                float statusX = panel.x + 108f * s;
                if (!row.alive)
                {
                    TX(statusX, rowY, "[倒]", new Color32(200, 90, 90, 255), font);
                }
                else if (row.deaths > 0)
                {
                    TX(statusX, rowY, string.Format("死{0}", row.deaths),
                        new Color32(170, 120, 120, 255), font);
                }

                if (row.cards.Count == 0)
                {
                    TX(chipStartX, rowY, "—", new Color32(110, 110, 118, 255), font);
                }

                float cursor = chipStartX;
                foreach (Card card in row.cards)
                {
                    string label = card.lv > 1
                        ? string.Format("{0}{1}", card.name, card.lv)
                        : card.name;
                    float measured = BitmapFont.Measure(label, font);
                    float chipWidth = measured + chipPadding;
                    if (cursor + chipWidth > chipRightLimit) break;

                    Rect chip = Snap(new Rect(cursor, rowY - 2f * s,
                        chipWidth, chipHeight));
                    bool hover = mouseX >= chip.x && mouseX < chip.x + chip.width &&
                        mouseY >= chip.y && mouseY < chip.y + chip.height;
                    if (hover)
                    {
                        hoveredDescription = DescOf(card.skillId, card.lv);
                        hoveredName = label;
                        hoveredChip = chip;
                    }

                    if (hover)
                        GUI.color = new Color(0.34f, 0.29f, 0.16f, 0.98f);
                    else if (row.alive)
                        GUI.color = new Color(0.2f, 0.17f, 0.26f, 0.95f);
                    else
                        GUI.color = new Color(0.14f, 0.13f, 0.16f, 0.9f);
                    GUI.DrawTexture(chip, dot);
                    GUI.color = Color.white;

                    Color32 cardColor = hover
                        ? new Color32(255, 238, 180, 255)
                        : row.alive
                            ? new Color32(206, 206, 218, 255)
                            : new Color32(120, 120, 126, 255);
                    TX(cursor + 4f * s, rowY, label, cardColor, font);
                    cursor += chipWidth + chipGap;
                }

                rowY += rowStep;
            }

            if (hoveredName != null)
            {
                string description = string.IsNullOrEmpty(hoveredDescription)
                    ? "(这张卡的说明还没传过来)"
                    : hoveredDescription;
                List<string> lines = WrapDesc(description, font, 300f * s);
                float maxWidth = BitmapFont.Measure(hoveredName, font);
                foreach (string line in lines)
                {
                    float lineWidth = BitmapFont.Measure(line, font);
                    if (maxWidth <= lineWidth) maxWidth = lineWidth;
                }

                float tooltipWidth = maxWidth + 16f * s;
                float tooltipHeight = (lines.Count * 14f + 18f) * s + 8f * s;
                float margin = 4f * s;
                float rightBound = (Screen.width - tooltipWidth) - margin;
                float tooltipX = rightBound <= hoveredChip.x ? rightBound : hoveredChip.x;
                tooltipX = margin <= tooltipX ? tooltipX : margin;
                float tooltipY = (hoveredChip.y - tooltipHeight) - margin;
                if (tooltipY < margin)
                    tooltipY = (hoveredChip.y + hoveredChip.height) + margin;
                Rect tooltip = Snap(new Rect(tooltipX, tooltipY,
                    tooltipWidth, tooltipHeight));

                GUI.color = new Color(0.07f, 0.06f, 0.1f, 0.98f);
                GUI.DrawTexture(tooltip, dot);
                GUI.color = new Color(183f / 255f, 155f / 255f, 98f / 255f, 0.9f);
                GUI.DrawTexture(Snap(new Rect(tooltip.x, tooltip.y,
                    tooltip.width, border)), dot);
                GUI.DrawTexture(Snap(new Rect(tooltip.x,
                    tooltip.y + tooltip.height - border, tooltip.width, border)), dot);
                GUI.DrawTexture(Snap(new Rect(tooltip.x, tooltip.y,
                    border, tooltip.height)), dot);
                GUI.DrawTexture(Snap(new Rect(tooltip.x + tooltip.width - border,
                    tooltip.y, border, tooltip.height)), dot);
                GUI.color = Color.white;

                TX(tooltip.x + 8f * s, tooltip.y + 5f * s, hoveredName,
                    new Color32(255, 226, 138, 255), font);
                float lineY = tooltip.y + 21f * s;
                float lineStep = 14f * s;
                foreach (string line in lines)
                {
                    TX(tooltip.x + 8f * s, lineY, line,
                        new Color32(206, 206, 218, 255), font);
                    lineY += lineStep;
                }
            }
        }

        GUI.color = originalColor;
    }
}
}
