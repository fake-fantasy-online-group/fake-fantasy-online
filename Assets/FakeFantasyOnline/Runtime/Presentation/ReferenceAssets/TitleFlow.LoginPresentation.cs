using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class TitleFlow
{
    private Texture2D _nativeLoginPanel;
    private Texture2D _nativeLoginPanelSource;
    private Texture2D _nativeLoginCheckOff, _nativeLoginCheckOn;
    private ResLoader _nativeLoginLoader;
    private object _nativeLoginPackage;
    private readonly GUIStyle[] _nativeLoginButtons = new GUIStyle[3];
    private GUIStyle _nativeLoginFallbackLabel;

    private void EnsureNativeLoginPresentation()
    {
        ResLoader loader = ResLoader.I;
        object package = loader == null ? null : loader.Pkg;
        Texture2D panel = _loginPanel;
        if (ReferenceEquals(_nativeLoginLoader, loader) &&
            ReferenceEquals(_nativeLoginPackage, package) &&
            ReferenceEquals(_nativeLoginPanelSource, panel) && _nativeLoginFallbackLabel != null)
            return;

        ReleaseNativeLoginPresentation();
        _nativeLoginLoader = loader;
        _nativeLoginPackage = package;
        _nativeLoginPanelSource = panel;
        _nativeLoginFallbackLabel = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Normal,
            padding = new RectOffset(),
            alignment = TextAnchor.MiddleLeft
        };
        _nativeLoginFallbackLabel.normal.textColor = new Color(.67f, .67f, .67f, 1f);

        // This is an in-memory presentation copy, never an edited package or bundled original
        // asset. The inspected 05diff panel has native 317 x 131 geometry and baked labels.
        _nativeLoginPanel = BuildNativeMemoryPanel(panel);
        if (loader == null || loader.Pkg == null) return;
        string[] names = { "loginbtn", "candidatebtn", "quitbtn" };
        for (int index = 0; index < names.Length; index++)
        {
            string path = "res/uires/titleui/" + names[index] + ".gsa";
            // Assembled frames also handle legacy 32-pixel-tiled button records correctly.
            Texture2D normal = loader.LoadGsaFrameA(path, 0);
            if (!normal || GsaTextureScale.Width(normal) != ButtonWidth ||
                GsaTextureScale.Height(normal) != ButtonHeight) continue;
            Texture2D hover = loader.LoadGsaFrameA(path, 1);
            Texture2D pressed = loader.LoadGsaFrameA(path, 2);
            GUIStyle style = new GUIStyle
            {
                border = new RectOffset(),
                margin = new RectOffset(),
                padding = new RectOffset(),
                overflow = new RectOffset(),
                fixedWidth = 0f,
                fixedHeight = 0f,
                imagePosition = ImagePosition.ImageOnly
            };
            style.normal.background = normal;
            style.hover.background = hover ? hover : normal;
            style.active.background = pressed ? pressed : normal;
            style.focused.background = style.hover.background;
            _nativeLoginButtons[index] = style;
        }
        _nativeLoginCheckOff = loader.LoadGsaFrameA("res/uires/titleui/check.gsa", 0);
        _nativeLoginCheckOn = loader.LoadGsaFrameA("res/uires/titleui/check.gsa", 1);
    }

    private bool NativeLoginButton(Rect rect, int index, string fallbackLabel)
    {
        GUIStyle style = _nativeLoginButtons[index];
        // Text is already baked in all original state images. GUI.Button retains normal
        // press/release, drag cancellation, keyboard activation and disabled semantics.
        return style != null
            ? GUI.Button(rect, GUIContent.none, style)
            : GUI.Button(rect, fallbackLabel, _regButtonStyle);
    }

    private void DrawNativeLoginPanel()
    {
        if (_nativeLoginPanel)
        {
            GUI.DrawTexture(new Rect(237f, 348f, 317f, 131f), _nativeLoginPanel);
            return;
        }

        // Missing or incompatible external artwork must not show the old invisible-login
        // label under a remember-password control. Keep a usable, explicitly plain fallback.
        GUI.Box(new Rect(237f, 348f, 317f, 131f), GUIContent.none, _panelStyle);
        GUI.Label(new Rect(370f, 372f, 60f, 18f), "帐号输入", _nativeLoginFallbackLabel);
        GUI.Label(new Rect(370f, 409f, 60f, 18f), "个人密码", _nativeLoginFallbackLabel);
        GUI.Box(new Rect(319f, 392f, 150f, 22f), GUIContent.none, _inputStyle);
        GUI.Box(new Rect(319f, 429f, 150f, 22f), GUIContent.none, _inputStyle);
        GUI.Label(new Rect(337f, 449f, 52f, 16f), "记忆帐号", _nativeLoginFallbackLabel);
        GUI.Label(new Rect(419f, 449f, 52f, 16f), "记忆密码", _nativeLoginFallbackLabel);
    }

    private bool NativeLoginCheck(Rect hit, bool on)
    {
        int control = GUIUtility.GetControlID(FocusType.Keyboard, hit);
        Texture2D texture = on ? _nativeLoginCheckOn : _nativeLoginCheckOff;
        Rect icon = new Rect(hit.x, hit.y, 12f, 12f);
        if (texture) GUI.DrawTexture(icon, texture);
        else
        {
            GUI.Box(icon, GUIContent.none, _inputStyle);
            if (on) GUI.DrawTexture(new Rect(hit.x + 3f, hit.y + 3f, 6f, 6f), Texture2D.whiteTexture);
        }

        Event current = Event.current;
        if (!GUI.enabled || current == null) return on;
        // Preserve the original checkbox's left-MouseDown timing, with the visible label
        // included in the target. Keyboard focus/Space also makes the new row navigable.
        if (current.type == EventType.MouseDown && current.button == 0 && hit.Contains(current.mousePosition))
        {
            GUIUtility.keyboardControl = control;
            current.Use();
            return !on;
        }
        if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Space &&
            GUIUtility.keyboardControl == control)
        {
            current.Use();
            return !on;
        }
        return on;
    }

    private static Texture2D BuildNativeMemoryPanel(Texture2D source)
    {
        if (!source || !source.isReadable ||
            GsaTextureScale.Width(source) != 317f || GsaTextureScale.Height(source) != 131f)
            return null;
        int scale = GsaTextureScale.Of(source);
        int width = source.width, height = source.height;
        Color32[] original = source.GetPixels32();
        Color32[] pixels = (Color32[])original.Clone();
        // Coordinates below are native, top-down pixels. Replicate the original empty pixel
        // in EACH row so the panel transparency/gradient survives removal of 隐身登录.
        for (int y = 101 * scale; y < 117 * scale; y++)
            for (int x = 166 * scale; x < 233 * scale; x++)
                pixels[(height - 1 - y) * width + x] = original[(height - 1 - y) * width + 237 * scale];

        Color32[] ink = new Color32[47 * 11];
        for (int y = 0; y < 11; y++)
        {
            Color32 gray = NativeMemoryRowColor(original, width, height, scale, y + 103);
            for (int x = 0; x < 47; x++)
            {
                Color32 sample;
                if (x < 23)
                {
                    sample = NativePanelPixel(original, width, height, scale, 100 + x, 103 + y);
                    if (IsNativeMemoryInk(sample)) ink[y * 47 + x] = sample;
                }
                else if ((x >= 24 && x < 35) || x >= 36)
                {
                    // 密/码 are copied from this same panel's existing 个人密码 label.
                    sample = NativePanelPixel(original, width, height, scale, 157 + x - 24, 66 + y);
                    if (sample.r >= 220 && sample.g >= 100 && sample.a >= 220)
                        ink[y * 47 + x] = gray;
                }
            }
        }
        // Native hard pixel strokes, 12px character advance and a one-pixel black edge.
        for (int y = 0; y < 11; y++)
            for (int x = 0; x < 47; x++)
                if (ink[y * 47 + x].a != 0)
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                            SetNativePanelPixel(pixels, width, height, scale, 182 + x + dx, 103 + y + dy,
                                new Color32(0, 0, 0, 255));
        for (int y = 0; y < 11; y++)
            for (int x = 0; x < 47; x++)
                if (ink[y * 47 + x].a != 0)
                    SetNativePanelPixel(pixels, width, height, scale, 182 + x, 103 + y, ink[y * 47 + x]);

        Texture2D panel = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "Runtime login memory panel",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        panel.SetPixels32(pixels);
        panel.Apply(false, false);
        GsaTextureScale.Register(panel, scale);
        return panel;
    }

    private static bool IsNativeMemoryInk(Color32 color)
    {
        return color.a >= 220 && color.r >= 120 &&
            Math.Abs(color.r - color.g) < 35 && Math.Abs(color.r - color.b) < 35;
    }

    private static Color32 NativeMemoryRowColor(Color32[] pixels, int width, int height, int scale, int y)
    {
        // Original gray letters have a constant ink color per row; use their actual pixels,
        // not a guessed font or a separately bundled raster mask.
        for (int x = 100; x < 149; x++)
        {
            Color32 sample = NativePanelPixel(pixels, width, height, scale, x, y);
            if (IsNativeMemoryInk(sample)) return sample;
        }
        return new Color32(170, 170, 170, 255);
    }

    private static Color32 NativePanelPixel(Color32[] pixels, int width, int height, int scale, int x, int y)
    {
        return pixels[(height - 1 - y * scale) * width + x * scale];
    }

    private static void SetNativePanelPixel(Color32[] pixels, int width, int height, int scale,
        int x, int y, Color32 color)
    {
        for (int dy = 0; dy < scale; dy++)
            for (int dx = 0; dx < scale; dx++)
                pixels[(height - 1 - (y * scale + dy)) * width + x * scale + dx] = color;
    }

    private void ReleaseNativeLoginPresentation()
    {
        RecoveredTitleLoginSurface.DestroyTexture(_nativeLoginPanel);
        _nativeLoginPanel = null;
        _nativeLoginPanelSource = null;
        _nativeLoginLoader = null;
        _nativeLoginPackage = null;
        _nativeLoginCheckOff = _nativeLoginCheckOn = null;
        _nativeLoginFallbackLabel = null;
        Array.Clear(_nativeLoginButtons, 0, _nativeLoginButtons.Length);
        // ResLoader owns all state-frame/checkbox textures; never destroy its cache here.
    }
}
}
