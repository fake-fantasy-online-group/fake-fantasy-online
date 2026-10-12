using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class SysConfigDlg
{
    private void OnGUI()
    {
        if (!visible)
            return;
        if (!tried)
            Load();
        if (!loaded)
            LoadPrefs();
        s = UIScale.Factor;
        if (WinDrag.Stale(winX, ref _wdGen))
        {
            winX = (Screen.width - NW * s) * .5f;
            winY = (Screen.height - NH * s) * .5f;
        }
        var stacking = WinZ.Frame(this, new Rect(winX, winY, NW * s, NH * s), 10);
        Vector2 savedMouse = Event.current.mousePosition;
        Color savedColor = GUI.color;
        int savedDepth = GUI.depth;
        if (stacking.Item2)
            Event.current.mousePosition = new Vector2(-9999f, -9999f);
        try
        {
            Vector2 mouse = Event.current.mousePosition;
            GUI.depth = stacking.Item1;
            if (bg != null)
            {
                Band(0f, 397f, 0f, 397f);
                Band(397f, 405f, 397f, 8f + Ext);
                Band(405f, 445f, 405f + Ext, 40f);
            }
            else
            {
                GUI.color = new Color(.1f, .09f, .13f, .98f);
                GUI.DrawTexture(L(0f, 0f, NW, NH), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            DrawExtensionBackground();

            uiBlocked = resOpen || pickOpen;
            if (curRes < 0)
                curRes = GameSettings.ResIndex();
            if (BitmapFont.Ready)
            {
                BitmapFont.Draw(winX + 30f * s, winY + 365f * s,
                    "设置分辨率:", new Color32(221, 198, 149, 255), s);
                BitmapFont.Draw(winX + 118f * s, winY + 365f * s,
                    string.Format("{0}×{1}", GameSettings.ResOpts[curRes].Item1,
                        GameSettings.ResOpts[curRes].Item2), Color.white, s);
            }
            Rect resArrow = L(210f, 364f, 16f, 15f);
            Tex(resArrow, resOpen ? dropHi : drop);
            Rect resHit = L(114f, 361f, 112f, 16f);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                (resHit.Contains(mouse) || resArrow.Contains(mouse)))
            {
                resOpen = !resOpen;
                Event.current.Use();
            }

            Tex(L(30f, SMARTY, 14f, 13f), smartCast ? chkOn : chkOff);
            if (BitmapFont.Ready)
                BitmapFont.Draw(winX + 50f * s, winY + 381f * s,
                    "启用智能施法(不弹选点框)", new Color32(221, 198, 149, 255), s);
            if (!uiBlocked)
            {
                Rect smartHit = L(28f, 378f, 200f, 16f);
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                    smartHit.Contains(mouse))
                {
                    smartCast = !smartCast;
                    Event.current.Use();
                }
            }
            Tex(L(30f, HPBARY, 14f, 13f), hpBar ? chkOn : chkOff);
            if (BitmapFont.Ready)
                BitmapFont.Draw(winX + 50f * s, winY + 397f * s,
                    "显示头顶血条(Insert 切换)", new Color32(221, 198, 149, 255), s);
            if (!uiBlocked)
            {
                Rect hpHit = L(28f, 394f, 200f, 16f);
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                    hpHit.Contains(mouse))
                {
                    hpBar = !hpBar;
                    Event.current.Use();
                }
            }

            for (int i = 0; i < PK.Length; i++)
            {
                Tex(L(PK[i].x, PK[i].y, 14f, 13f), pk == i ? radioOn : radioOff);
                if (Click(L(PK[i].x, PK[i].y, 75f, 14f), mouse))
                    pk = i;
            }
            for (int i = 0; i < OPTY.Length; i++)
            {
                if (i == 0 || i == 5)
                    MaskBaked(OPTY[i]);
                Tex(L(30f, OPTY[i], 14f, 13f), opt[i] ? chkOn : chkOff);
                if (Click(L(30f, OPTY[i], 116f, 14f), mouse))
                    opt[i] = !opt[i];
                if ((i == 0 || i == 5) && BitmapFont.Ready)
                    BitmapFont.Draw(winX + 48f * s, winY + (OPTY[i] + 1f) * s,
                        i == 0 ? "快捷栏放在屏幕底部" : "色弱模式(金色显示为橙黄)",
                        new Color32(221, 198, 149, 255), s);
            }
            for (int i = 0; i < SNDY.Length; i++)
            {
                float y = SNDY[i];
                if (i == 1)
                {
                    MaskBaked(y);
                    if (BitmapFont.Ready)
                        BitmapFont.Draw(winX + 30f * s, winY + (y + 1f) * s,
                            string.Format("界面大小 {0:0.0}×", sndVal[1] / 100f * 1.4f + .6f),
                            new Color32(221, 198, 149, 255), s);
                    Slider(1, L(100f, y, 146f, 14f), mouse);
                }
                else
                {
                    Tex(L(30f, y, 14f, 13f), sndOn[i] ? chkOn : chkOff);
                    if (Click(L(30f, y, 66f, 14f), mouse))
                        sndOn[i] = !sndOn[i];
                    Slider(i, L(100f, y, 146f, 14f), mouse);
                }
            }
            if (PickRowOn())
            {
                if (BitmapFont.Ready)
                {
                    BitmapFont.Draw(winX + 30f * s, winY + 414f * s,
                        "宠物拾取:", new Color32(221, 198, 149, 255), s);
                    BitmapFont.Draw(winX + 118f * s, winY + 414f * s, PickName(pickQ), Color.white, s);
                }
                Rect pickArrow = L(210f, 413f, 16f, 15f);
                Tex(pickArrow, pickOpen ? dropHi : drop);
                Rect pickHit = L(114f, 410f, 112f, 16f);
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                    (pickHit.Contains(mouse) || pickArrow.Contains(mouse)))
                {
                    pickOpen = !pickOpen;
                    Event.current.Use();
                }
            }
            if (Btn(L(31f, 419f + Ext, 90f, 22f), icoApply, mouse))
            {
                Apply();
                visible = false;
            }
            if (Btn(L(167f, 419f + Ext, 70f, 22f), icoCancel, mouse))
            {
                loaded = false;
                visible = false;
            }
            Rect close = L(241f, 6f, 20f, 19f);
            if (icoClose != null)
                GUI.DrawTexture(close, icoClose);
            if (Click(close, mouse))
                visible = false;
            if (resOpen)
                DrawResList(mouse);
            if (pickOpen && PickRowOn())
                DrawPickList(mouse);
            else if (!PickRowOn())
                pickOpen = false;
            if (Event.current.type == EventType.MouseUp)
                dragSlider = -1;
            WinDrag.Handle(this, L(0f, 0f, NW, 28f), ref winX, ref winY);
            if (Event.current.type == EventType.MouseDown && L(0f, 0f, NW, NH).Contains(mouse) &&
                !SysMsgBox.Visible)
                Event.current.Use();
        }
        finally
        {
            GUI.color = savedColor;
            GUI.depth = savedDepth;
            if (stacking.Item2)
                Event.current.mousePosition = savedMouse;
        }
    }
}
}
