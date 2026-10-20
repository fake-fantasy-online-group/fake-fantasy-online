using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    // IDs describe physical controls, never a hotbar binding or the active group.
    // The same six skill positions are shared by all four groups (24 bindings).
    internal enum MobileControlId
    {
        Joystick, Attack, Pickup, GroupWheel,
        Skill0, Skill1, Skill2, Skill3, Skill4, Skill5,
        Mount, HealthPotion, ManaPotion, Sit
    }

    internal readonly struct MobileControlPlacement
    {
        internal readonly float CenterX, CenterY, Scale;
        internal MobileControlPlacement(float centerX, float centerY, float scale)
        {
            CenterX = centerX; CenterY = centerY; Scale = scale;
        }
    }

    /// <summary>
    /// Value-only layout state. Centers are normalized within the current GUI safe area.
    /// Missing controls retain the caller's defaults; no screen, map, chat or binding state
    /// is stored here. Resolving another orientation never mutates the saved placement.
    /// </summary>
    internal sealed class MobileControlLayout
    {
        internal const int ControlCount = 14;
        internal const float MinScale = .65f;
        internal const float MaxScale = 1.5f;
        internal const float MinimumTouchSize = 44f;
        private readonly Dictionary<MobileControlId, MobileControlPlacement> placements =
            new Dictionary<MobileControlId, MobileControlPlacement>();

        internal int Count => placements.Count;

        internal MobileControlLayout Clone()
        {
            var copy = new MobileControlLayout();
            foreach (var entry in placements) copy.placements.Add(entry.Key, entry.Value);
            return copy;
        }

        // Reset an editing copy. Neither clearing nor cloning reads/writes any file.
        internal void Clear() => placements.Clear();
        internal bool TryGet(MobileControlId id, out MobileControlPlacement placement) =>
            placements.TryGetValue(id, out placement);

        internal Rect ResolveRect(MobileControlId id, Rect defaultRect, Rect safe, float uiScale, float defaultScale = 1f)
        {
            if (!ValidRect(safe)) return default(Rect);
            if (!Finite(uiScale) || uiScale <= 0f) uiScale = 1f;
            if (!ValidRect(defaultRect))
                defaultRect = new Rect(safe.x + safe.width * .5f - .5f, safe.y + safe.height * .5f - .5f, 1f, 1f);
            if (placements.TryGetValue(id, out MobileControlPlacement placement))
                return Fit(defaultRect, safe, uiScale, placement.Scale,
                    safe.x + placement.CenterX * safe.width, safe.y + placement.CenterY * safe.height);
            // Defaults are fallbacks, never persisted overrides. Keep the original
            // baseline so existing saved sizes retain their exact v1 meaning.
            if (!Finite(defaultScale) || defaultScale < MinScale || defaultScale > MaxScale) defaultScale = 1f;
            return Fit(defaultRect, safe, uiScale, defaultScale, defaultRect.center.x, defaultRect.center.y);
        }

        // The editor passes GUI coordinates. Clamp the entire touch region, then persist
        // its normalized center. Invalid transient samples do not corrupt the edit copy.
        internal bool Set(MobileControlId id, Vector2 center, float sizeMultiplier,
            Rect defaultRect, Rect safe, float uiScale)
        {
            if (!ValidId(id) || !Finite(center.x) || !Finite(center.y) || !Finite(sizeMultiplier) ||
                !ValidRect(defaultRect) || !ValidRect(safe) || !Finite(uiScale) || uiScale <= 0f)
                return false;
            float scale = Clamp(sizeMultiplier, MinScale, MaxScale);
            Rect fitted = Fit(defaultRect, safe, uiScale, scale, center.x, center.y);
            return SetNormalized(id,
                Clamp((fitted.center.x - safe.x) / safe.width, 0f, 1f),
                Clamp((fitted.center.y - safe.y) / safe.height, 0f, 1f), scale);
        }

        // The parser is deliberately stricter than the drag/resize path: a damaged
        // persisted value rejects the whole file rather than silently changing its meaning.
        internal bool SetNormalized(MobileControlId id, float centerX, float centerY, float scale)
        {
            if (!ValidId(id) || !Finite(centerX) || !Finite(centerY) || !Finite(scale) ||
                centerX < 0f || centerX > 1f || centerY < 0f || centerY > 1f ||
                scale < MinScale || scale > MaxScale) return false;
            placements[id] = new MobileControlPlacement(centerX, centerY, scale);
            return true;
        }

        private static Rect Fit(Rect baseline, Rect safe, float uiScale, float sizeMultiplier,
            float centerX, float centerY)
        {
            // Double intermediates also keep extreme but finite samples from overflowing.
            double minimum = MinimumTouchSize * (double)uiScale;
            double width = Math.Max(minimum, baseline.width * (double)sizeMultiplier);
            double height = Math.Max(minimum, baseline.height * (double)sizeMultiplier);
            double fit = Math.Min(1d, Math.Min(safe.width / width, safe.height / height));
            float w = (float)(width * fit), h = (float)(height * fit);
            // A safe area smaller than 44 logical pixels must win over the touch minimum.
            float x = Clamp(centerX - w * .5f, safe.x, safe.x + safe.width - w);
            float y = Clamp(centerY - h * .5f, safe.y, safe.y + safe.height - h);
            return new Rect(x, y, w, h);
        }

        internal static bool ValidId(MobileControlId id) => (uint)id < ControlCount;
        private static bool ValidRect(Rect rect) =>
            Finite(rect.x) && Finite(rect.y) && Finite(rect.width) && Finite(rect.height) &&
            rect.width > 0f && rect.height > 0f && Finite(rect.x + rect.width) && Finite(rect.y + rect.height);
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
    }
}
