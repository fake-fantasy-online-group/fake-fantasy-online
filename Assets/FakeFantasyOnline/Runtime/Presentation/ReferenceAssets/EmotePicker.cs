using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public static class EmotePicker
    {
        private const int Count = 96;
        private const int Cols = 8;
        private const int Pad = 6, VisRows = 6;
        private const int Cell = 22;
        private const float Icon = 20f;
        private static Texture2D[] icons;
        private static Vector2 scroll;

        // Official 0x060002e8.
        public static float PanelW(float scale) => 204f * scale;

        // Official 0x060002e9.
        public static float PanelH(float scale) => 144f * scale;

        // Official 0x060002ea. The managed declaration orders Event before scale.
        public static int Draw(Rect rect, Event e, float scale)
        {
            EnsureFrames();
            ArtworkSkin.Panel(rect, scale);

            Rect view = new Rect(rect.x + 6f * scale, rect.y + 6f * scale,
                rect.width - 12f * scale, rect.height - 12f * scale);
            float contentHeight = 12f * Cell * scale;
            Rect content = new Rect(0f, 0f, view.width - 16f * scale, contentHeight);
            scroll = GUI.BeginScrollView(view, scroll, content, false, true, GUIStyle.none, GUIStyle.none);
            int selected = 0;
            for (int index = 0; index < Count; index++)
            {
                Rect cell = new Rect((index & 7) * Cell * scale, (index >> 3) * Cell * scale,
                    Icon * scale, Icon * scale);
                ArtworkSkin.Row(cell, scale, cell.Contains(e.mousePosition));
                Texture2D texture = icons == null ? null : icons[index];
                if (texture != null)
                    GUI.DrawTexture(new Rect(cell.x, cell.y, Icon * scale, Icon * scale), texture,
                        ScaleMode.ScaleToFit, true);
                if (GUI.Button(cell, GUIContent.none, GUIStyle.none)) selected = index + 1;
            }
            GUI.EndScrollView();
            NativeScroll.Bar(typeof(EmotePicker), view, contentHeight, ref scroll);
            return selected;
        }

        private static void EnsureFrames()
        {
            if (icons != null || ResLoader.I == null) return;
            icons = new Texture2D[Count];
            for (int index = 0; index < Count; index++)
                icons[index] = ResLoader.I.LoadGsaFrame("res/biaoqing/" + (index + 1).ToString("D2") + ".gsa", 0);
        }
    }
}
