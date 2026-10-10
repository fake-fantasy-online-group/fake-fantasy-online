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
            _e.hover.textColor = Color.white;
            _e.active.textColor = Color.white;
            _e.focused.textColor = Color.white;
        }
        _e.fontSize = Mathf.RoundToInt(13f * s);
        return _e;
    }

    private void OnGUI()
    {
        if (!visible) return;
        Color previousColor = GUI.color;
        int previousDepth = GUI.depth;
        try { DrawWindow(); }
        finally
        {
            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }
    }

    private void DrawWindow()
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
        ArtworkSkin.Panel(L(0f, 0f, NW, h), s);
        ArtworkSkin.Panel(L(2f, 2f, 258f, 18f), s, true);
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
        ArtworkSkin.Input(new Rect(input.x - 2f * s, input.y - 2f * s, input.width + 4f * s, input.height + 4f * s),
            s, GUI.GetNameOfFocusedControl() == "sec_ask");
        GUI.SetNextControlName("sec_ask");
        code = GUI.PasswordField(new Rect(input.x + 5f * s, input.y + 2f * s, input.width - 10f * s, input.height - 4f * s),
            code ?? "", '*', 32, E());
        Rect confirm = L(63f, h - 26f, 60f, 18f);
        Rect cancel = L(139f, h - 26f, 60f, 18f);
        bool confirmHovered = confirm.Contains(mouse);
        bool cancelHovered = cancel.Contains(mouse);
        ArtworkSkin.ButtonFace(confirm, "确定", s, selected: confirmHovered,
            pressed: confirmHovered && Input.GetMouseButton(0));
        ArtworkSkin.ButtonFace(cancel, "取消", s, selected: cancelHovered,
            pressed: cancelHovered && Input.GetMouseButton(0));
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
