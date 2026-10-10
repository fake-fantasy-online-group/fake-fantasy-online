#if UNITY_EDITOR
// Historical editor audit surface. Player builds use the original ReviveDlg owner.
using System;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Source translation of the locked 1.3.4 ordinary <c>ReviveDlg</c> surface.
/// </summary>
/// <remarks>
/// A click emits only the exact option byte and hides this surface immediately. The local actor
/// remains dead until a later authoritative player-life edge says otherwise.
/// </remarks>
public sealed class ReferenceFirstWorldReviveChoiceDialog : IDisposable
{
    public const string BackgroundPath =
        "res/uires/revivedlg/revivedlgbk.gsa";
    public const string DefaultButtonPath =
        "res/uires/revivedlg/revivebtn.gsa";
    public const string ItemButtonPath =
        "res/uires/revivedlg/itemrevivebtn.gsa";
    public const string SkillButtonPath =
        "res/uires/revivedlg/skillrevivebtn.gsa";

    public const string DefaultLabel = "重生角色";
    public const string ItemLabel = "使用替身娃娃";
    public const string SkillLabel = "使用回魂娃娃";

    public const float WindowWidth = 192f;
    public const float WindowHeight = 169f;
    public const float DragHeight = 28f;
    public const float ButtonX = 19f;
    public const float FirstButtonY = 35f;
    public const float ButtonStepY = 35f;
    public const float ButtonWidth = 152f;
    public const float ButtonHeight = 33f;

    public static readonly Color NormalTint = Color.white;
    public static readonly Color HoverTint = new Color(1.3f, 1.3f, 1.3f, 1f);
    public static readonly Color DisabledTint = new Color(0.55f, 0.55f, 0.55f, 0.85f);

    private readonly Texture2D background;
    private readonly Texture2D[] enabledButtons;
    private readonly Texture2D[] disabledButtons;

    private bool hasPendingChoice;
    private WorldReviveOption pendingChoice;
    private bool dragging;
    private Vector2 dragOffset;
    private int positionViewportWidth;
    private int positionViewportHeight;
    private float positionScale;
    private bool disposed;

    private ReferenceFirstWorldReviveChoiceDialog(
        Texture2D background,
        Texture2D[] enabledButtons,
        Texture2D[] disabledButtons)
    {
        this.background = background;
        this.enabledButtons = enabledButtons;
        this.disabledButtons = disabledButtons;
        WindowX = float.NaN;
        WindowY = float.NaN;
        Ready = true;
    }

    public bool Ready { get; private set; }

    public bool Visible { get; private set; }

    public int ReviveFlags { get; private set; }

    public float WindowX { get; private set; }

    public float WindowY { get; private set; }

    public static bool TryCreate(
        ReferenceResourceLoader resources,
        out ReferenceFirstWorldReviveChoiceDialog owner)
    {
        owner = null;
        if (resources == null)
        {
            return false;
        }

        Texture2D background = resources.LoadGsa(BackgroundPath);
        Texture2D[] enabled =
        {
            resources.LoadGsaFrameA(DefaultButtonPath, 0),
            resources.LoadGsaFrameA(ItemButtonPath, 0),
            resources.LoadGsaFrameA(SkillButtonPath, 0)
        };
        Texture2D[] disabled =
        {
            resources.LoadGsaFrameA(DefaultButtonPath, 3),
            resources.LoadGsaFrameA(ItemButtonPath, 3),
            resources.LoadGsaFrameA(SkillButtonPath, 3)
        };
        if (!background || background.width != (int)WindowWidth ||
            background.height != (int)WindowHeight ||
            !FramesReady(enabled) || !FramesReady(disabled))
        {
            return false;
        }

        owner = new ReferenceFirstWorldReviveChoiceDialog(
            background,
            enabled,
            disabled);
        return true;
    }

    public bool Open(int reviveFlags)
    {
        ThrowIfDisposed();
        if (!Ready || reviveFlags < 0 || reviveFlags > byte.MaxValue)
        {
            return false;
        }

        ReviveFlags = reviveFlags;
        Visible = true;
        dragging = false;
        return true;
    }

    public void Hide()
    {
        ThrowIfDisposed();
        Visible = false;
        dragging = false;
    }

    public bool Allows(WorldReviveOption option)
    {
        switch (option)
        {
            case WorldReviveOption.RespawnCharacter:
                return true;
            case WorldReviveOption.SubstituteDoll:
                return (ReviveFlags & 1) != 0;
            case WorldReviveOption.SoulReturnDoll:
                return (ReviveFlags & 2) != 0;
            default:
                return false;
        }
    }

    /// <summary>
    /// Applies one enabled option exactly as the official button callback does.
    /// </summary>
    public bool TryChoose(WorldReviveOption option)
    {
        ThrowIfDisposed();
        if (!Ready || !Visible || !Allows(option))
        {
            return false;
        }

        pendingChoice = option;
        hasPendingChoice = true;
        Visible = false;
        dragging = false;
        return true;
    }

    public bool TryConsumeChoice(out WorldReviveOption option)
    {
        ThrowIfDisposed();
        if (!hasPendingChoice)
        {
            option = default(WorldReviveOption);
            return false;
        }

        option = pendingChoice;
        pendingChoice = default(WorldReviveOption);
        hasPendingChoice = false;
        return true;
    }

    public Rect WindowRect(
        int viewportWidth,
        int viewportHeight,
        float scale)
    {
        EnsurePosition(viewportWidth, viewportHeight, scale);
        return new Rect(
            WindowX,
            WindowY,
            WindowWidth * scale,
            WindowHeight * scale);
    }

    public Rect ButtonRect(WorldReviveOption option, float scale)
    {
        int index = OptionIndex(option);
        if (index < 0)
        {
            return new Rect();
        }

        return new Rect(
            WindowX + ButtonX * scale,
            WindowY + (FirstButtonY + ButtonStepY * index) * scale,
            ButtonWidth * scale,
            ButtonHeight * scale);
    }

    public bool MouseOver(
        Vector2 topOriginPointer,
        int viewportWidth,
        int viewportHeight,
        float scale)
    {
        if (!Ready || !Visible || !IsPositiveFinite(scale) ||
            viewportWidth < 1 || viewportHeight < 1)
        {
            return false;
        }

        return ContainsInclusive(
            WindowRect(viewportWidth, viewportHeight, scale),
            topOriginPointer);
    }

    public bool Draw(float scale, bool interactionBlocked)
    {
        ThrowIfDisposed();
        if (!Ready || !IsPositiveFinite(scale))
        {
            return false;
        }

        if (!Visible)
        {
            return true;
        }

        Rect window = WindowRect(Screen.width, Screen.height, scale);
        Event current = Event.current;
        Vector2 pointer = current == null
            ? new Vector2(float.NegativeInfinity, float.NegativeInfinity)
            : current.mousePosition;
        int previousDepth = GUI.depth;
        Color previousColor = GUI.color;
        GUI.depth = 1;
        try
        {
            GUI.color = Color.white;
            GUI.DrawTexture(window, background, ScaleMode.StretchToFill, true);

            for (int index = 0; index < 3; index++)
            {
                WorldReviveOption option = (WorldReviveOption)index;
                Rect button = ButtonRect(option, scale);
                bool enabled = Allows(option);
                bool hovered = enabled && ContainsInclusive(button, pointer);
                GUI.color = enabled
                    ? (hovered ? HoverTint : NormalTint)
                    : DisabledTint;
                GUI.DrawTexture(
                    button,
                    enabled ? enabledButtons[index] : disabledButtons[index],
                    ScaleMode.StretchToFill,
                    true);

                if (!interactionBlocked && enabled && hovered && current != null &&
                    current.type == EventType.MouseDown && current.button == 0)
                {
                    TryChoose(option);
                    current.Use();
                    break;
                }
            }

            GUI.color = Color.white;
            HandleDrag(
                new Rect(
                    WindowX,
                    WindowY,
                    WindowWidth * scale,
                    DragHeight * scale),
                current,
                interactionBlocked);

            if (current != null && current.type == EventType.MouseDown &&
                current.button == 0 && ContainsInclusive(window, pointer))
            {
                current.Use();
            }

            return true;
        }
        finally
        {
            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        Ready = false;
        Visible = false;
        ReviveFlags = 0;
        hasPendingChoice = false;
        pendingChoice = default(WorldReviveOption);
        dragging = false;
    }

    private void EnsurePosition(
        int viewportWidth,
        int viewportHeight,
        float scale)
    {
        if (!IsPositiveFinite(scale) || viewportWidth < 1 || viewportHeight < 1)
        {
            return;
        }

        if (!float.IsNaN(WindowX) && !float.IsNaN(WindowY) &&
            positionViewportWidth == viewportWidth &&
            positionViewportHeight == viewportHeight &&
            Mathf.Approximately(positionScale, scale))
        {
            return;
        }

        WindowX = (viewportWidth - WindowWidth * scale) * 0.5f;
        WindowY = (viewportHeight - WindowHeight * scale) * 0.5f;
        positionViewportWidth = viewportWidth;
        positionViewportHeight = viewportHeight;
        positionScale = scale;
        dragging = false;
    }

    private void HandleDrag(Rect title, Event current, bool interactionBlocked)
    {
        if (current == null || interactionBlocked)
        {
            return;
        }

        if (!dragging && current.type == EventType.MouseDown && current.button == 0 &&
            ContainsInclusive(title, current.mousePosition))
        {
            dragging = true;
            dragOffset = current.mousePosition - new Vector2(WindowX, WindowY);
            current.Use();
        }
        else if (dragging && current.type == EventType.MouseDrag)
        {
            WindowX = current.mousePosition.x - dragOffset.x;
            WindowY = current.mousePosition.y - dragOffset.y;
            current.Use();
        }
        else if (dragging && current.type == EventType.MouseUp)
        {
            dragging = false;
            current.Use();
        }
    }

    private static int OptionIndex(WorldReviveOption option)
    {
        int index = (int)option;
        return index >= 0 && index <= 2 ? index : -1;
    }

    private static bool FramesReady(Texture2D[] frames)
    {
        if (frames == null || frames.Length != 3)
        {
            return false;
        }

        for (int index = 0; index < frames.Length; index++)
        {
            if (!frames[index] || frames[index].width != (int)ButtonWidth ||
                frames[index].height != (int)ButtonHeight)
            {
                return false;
            }
        }

        return true;
    }

    private static bool ContainsInclusive(Rect rect, Vector2 point)
    {
        return point.x >= rect.x && point.x <= rect.xMax &&
               point.y >= rect.y && point.y <= rect.yMax;
    }

    private static bool IsPositiveFinite(float value)
    {
        return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(
                nameof(ReferenceFirstWorldReviveChoiceDialog));
        }
    }
}

}

#endif
