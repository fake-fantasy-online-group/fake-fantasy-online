using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class AttrWashDlg
{
    private Rect L(float x, float y, float w, float h) => new Rect(winX + x * s, winY + y * s, w * s, h * s);

    private bool Click(Rect r, Vector2 m)
    {
        if (!r.Contains(m) || SysMsgBox.Visible || Event.current.type != EventType.MouseDown || Event.current.button != 0)
            return false;
        Event.current.Use();
        return true;
    }

    private bool Btn(Rect r, string text, Vector2 m)
    {
        bool hover = r.Contains(m);
        GUI.color = hover ? new Color(0.55f, 0.42f, 0.18f, 0.95f) : new Color(0.32f, 0.27f, 0.16f, 0.9f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;
        if (BitmapFont.Ready) BitmapFont.CenterOutline(r, text, new Color(1f, 0.92f, 0.66f, 1f), s);
        if (!hover || SysMsgBox.Visible || Event.current.type != EventType.MouseDown || Event.current.button != 0)
            return false;
        Event.current.Use();
        return true;
    }

    private void OnGUI()
    {
        if (!visible) return;
        if (!tried) Load();
        s = UIScale.Factor;
        if (WinDrag.Stale(winX, ref _wdGen))
        {
            winX = (Screen.width - NW * s) * 0.5f;
            winY = (Screen.height - NH * s) * 0.5f;
        }
        var stacking = WinZ.Frame(this, new Rect(winX, winY, NW * s, NH * s), 10);
        Vector2 savedMouse = Event.current.mousePosition;
        if (stacking.Item2) Event.current.mousePosition = new Vector2(-9999f, -9999f);
        try
        {
            Vector2 m = Event.current.mousePosition;
            GUI.depth = stacking.Item1;
            if (bg != null) GUI.DrawTexture(L(0, 0, NW, NH), bg);
            else
            {
                GUI.color = new Color(0.1f, 0.09f, 0.13f, 0.98f);
                GUI.DrawTexture(L(0, 0, NW, NH), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            float titleScale = Mathf.Max(1f, Mathf.Round(s));
            if (BitmapFont.Ready)
                BitmapFont.CenterOutlineBold(L(44, 24, 226, 18), "装备洗炼", new Color(1f, 0.92f, 0.66f, 1f), titleScale);
            Rect target = L(153.5f, 66, 36, 36);
            GUI.color = new Color(0, 0, 0, 0.45f);
            GUI.DrawTexture(target, Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (tIcon != null)
                ItemIconUI.Draw(new Rect(target.x + 4f * s, target.y + 4f * s, target.width - 8f * s, target.height - 8f * s), tIcon, s);
            if (tKind < 0)
            {
                if (BitmapFont.Ready) BitmapFont.CenterOutline(L(0, 112, NW, 14),
                    "点一件装备放入洗炼格(无属性白板不可)", new Color(0.72f, 0.72f, 0.72f, 1), s);
            }
            else
            {
                if (BitmapFont.Ready) BitmapFont.CenterOutline(L(0, 110, NW, 16), tName, UITip.QualityColor(quality), s);
                if (target.Contains(m))
                {
                    string name = quality > 0 ? unchecked((char)(quality + '0')).ToString() + tName : tName;
                    UITip.Show(TooltipContent.Fmt(name, string.Join("\n", affixes)), s);
                }
                if (affixes.Count == 0 && BitmapFont.Ready)
                    BitmapFont.CenterOutline(L(0, 156, NW, 14), "该装备无可洗词条", new Color(0.72f, 0.72f, 0.72f, 1), s);
                for (int i = 0; i < 5 && i < affixes.Count; i++)
                {
                    float y = 132f + i * 30f;
                    if (BitmapFont.Ready)
                        BitmapFont.DrawOutline(winX + 46f * s, winY + (y + 4f) * s, affixes[i], UITip.QualityColor(quality), s);
                    bool max = i < atMax.Count && atMax[i];
                    if (max && BitmapFont.Ready)
                        BitmapFont.DrawOutline(winX + 228f * s, winY + (y + 4f) * s, "满", new Color(1f, 0.84f, 0.35f, 1), s);
                    if (Btn(L(246, y, 40, 22), "洗", m))
                    {
                        int index = i;
                        if (max)
                            SysMsgBox.Show("这一条属性已经是最高值了。\n继续洗炼会重新随机属性和数值,\n有可能变低。是否继续?",
                                () => NetClient.I?.WashAffixForIndependentRuntime(unchecked((byte)tKind), unchecked((byte)tTab), tSlot, index), null);
                        else NetClient.I?.WashAffixForIndependentRuntime(unchecked((byte)tKind), unchecked((byte)tTab), tSlot, index);
                    }
                }
                if (affixes.Count > 0 && Btn(L(116.5f, 322, 110, 26), "全部重随", m))
                {
                    bool allMax = atMax.Count >= affixes.Count && affixes.Count > 0;
                    if (allMax)
                        for (int i = 0; i < affixes.Count; i++)
                            if (!atMax[i]) { allMax = false; break; }
                    if (allMax)
                        SysMsgBox.Show("这件装备的属性已经全是最高值了。\n继续洗炼会重新随机属性和数值,\n有可能变低。是否继续?",
                            () => NetClient.I?.WashAffixForIndependentRuntime(unchecked((byte)tKind), unchecked((byte)tTab), tSlot, -2), null);
                    else NetClient.I?.WashAffixForIndependentRuntime(unchecked((byte)tKind), unchecked((byte)tTab), tSlot, -2);
                }
                if (BitmapFont.Ready)
                {
                    Color muted = new Color(0.64f, 0.62f, 0.54f, 1);
                    BitmapFont.CenterOutline(L(34, 352, 258, 12), "单条洗=同色属性洗炼卷轴", muted, s);
                    BitmapFont.CenterOutline(L(34, 366, 258, 12), "全部重随=同色品质洗炼卷轴", muted, s);
                    BitmapFont.CenterOutline(L(34, 380, 258, 12), "关闭窗口不消耗", muted, s);
                }
            }
            Rect close = L(274, 20, 22, 22);
            if (icoClose != null) GUI.DrawTexture(close, icoClose);
            if (Click(close, m)) visible = false;
            WinDrag.Handle(this, L(0, 0, NW, 44), ref winX, ref winY);
            if (Event.current.type == EventType.MouseDown && L(0, 0, NW, NH).Contains(m) && !SysMsgBox.Visible)
                Event.current.Use();
        }
        finally
        {
            if (stacking.Item2) Event.current.mousePosition = savedMouse;
        }
    }
}
}
