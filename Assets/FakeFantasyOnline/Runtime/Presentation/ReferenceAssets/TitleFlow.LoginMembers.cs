using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class TitleFlow
{
    private const float VirtualWidth = 800f;
    private const float VirtualHeight = 600f;
    private const float ButtonWidth = 152f;
    private const float ButtonHeight = 33f;

    // Official 1.5.8 06000edc: the private three-argument overload fixes only width.
    private bool RegBtn(float x, float y, string label)
    {
        return RegBtn(x, y, 152f, label);
    }

    // Official 1.5.8 06000edd, shared with the existing update-gate buttons.
    private bool RegBtn(float x, float y, float w, string label)
    {
        return ArtworkSkin.Button(new Rect(x, y, w, 33f), label, fontScale: 1.15f);
    }

    internal GUIStyle _fieldStyle;
    internal GUIStyle _loginLabel;
    internal GUIStyle _hintStyle;
    private GUIStyle _noticeTitleStyle;
    private GUIStyle _noticeTextStyle;
    internal bool _noticeOpen;
    internal float _noticeScroll;
    private Changelog _noticeContentSource;
    private GUIContent _noticeContent;
    private string _hintText;
    private float _hintWidth;
    private System.Collections.Generic.List<string> _hintLines;
    internal bool _changelogTried;
    private Changelog _changelog;

    /// <summary>
    /// Official 1.5.8 TitleFlow.LoadBakedChangelog (06000eca). Resource key, the null
    /// branch, the parse target and the PublishJson argument are all native: the text is
    /// read twice, once for TitleFlow.Changelog and once for the notice board.
    /// </summary>
    internal void LoadBakedChangelog()
    {
        TextAsset asset = Resources.Load<TextAsset>("changelog");
        if (asset == null)
        {
            return;
        }

        _changelog = JsonUtility.FromJson<Changelog>(asset.text);
        NoticeBoard.PublishJson(asset.text);
    }

    internal void DrawUpdateNotice()
    {
        Color previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(0f, 0f, VirtualWidth, VirtualHeight), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.BeginGroup(new Rect(170f, 85f, 460f, 430f));
        ArtworkSkin.Panel(new Rect(0f, 0f, 460f, 430f));

        EnsureNoticeStyles();

        // Official DrawUpdateNotice (06000ecc): "更新公告  v" + the loaded changelog
        // version, falling back to the baked literal "1.5.8" when there is none.
        // Official 1.6.0 DrawUpdateNotice (06000ed8) changes that fallback to "1.6.0".
        string version = _changelog != null && !string.IsNullOrEmpty(_changelog.version)
            ? _changelog.version
            : "1.6.0";
        GUI.Label(new Rect(0f, 12f, 460f, 26f), "更新公告  v" + version, _noticeTitleStyle);

        if (_noticeContent == null || !ReferenceEquals(_noticeContentSource, _changelog))
        {
            _noticeContentSource = _changelog;
            string noticeBody = _changelog == null || _changelog.notes == null || _changelog.notes.Length < 1
                ? "(暂无公告内容)"
                : "· " + string.Join("\n\n· ", Array.ConvertAll(_changelog.notes, NoticeBoard.Rich));
            _noticeContent = new GUIContent(noticeBody);
        }
        GUIContent content = _noticeContent;
        float contentHeight = _noticeTextStyle.CalcHeight(content, 416f);
        Event current = Event.current;
        Rect scrollRegion = new Rect(22f, 48f, 416f, 326f);
        if (current.type == EventType.ScrollWheel && scrollRegion.Contains(current.mousePosition))
        {
            float maximum = Mathf.Max(0f, contentHeight - 326f);
            _noticeScroll = Mathf.Clamp(
                _noticeScroll + current.delta.y * 16f,
                0f,
                maximum);
            current.Use();
        }

        GUI.BeginGroup(scrollRegion);
        GUI.Label(
            new Rect(0f, -_noticeScroll, 416f, contentHeight),
            content,
            _noticeTextStyle);
        GUI.EndGroup();

        if (contentHeight > 326f)
        {
            Texture2D[] thumb = ArtworkSkin.Frames("res/uires/common/slidebtn.gsa", 1);
            float thumbY = 48f + Mathf.Clamp01(_noticeScroll / (contentHeight - 326f)) * 306f;
            Rect thumbRect = new Rect(441f, thumbY, 15f, 20f);
            if (thumb != null && thumb[0] != null) ArtworkSkin.IconFace(thumbRect, thumb);
            else ArtworkSkin.Input(thumbRect);
        }

        if (ArtworkSkin.Button(new Rect(154f, 384f, ButtonWidth, ButtonHeight), "关 闭", fontScale: 1.15f))
        {
            _noticeOpen = false;
        }

        GUI.EndGroup();

        if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
        {
            _noticeOpen = false;
            current.Use();
        }

        GUI.color = previousColor;
    }

    private void EnsureNoticeStyles()
    {
        if (_noticeTitleStyle != null)
        {
            return;
        }

        _noticeTitleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };
        _noticeTitleStyle.normal.textColor = new Color(1f, 0.82f, 0.4f, 1f);

        _noticeTextStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 14,
            wordWrap = true,

            // Official DrawUpdateNotice sets richText on the body style; the note text runs
            // through NoticeBoard.Rich and carries <b> markup.
            richText = true
        };
        _noticeTextStyle.normal.textColor = new Color(0.92f, 0.9f, 0.85f, 1f);

        // Official DrawUpdateNotice (06000ecc) writes clipping last, body style first and title
        // style second (arm64 GUIStyle.set_clipping 0x108adfc twice; x86_64 the same two setter
        // positions, FUN_0107ac90). No style background is cleared here: the native body only
        // allocates from GUI.skin.label and sets the six members above plus clipping.
        _noticeTextStyle.clipping = TextClipping.Overflow;
        _noticeTitleStyle.clipping = TextClipping.Overflow;
    }

    internal bool LoginCheck(Rect fieldBox, bool on, string tip)
    {
        float x = fieldBox.x + fieldBox.width + 8f;
        float y = fieldBox.y + (fieldBox.height - 13f) * 0.5f;
        Texture2D texture = on ? chkOn : chkOff;
        if (texture)
        {
            GUI.DrawTexture(new Rect(x, y, 14f, 13f), texture);
        }
        else
        {
            ArtworkSkin.Input(new Rect(x, y, 14f, 13f));
            if (on)
            {
                Color previous = GUI.color;
                GUI.color = new Color(1f, 0.85f, 0.35f, 1f);
                GUI.DrawTexture(new Rect(x + 3f, y + 3f, 8f, 7f), Texture2D.whiteTexture);
                GUI.color = previous;
            }
        }

        Rect hit = new Rect(x - 3f, y - 3f, 20f, 19f);
        Event current = Event.current;
        if (!hit.Contains(current.mousePosition))
        {
            return on;
        }

        DrawHintBox(x + 74f, y - 22f, tip, 200f);

        // Official TitleFlow.LoginCheck (06000ed4) tests `Event.get_type() == 0` before
        // `Event.get_button() == 0` - arm64 :167-173 (get_type 1071a70, get_button 1071164) and
        // x86_64 :142-146 (FUN_01062260, FUN_01061ae0); both architectures then flip with `uVar5 ^ 1`
        // and call Event.Use. Zero is EventType.MouseDown: the same ordinal scale is already frozen
        // in this repo for Win05.OnGUI 06001042 (type 0 = MouseDown, 1 = MouseUp, 3 = MouseDrag),
        // so the box toggles on press, not on release.
        if (current.type == EventType.MouseDown && current.button == 0)
        {
            current.Use();
            return !on;
        }

        return on;
    }

    internal string DrawMaskedField(Rect rect, string value)
    {
        string mask = new string('\u25cf', value.Length);
        string edited = GUI.TextField(rect, mask, 40, _fieldStyle);
        if (string.Equals(edited, mask, StringComparison.Ordinal))
        {
            return value;
        }

        System.Text.StringBuilder rebuilt = new System.Text.StringBuilder();
        for (int index = 0; index < edited.Length; index++)
        {
            rebuilt.Append(edited[index] == '\u25cf' && index < value.Length
                ? value[index]
                : edited[index]);
        }

        return rebuilt.ToString();
    }

    internal void DrawHintBox(float centerX, float y, string text, float maxWidth)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (_hintLines == null || _hintText != text || _hintWidth != maxWidth)
        {
            _hintText = text;
            _hintWidth = maxWidth;
            _hintLines = RecoveredTitleLoginSurface.WrapEstimatedText(text, 13f, maxWidth - 30f);
        }
        System.Collections.Generic.List<string> lines = _hintLines;
        float textWidth = 0f;
        for (int index = 0; index < lines.Count; index++)
        {
            textWidth = Mathf.Max(textWidth, RecoveredTitleLoginSurface.EstimateTextWidth(lines[index], 13f));
        }

        float width = Mathf.Clamp(textWidth + 30f, 60f, maxWidth);
        float height = Mathf.Max(24f, lines.Count * 19f + 10f);
        float x = Mathf.Round(centerX - width * 0.5f);
        float top = Mathf.Round(y);
        Color previous = GUI.color;
        ArtworkSkin.Panel(new Rect(x, top, width, height));
        for (int index = 0; index < lines.Count; index++)
        {
            GUI.Label(
                new Rect(x, top + 5f + index * 19f, width, 19f),
                lines[index],
                _hintStyle);
        }

        GUI.color = previous;
    }

    /// <summary>
    /// Official 1.5.8 TitleFlow.FormRow (0x06000eda, arm64 0x38e37c / x86_64 0x3a1990).
    /// </summary>
    internal void FormRow(float px, float pw, float y, string label, ref string val, bool pwd)
    {
        // GUI.Label(px + 40, y, 100, 24, label, style +0x2a8): the gold bold 15px label.
        GUI.Label(new Rect(px + 40f, y, 100f, 24f), label, _loginLabel);
        // GUI.Box(px + 146, y, pw - 186, 24, GUIContent.none, style +0x2b8): the 6px input frame,
        // i.e. _inputStyle (MakeRoundTex(0.16,0.15,0.13,1 / 0,0,0,0, 6, 0), border 6).
        ArtworkSkin.Input(new Rect(px + 146f, y, pw - 186f, 24f),
            focused: GUI.GetNameOfFocusedControl() == label);

        Rect field = new Rect(px + 146f + 6f, y, pw - 186f - 12f, 24f);
        GUI.SetNextControlName(label);
        if (pwd)
        {
            // MaskField (0x06000ed9): the bullet string has the length of the val and the
            // field is offered with maxLength 40 (immediate 0x28), the same helper the title
            // already uses for the login password row.
            val = DrawMaskedField(field, val);
            return;
        }

        // GUI.TextField(rect, val, 24, style +0x290): 0x18 is the official maxLength of the
        // three plain rows - account, security code and invite code.
        val = GUI.TextField(field, val, 24, _fieldStyle);
    }

    /// <summary>
    /// Official 1.5.8 TitleFlow+Changelog (0x02000267): version (+0x10) and notes (+0x18),
    /// the parse target of LoadBakedChangelog and the source DrawUpdateNotice renders.
    /// </summary>
    [Serializable]
    private sealed class Changelog
    {
        public string version;
        public string[] notes;
    }

}
}
