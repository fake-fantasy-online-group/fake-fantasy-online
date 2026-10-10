using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal partial class PetForgeDlg
{
    public static bool MouseOver()
    {
        if (I == null || !I.visible) return false;
        float scale = UIScale.Factor;
        float mx = Input.mousePosition.x;
        float my = Screen.height - Input.mousePosition.y;
        float x = float.IsNaN(I.winX) ? (Screen.width - NW * scale) * 0.5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height - NH * scale) * 0.5f : I.winY;
        return new Rect(x, y, NW * scale, NH * scale).Contains(new Vector2(mx, my));
    }

    private Rect L(float x, float y, float w, float h) =>
        new Rect(winX + x * s, winY + y * s, w * s, h * s);

    private bool HasSkill(int sid, out int lv)
    {
        lv = 0;
        if (sid == 0) return false;
        for (int i = 0; i < skills.Count; i++)
            if (skills[i].Item1 == sid)
            {
                lv = skills[i].Item4;
                return lv > 0;
            }
        return false;
    }

    private static bool IsLifeSkillId(int sid) =>
        unchecked((uint)(sid - 13001)) <= 2u || unchecked((uint)(sid - 13007)) <= 8u ||
        sid == 13081 || sid == 13084 || sid == 13088 || sid == 13136;

    private string Ellipsis(string text, float maxW)
    {
        if (string.IsNullOrEmpty(text) || !BitmapFont.Ready || BitmapFont.Measure(text, s) <= maxW) return text;
        var result = new StringBuilder();
        foreach (char c in text)
        {
            if (BitmapFont.Measure(result.ToString() + c + "…", s) > maxW) break;
            result.Append(c);
        }
        return result.Append('…').ToString();
    }

    private List<string> Wrap(string text, float maxW)
    {
        _wrapBuf.Clear();
        if (string.IsNullOrEmpty(text) || !BitmapFont.Ready) return _wrapBuf;
        foreach (string line in text.Split('\n'))
        {
            var current = new StringBuilder();
            foreach (char c in line)
            {
                if (current.Length > 0 && BitmapFont.Measure(current.ToString() + c, s) > maxW)
                {
                    _wrapBuf.Add(current.ToString());
                    current.Clear();
                }
                current.Append(c);
            }
            _wrapBuf.Add(current.ToString());
        }
        return _wrapBuf;
    }
}

}
