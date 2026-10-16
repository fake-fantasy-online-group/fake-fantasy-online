using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class SysConfigDlg
{
    private static bool PickRowOn()
    {
        if (CharData.petSkills == null) return false;
        foreach (CharData.PetSkill skill in CharData.petSkills)
            if (skill.id == 13084 && skill.level > 0) return true;
        return false;
    }

    private static float Ext => PickRowOn() ? EXT_H : 0f;
    private static float NH => BG_H + Ext;

    // GUI-space, explicit physical-pointer hit testing; independent of Input.mousePosition.
    internal static bool MobileContainsPointer(Vector2 point)
    {
        if (!Visible) return false;
        float scale = UIScale.Factor;
        float width = NW, height = NH;
        bool centered = float.IsNaN(I.winX) || I._wdGen != WinDrag.Gen;
        float x = centered ? (Screen.width - width * scale) * .5f : I.winX;
        float y = centered ? (Screen.height - height * scale) * .5f : I.winY;
        if (new Rect(x, y, width * scale, height * scale).Contains(point)) return true;
        if (I.pickOpen && PickRowOn() && new Rect(x + 104f * scale, y + 303f * scale, 122f * scale, 108f * scale).Contains(point)) return true;
        float listHeight = GameSettings.ResOpts.Length * 20f + 8f;
        if (I.resOpen && new Rect(x + 104f * scale, y + (RESY - listHeight - 2f) * scale, 122f * scale, listHeight * scale).Contains(point)) return true;
        return false;
    }


    public static bool MouseOver()
    {
        if (I == null || !I.visible) return false;
        float scale = UIScale.Factor;
        float px = Input.mousePosition.x;
        float py = Screen.height - Input.mousePosition.y;
        float x = float.IsNaN(I.winX) ? (Screen.width - NW * scale) * 0.5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height - NH * scale) * 0.5f : I.winY;
        return new Rect(x, y, NW * scale, NH * scale).Contains(new Vector2(px, py));
    }

    private static string PickName(int quality)
    {
        switch (quality)
        {
            case 1: return "绿色以上";
            case 2: return "蓝色以上";
            case 3: return "紫色以上";
            case 4: return "只捡金色";
            default: return "全部拾取";
        }
    }
}

}
