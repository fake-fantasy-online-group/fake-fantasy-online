using System;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class EndgamePanel
{
    // Original client resources, shared with SysMsgBox, GameHud and SysConfigDlg.
    // Nothing is copied into the project and ResLoader retains texture ownership.
    private const string RiftBackgroundPath = "res/uires/common/msgboxbk.gsa";
    private const string RiftProgressBackPath = "res/uires/gameui/intonatebarbk.gsa";
    private const string RiftProgressFillPath = "res/uires/gameui/intonatebar.gsa";
    private const string RiftDropdownPath = "res/uires/newcommon/downbtn.gsa";
    private Texture2D riftTimeSource, riftTimeFill;
    private bool riftTierOpen;

    // Native portrait ends at y=60, pet vitals at y=104, and karma is (4,104,72,14).
    // The selected original frame has 8px transparent margins: visible y=104..122.
    // Use the native HUD scale, not the unrelated 940x660 workbench fit scale.
    private static float RiftHudScale => GameHud.I != null && GameHud.I.uiScale > 0f ? GameHud.I.uiScale : UIScale.Factor;
    internal static bool RiftHudVisible => Current != null && Current.isActiveAndEnabled &&
        Current.client != null && Current.client.EndgameInWorld && Current.State.Negotiated && !Current.open &&
        (GameHud.I == null || GameHud.I.showAvatar) && Current.State.Rift != null &&
        (Current.State.RiftInInstance || PollRun(Current.State.Rift) || Current.State.Rift.Claimable);
    // Target drawing, all target rows and hit tests share this offset. It vanishes
    // immediately on close, disconnect or leaving the run; no frame-order cache.
    internal static float RiftTargetHudOffset => RiftHudVisible ? 38f : 0f;
    private static Rect RiftHudBarRect(float scale) => new Rect(80f * scale, 96f * scale, 140f * scale, 34f * scale);

    private void ReleaseRiftProgressArtwork()
    {
        if (riftTimeFill != null) Destroy(riftTimeFill);
        riftTimeFill = null; riftTimeSource = null;
    }
    private Texture2D RiftSilverTimeFill(Texture2D source)
    {
        if (source == null) { ReleaseRiftProgressArtwork(); return null; }
        if (source == riftTimeSource && riftTimeFill != null) return riftTimeFill;
        ReleaseRiftProgressArtwork();
        // Desaturate the original fill using HSV value (max channel), preserving
        // each pixel's highlight/shadow and alpha. A uniform white RGB copy would
        // flatten its authored bevel, while GUI.color cannot remove green tint.
        // Never mutate ResLoader's shared original texture.
        Color32[] pixels = source.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 pixel = pixels[i];
            byte value = Math.Max(pixel.r, Math.Max(pixel.g, pixel.b));
            pixels[i] = new Color32(value, value, value, pixel.a);
        }
        riftTimeFill = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, false);
        riftTimeFill.filterMode = FilterMode.Point;
        riftTimeFill.wrapMode = TextureWrapMode.Clamp;
        riftTimeFill.SetPixels32(pixels); riftTimeFill.Apply(false, false);
        riftTimeSource = source;
        return riftTimeFill;
    }
    private void DrawRiftProgressArtwork(Rect frame, float progressRatio, float elapsedRatio)
    {
        Texture2D back = RiftTexture(RiftProgressBackPath), fill = RiftTexture(RiftProgressFillPath);
        float progress = Mathf.Clamp01(progressRatio), elapsed = Mathf.Clamp01(elapsedRatio);
        Color color = GUI.color;
        try
        {
            GUI.color = new Color(1f, 1f, 1f, 1f);
            if (back != null) GUI.DrawTexture(frame, back);
            if (fill == null) return;
            Rect slot = new Rect(frame.x + frame.width * 12f / 140f, frame.y + frame.height * 12f / 34f,
                frame.width * 116f / 140f, frame.height * 10f / 34f);
            // One slot: white elapsed time is behind green progress. Omit white when
            // completely covered, including equality and 100% green, to avoid alpha bleed.
            if (elapsed > progress)
            {
                Texture2D white = RiftSilverTimeFill(fill);
                GUI.DrawTextureWithTexCoords(new Rect(slot.x, slot.y, slot.width * elapsed, slot.height), white, new Rect(0, 0, elapsed, 1));
            }
            if (progress > 0)
                GUI.DrawTextureWithTexCoords(new Rect(slot.x, slot.y, slot.width * progress, slot.height), fill, new Rect(0, 0, progress, 1));
        }
        finally { GUI.color = color; }
    }
    private static int RiftRemainingSeconds(EndgameRiftView rift, DateTime received, DateTime now)
    {
        if (rift == null) return 0;
        // Reserved time has not started. Successful completion and all terminal
        // snapshots keep the server's exact result, including during reward saving.
        if (rift.Phase != "active" && rift.Phase != "boss_arriving" && rift.Phase != "boss") return rift.RemainingSeconds;
        double elapsed = Math.Max(0, (now - received).TotalSeconds);
        return (int)Math.Max(0, rift.RemainingSeconds - Math.Min(int.MaxValue, Math.Floor(elapsed)));
    }
    private static float RiftElapsedRatio(EndgameRiftView rift, int remaining)
    {
        if (rift == null || rift.TotalSeconds <= 0 || rift.Phase == "reserved") return 0;
        return Mathf.Clamp01(1f - (float)Math.Max(0, remaining) / rift.TotalSeconds);
    }
    private int riftTierScroll, riftTierHighlight = 1;
    private const float RiftWindowWidth = 600f, RiftWindowHeight = 450f;
    private const int RiftVisibleTierRows = 8;
    private static readonly Rect RiftTierField = new Rect(32, 139, 250, 32);

    private static Texture2D RiftTexture(string path, int frame = 0)
    {
        Texture2D[] frames = ArtworkSkin.Frames(path, frame + 1);
        return frames != null && frames.Length > frame ? frames[frame] ?? frames[0] : null;
    }

    private static void DrawRiftWindowArtwork(Rect rect, float scale = 1f, bool opaque = false)
    {
        Texture2D texture = RiftTexture(RiftBackgroundPath);
        if (texture == null) return; // Do not fabricate replacement artwork when packages are missing.
        // The original 346x128 dialog has 44px frame caps. Preserve all four corners;
        // only its flat middle and straight frame runs extend to fit the workbench.
        // The original center is translucent. Reuse its own RGB as an opaque inner
        // backing so an expanded dropdown cannot show the text/buttons beneath it.
        Rect inside = opaque ? rect : new Rect(rect.x + 18f * scale, rect.y + 16f * scale,
            rect.width - 36f * scale, rect.height - 51f * scale);
        if (inside.width > 0 && inside.height > 0)
            GUI.DrawTextureWithTexCoords(inside, texture, new Rect(100f / 346f, 60f / 128f, 1f / 346f, 1f / 128f), false);
        float sx = 44f / 346f, sy = 44f / 128f;
        float ex = Mathf.Min(44f * scale, rect.width * .5f);
        float ey = Mathf.Min(44f * scale, rect.height * .5f);
        for (int y = 0; y < 3; y++)
        for (int x = 0; x < 3; x++)
        {
            Rect destination = new Rect(x == 0 ? rect.x : x == 1 ? rect.x + ex : rect.xMax - ex,
                y == 0 ? rect.y : y == 1 ? rect.y + ey : rect.yMax - ey,
                x == 1 ? Mathf.Max(0, rect.width - 2 * ex) : ex,
                y == 1 ? Mathf.Max(0, rect.height - 2 * ey) : ey);
            float u = x == 0 ? 0 : x == 1 ? sx : 1 - sx;
            float top = y == 0 ? 0 : y == 1 ? sy : 1 - sy;
            float w = x == 1 ? 1 - 2 * sx : sx, h = y == 1 ? 1 - 2 * sy : sy;
            if (destination.width > 0 && destination.height > 0)
                GUI.DrawTextureWithTexCoords(destination, texture, new Rect(u, 1 - top - h, w, h));
        }
    }

    private static int RiftTierLimit(EndgameRiftView rift)
    {
        if (rift == null) return 1;
        int maximum = rift.MaxTier > 0 ? Math.Min(150, rift.MaxTier) : 150;
        // Missing/zero unlock data never grants access to all 150 difficulties.
        return Math.Max(1, Math.Min(maximum, rift.UnlockedTier));
    }
    private void ResetRiftTierDropdown()
    {
        riftTierOpen = false; riftTierScroll = 0; riftTierHighlight = tier;
    }
    private bool RiftTierEditable => State != null && !State.RiftInInstance && !State.Pending && State.Rift != null &&
        !(State.Rift.RunID.Length > 0 && (PollRun(State.Rift) || State.Rift.Claimable || State.Rift.Phase == "interrupted"));

    private void DrawRiftTierSelector(EndgameRiftView rift)
    {
        int maximum = RiftTierLimit(rift);
        tier = Math.Max(1, Math.Min(tier, maximum));
        bool editable = RiftTierEditable;
        if (!editable) riftTierOpen = false;
        ArtworkSkin.Input(RiftTierField, focused: riftTierOpen);
        Text(new Rect(RiftTierField.x + 12, RiftTierField.y + 3, 196, 26), "第 " + tier + " 阶", label, editable ? Gold : Muted);
        Texture2D arrow = RiftTexture(RiftDropdownPath, riftTierOpen ? 1 : 2);
        if (arrow != null) GUI.DrawTexture(new Rect(RiftTierField.xMax - 28, RiftTierField.y + 6, 20, 19), arrow);
        Event current = Event.current;
        if (editable && current.type == EventType.MouseDown && current.button == 0 && RiftTierField.Contains(current.mousePosition))
        {
            riftTierOpen = !riftTierOpen; riftTierHighlight = tier;
            riftTierScroll = Math.Max(0, Math.Min(tier - 1, maximum - RiftVisibleTierRows));
            current.Use(); PlayerController.SuppressWorldClick();
        }
    }

    // Event handling runs before any covered buttons; paint runs last in DrawRift.
    // Dismissing the popup cannot also spend a key, leave, refresh, or click the world.
    private void DrawRiftTierDropdown()
    {
        if (!riftTierOpen) return;
        if (!RiftTierEditable) { ResetRiftTierDropdown(); return; }
        int maximum = RiftTierLimit(State.Rift), visible = Math.Min(RiftVisibleTierRows, maximum);
        riftTierScroll = Math.Max(0, Math.Min(riftTierScroll, maximum - visible));
        riftTierHighlight = Math.Max(1, Math.Min(riftTierHighlight, maximum));
        Rect popup = new Rect(RiftTierField.x, RiftTierField.yMax + 3, RiftTierField.width, visible * 24 + 60);
        Event current = Event.current;
        if (current.type == EventType.KeyDown)
        {
            switch (current.keyCode)
            {
                case KeyCode.Escape: riftTierOpen = false; break;
                case KeyCode.Return: case KeyCode.KeypadEnter: tier = riftTierHighlight; riftTierOpen = false; break;
                case KeyCode.Home: riftTierHighlight = 1; break;
                case KeyCode.End: riftTierHighlight = maximum; break;
                case KeyCode.UpArrow: riftTierHighlight = Math.Max(1, riftTierHighlight - 1); break;
                case KeyCode.DownArrow: riftTierHighlight = Math.Min(maximum, riftTierHighlight + 1); break;
                case KeyCode.PageUp: riftTierHighlight = Math.Max(1, riftTierHighlight - visible); break;
                case KeyCode.PageDown: riftTierHighlight = Math.Min(maximum, riftTierHighlight + visible); break;
                default: return;
            }
            riftTierScroll = Math.Max(0, Math.Min(maximum - visible,
                Math.Max(riftTierHighlight - visible, Math.Min(riftTierScroll, riftTierHighlight - 1))));
            current.Use(); PlayerController.SuppressWorldClick(); return;
        }
        if (current.type == EventType.MouseDown && !popup.Contains(current.mousePosition))
        { riftTierOpen = false; current.Use(); PlayerController.SuppressWorldClick(); return; }
        if (current.type == EventType.ScrollWheel && popup.Contains(current.mousePosition))
        {
            riftTierScroll = Math.Max(0, Math.Min(maximum - visible, riftTierScroll + (current.delta.y >= 0 ? 3 : -3)));
            current.Use(); PlayerController.SuppressWorldClick();
        }
        DrawRiftWindowArtwork(popup, opaque: true);
        NativeScroll.BarRows(this, new Rect(popup.xMax - 36, popup.y + 20, 15, visible * 24 - 2), visible, maximum, ref riftTierScroll);
        for (int index = 0; index < visible; index++)
        {
            int option = riftTierScroll + index + 1;
            Rect row = new Rect(popup.x + 18, popup.y + 20 + index * 24, popup.width - 60, 24);
            bool hover = row.Contains(current.mousePosition);
            ArtworkSkin.Row(row, highlighted: hover || option == riftTierHighlight);
            Text(new Rect(row.x + 6, row.y + 1, row.width - 10, row.height), "第 " + option + " 阶" + (option == tier ? " · 已选" : ""), label, option == tier ? Gold : Jade);
            if (hover && current.type == EventType.MouseDown && current.button == 0)
            { tier = option; riftTierOpen = false; current.Use(); PlayerController.SuppressWorldClick(); return; }
        }
        if (popup.Contains(current.mousePosition) && (current.type == EventType.MouseDown || current.type == EventType.MouseUp))
        { current.Use(); PlayerController.SuppressWorldClick(); }
    }

    private static string RiftUnlockSummary(EndgameRiftView rift)
    {
        if (rift == null) return "正在查询已开放劫数…";
        string unlocked = "已开放至第 " + RiftTierLimit(rift) + " 阶（最高 150 阶）";
        if (rift.RunID.Length > 0 && (rift.Phase == "completed" || rift.Phase == "closed"))
            return "通关余时 " + RiftClock(rift.RemainingSeconds) + " · " + unlocked;
        return unlocked;
    }
    private static string RiftClock(int seconds)
    {
        seconds = Math.Max(0, seconds);
        return (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
    }
}
}
