using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Last native action observed in the GameHud control-bar branch.
/// </summary>
public enum ReferenceFirstWorldControlBarAction
{
    None = 0,
    Equipment = 1,
    Inventory = 2,
    Team = 3,
    Family = 4,
    Skills = 5,
    Tasks = 6,
    Mail = 7,
    Rack = 8,
    Options = 9
}

/// <summary>
/// Source translation of the default-visible GameHud control-bar chrome and button calls.
/// </summary>
/// <remarks>
/// The official destination dialogs remain separate owners. The native button branch calls
/// them immediately; the returned action is observation only.
/// </remarks>
public sealed class ReferenceFirstWorldControlBar : IDisposable
{
    public const bool ShowMenuByDefault = true;

    private static readonly ButtonDescriptor[] Buttons =
    {
        new ButtonDescriptor(0, ReferenceFirstWorldControlBarAction.Equipment),
        new ButtonDescriptor(1, ReferenceFirstWorldControlBarAction.Inventory),
        new ButtonDescriptor(2, ReferenceFirstWorldControlBarAction.Team),
        new ButtonDescriptor(3, ReferenceFirstWorldControlBarAction.Family),
        new ButtonDescriptor(4, ReferenceFirstWorldControlBarAction.Skills),
        new ButtonDescriptor(5, ReferenceFirstWorldControlBarAction.Tasks),
        new ButtonDescriptor(6, ReferenceFirstWorldControlBarAction.Mail),
        new ButtonDescriptor(7, ReferenceFirstWorldControlBarAction.Rack),
        new ButtonDescriptor(8, ReferenceFirstWorldControlBarAction.Options)
    };

    private readonly ReferenceFirstWorldHudResources resources;
    private readonly ReferenceWorldBitmapFont font;
    private bool disposed;
    private GameHud hudOwner;
    internal void BindHudOwnerForIndependentRuntime(GameHud owner) => hudOwner = owner;

    private ReferenceFirstWorldControlBar(
        ReferenceFirstWorldHudResources resources,
        ReferenceWorldBitmapFont font)
    {
        this.resources = resources;
        this.font = font;
        Ready = true;
    }

    public bool Ready { get; private set; }

    public string HoverShortcut { get; private set; } = string.Empty;

    public ReferenceFirstWorldControlBarAction LastAction { get; private set; }

    public static int ButtonCount => Buttons.Length;

    public static bool TryCreate(
        ReferenceResourceLoader loader,
        ReferenceFirstWorldHudResources resources,
        out ReferenceFirstWorldControlBar owner)
    {
        owner = null;
        // The resource view is attached before native Load. Texture/array results are
        // borrowed at their existing draw points, never an all-present startup gate.
        if (loader == null || resources == null || !resources.Ready)
        {
            return false;
        }

        ReferenceWorldBitmapFont font;
        if (!ReferenceWorldBitmapFont.TryCreate(loader, out font) ||
            font == null)
        {
            return false;
        }

        owner = new ReferenceFirstWorldControlBar(resources, font);
        return true;
    }

    /// <summary>
    /// Draws the official control-bar branch and emits at most one destination intent.
    /// </summary>
    public bool Draw(out ReferenceFirstWorldControlBarAction action,
        int? viewportWidth = null, int? viewportHeight = null)
    {
        action = ReferenceFirstWorldControlBarAction.None;
        if (hudOwner != null && !hudOwner.showMenu) return true;
        // The native control branch returns when ctrlTex is null, even in bottom mode.
        if (resources.ControlButtonTextures == null) return true;
        if (!Ready || disposed || (viewportWidth ?? Screen.width) < 1 ||
            (viewportHeight ?? Screen.height) < 1)
        {
            return false;
        }

        float scale = hudOwner != null ? hudOwner.s : UIScale.Factor;
        // These are stages of one native OnGUI call: preserve incoming GUI state and
        // each reached leaf's own normal/exceptional restoration behavior.
        action = DrawAt(viewportWidth ?? Screen.width, viewportHeight ?? Screen.height, scale);

        if (action != ReferenceFirstWorldControlBarAction.None)
        {
            LastAction = action;
        }

        return true;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        Ready = false;
        HoverShortcut = string.Empty;
        LastAction = ReferenceFirstWorldControlBarAction.None;
        disposed = true;
    }

    public static Rect BackgroundRect(
        int screenWidth,
        int screenHeight,
        float scale)
    {
        return new Rect(
            screenWidth - 382f * scale,
            screenHeight - 36f * scale,
            382f * scale,
            30f * scale);
    }

    public static Rect ButtonRect(
        int index,
        int screenWidth,
        int screenHeight,
        float scale)
    {
        if (index < 0 || index >= Buttons.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        Rect background = BackgroundRect(screenWidth, screenHeight, scale);
        return new Rect(
            background.x + Buttons[index].X * scale,
            background.y - 26f * scale,
            42f * scale,
            55f * scale);
    }

    public static Rect HoverLabelRect(Rect button, float scale)
    {
        return new Rect(
            button.x - 29f * scale,
            button.y - 14f * scale,
            100f * scale,
            12f * scale);
    }

    public static int FrameIndex(bool pointerInside, bool pointerHeld)
    {
        return !pointerInside ? 2 : pointerHeld ? 0 : 1;
    }

    public static string ButtonName(int index)
    {
        return Descriptor(index).Name;
    }

    public static string ButtonShortcut(int index)
    {
        return Descriptor(index).Shortcut;
    }

    public static string ButtonHoverLabel(int index)
    {
        return Descriptor(index).HoverLabel;
    }

    public static ReferenceFirstWorldControlBarAction ButtonAction(int index)
    {
        return Descriptor(index).Action;
    }

    private ReferenceFirstWorldControlBarAction DrawAt(
        int screenWidth,
        int screenHeight,
        float scale)
    {
        GameSettings.EnsureLoaded();
        bool bottom = GameSettings.HotbarBottom && resources.BottomHotbarBackground != null;
        float ownerScale = hudOwner != null ? hudOwner.s : scale;
        scale = ownerScale;
        float baseX;
        float baseY;
        if (bottom)
        {
            scale = GameHud.BottomHotbarScaleForIndependentRuntime(ownerScale, screenWidth);
            baseX = (GameHud.BottomHotbarXForIndependentRuntime(scale, screenWidth) + 426f) * scale;
            baseY = screenHeight - 50f * scale;
        }
        else
        {
            Rect background = BackgroundRect(screenWidth, screenHeight, scale);
            baseX = background.x;
            baseY = background.y;
            if (resources.ControlBarBackground != null) GUI.DrawTexture(background, resources.ControlBarBackground);
        }
        Texture2D[] bottomFrames = resources.BottomControlButtonTextures;
        bool legacyFrames = !bottom || bottomFrames == null ||
            bottomFrames.Length < GameHud.ControlButtonCountForIndependentRuntime * 3 || bottomFrames[2] == null;
        IReadOnlyList<Texture2D> textures = legacyFrames ? resources.ControlButtonTextures : bottomFrames;

        HoverShortcut = string.Empty;
        ReferenceFirstWorldControlBarAction action =
            ReferenceFirstWorldControlBarAction.None;
        Event current = Event.current;
        Vector2 mouse = current == null
            ? new Vector2(float.NegativeInfinity, float.NegativeInfinity)
            : current.mousePosition;
        bool pointerHeld = Input.GetMouseButton(0);
        for (int index = 0; index < Buttons.Length; index++)
        {
            ButtonDescriptor button = Buttons[index];
            Rect rect = bottom
                ? new Rect(baseX + (button.X - 6f - 4f) * scale, baseY + 4f * scale, 42f * scale, 42f * scale)
                : ButtonRect(index, screenWidth, screenHeight, scale);
            Rect image = legacyFrames ? rect
                : new Rect(baseX + (button.X - 6f - 8f) * scale, baseY - 3f * scale, 50f * scale, 50f * scale);
            bool pointerInside = rect.Contains(mouse);
            int frame = FrameIndex(pointerInside, pointerHeld);
            Texture2D texture = textures[index * 3 + frame];
            if (ReferenceEquals(texture, null)) texture = textures[index * 3 + 2];
            if (legacyFrames && button.Name == "rackbtn")
            {
                Texture2D rack = resources.Rack2021Frames[frame];
                if (ReferenceEquals(rack, null)) rack = resources.Rack2021Frames[1];
                if (rack != null)
                    GameHud.DrawControlIconForIndependentRuntime(rect, rack, bottom);
                else
                {
                    if (texture != null)
                    {
                        float rackScale = hudOwner != null ? hudOwner.s : ownerScale;
                        GUI.DrawTexture(new Rect(rect.x + 4f * rackScale, rect.y + 2f * rackScale,
                            34f * rackScale, 34f * rackScale), texture);
                    }
                    // The original fixed label belongs only to this missing-rack
                    // texture branch, independently of the hover caption below.
                    if (font.Ready)
                    {
                        float labelScale = hudOwner != null ? hudOwner.s : ownerScale;
                        font.CenterOutline(
                            new Rect(rect.x, rect.y + 40f * labelScale,
                                42f * labelScale, 12f * labelScale),
                            "货架", new Color(250f / 255f, 235f / 255f, 190f / 255f, 1f),
                            labelScale);
                    }
                }
            }
            else if (legacyFrames)
                GameHud.DrawControlIconForIndependentRuntime(rect, texture, bottom);
            else if (texture != null) GUI.DrawTexture(image, texture);

            if (pointerInside)
            {
                HoverShortcut = button.Shortcut;
                if (font.Ready)
                {
                    float hoverScale = hudOwner != null ? hudOwner.s : ownerScale;
                    font.CenterOutline(HoverLabelRect(rect, hoverScale), button.Shortcut, Color.white,
                        hudOwner != null ? hudOwner.s : ownerScale);
                }
            }

            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                action = button.Action;
                GameHud.ActivateControlButtonForIndependentRuntime(index);
            }
        }

        return action;
    }

    private static ButtonDescriptor Descriptor(int index)
    {
        if (index < 0 || index >= Buttons.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return Buttons[index];
    }

    private sealed class ButtonDescriptor
    {
        public ButtonDescriptor(int index, ReferenceFirstWorldControlBarAction action)
        {
            var original = GameHud.ControlButtonForIndependentRuntime(index);
            X = original.X;
            Name = original.Name;
            Shortcut = original.Shortcut;
            HoverLabel = original.Label;
            Action = action;
        }

        public float X { get; }
        public string Name { get; }
        public string Shortcut { get; }
        public string HoverLabel { get; }
        public ReferenceFirstWorldControlBarAction Action { get; }
    }
}

}
