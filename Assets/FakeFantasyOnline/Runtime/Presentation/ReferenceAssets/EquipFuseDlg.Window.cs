using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class EquipFuseDlg
{
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
            float px = Mathf.Max(1f, Mathf.Round(s));
            if (bg != null) GUI.DrawTexture(L(0, 0, NW, NH), bg);
            else
            {
                GUI.color = new Color(0.1f, 0.09f, 0.13f, 0.98f);
                GUI.DrawTexture(L(0, 0, NW, NH), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            if (BitmapFont.Ready)
                BitmapFont.CenterOutlineBold(L(44, 24, 226, 18), "装 备 融 合", new Color(1f, 0.92f, 0.66f, 1f), px);
            Rect main = L(136, 62, 36, 36), sub = L(214, 62, 36, 36);
            bool mainHi = !pickSub && mainTab < 0;
            bool subHi = !mainHi && subTab < 0;
            if (!mainHi && !subHi) mainHi = mainTab < 0;
            Box(main, mainIcon, mainHi, px);
            Box(sub, subIcon, subHi, px);
            if (BitmapFont.Ready)
            {
                Color empty = new Color(0.7f, 0.7f, 0.65f, 1f);
                BitmapFont.CenterOutline(L(136, 102, 36, 12), mainTab < 0 ? "主" : Ellipsis(mainName, 56f * s),
                    mainTab < 0 ? empty : (Color)UITip.QualityColor(mainQ), s);
                BitmapFont.CenterOutline(L(214, 102, 36, 12), subTab < 0 ? "副" : Ellipsis(subName, 56f * s),
                    subTab < 0 ? empty : (Color)UITip.QualityColor(subQ), s);
                BitmapFont.CenterOutline(L(180, 74, 24, 12), "+", new Color(0.85f, 0.85f, 0.8f, 1f), s);
            }
            if (Click(main, m) && mainTab >= 0)
            {
                mainTab = -1; mainIcon = null; mainName = ""; mainDesc = "";
                mainId = 0; mainQ = 0; pickSub = false; Query();
            }
            if (Click(sub, m) && subTab >= 0)
            {
                subTab = -1; subIcon = null; subName = ""; subDesc = "";
                subId = 0; subQ = 0; pickSub = true; Query();
            }
            if (BitmapFont.Ready)
            {
                BitmapFont.DrawOutline(winX + 48f * s, winY + 116f * s,
                    mainTab >= 0 ? mainName + " 当前属性:" : "主件属性:", new Color(0.86f, 0.78f, 0.55f, 1f), s);
                if (mainTab >= 0 && !string.IsNullOrEmpty(mainDesc))
                {
                    float y = 132f;
                    foreach (string line in mainDesc.Split('\n'))
                    {
                        BitmapFont.DrawOutline(winX + 56f * s, winY + y * s, "· " + line, new Color(0.62f, 0.86f, 0.86f, 1f), s);
                        y += 15f;
                    }
                }
                else BitmapFont.DrawOutline(winX + 56f * s, winY + 132f * s,
                    mainTab >= 0 ? "(无随机属性)" : "(把要加词条的装备放进「主」格)", new Color(0.6f, 0.6f, 0.55f, 1f), s);
            }
            if (BitmapFont.Ready)
            {
                Color color = ok ? new Color32(206, 192, 158, 255) : new Color32(255, 190, 130, 255);
                float y = 304f;
                foreach (string line in WrapTip(string.IsNullOrEmpty(tip) ? "把两件【同款同色】的装备放进主/副格" : tip))
                {
                    BitmapFont.DrawOutline(winX + 48f * s, winY + y * s, line, color, s);
                    y += 15f;
                }
            }
            if (main.Contains(m) && mainTab >= 0)
                UITip.Show(TooltipContent.Fmt(TooltipContent.QualityName(mainName, mainQ), mainDesc), s);
            else if (sub.Contains(m) && subTab >= 0)
                UITip.Show(TooltipContent.Fmt(TooltipContent.QualityName(subName, subQ), subDesc), s);
            Rect confirm = L(165, 348, 90, 26);
            bool hover = confirm.Contains(m);
            GUI.color = !ok ? new Color(0.25f, 0.24f, 0.22f, 0.85f)
                : hover ? new Color(0.5f, 0.4f, 0.16f, 0.95f) : new Color(0.4f, 0.32f, 0.13f, 0.9f);
            GUI.DrawTexture(confirm, Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (BitmapFont.Ready)
                BitmapFont.CenterOutline(new Rect(confirm.x, confirm.y + 7f * s, confirm.width, 12f * s), "融 合",
                    ok ? (Color)new Color32(255, 230, 150, 255) : new Color32(140, 140, 135, 255), s);
            if (ok && Click(confirm, m))
            {
                byte mt = unchecked((byte)mainTab), st = unchecked((byte)subTab);
                int ms = mainSlot, ss = subSlot, capturedSig = sig;
                SysMsgBox.Show(string.IsNullOrEmpty(confirmText) ? "确定融合吗?" : confirmText,
                    () => NetClient.I?.EquipFuseForIndependentRuntime(1, 0, mt, ms, st, ss, capturedSig), null);
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
        if (visible && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
        {
            visible = false;
            Event.current.Use();
        }
    }
}
}
