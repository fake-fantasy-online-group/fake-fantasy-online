using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal partial class PetForgeDlg
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
        Vector2 originalMouse = Event.current.mousePosition;
        if (stacking.Item2) Event.current.mousePosition = new Vector2(-9999f, -9999f);
        Color savedColor = GUI.color;
        int savedDepth = GUI.depth;
        try
        {
            Vector2 mp = Event.current.mousePosition;
            GUI.depth = stacking.Item1;
            if (bg != null) GUI.DrawTexture(L(0f, 0f, NW, NH), bg);
            else
            {
                GUI.color = new Color(0.1f, 0.09f, 0.13f, 0.98f);
                GUI.DrawTexture(L(0f, 0f, NW, NH), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            float titleScale = Mathf.Max(1f, Mathf.Round(s));
            if (BitmapFont.Ready)
                BitmapFont.CenterOutlineBold(L(44f, 24f, 226f, 18f),
                    kind == 2 ? "宠 物 炼 妖" : kind == 0 ? "宠物炼化" : "技能学习",
                    new Color(1f, 0.92f, 0.66f, 1f), titleScale);
            int slotKind = kind;
            DrawSlots(mp, slotKind);
            DrawContent(mp, slotKind);
            DrawConfirmation(mp);
            Color muted = new Color(0.64f, 0.62f, 0.54f, 1f);
            string warning = kind == 2 ? "★炼妖后这两只宠物都会消失, 只留下新的那一只"
                : kind == 0 ? "★炼化成功后这只宠物会消失" : "灰书有几率失败(失败也消耗, 技能不变)";
            float endY = Para(406f, warning, muted, 236f, 2, 13f);
            Para(endY + 2f, "关闭窗口不消耗", muted, 236f, 1, 13f);
            Rect close = L(274f, 20f, 22f, 22f);
            if (icoClose != null) GUI.DrawTexture(close, icoClose);
            if (Click(close, mp)) visible = false;
            WinDrag.Handle(this, L(0f, 0f, NW, 44f), ref winX, ref winY);
            if (Event.current.type == EventType.MouseDown && L(0f, 0f, NW, NH).Contains(mp) && !SysMsgBox.Visible)
                Event.current.Use();
        }
        finally
        {
            GUI.color = savedColor;
            GUI.depth = savedDepth;
            if (stacking.Item2) Event.current.mousePosition = originalMouse;
        }
    }
}
}
