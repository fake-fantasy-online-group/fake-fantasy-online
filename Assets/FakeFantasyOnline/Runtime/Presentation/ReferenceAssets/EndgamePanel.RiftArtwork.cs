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
    private const string RiftProgressBackPath = "res/uires/statusbar/expbarbk.gsa";
    private const string RiftProgressFillPath = "res/uires/statusbar/expbar.gsa";
    private const string RiftDropdownPath = "res/uires/newcommon/downbtn.gsa";
    private bool riftTierOpen;
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
