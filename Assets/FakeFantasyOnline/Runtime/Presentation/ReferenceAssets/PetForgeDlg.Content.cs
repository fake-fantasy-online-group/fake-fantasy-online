using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal partial class PetForgeDlg
{
    private void DrawContent(Vector2 mp, int slotKind)
    {
        Color gray = new Color(0.72f, 0.72f, 0.72f, 1f);
        Color muted = new Color(0.64f, 0.62f, 0.54f, 1f);
        if (BitmapFont.Ready && slotKind == 2)
        {
            string summary = petTab >= 0 && pet2Tab >= 0
                ? string.Format("{0} Lv.{1}  ✚  {2} Lv.{3}", petSpecies, petLevel, pet2Species, pet2Level)
                : "把要炼的两只【魂之精】放进左右两格, 中间放炼妖壶";
            Para(118f, summary, gray, 236f, 1, 12f);
        }
        else if (BitmapFont.Ready && petTab >= 0)
            Para(118f, string.Format("{0} Lv.{1}   战斗 {2}/{3}   生活 {4}/{5}",
                petSpecies, petLevel, fUsed, fCap, lUsed, lCap), gray, 236f, 1, 12f);

        bool bookLife = kind == 1 && bookSkillId != 0 && IsLifeSkillId(bookSkillId);
        bool needReplace = kind == 1 && !ok && bookSkillId != 0 && (bookLife ? lUsed >= lCap : fUsed >= fCap);
        if (petTab < 0) Para(218f, "把【魂之精】放进左边的格子", gray, 236f, 2, 14f);
        else if (skills.Count == 0) Para(218f, "这只宠物还没有领悟任何技能", gray, 236f, 2, 14f);
        else
        {
            string heading = kind == 2 ? "两只加起来的技能(新宠学不了的会失传)"
                : kind == 0 ? "炼化会从下面这些技能里【随机】抽一个"
                : needReplace ? "选一个要被顶替掉的技能" : "这只宠物现有的技能";
            Rect view = L(34f, 152f, 256f, 146f);
            Para(136f, heading, muted, 236f, 2, 12f);
            float contentH = skills.Count * 20f * s;
            NativeScroll.Wheel(view, view.height, contentH, ref scroll, 20f * s);
            scroll.y = Mathf.Clamp(scroll.y, 0f, Mathf.Max(0f, contentH - view.height));
            GUI.BeginGroup(view);
            Vector2 localMouse = mp - view.position;
            for (int i = 0; i < skills.Count; i++)
            {
                var row = skills[i];
                float rowY = i * 20f * s - scroll.y;
                if (rowY + 20f * s <= 0f || rowY >= view.height) continue;
                Rect r = new Rect(2f * s, rowY, view.width - 6f * s, 18f * s);
                bool combat = row.Item6 != 0;
                bool selectable = needReplace && (bookLife != combat);
                if (selSkill == row.Item1)
                {
                    GUI.color = new Color(0.55f, 0.42f, 0.18f, 0.55f);
                    GUI.DrawTexture(r, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
                else if (selectable && r.Contains(localMouse))
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.1f);
                    GUI.DrawTexture(r, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
                Texture2D icon = PetSkillIconUI.Get(row.Item1, row.Item3);
                if (icon != null) GUI.DrawTexture(new Rect(r.x + s, rowY + s, 16f * s, 16f * s), icon);
                if (BitmapFont.Ready)
                {
                    BitmapFont.DrawOutline(r.x + 21f * s, rowY + 4f * s, (combat ? "[战] " : "[生] ") + row.Item2,
                        combat ? new Color(1f, 0.86f, 0.6f, 1f) : new Color(0.72f, 0.86f, 0.72f, 1f), s);
                    BitmapFont.DrawOutline(r.x + 205f * s, rowY + 4f * s,
                        string.Format("Lv.{0}/{1}", row.Item4, row.Item5), new Color(0.86f, 0.86f, 0.86f, 1f), s);
                }
                if (selectable && r.Contains(localMouse) && !SysMsgBox.Visible &&
                    Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    selSkill = selSkill == row.Item1 ? 0 : row.Item1;
                    Query();
                    Event.current.Use();
                }
            }
            GUI.EndGroup();
            NativeScroll.BarAt(this, L(295f, 152f, 15f, 146f), view.height, contentH, ref scroll);
        }
        float tipY = 308f;
        if (slotKind == 2)
        {
            if (tierOdds.Count > 0 && BitmapFont.Ready)
            {
                float cellW = 240f / Mathf.Max(1, tierOdds.Count);
                for (int i = 0; i < tierOdds.Count; i++)
                {
                    var odds = tierOdds[i];
                    Color color = odds.Item2 >= 50 ? new Color(1f, 0.92f, 0.66f, 1f) : new Color(0.82f, 0.82f, 0.7f, 1f);
                    float x = 42f + cellW * i;
                    BitmapFont.CenterOutline(L(x, 292f, cellW, 12f), CharData.PetTierName(odds.Item1), color, s);
                    BitmapFont.CenterOutline(L(x, 304f, cellW, 12f), odds.Item2.ToString() + "%", color, s);
                }
            }
            if (tierOdds.Count > 0) tipY = 320f;
        }
        Para(tipY, tip, ok ? new Color(1f, 0.92f, 0.66f, 1f) : new Color(0.78f, 0.62f, 0.62f, 1f), 236f, 4, 14f);
    }
}
}
