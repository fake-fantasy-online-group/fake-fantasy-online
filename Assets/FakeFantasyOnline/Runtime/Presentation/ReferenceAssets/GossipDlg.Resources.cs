using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class GossipDlg
{
    private void Load()
    {
        tried = true;
        ResLoader loader = ResLoader.I;
        if (loader == null) { tried = false; return; }
        bg = loader.LoadUI("ui/gossipdlg/gossipdlg.png");
        if (bg == null) bg = loader.LoadGsa("res/uires/gossipdlg/gossipdlg.gsa");
        bar0 = loader.LoadGsaFrameA("res/uires/gossipdlg/gossipbar.gsa", 0);
        bar1 = loader.LoadGsaFrameA("res/uires/gossipdlg/gossipbar.gsa", 1);
        icoClose = loader.LoadGsaFrame("res/uires/common/miniclose.gsa", 0);
        icoMin = loader.LoadGsaFrame("res/uires/common/minimizebtn.gsa", 0);
        icoVoice = loader.LoadGsaFrame("res/uires/gameui/fetchphonebtn.gsa", 0);
        icoRecord = loader.LoadGsaFrame("res/uires/gossipdlg/recordbtn.gsa", 0);
        icoFace = loader.LoadGsaFrame("res/uires/gossipdlg/facebtn.gsa", 0);
        icoSend = loader.LoadGsaFrame("res/uires/gossipdlg/sendbtn.gsa", 0);
    }
    private GUIStyle E()
    {
        if (_e == null)
        {
            _e = new GUIStyle(GUIStyle.none) { font = NpcChatter.CnFont(), alignment = TextAnchor.UpperLeft, wordWrap = true };
            _e.normal.textColor = Color.white;
        }
        _e.fontSize = Mathf.RoundToInt(12f * s);
        return _e;
    }
    private Rect FacePanel()
    {
        float width = EmotePicker.PanelW(s), height = EmotePicker.PanelH(s);
        return new Rect(winX + 42f * s, winY + 278f * s - height - 4f * s, width, height);
    }
    public static bool MouseOver()
    {
        if (I == null || !I.visible) return false;
        float scale = UIScale.Factor;
        float x = float.IsNaN(I.winX) ? (Screen.width - NW * scale) * 0.5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height - NH * scale) * 0.5f : I.winY;
        if (I.minimized) { x -= 3f * scale; y -= 3f * scale; }
        Vector2 pointer = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        if (!I.minimized && I.showFace && I.FacePanel().Contains(pointer)) return true;
        if (!I.minimized && I.stripRect.width > 0 && I.stripRect.Contains(pointer)) return true;
        return new Rect(x, y, I.CurW * scale, I.CurH * scale).Contains(pointer);
    }
}
}
