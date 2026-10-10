using System;
using System.Collections.Generic;
using System.Globalization;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// The four source-owned pieces selected by NpcChatter.BubblePieces.
/// </summary>
internal sealed class ReferenceWorldChatBubbleThemeAssets
{
    public ReferenceWorldChatBubbleThemeAssets(
        Texture2D left,
        Texture2D middle,
        Texture2D right,
        Texture2D pointer)
    {
        Left = left == null ? throw new ArgumentNullException(nameof(left)) : left;
        Middle = middle == null ? throw new ArgumentNullException(nameof(middle)) : middle;
        Right = right == null ? throw new ArgumentNullException(nameof(right)) : right;
        Pointer = pointer;
    }

    public Texture2D Left { get; }
    public Texture2D Middle { get; }
    public Texture2D Right { get; }
    public Texture2D Pointer { get; }
}

/// <summary>
/// Hash-locked yellow/red bubble pieces and the complete 01..96 EmoteText frame source.
/// </summary>
internal sealed class ReferenceWorldChatBubbleAssets
{
    public const int MinimumEmoteId = 1;
    public const int MaximumEmoteId = 96;
    public const string EmoteDirectory = "res/biaoqing/";

    private readonly ReferenceResourceLoader resources;
    private readonly Dictionary<int, Texture2D[]> emoteFrames =
        new Dictionary<int, Texture2D[]>();

    private ReferenceWorldChatBubbleAssets(
        ReferenceResourceLoader resources,
        ReferenceWorldChatBubbleThemeAssets yellow,
        ReferenceWorldChatBubbleThemeAssets red)
    {
        this.resources = resources;
        Yellow = yellow;
        Red = red;
    }

    public ReferenceWorldChatBubbleThemeAssets Yellow { get; }
    public ReferenceWorldChatBubbleThemeAssets Red { get; }

    public ReferenceWorldChatBubbleThemeAssets ThemeFor(byte selector)
    {
        return selector == 1 ? Red : Yellow;
    }

    public Texture2D[] FramesFor(int emoteId)
    {
        if (emoteId < MinimumEmoteId || emoteId > MaximumEmoteId)
        {
            return null;
        }

        Texture2D[] cached;
        if (emoteFrames.TryGetValue(emoteId, out cached))
        {
            return cached;
        }

        string path = EmotePath(emoteId);
        int count = resources.GsaFrameCount(path);
        if (count < 1)
        {
            emoteFrames[emoteId] = null;
            return null;
        }

        Texture2D[] loaded = new Texture2D[count];
        for (int frame = 0; frame < count; frame++)
        {
            loaded[frame] = resources.LoadGsaFrameA(path, frame);
            if (!loaded[frame])
            {
                emoteFrames[emoteId] = null;
                return null;
            }
        }

        emoteFrames[emoteId] = loaded;
        return loaded;
    }

    public static bool TryCreate(
        ReferenceResourceLoader resources,
        out ReferenceWorldChatBubbleAssets assets)
    {
        assets = null;
        if (resources == null)
        {
            return false;
        }

        ReferenceWorldChatBubbleThemeAssets yellow;
        ReferenceWorldChatBubbleThemeAssets red;
        if (!TryLoadTheme(resources, "huang", out yellow) ||
            !TryLoadTheme(resources, "red", out red))
        {
            return false;
        }

        for (int id = MinimumEmoteId; id <= MaximumEmoteId; id++)
        {
            if (resources.GsaFrameCount(EmotePath(id)) < 1)
            {
                return false;
            }
        }

        ReferenceWorldChatBubbleRenderer.InitializeFont();
        assets = new ReferenceWorldChatBubbleAssets(resources, yellow, red);
        return true;
    }

    internal static string EmotePath(int emoteId)
    {
        return EmoteDirectory +
               emoteId.ToString("D2", CultureInfo.InvariantCulture) +
               ".gsa";
    }

    private static bool TryLoadTheme(
        ReferenceResourceLoader resources,
        string theme,
        out ReferenceWorldChatBubbleThemeAssets assets)
    {
        assets = null;
        Texture2D left = resources.LoadGsaFrameA(
            "res/npcicon/" + theme + "_left.gsa",
            0);
        Texture2D middle = resources.LoadGsaFrameA(
            "res/npcicon/" + theme + "_middle.gsa",
            0);
        Texture2D right = resources.LoadGsaFrameA(
            "res/npcicon/" + theme + "_right.gsa",
            0);
        Texture2D pointer = resources.LoadGsaFrameA(
            "res/npcicon/" + theme + "_zhiyin.gsa",
            0);
        if (!left || !middle || !right)
        {
            return false;
        }

        assets = new ReferenceWorldChatBubbleThemeAssets(
            left,
            middle,
            right,
            pointer);
        return true;
    }
}

/// <summary>
/// Exact text/emote tokenization and immediate-mode drawing recovered from EmoteText.
/// </summary>
internal static class ReferenceWorldEmoteText
{
    public const float EmoteAdvancePixels = 17f;
    public const float EmoteSizePixels = 16f;
    public const float EmoteTopOffsetPixels = 3f;
    public const float SecondsPerFrame = 0.2f;

    public static bool TryReadEmote(string value, int offset, out int emoteId)
    {
        emoteId = 0;
        if (string.IsNullOrEmpty(value) || offset < 0 || offset + 3 >= value.Length ||
            value[offset] != '\\' || value[offset + 1] != 'e' ||
            value[offset + 2] < '0' || value[offset + 2] > '9' ||
            value[offset + 3] < '0' || value[offset + 3] > '9')
        {
            return false;
        }

        emoteId = (value[offset + 2] - '0') * 10 + value[offset + 3] - '0';
        return emoteId >= ReferenceWorldChatBubbleAssets.MinimumEmoteId &&
               emoteId <= ReferenceWorldChatBubbleAssets.MaximumEmoteId;
    }

    public static float Measure(
        string value,
        ReferenceWorldBitmapFont font,
        float scale)
    {
        if (string.IsNullOrEmpty(value) || font == null || !(scale > 0f))
        {
            return 0f;
        }

        float width = 0f;
        int plainStart = 0;
        for (int offset = 0; offset < value.Length; offset++)
        {
            int emoteId;
            if (!TryReadEmote(value, offset, out emoteId))
            {
                continue;
            }

            if (offset > plainStart)
            {
                width += font.Measure(
                    value.Substring(plainStart, offset - plainStart),
                    scale);
            }

            width += EmoteAdvancePixels * scale;
            offset += 3;
            plainStart = offset + 1;
        }

        if (plainStart < value.Length)
        {
            width += font.Measure(value.Substring(plainStart), scale);
        }

        return width;
    }

    public static void DrawColored(
        float x,
        float y,
        string value,
        Color color,
        float scale,
        float timeSeconds,
        ReferenceWorldBitmapFont font,
        ReferenceWorldChatBubbleAssets assets)
    {
        if (string.IsNullOrEmpty(value) || font == null || assets == null ||
            !(scale > 0f))
        {
            return;
        }

        float cursor = x;
        int plainStart = 0;
        for (int offset = 0; offset < value.Length; offset++)
        {
            int emoteId;
            if (!TryReadEmote(value, offset, out emoteId))
            {
                continue;
            }

            if (offset > plainStart)
            {
                string plain = value.Substring(plainStart, offset - plainStart);
                font.Draw(cursor, y, plain, color, scale);
                cursor += font.Measure(plain, scale);
            }

            Texture2D[] frames = assets.FramesFor(emoteId);
            Texture2D frame = null;
            if (frames != null && frames.Length > 0)
            {
                int frameIndex = Math.Max(
                    0,
                    (int)(timeSeconds / SecondsPerFrame)) % frames.Length;
                frame = frames[frameIndex] ? frames[frameIndex] : frames[0];
            }

            if (frame)
            {
                Color previousColor = GUI.color;
                GUI.color = color;
                GUI.DrawTexture(
                    new Rect(
                        cursor,
                        y - EmoteTopOffsetPixels * scale,
                        EmoteSizePixels * scale,
                        EmoteSizePixels * scale),
                    frame,
                    ScaleMode.StretchToFill,
                    true);
                GUI.color = previousColor;
            }

            cursor += EmoteAdvancePixels * scale;
            offset += 3;
            plainStart = offset + 1;
        }

        if (plainStart < value.Length)
        {
            font.Draw(cursor, y, value.Substring(plainStart), color, scale);
        }
    }
}

internal struct ReferenceWorldChatBubbleLayout
{
    public Rect Left;
    public Rect[] Middles;
    public Rect Right;
    public Rect Pointer;
    public Rect TextBounds;
    public Vector2 TextPosition;
}

/// <summary>
/// Pure geometry translation of NpcChatter.DrawBubble for audit and OnGUI.
/// </summary>
internal static class ReferenceWorldChatBubbleMath
{
    public static bool TryPlan(
        float centerX,
        float anchorY,
        float textWidth,
        float scale,
        ReferenceWorldChatBubbleThemeAssets theme,
        out ReferenceWorldChatBubbleLayout layout)
    {
        layout = new ReferenceWorldChatBubbleLayout();
        if (theme == null || !(scale > 0f) || textWidth < 0f ||
            float.IsNaN(centerX) || float.IsInfinity(centerX) ||
            float.IsNaN(anchorY) || float.IsInfinity(anchorY))
        {
            return false;
        }

        float leftWidth = GsaDecoder.W(theme.Left) * scale;
        float middleWidth = GsaDecoder.W(theme.Middle) * scale;
        float rightWidth = GsaDecoder.W(theme.Right) * scale;
        float leftHeight = GsaDecoder.H(theme.Left) * scale;
        float pointerWidth = (theme.Pointer != null ? GsaDecoder.W(theme.Pointer) : 11f) * scale;
        float pointerHeight = (theme.Pointer != null ? GsaDecoder.H(theme.Pointer) : 15f) * scale;

        int middleCount = Math.Max(
            1,
            Mathf.CeilToInt(textWidth / Mathf.Max(middleWidth, 1f)));
        float totalWidth = rightWidth + leftWidth + middleWidth * middleCount;
        float leftX = UIScale.SnapX(centerX - totalWidth * 0.5f);
        float topY = UIScale.SnapY(
            scale * 5f + (anchorY - 2f - pointerHeight) - leftHeight);
        Rect[] middles = new Rect[middleCount];
        for (int index = 0; index < middleCount; index++)
        {
            middles[index] = new Rect(
                leftWidth + leftX + middleWidth * index,
                topY,
                middleWidth,
                leftHeight);
        }

        layout.Left = new Rect(leftX, topY, leftWidth, leftHeight);
        layout.Middles = middles;
        layout.Right = new Rect(
            middleWidth * middleCount + leftWidth + leftX,
            topY,
            rightWidth,
            leftHeight);
        layout.Pointer = new Rect(
            UIScale.SnapX(centerX - pointerWidth * 0.5f),
            topY + leftHeight - scale * 5f,
            pointerWidth,
            pointerHeight);
        layout.TextBounds = new Rect(leftX, topY, totalWidth, leftHeight);
        layout.TextPosition = new Vector2(
            leftX + Mathf.Round((totalWidth - textWidth) * 0.5f),
            topY + Mathf.Round((leftHeight - scale * 12f) * 0.5f));
        return true;
    }
}

/// <summary>
/// Shared immediate-mode translation of NpcChatter.DrawBubble. Remote-player chat and ordinary
/// NPC ambient chatter deliberately pass through the same source geometry and bitmap/emote path.
/// </summary>
internal static class ReferenceWorldChatBubbleRenderer
{
    // NpcChatter.font/box are shared by the NPC and remote-player bubble paths.
    private static Font font;
    private static GUIStyle box;
    private static bool fontInitialized;

    internal static Font CnFont()
    {
        InitializeFont();
        return font;
    }

    internal static void InitializeFont()
    {
        if (fontInitialized) return;
        fontInitialized = true;
        font = NpcChatter.CnFont();
    }

    public static bool TryDraw(
        float centerX,
        float anchorY,
        string text,
        float scale,
        float timeSeconds,
        ReferenceWorldChatBubbleThemeAssets theme,
        ReferenceWorldBitmapFont font,
        ReferenceWorldChatBubbleAssets assets)
    {
        if (string.IsNullOrEmpty(text) || theme == null ||
            theme.Left == null || theme.Middle == null || theme.Right == null ||
            font == null || assets == null ||
            !(scale > 0f))
        {
            return false;
        }

        float textWidth = font.Ready
            ? ReferenceWorldEmoteText.Measure(text, font, scale)
            : scale * text.Length * 7f;

        ReferenceWorldChatBubbleLayout layout;
        if (!ReferenceWorldChatBubbleMath.TryPlan(
                centerX,
                anchorY,
                textWidth,
                scale,
                theme,
                out layout))
        {
            return false;
        }

        GUI.DrawTexture(layout.Left, theme.Left, ScaleMode.StretchToFill, true);
        foreach (Rect middle in layout.Middles)
        {
            GUI.DrawTexture(middle, theme.Middle, ScaleMode.StretchToFill, true);
        }

        GUI.DrawTexture(layout.Right, theme.Right, ScaleMode.StretchToFill, true);
        if (theme.Pointer != null)
        {
            GUI.DrawTexture(layout.Pointer, theme.Pointer, ScaleMode.StretchToFill, true);
        }

        if (font.Ready)
        {
            ReferenceWorldEmoteText.DrawColored(
                layout.TextPosition.x,
                layout.TextPosition.y,
                text,
                Color.black,
                scale,
                timeSeconds,
                font,
                assets);
            return true;
        }

        if (box == null)
        {
            box = new GUIStyle
            {
                font = ReferenceWorldChatBubbleRenderer.font,
                fontSize = Mathf.RoundToInt(12f * 1.8f),
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false
            };
            box.normal.textColor = Color.black;
        }

        GUI.Label(layout.TextBounds, text, box);
        return true;
    }
}

/// <summary>
/// Six-second state edge and screen projection owned by RemotePlayer.ShowChatBubble/OnGUI.
/// </summary>
internal sealed class ReferenceWorldRemoteChatBubbleView : IDisposable
{
    public const float DisplaySeconds = 6f;
    public const float DefaultHeadHeight = 1.6f;
    public const float HeadPaddingWorld = 0.2f;

    private readonly ReferenceWorldChatBubbleAssets assets;
    private readonly ReferenceWorldBitmapFont font;
    private ReferenceWorldRemoteChatBubbleSurface surface;
    private bool disposed;

    public ReferenceWorldRemoteChatBubbleView(
        ReferenceWorldChatBubbleAssets assets,
        ReferenceWorldBitmapFont font)
    {
        this.assets = assets ?? throw new ArgumentNullException(nameof(assets));
        this.font = font ?? throw new ArgumentNullException(nameof(font));
    }

    public long LastRevision { get; private set; }
    public string Text => surface == null ? string.Empty : surface.Text;
    public float Until => surface == null ? 0f : surface.Until;
    internal ReferenceWorldRemoteChatBubbleSurface Surface => surface;

    public bool TryShow(
        Transform owner,
        Camera worldCamera,
        float headHeight,
        WorldRemoteChatBubbleState state,
        float now)
    {
        if (disposed || owner == null || worldCamera == null || state == null ||
            state.Revision <= LastRevision || !(headHeight > 0f) ||
            float.IsNaN(now) || float.IsInfinity(now))
        {
            return false;
        }

        if (string.IsNullOrEmpty(state.Text))
        {
            LastRevision = state.Revision;
            return true;
        }

        if (surface == null)
        {
            surface = owner.gameObject.AddComponent<ReferenceWorldRemoteChatBubbleSurface>();
            surface.hideFlags = HideFlags.HideAndDontSave;
        }

        surface.Configure(
            owner,
            worldCamera,
            headHeight,
            state.Text,
            assets.ThemeFor(state.ThemeSelector),
            now + DisplaySeconds,
            assets,
            font);
        LastRevision = state.Revision;
        return true;
    }

    public bool TryTick(Transform owner, Camera worldCamera, float headHeight)
    {
        if (disposed || surface == null)
        {
            return !disposed;
        }

        return surface.TryRetarget(owner, worldCamera, headHeight);
    }

    public void Clear()
    {
        if (surface != null)
        {
            surface.Clear();
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        if (surface != null)
        {
            if (UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.Destroy(surface);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(surface);
            }

            surface = null;
        }
    }
}

internal sealed class ReferenceWorldRemoteChatBubbleSurface : MonoBehaviour
{
    private Transform owner;
    private Camera worldCamera;
    private float headHeight;
    private string text = string.Empty;
    private ReferenceWorldChatBubbleThemeAssets theme;
    private ReferenceWorldBitmapFont font;
    private ReferenceWorldChatBubbleAssets assets;

    public string Text => text;
    public float Until { get; private set; }

    public void Configure(
        Transform targetOwner,
        Camera targetCamera,
        float targetHeadHeight,
        string targetText,
        ReferenceWorldChatBubbleThemeAssets targetTheme,
        float until,
        ReferenceWorldChatBubbleAssets targetAssets,
        ReferenceWorldBitmapFont targetFont)
    {
        owner = targetOwner;
        worldCamera = targetCamera;
        headHeight = targetHeadHeight;
        text = targetText ?? string.Empty;
        theme = targetTheme;
        Until = until;
        assets = targetAssets;
        font = targetFont;
    }

    public bool TryRetarget(
        Transform targetOwner,
        Camera targetCamera,
        float targetHeadHeight)
    {
        if (targetOwner == null || targetCamera == null || !(targetHeadHeight > 0f))
        {
            return false;
        }

        owner = targetOwner;
        worldCamera = targetCamera;
        headHeight = targetHeadHeight;
        return true;
    }

    public void Clear()
    {
        text = string.Empty;
        Until = 0f;
    }

    private void OnGUI()
    {
        if (string.IsNullOrEmpty(text) || Time.time > Until || owner == null ||
            worldCamera == null || theme == null || font == null || assets == null)
        {
            return;
        }

        Vector3 screen = worldCamera.WorldToScreenPoint(
            owner.position + Vector3.up * headHeight);
        if (screen.z <= 0f)
        {
            return;
        }

        GUI.depth = ReferenceWorldNpcNameMath.GuiDepth;
        ReferenceWorldChatBubbleRenderer.TryDraw(
            screen.x,
            Screen.height - screen.y,
            text,
            UIScale.Factor,
            Time.time,
            theme,
            font,
            assets);
    }
}

}
