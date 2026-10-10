using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ComposeDlg
{
    private void DrawCombineList(Rect area, Vector2 mp)
    {
        if (recipes.Count == 0)
        {
            if (BitmapFont.Ready) BitmapFont.CenterOutline(new Rect(area.x,
                area.y + area.height * 0.5f - 6f * s, area.width, 12f * s), "暂时没有可以合成的物品", CraftUI.GRAY, s);
            return;
        }
        float rowHeight = 34f * s;
        float contentHeight = recipes.Count * rowHeight;
        scroll = GUI.BeginScrollView(area, scroll, new Rect(0, 0, area.width, contentHeight),
            GUIStyle.none, GUIStyle.none);
        for (int i = 0; i < recipes.Count; i++)
        {
            var recipe = recipes[i]; var row = new Rect(0, i * rowHeight, area.width, rowHeight);
            if (i == sel)
            {
                GUI.color = new Color(0.55f, 0.42f, 0.18f, 0.5f);
                GUI.DrawTexture(row, Texture2D.whiteTexture); GUI.color = Color.white;
            }
            if (recipe.icon != null) ItemIconUI.Draw(new Rect(2f * s, row.y + 4f * s, 24f * s, 24f * s), recipe.icon, s);
            if (BitmapFont.Ready)
            {
                float width = area.width - 34f * s;
                BitmapFont.DrawOutline(30f * s, row.y + 3f * s, BitmapFont.Elide(recipe.name, width, s),
                    recipe.canMake ? CraftUI.GOLD : CraftUI.GRAY, s);
                if (recipe.mats != null && recipe.mats.Count > 0)
                {
                    var mat = recipe.mats[0];
                    bool enough = mat.have >= mat.need;
                    BitmapFont.DrawOutline(30f * s, row.y + 18f * s,
                        BitmapFont.Elide(string.Format("{0} {1}/{2}", mat.name, mat.have, mat.need), width, s),
                        enough ? new Color(0.7f, 0.85f, 0.7f, 1f) : new Color(1f, 0.45f, 0.45f, 1f), s);
                }
            }
            if (row.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            { sel = i; Event.current.Use(); }
        }
        GUI.EndScrollView();
        NativeScroll.BarAt(this, L(335, 79, 15, 206), area.height, contentHeight, ref scroll);
    }

    private void DrawMatSlots(Recipe recipe, Vector2 mp)
    {
        if (recipe == null || recipe.mats == null) return;
        string tip = null;
        for (int i = 0; i < recipe.mats.Count && i < 10; i++)
        {
            var mat = recipe.mats[i];
            Rect slot = L(MatSlotX[i % 5], i < 5 ? 331 : 372, 36, 36);
            if (mat.icon != null) ItemIconUI.Draw(slot, mat.icon, s);
            else
            {
                GUI.color = new Color(0f, 0f, 0f, 0.25f); GUI.DrawTexture(slot, Texture2D.whiteTexture); GUI.color = Color.white;
            }
            if (BitmapFont.Ready)
            {
                bool enough = mat.have >= mat.need;
                BitmapFont.DrawOutline(slot.x + 2f * s, slot.yMax - 11f * s,
                    string.Format("{0}/{1}", mat.have, mat.need),
                    enough ? new Color(0.75f, 0.92f, 0.75f, 1f) : new Color(1f, 0.5f, 0.5f, 1f), s);
            }
            if (slot.Contains(mp)) tip = TooltipContent.Fmt(mat.name, string.Format("拥有 {0} / 需要 {1}", mat.have, mat.need));
        }
        if (tip != null) UITip.Show(tip, s);
    }
    private void DrawRefineMats(Vector2 mp)
    {
        if (CraftUpgradeUI.RefBagSlot < 0) return;
        var mats = CraftUpgradeUI.Mats;
        if (CraftUpgradeUI.CurLevel < 0) return;
        string tip = null;
        for (int i = 0; i < mats.Count && i < 10; i++)
        {
            var mat = mats[i];
            Rect slot = L(MatSlotX[i % 5], i < 5 ? 331 : 372, 36, 36);
            if (mat.icon != null) ItemIconUI.Draw(slot, mat.icon, s);
            else
            {
                GUI.color = new Color(0f, 0f, 0f, 0.25f); GUI.DrawTexture(slot, Texture2D.whiteTexture); GUI.color = Color.white;
            }
            if (BitmapFont.Ready)
            {
                bool enough = mat.have >= mat.need;
                BitmapFont.DrawOutline(slot.x + 2f * s, slot.yMax - 11f * s,
                    string.Format("{0}/{1}", mat.have, mat.need),
                    enough ? new Color(0.75f, 0.92f, 0.75f, 1f) : new Color(1f, 0.5f, 0.5f, 1f), s);
            }
            if (slot.Contains(mp)) tip = TooltipContent.Fmt(mat.name, string.Format("拥有 {0} / 需要 {1}", mat.have, mat.need));
        }
        if (tip != null) UITip.Show(tip, s);
    }
    private void DrawCoin(Texture2D icon, float x, long value)
    {
        if (icon != null) GUI.DrawTexture(L(x, 422, 12, 12), icon);
        if (BitmapFont.Ready) BitmapFont.DrawOutline(winX + (x + 13f) * s, winY + 422f * s, value.ToString(), CraftUI.GOLD, s);
    }
}
}
