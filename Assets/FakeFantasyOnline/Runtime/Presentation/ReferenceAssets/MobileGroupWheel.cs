using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    /// <summary>Four cardinal directions in GUI coordinates; no swipe/page cycling.</summary>
    internal sealed class MobileGroupWheel
    {
        internal const int GroupCount = 4;
        internal const int SlotsPerGroup = 6;
        internal const int SlotCount = GroupCount * SlotsPerGroup;
        // Desktop owns 30 persisted bindings. Mobile exposes only the first 24.
        internal static int HotbarSlot(int group, int button) =>
            (uint)group < GroupCount && (uint)button < SlotsPerGroup ? group * SlotsPerGroup + button : -1;
        internal int Pointer { get; private set; } = -1;
        internal int Hovered { get; private set; } = -1;
        private float centerX, centerY, deadZone;
        internal void Begin(int pointer, float x, float y, float deadZoneRadius)
        {
            if (Pointer != -1) return;
            Pointer = pointer; centerX = x; centerY = y; deadZone = deadZoneRadius; Hovered = -1;
        }
        internal int Sample(int pointer, float x, float y, bool end, bool cancel)
        {
            if (Pointer == -1 || Pointer != pointer) return -1;
            Hovered = Hit(x - centerX, y - centerY, deadZone);
            if (!end && !cancel) return -1;
            int selected = cancel ? -1 : Hovered;
            Reset();
            return selected;
        }
        internal static int Hit(float dx, float dy, float deadZoneRadius)
        {
            if (dx * dx + dy * dy <= deadZoneRadius * deadZoneRadius) return -1;
            double angle = Math.Atan2(dx, -dy);
            int sector = (int)Math.Floor(angle / (Math.PI / 2) + .5);
            return (sector + GroupCount) % GroupCount;
        }
        internal static void Direction(int group, out float x, out float y)
        {
            if ((uint)group >= GroupCount) { x = y = 0f; return; }
            double angle = group * Math.PI / 2;
            x = (float)Math.Sin(angle); y = -(float)Math.Cos(angle);
        }
        internal static float ClampCenter(float desired, float min, float extent, float padding)
        {
            if (extent <= padding * 2) return min + extent * .5f;
            return Math.Max(min + padding, Math.Min(desired, min + extent - padding));
        }
        internal void Reset() { Pointer = -1; Hovered = -1; }
    }
}
