using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class TitleFlow
{
    private static readonly string[] RegistrationLabels = { "注册新帐号", "帐号", "密码", "确认密码", "安全码" };
    private readonly Texture2D[] _registrationLabels = new Texture2D[5];
    private readonly GUIStyle[] _registrationButtons = new GUIStyle[2];
    private Texture2D _registrationPanel, _registrationInput, _registrationLoginSource;
    private ResLoader _registrationLoader;
    private object _registrationPackage, _registrationPackage24, _registrationPackage21;
    private GUIStyle _registrationFallbackLabel;

    private void EnsureRegistrationPresentation()
    {
        ResLoader loader = ResLoader.I;
        Texture2D login = _loginPanel;
        if (_registrationFallbackLabel != null && ReferenceEquals(_registrationLoader, loader) &&
            ReferenceEquals(_registrationLoginSource, login) &&
            ReferenceEquals(_registrationPackage, loader?.Pkg) &&
            ReferenceEquals(_registrationPackage24, loader?.Pkg24) &&
            ReferenceEquals(_registrationPackage21, loader?.PkgFf21)) return;

        ReleaseRegistrationPresentation();
        _registrationLoader = loader;
        _registrationLoginSource = login;
        _registrationPackage = loader?.Pkg;
        _registrationPackage24 = loader?.Pkg24;
        _registrationPackage21 = loader?.PkgFf21;
        _registrationFallbackLabel = new GUIStyle(GUI.skin.label)
        { fontSize = 12, fontStyle = FontStyle.Normal, alignment = TextAnchor.MiddleCenter, padding = new RectOffset() };
        _registrationFallbackLabel.normal.textColor = new Color(1f, .75f, .3f, 1f);

        TextAsset subset = Resources.Load<TextAsset>(RegistrationPixelArt.SubsetResource);
        var glyphs = RegistrationPixelArt.ReadSubset(subset ? subset.text : null);
        Color32[] gradient = RegistrationPixelArt.AddNativeGlyphs(login, glyphs);
        for (int i = 0; i < _registrationLabels.Length; i++)
            _registrationLabels[i] = RegistrationPixelArt.BuildLabel(RegistrationLabels[i], i == 0 ? 4 : 0, glyphs, gradient);
        if (loader == null || loader.Pkg == null) return;

        _registrationPanel = RegistrationPixelArt.BuildPanel(
            loader.LoadGsaFrameA("res/uires/titleui/topbar.gsa", 0),
            loader.LoadGsaFrameA("res/uires/titleui/middlebar.gsa", 0),
            loader.LoadGsaFrameA("res/uires/titleui/bottombar.gsa", 0));
        _registrationInput = loader.LoadGsaFrameA("res/uires/sysdlg/editbk.gsa", 0);
        if (_registrationInput && (GsaTextureScale.Width(_registrationInput) != 166 ||
            GsaTextureScale.Height(_registrationInput) != 24)) _registrationInput = null;
        string[] buttons = { "completebtn", "returnbtn" };
        for (int i = 0; i < buttons.Length; i++)
        {
            string path = "res/uires/titleui/" + buttons[i] + ".gsa";
            Texture2D normal = loader.LoadGsaFrameA(path, 0);
            if (!normal || GsaTextureScale.Width(normal) != 152 || GsaTextureScale.Height(normal) != 33) continue;
            Texture2D hover = loader.LoadGsaFrameA(path, 1), active = loader.LoadGsaFrameA(path, 2);
            var style = new GUIStyle
            {
                border = new RectOffset(), margin = new RectOffset(), padding = new RectOffset(),
                overflow = new RectOffset(), imagePosition = ImagePosition.ImageOnly
            };
            style.normal.background = normal;
            style.hover.background = hover ? hover : normal;
            style.active.background = active ? active : normal;
            style.focused.background = style.hover.background;
            _registrationButtons[i] = style;
        }
    }

    private void DrawRegistrationPresentation()
    {
        EnsureRegistrationPresentation();
        Rect panel = new Rect(226f, 179f, 348f, 242f);
        if (_registrationPanel) GUI.DrawTexture(panel, _registrationPanel);
        else GUI.Box(panel, GUIContent.none, _panelStyle);
        DrawRegistrationLabel(new Rect(244f, 195f, 312f, 22f), 0);
        DrawRegistrationField(0, ref _registerAccount, false);
        DrawRegistrationField(1, ref _registerPassword, true);
        DrawRegistrationField(2, ref _registerPasswordConfirm, true);
        DrawRegistrationField(3, ref _registerSecurityCode, false);

        // The invitation field remains in the unchanged registration packet. Opening the
        // form clears it to empty, which the server accepts; it has no visible input row.
        if (DrawRegistrationImageButton(new Rect(242f, 371f, 152f, 33f), 0, "完成创建")) DoRegister();
        if (DrawRegistrationImageButton(new Rect(406f, 371f, 152f, 33f), 1, "返回上层"))
        {
            _page = TitlePage.Login;
            netMsg = string.Empty;
            mobileTitleInput.Close();
        }
        DrawHintBox(400f, 433f, netMsg, 560f);
    }

    private void DrawRegistrationLabel(Rect rect, int index)
    {
        Texture2D texture = _registrationLabels[index];
        if (!texture) { GUI.Label(rect, RegistrationLabels[index], _registrationFallbackLabel); return; }
        GUI.DrawTexture(new Rect(Mathf.Round(rect.x + (rect.width - texture.width) * .5f),
            Mathf.Round(rect.y + (rect.height - texture.height) * .5f) - 1f,
            texture.width, texture.height), texture);
    }

    private void DrawRegistrationField(int index, ref string value, bool secure)
    {
        float y = 228f + index * 31f;
        DrawRegistrationLabel(new Rect(254f, y, 100f, 24f), index + 1);
        Rect frame = new Rect(366f, y, 166f, 24f);
        if (_registrationInput) GUI.DrawTexture(frame, _registrationInput);
        else GUI.Box(frame, GUIContent.none, _inputStyle);
        Rect field = new Rect(371f, y, 156f, 24f);
        // 30px mobile touch targets fit the 31px row pitch without overlapping.
        if (MobileClientPresentation.Enabled)
        {
            field.y -= 3f;
            field.height += 6f;
            value = mobileTitleInput.Draw("register-" + index, field, value ?? string.Empty,
                secure ? 40 : 24, secure, _fieldStyle);
        }
        else value = secure ? DrawMaskedField(field, value ?? string.Empty)
            : GUI.TextField(field, value ?? string.Empty, 24, _fieldStyle);
    }

    private bool DrawRegistrationImageButton(Rect rect, int index, string fallback)
    {
        GUIStyle style = _registrationButtons[index];
        return style != null ? GUI.Button(rect, GUIContent.none, style) : GUI.Button(rect, fallback, _regButtonStyle);
    }

    private void ReleaseRegistrationPresentation()
    {
        // Loader-owned originals are shared. Only destroy our in-memory compositions.
        RecoveredTitleLoginSurface.DestroyTexture(_registrationPanel);
        _registrationPanel = null;
        for (int i = 0; i < _registrationLabels.Length; i++)
        {
            RecoveredTitleLoginSurface.DestroyTexture(_registrationLabels[i]);
            _registrationLabels[i] = null;
        }
        Array.Clear(_registrationButtons, 0, _registrationButtons.Length);
        _registrationInput = _registrationLoginSource = null;
        _registrationLoader = null;
        _registrationPackage = _registrationPackage24 = _registrationPackage21 = null;
        _registrationFallbackLabel = null;
    }
}
}
