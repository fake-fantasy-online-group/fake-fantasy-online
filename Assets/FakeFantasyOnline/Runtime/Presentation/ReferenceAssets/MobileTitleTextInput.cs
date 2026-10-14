using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Mobile-only title input. IMGUI never receives native password text: it draws a mask,
// while the OS secure keyboard edits the actual value. No persistence or diagnostics.
internal sealed class MobileTitleTextInput
{
    private TouchScreenKeyboard keyboard;
    private string fieldId;
    private string original;
    private int page = -1;
    internal Rect FocusRect { get; private set; }
    internal bool Editing => keyboard != null && keyboard.status == TouchScreenKeyboard.Status.Visible;

    internal void BeginPage(int nextPage)
    {
        if (page != nextPage) Close();
        page = nextPage;
    }

    internal string Draw(string id, Rect rect, string value, int limit, bool secure, GUIStyle style)
    {
        value = value ?? string.Empty;
        // Desktop simulation stays editable without pretending it exercised a device IME.
        if (!TouchScreenKeyboard.isSupported)
            return secure ? GUI.PasswordField(rect, value, '\u25cf', limit, style)
                : GUI.TextField(rect, value, limit, style);

        if (keyboard != null && fieldId == id)
        {
            FocusRect = rect;
            if (keyboard.status == TouchScreenKeyboard.Status.Canceled)
            {
                value = original;
                Close();
            }
            else
            {
                value = LimitText(keyboard.text, limit);
                if (keyboard.status != TouchScreenKeyboard.Status.Visible) Close();
                else if (Event.current != null && Event.current.type == EventType.MouseUp &&
                    !rect.Contains(Event.current.mousePosition)) Close();
            }
        }
        string display = secure ? new string('\u25cf', value.Length) : value;
        if (GUI.Button(rect, display, style))
        {
            if (keyboard == null || fieldId != id)
            {
                Close();
                original = value;
                fieldId = id;
                FocusRect = rect;
                // Default keyboard allows Chinese IME. Autocorrection and multiline are off;
                // secure is set at creation, never by reconstructing edits to a mask.
                keyboard = TouchScreenKeyboard.Open(value, TouchScreenKeyboardType.Default,
                    false, false, secure, false, string.Empty, limit);
                GUI.FocusControl(null);
            }
        }
        return value;
    }

    internal void FocusChanged(bool focused, bool android)
    {
        // Android sends focus=false when its keyboard takes focus. Closing here would
        // dismiss it immediately. Real app backgrounding is handled by OnApplicationPause.
        if (!focused && !(android && Editing)) Close();
    }

    internal void Close()
    {
        if (keyboard != null) keyboard.active = false;
        keyboard = null;
        fieldId = null;
        original = null;
        FocusRect = new Rect();
    }

    internal static string LimitText(string value, int limit)
    {
        value = value ?? string.Empty;
        if (value.Length <= limit) return value;
        // Preserve the existing UTF-16 field limits without cutting a surrogate pair.
        int length = limit;
        if (length > 0 && char.IsHighSurrogate(value[length - 1])) length--;
        return value.Substring(0, length);
    }

    internal static Rect VisibleArea(Rect safe, Rect keyboardArea, bool editing)
    {
        if (!editing) return safe;
        // Unity reports keyboard bounds from the screen bottom. Some Android keyboards
        // report zero bounds; keep a conservative upper editing region in that case.
        float top = keyboardArea.height > 0f ? Screen.height - keyboardArea.yMax
            : safe.y + safe.height * .55f;
        safe.height = Mathf.Max(1f, Mathf.Min(safe.yMax, top) - safe.y);
        return safe;
    }

    internal static float PanY(float offset, float scale, Rect focused, Rect visible)
    {
        float bottom = offset + focused.yMax * scale;
        if (bottom > visible.yMax - 12f) offset -= bottom - (visible.yMax - 12f);
        float top = offset + focused.y * scale;
        if (top < visible.y + 8f) offset += visible.y + 8f - top;
        return offset;
    }
}
}
