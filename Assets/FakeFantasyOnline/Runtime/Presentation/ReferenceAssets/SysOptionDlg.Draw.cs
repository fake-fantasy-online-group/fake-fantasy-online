using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class SysOptionDlg
{
    private void OnGUI()
    {
        if (!visible) return;
        if (!tried) Load();
        s = UIScale.Factor;
        if (MobileClientPresentation.Enabled)
        {
            Rect layout = MobileLayout(out s);
            winX = layout.x;
            winY = layout.y;
            _wdGen = WinDrag.Gen;
        }
        else if (WinDrag.Stale(winX, ref _wdGen))
        {
            winX = (Screen.width - NW * s) * 0.5f;
            winY = (Screen.height - NH * s) * 0.5f;
        }
        Vector2 mouse = Event.current.mousePosition;
        GUI.depth = 2;
        if (bg != null) GUI.DrawTexture(L(0f, 0f, NW, NH), bg);
        else
        {
            GUI.color = new Color(0.1f, 0.09f, 0.13f, 0.98f);
            GUI.DrawTexture(L(0f, 0f, NW, NH), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        if (PlayerDead)
        {
            if (BtnImg(L(18f, 27f, 152f, 33f), bRelive, "复活", mouse))
            {
                if (NetClient.I != null) NetClient.I.ReviveForIndependentRuntime(0);
                visible = false;
            }
        }
        else
        {
            if (BtnImg(L(18f, 27f, 152f, 33f), bReturn, "返回游戏", mouse)) visible = false;
            if (BtnImg(L(18f, 60f, 152f, 33f), bSelPlayer, "选择角色", mouse))
            {
                visible = false;
                if (GameBootstrap.I != null) GameBootstrap.I.ReturnToTitle(true);
            }
            if (BtnImg(L(18f, 93f, 152f, 33f), bSelServer, "选择服务器", mouse))
            {
                visible = false;
                if (GameBootstrap.I != null) GameBootstrap.I.ReturnToTitle(false);
            }
            if (BtnImg(L(18f, 126f, 152f, 33f), bSysConfig, "系统设置", mouse))
            {
                SysConfigDlg.Toggle();
                visible = false;
            }
        }
        if (BtnImg(L(18f, 165f, 152f, 33f), bQuit, "退出游戏", mouse)) GameBootstrap.QuitGame();
        WinDrag.Handle(this, L(0f, 0f, NW, 28f), ref winX, ref winY);
        if (Event.current.type == EventType.MouseDown && L(0f, 0f, NW, NH).Contains(mouse) && !SysMsgBox.Visible)
            Event.current.Use();
    }
}

}
