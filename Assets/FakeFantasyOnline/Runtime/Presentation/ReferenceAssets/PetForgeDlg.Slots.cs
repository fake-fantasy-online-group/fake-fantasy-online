using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal partial class PetForgeDlg
{
    private void DrawSlots(Vector2 mp, int mode)
    {
        Rect a = L(mode == 2 ? 88f : 106f, 62f, 36f, 36f);
        Rect item = L(mode == 2 ? 144f : 182f, 62f, 36f, 36f);
        Rect b = L(200f, 62f, 36f, 36f);
        bool hiA = false;
        bool hiB = false;
        if (mode == 2)
        {
            if ((!pickB && petTab < 0) || pet2Tab >= 0) hiA = petTab < 0;
            else hiB = true;
            if (BitmapFont.Ready)
            {
                BitmapFont.CenterOutline(L(88f, 48f, 36f, 12f), "主", new Color(1f, 0.86f, 0.44f, 1f), s);
                BitmapFont.CenterOutline(L(200f, 48f, 36f, 12f), "副", new Color(0.78f, 0.72f, 0.56f, 1f), s);
            }
        }
        Box(a, petIcon, hiA);
        Box(item, itemIcon);
        if (Click(a, mp))
        {
            petTab = -1;
            petBrief = null;
            petIcon = null;
            pickB = false;
            Query();
        }
        if (Click(item, mp))
        {
            itemTab = -1;
            itemIcon = null;
            itemName = "";
            Query();
        }
        if (mode == 2)
        {
            Box(b, pet2Icon, hiB);
            if (Click(b, mp))
            {
                pet2Tab = -1;
                pet2Brief = null;
                pet2Icon = null;
                pickB = true;
                Query();
            }
            if (b.Contains(mp) && pet2Brief.HasValue) PetInfoTooltip.Show(pet2Brief.Value, mp, s);
            if (BitmapFont.Ready)
            {
                string tierA = CharData.PetTierName(petTier);
                string tierB = CharData.PetTierName(pet2Tier);
                BitmapFont.CenterOutline(L(a.x / s - winX / s, 102f, 36f, 12f),
                    petTab < 0 ? "魂之精" : tierA.Length > 0 ? tierA + "档" : "?",
                    new Color(0.86f, 0.86f, 0.7f, 1f), s);
                BitmapFont.CenterOutline(L(b.x / s - winX / s, 102f, 36f, 12f),
                    pet2Tab < 0 ? "魂之精" : tierB.Length > 0 ? tierB + "档" : "?",
                    new Color(0.86f, 0.86f, 0.7f, 1f), s);
            }
        }
        if (BitmapFont.Ready && mode != 2)
        {
            BitmapFont.CenterOutline(L(34f, 102f, 122f, 12f),
                Ellipsis(petTab < 0 ? "魂之精" : petName, 116f * s), new Color(0.86f, 0.86f, 0.7f, 1f), s);
            BitmapFont.CenterOutline(L(168f, 102f, 122f, 12f),
                Ellipsis(itemTab < 0 ? kind == 0 ? "炼化丹" : "技能书" : itemName, 116f * s),
                new Color(0.86f, 0.86f, 0.7f, 1f), s);
        }
        if (a.Contains(mp) && petBrief.HasValue) PetInfoTooltip.Show(petBrief.Value, mp, s);
        if (item.Contains(mp) && itemTab >= 0)
        {
            string title = itemName;
            string description;
            if (kind == 0) description = "随机抽一个技能炼成技能书(宠物会消失)";
            else if (kind == 2) description = "把两只魂之精炼成一只(两只都会消失)";
            else if (HasSkill(bookSkillId, out int lv))
                description = string.Format("把【{0}】从 Lv.{1} 升到 Lv.{2}", bookSkillName, lv, bookSkillLv);
            else description = string.Format("教会宠物【{0}】Lv.{1}", bookSkillName, bookSkillLv);
            UITip.Show(TooltipContent.Fmt(title, description), s);
        }
    }
}
}
