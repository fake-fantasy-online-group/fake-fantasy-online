using System;
using System.Text;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

namespace FakeFantasyOnline.Presentation
{

/// <summary>
/// The one page the official <c>TitleFlow.state</c> (+0x28) selects. 0 is the login form, 1 the
/// registration form, 2 the password-recovery form, 3 the appeal form, 4 server selection,
/// 5 character selection and 6 character creation. OnGUI (0x06000ec7) dispatches from this
/// single title-owned field; the application's connection/session stage does not select a page.
/// </summary>
internal enum TitlePage
{
    Login,
    Register,
    Recover,
    Appeal,
    ServerSelection,
    CharacterSelection,
    CharacterCreation
}

/// <summary>
/// Faithful static helpers shared by the actual TitleFlow and qualified diagnostics.
/// Stateful GUI bodies, fields, and automatic dispatch belong to TitleFlow.
/// </summary>
internal static partial class RecoveredTitleLoginSurface
{
    private const string RememberAccountKey = "login_remember_acc";
    private const string RememberPasswordKey = "login_remember_pwd";
    private const string SavedAccountKey = "login_saved_acc";
    private const string SavedPasswordKey = "login_saved_pwd";

    internal static bool TryLoadRememberedCredentialPair(
        out string account,
        out string password)
    {
        account = string.Empty;
        password = string.Empty;
        if (PlayerPrefs.GetInt(RememberAccountKey, 0) == 0 ||
            PlayerPrefs.GetInt(RememberPasswordKey, 0) == 0 ||
            !PlayerPrefs.HasKey(SavedAccountKey) ||
            !PlayerPrefs.HasKey(SavedPasswordKey))
        {
            return false;
        }

        account = PlayerPrefs.GetString(SavedAccountKey, string.Empty);
        password = Deobfuscate(
            PlayerPrefs.GetString(SavedPasswordKey, string.Empty));
        if (!string.IsNullOrWhiteSpace(account) &&
            !string.IsNullOrEmpty(password))
        {
            return true;
        }

        account = string.Empty;
        password = string.Empty;
        return false;
    }

    internal static bool DrawImageButton(Rect rect, Texture2D texture)
    {
        if (texture)
        {
            bool hover = GUI.enabled && rect.Contains(Event.current.mousePosition);
            ArtworkSkin.SingleImageFace(rect, texture, hover,
                hover && Input.GetMouseButton(0));
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        return GUI.Button(rect, GUIContent.none, GUI.skin.button);
    }

    internal static string Obfuscate(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(value);
        for (int index = 0; index < bytes.Length; index++)
        {
            bytes[index] ^= (byte)((index & 7) + 0x5a);
        }

        return Convert.ToBase64String(bytes);
    }

    internal static string Deobfuscate(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        try
        {
            byte[] bytes = Convert.FromBase64String(value);
            for (int index = 0; index < bytes.Length; index++)
            {
                bytes[index] ^= (byte)((index & 7) + 0x5a);
            }

            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return string.Empty;
        }
    }

    internal static Texture2D MakeRoundTexture(
        int radius,
        int borderWidth,
        Color fill,
        Color border,
        Color? inner)
    {
        // Official 1.5.8 TitleFlow.MakeRoundTex (06000ed8) keeps a two-pixel centre.
        // Both native bodies set wrapMode, then filterMode; neither writes hideFlags.
        int size = radius * 2 + 2;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        Color[] pixels = new Color[size * size];
        float fillRadius = radius - borderWidth;
        float innerRingStart = radius - borderWidth - 1;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int centerX = x < radius ? radius : Mathf.Min(x, radius + 1);
                int centerY = y < radius ? radius : Mathf.Min(y, radius + 1);
                float dx = (float)x - centerX;
                float dy = (float)y - centerY;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                Color pixel = Color.clear;
                if (distance <= radius)
                {
                    pixel = distance <= fillRadius ? fill : border;
                    if (inner.HasValue &&
                        distance <= fillRadius &&
                        distance > innerRingStart)
                    {
                        pixel = inner.Value;
                    }
                }

                pixels[y * size + x] = pixel;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    internal static float EstimateTextWidth(string text, float fontSize)
    {
        float width = 0f;
        foreach (char character in text)
        {
            width += character > 127 ? fontSize : fontSize * 0.55f;
        }

        return width;
    }

    internal static System.Collections.Generic.List<string> WrapEstimatedText(string text, float fontSize, float maxWidth)
    {
        // Official 1.5.8 TitleFlow.WrapEst (0x06000ed6) strips CR before it splits: arm64
        // 06000ed6.c:44-46 calls String.Replace(text, metadata_usage_10438_kind5,
        // metadata_usage_10335_kind5) and only then String.Split(10, 0); the frozen index resolves
        // 10438 to "\r" and 10335 to "" (both decode_state exact), and x86_64 :42-43 is the same
        // pair. So CR never reaches the per-character width sum and only '\n' starts a new line.
        string[] paragraphs = text.Replace("\r", string.Empty).Split(new[] { '\n' }, StringSplitOptions.None);
        System.Collections.Generic.List<string> lines =
            new System.Collections.Generic.List<string>();
        float latinWidth = fontSize * 0.55f;
        for (int paragraphIndex = 0; paragraphIndex < paragraphs.Length; paragraphIndex++)
        {
            string paragraph = paragraphs[paragraphIndex];
            if (paragraph.Length == 0)
            {
                lines.Add(string.Empty);
                continue;
            }

            int start = 0;
            float width = 0f;
            for (int index = 0; index < paragraph.Length; index++)
            {
                float characterWidth = paragraph[index] > 127 ? fontSize : latinWidth;
                if (width + characterWidth > maxWidth && start < index)
                {
                    lines.Add(paragraph.Substring(start, index - start));
                    start = index;
                    width = 0f;
                }

                width += characterWidth;
            }

            lines.Add(paragraph.Substring(start));
        }

        return lines;
    }

    internal static void ClearStyleBackgrounds(GUIStyle style)
    {
        SetAllStyleBackgrounds(style, null);
    }

    internal static void SetAllStyleBackgrounds(GUIStyle style, Texture2D texture)
    {
        style.normal.background = texture;
        style.hover.background = texture;
        style.active.background = texture;
        style.focused.background = texture;
        style.onNormal.background = texture;
        style.onHover.background = texture;
        style.onActive.background = texture;
        style.onFocused.background = texture;
    }

    internal static void SetAllTextColors(GUIStyle style, Color color)
    {
        style.normal.textColor = color;
        style.hover.textColor = color;
        style.active.textColor = color;
        style.focused.textColor = color;
        style.onNormal.textColor = color;
        style.onHover.textColor = color;
        style.onActive.textColor = color;
        style.onFocused.textColor = color;
    }

    internal static void DestroyTexture(Texture2D texture)
    {
        if (!texture)
        {
            return;
        }

#if UNITY_EDITOR
        if (!UnityEngine.Application.isPlaying)
        {
            UnityEngine.Object.DestroyImmediate(texture);
            return;
        }
#endif
        UnityEngine.Object.Destroy(texture);
    }
}
}
