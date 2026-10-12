using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    /// Native UiInput text controls. Static state intentionally mirrors the original focus/menu owner.
    internal static class UiInput
    {
        private const float ITEM_W = 62f;
        private const float ITEM_H = 17f;
        private static string _menuOwner = string.Empty;
        private static Vector2 _menuAt;
        private static float _menuScale = 1f;
        // Official .cctor (06000fd4) builds the four rows in this order from the literals 17256
        // 复制 / 17892 粘贴 / 17135 剪切 / 17755 清空 and stores them at UiInput static +0x18.
        private static readonly string[] ITEMS = { "复制", "粘贴", "剪切", "清空" };
        internal static bool MenuOpen => !string.IsNullOrEmpty(_menuOwner);
        internal static string MenuOwnerForIndependentRuntime => _menuOwner;
        // Official 06000fcd: MouseOver takes no argument. It reads the live mouse position and
        // converts the screen y into GUI space, exactly as the native body does (Screen.height - y).
        internal static bool MouseOver()
        {
            if (!MenuOpen) return false;
            Vector2 pointer = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            return Rect(_menuAt, _menuScale).Contains(pointer);
        }
        // Official 06000fce: Rect(Vector2 at, float scale) is a pure function of its arguments and
        // the static item table; it does not read the stored menu position.
        internal static Rect Rect(Vector2 at, float scale) =>
            new Rect(at.x, at.y, ITEM_W * scale, ITEM_H * ITEMS.Length * scale);
        // Official 06000fd1 stores owner, position and scale in that order; a non-positive scale
        // falls back to 1 (arm64 `if (s <= 0.0) s = 1.0`, x86_64 blendvps against the 1.0 literal).
        internal static void OpenMenu(string owner, Vector2 at, float scale)
        { _menuOwner = owner ?? string.Empty; _menuAt = at; _menuScale = scale <= 0f ? 1f : scale; }
        internal static void Close() { _menuOwner = string.Empty; }

        // Official 06000fd2 has no MenuOpen test: the body never reads the owner string. The only
        // native caller is UiInput.Draw (06000fd1), which reaches it when the stored owner equals
        // the control name, so a closed menu simply never gets here.
        internal static string DrawMenu(string value, int maximum)
        {
            Event e = Event.current;
            float scale = _menuScale;
            float x = _menuAt.x;
            float y = _menuAt.y;
            float width = ITEM_W * scale;
            float rowHeight = ITEM_H * scale;
            float height = rowHeight * ITEMS.Length;
            Rect menu = new Rect(x, y, width, height);

            // Draw the frame outside the original hit rectangle so its 7px corners do not
            // consume the compact 17px command rows. Owner, clipboard and dismissal stay intact.
            ArtworkSkin.Panel(new Rect(x - 5f * scale, y - 5f * scale,
                width + 10f * scale, height + 10f * scale), scale);

            // Official reads the system clipboard once before the rows are drawn, not inside the
            // click arm: it feeds both the 粘贴 row shading and the paste itself.
            string clip = GUIUtility.systemCopyBuffer ?? string.Empty;
            for (int index = 0; index < ITEMS.Length; index++)
            {
                Rect row = new Rect(x, y + index * rowHeight, width, rowHeight);
                bool hover = row.Contains(e.mousePosition);
                if (hover)
                {
                    ArtworkSkin.Row(row, scale, true);
                }
                string source = index == 1 ? clip : value;
                int length = source.Length;
                if (BitmapFont.Ready) BitmapFont.DrawOutline(row.x + 6f * scale,
                    row.y + 2f * scale, ITEMS[index],
                    length != 0 ? new Color(1f, .92f, .66f, 1f) : new Color(.55f, .53f, .46f, 1f),
                    scale);
                if (hover && e.type == EventType.MouseDown && e.button == 0)
                {
                    // Official runs the command only when the text it works on is not empty; an
                    // empty source just closes the menu and consumes the click (arm64 joins the
                    // `if (iVar2 == 0)` arm with the close+Use arm, x86_64 skips the whole switch).
                    if (length != 0)
                    {
                        if (index == 0) GUIUtility.systemCopyBuffer = value;
                        else if (index == 1) value = Clamp(value + clip, maximum);
                        else if (index == 2) { GUIUtility.systemCopyBuffer = value; value = string.Empty; }
                        else value = string.Empty;
                    }
                    Close();
                    e.Use();
                    return value;
                }
            }

            if (e.type == EventType.MouseDown && !menu.Contains(e.mousePosition))
            {
                // Official closes on a click outside the menu without consuming the event.
                Close();
            }
            else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                Close();
                e.Use();
            }
            return value;
        }
        private static GUIStyle fallbackStyle;

        internal static string Clamp(string value, int maximum)
        {
            if (maximum > 0 && value == null) throw new ArgumentNullException(nameof(value));
            return maximum > 0 && value.Length > maximum ? value.Substring(0, maximum) : value;
        }

        internal static string Field(Rect r, string name, string value, int max, GUIStyle style, float scale)
            => Draw(r, name, value, max, style, scale, false);

        internal static string Area(Rect r, string name, string value, int max, GUIStyle style, float scale)
            => Draw(r, name, value, max, style, scale, true);

        internal static string Draw(Rect r, string name, string value, int max, GUIStyle style, float scale, bool multiline)
        {
            if (name == null) name = string.Empty;
            if (value == null) value = string.Empty;
            if (style == null)
            {
                fallbackStyle ??= new GUIStyle(GUIStyle.none);
                style = fallbackStyle;
            }
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 1 && r.Contains(e.mousePosition))
            {
                OpenMenu(name, e.mousePosition, scale);
                GUI.FocusControl(name);
                e.Use();
            }
            bool copy = false;
            string copyBefore = null;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.C && (e.control || e.command)
                && string.Equals(GUI.GetNameOfFocusedControl(), name, StringComparison.Ordinal))
            {
                copyBefore = GUIUtility.systemCopyBuffer ?? string.Empty;
                copy = true;
            }
            GUI.SetNextControlName(name);
            string result = multiline
                ? GUI.TextArea(r, value, max, style)
                : GUI.TextField(r, value, max, style);
            if (copy)
            {
                // Official copies the text the control returned, only when the control itself did
                // not change the clipboard, only when there is something to copy, and it never
                // consumes the Ctrl+C event.
                string copyAfter = GUIUtility.systemCopyBuffer ?? string.Empty;
                if (string.Equals(copyAfter, copyBefore, StringComparison.Ordinal) && result.Length > 0)
                {
                    GUIUtility.systemCopyBuffer = result;
                }
            }
            // Official 06000fd1 ends with `if (owner == name) result = DrawMenu(result, max)`, which
            // is the only native edge into the menu: the menu is drawn by the control that opened it.
            if (string.Equals(_menuOwner, name, StringComparison.Ordinal))
            {
                result = DrawMenu(result, max);
            }
            return result;
        }
    }
}
