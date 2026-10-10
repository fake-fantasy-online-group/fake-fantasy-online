using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class SecCodeAskDlg
{
    private GUIStyle E()
    {
        if (_e == null)
        {
            _e = new GUIStyle(GUIStyle.none);
            _e.font = NpcChatter.CnFont();
            _e.alignment = TextAnchor.MiddleLeft;
            _e.normal.textColor = Color.white;
        }
        _e.fontSize = Mathf.RoundToInt(13f * s);
        return _e;
    }

    private void Fill(Rect r, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private void OnGUI()
    {
        if (!visible) return;
        s = UIScale.Factor;
        if (!Mathf.Approximately(tipWrapScale, s)) RewrapTip();
        float h = NH;
        if (WinDrag.Stale(winX, ref _wdGen))
        {
            winX = (Screen.width - NW * s) * 0.5f;
            winY = (Screen.height - h * s) * 0.5f;
        }
        Vector2 mouse = Event.current.mousePosition;
        GUI.depth = 2;
        PopUI.Panel(L(0f, 0f, NW, h));
        Fill(L(2f, 2f, 258f, 18f), new Color(0.18f, 0.15f, 0.1f, 1f));
        if (BitmapFont.Ready)
        {
            BitmapFont.CenterOutline(L(0f, 5f, NW, 14f), "财产安全锁", new Color(1f, 0.9f, 0.55f, 1f), s);
            BitmapFont.CenterOutline(L(0f, 26f, NW, 12f), "解锁【" + BitmapFont.Elide(what, 202f * s, s) + "】",
                new Color(0.9f, 0.87f, 0.8f, 1f), s);
            if (tipLines != null)
                for (int i = 0; i < tipLines.Count; i++)
                    BitmapFont.CenterOutline(L(0f, 42f + i * 13f, NW, 12f), tipLines[i],
                        new Color(0.78f, 0.75f, 0.68f, 1f), s);
        }
        Rect input = L(46f, 60f + ExtraTipH, 170f, 20f);
        Fill(new Rect(input.x - 2f, input.y - 2f, input.width + 4f, input.height + 4f), new Color(0.62f, 0.5f, 0.26f, 1f));
        Fill(input, new Color(0.24f, 0.22f, 0.18f, 1f));
        GUI.SetNextControlName("sec_ask");
        code = GUI.PasswordField(new Rect(input.x + 5f, input.y + 2f, input.width - 10f, input.height - 4f),
            code ?? "", '*', 32, E());
        Rect confirm = L(63f, h - 26f, 60f, 18f);
        Rect cancel = L(139f, h - 26f, 60f, 18f);
        bool confirmHovered = confirm.Contains(mouse);
        bool cancelHovered = cancel.Contains(mouse);
        Fill(confirm, confirmHovered ? new Color(0.62f, 0.5f, 0.2f, 1f) : new Color(0.42f, 0.34f, 0.18f, 1f));
        Fill(cancel, cancelHovered ? new Color(0.55f, 0.3f, 0.25f, 1f) : new Color(0.38f, 0.28f, 0.24f, 1f));
        if (BitmapFont.Ready)
        {
            BitmapFont.CenterOutline(confirm, "确定", new Color(1f, 0.95f, 0.7f, 1f), s);
            BitmapFont.CenterOutline(cancel, "取消", new Color(0.92f, 0.86f, 0.8f, 1f), s);
        }
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && confirmHovered)
        { Confirm(); Event.current.Use(); }
        else if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && cancelHovered)
        { CloseNow(); Event.current.Use(); }
        else if (Event.current.type == EventType.KeyDown &&
            (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
        { Confirm(); Event.current.Use(); }
        else if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
        { CloseNow(); Event.current.Use(); }
        WinDrag.Handle(this, L(0f, 0f, NW, 20f), ref winX, ref winY);
        if (Event.current.type == EventType.MouseDown && L(0f, 0f, NW, h).Contains(mouse) && !SysMsgBox.Visible)
            Event.current.Use();
    }
}
}
