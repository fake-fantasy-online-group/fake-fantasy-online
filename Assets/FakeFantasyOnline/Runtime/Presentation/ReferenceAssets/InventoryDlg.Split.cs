using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class InventoryDlg
{
    public static InventoryDlg I;
    private float s;
    private static bool splitOpen, splitDrag;
    private static byte splitTab, splitZone, splitWhTab;
    private static int splitSlot, splitMax, splitVal, splitItemId;
    private static Texture2D splitIcon;
    private static Vector2 splitWin;
    private static string splitText;
    private static GUIStyle _splitNumSt;
    internal Texture2D splitBk, splitSlide;
    internal Texture2D[] splitOk, spinL, spinR, splitMinBtn, splitMaxBtn;

    // Official 1.5.8 06000516–06000517. Editing may temporarily hold zero; button changes cannot.
    private void SetSplitVal(int value)
    { splitVal = Mathf.Clamp(value, 1, splitMax); splitText = splitVal.ToString(); }
    private GUIStyle SplitNumStyle()
    {
        if (_splitNumSt == null)
        {
            _splitNumSt = new GUIStyle(GUIStyle.none) { alignment = TextAnchor.MiddleCenter, font = NpcChatter.CnFont() };
            _splitNumSt.normal.textColor = Color.white;
        }
        _splitNumSt.fontSize = Mathf.RoundToInt(12 * s);
        return _splitNumSt;
    }

    // Official 06000528: bag count excludes one retained item and caps the input at 999.
    private void OpenSplit(byte tab, int slot, int count, int itemId, Texture2D icon, Vector2 mouse)
    {
        splitOpen = true; splitZone = 0; splitTab = tab; splitSlot = slot; splitItemId = itemId; splitIcon = icon;
        splitMax = Mathf.Clamp(unchecked(count - 1), 1, SplitMaxCap);
        splitVal = Mathf.Clamp(count / 2, 1, splitMax); splitText = splitVal.ToString();
        splitWin = new Vector2(Mathf.Clamp(mouse.x - 131 * s, 0, Screen.width - 147 * s),
            Mathf.Clamp(mouse.y - 37 * s, 0, Screen.height - 41 * s));
        splitDrag = false;
    }
    public static bool SplitOpen => splitOpen;
    // Official 0600052a uses UIScale directly, so warehouse opening does not require an inventory instance.
    public static void OpenSplitWarehouse(byte whTab, int slot, int count, Texture2D icon, Vector2 mouse)
    {
        float scale = UIScale.Factor;
        splitOpen = true; splitZone = 1; splitWhTab = whTab; splitTab = 0; splitSlot = slot; splitItemId = 0; splitIcon = icon;
        splitMax = Mathf.Clamp(unchecked(count - 1), 1, SplitMaxCap);
        splitVal = Mathf.Clamp(count / 2, 1, splitMax); splitText = splitVal.ToString();
        splitWin = new Vector2(Mathf.Clamp(mouse.x - 131 * scale, 0, Screen.width - 147 * scale),
            Mathf.Clamp(mouse.y - 37 * scale, 0, Screen.height - 41 * scale));
        splitDrag = false;
    }
    public static void CloseSplit() { splitOpen = false; splitDrag = false; splitIcon = null; }
    // Official 0600052c, direct original-image ARM instructions 0x464314–0x464330.
    private Rect SL(float x, float y, float width, float height)
        => new Rect(splitWin.x + x * s, splitWin.y + y * s, width * s, height * s);

    // Official 06000537: the slider tracks the knob centre over an 80-pixel travel.
    private void SetSplitFromX(float mouseX, float travel, float knobWidth, float originX)
    {
        float fraction = travel > 0 ? Mathf.Clamp01((mouseX - knobWidth * .5f - originX) / travel) : 0;
        SetSplitVal(1 + Mathf.RoundToInt(fraction * (splitMax - 1)));
    }
    // Official 06000538: parse fallback precedes clamp; close after dispatch, even without NetClient.
    private static void CommitSplit()
    {
        int amount = int.TryParse(splitText, out int value) ? value : splitVal;
        amount = Mathf.Clamp(amount, 1, splitMax);
        if (NetClient.I != null)
        {
            if (splitZone == 1) NetClient.I.WarehouseSplitForIndependentRuntime(splitWhTab, splitSlot, amount);
            else NetClient.I.SplitItemForIndependentRuntime(splitTab, splitSlot, amount);
        }
        CloseSplit();
    }
    // Official 0600052d. MouseUp only ends dragging; any MouseDown participates, not only button zero.
    private void SplitInput(Event ev)
    {
        Rect panel = SL(0, 0, 147, 41), slider = SL(21, 27, 104, 14), edit = SL(24, 7, 54, 12);
        float travel = 80 * s, knobWidth = 24 * s, originX = slider.x;
        Vector2 mouse = ev.mousePosition;
        if (ev.type == EventType.KeyDown && (ev.keyCode == KeyCode.Return || ev.keyCode == KeyCode.KeypadEnter))
        { CommitSplit(); ev.Use(); return; }
        if (ev.type == EventType.KeyDown && ev.keyCode == KeyCode.Escape)
        { CloseSplit(); ev.Use(); return; }
        if (ev.type == EventType.MouseUp) { splitDrag = false; return; }
        if (ev.type == EventType.MouseDrag && splitDrag)
        { SetSplitFromX(mouse.x, travel, knobWidth, originX); ev.Use(); return; }
        if (ev.type != EventType.MouseDown) return;
        if (!panel.Contains(mouse)) { CloseSplit(); ev.Use(); return; }
        if (edit.Contains(mouse)) return;
        if (SL(2, 3, 15, 20).Contains(mouse)) SetSplitVal(splitVal - 1);
        else if (SL(83, 3, 15, 20).Contains(mouse)) SetSplitVal(splitVal + 1);
        else if (SL(2, 26, 18, 10).Contains(mouse)) SetSplitVal(1);
        else if (SL(124, 28, 18, 10).Contains(mouse)) SetSplitVal(splitMax);
        else if (SL(100, 2, 44, 22).Contains(mouse)) CommitSplit();
        else if (slider.Contains(mouse)) { splitDrag = true; SetSplitFromX(mouse.x, travel, knobWidth, originX); }
        ev.Use();
    }
    // Official 0600052e/06000539. Only the three-character text field owns keyboard editing.
    private void SplitDraw()
    {
        Rect panel = SL(0, 0, 147, 41);
        if (splitBk != null) GUI.DrawTexture(panel, splitBk);
        else
        {
            GUI.color = new Color(.1f, .1f, .12f, .95f); GUI.DrawTexture(panel, Texture2D.whiteTexture); GUI.color = Color.white;
        }
        Vector2 mouse = Event.current.mousePosition;
        GUI.SetNextControlName("splitNum");
        string text = GUI.TextField(SL(24, 7, 54, 12), splitText ?? "", 3, SplitNumStyle());
        if (text != splitText)
        {
            splitText = new string(Array.FindAll((text ?? "").ToCharArray(), char.IsDigit));
            if (!int.TryParse(splitText, out int value)) splitVal = 0;
            else if (value > splitMax) { splitVal = splitMax; splitText = splitVal.ToString(); }
            else splitVal = Mathf.Max(0, value);
        }
        DrawSplitButton(SL(2, 3, 15, 20), spinL, mouse);
        DrawSplitButton(SL(83, 3, 15, 20), spinR, mouse);
        DrawSplitButton(SL(2, 26, 18, 10), splitMinBtn, mouse);
        DrawSplitButton(SL(124, 28, 18, 10), splitMaxBtn, mouse);
        DrawSplitButton(SL(100, 2, 44, 22), splitOk, mouse);
        float fraction = splitMax > 1 ? (float)(splitVal - 1) / (splitMax - 1) : 0;
        Rect knob = SL(21 + 80 * fraction, 27, 24, 14);
        if (splitSlide != null) GUI.DrawTexture(knob, splitSlide);
        else
        {
            GUI.color = new Color(.7f, .7f, .8f, 1); GUI.DrawTexture(knob, Texture2D.whiteTexture); GUI.color = Color.white;
        }
    }
    private static void DrawSplitButton(Rect r, Texture2D[] frames, Vector2 mouse)
    {
        Texture2D texture = frames != null && frames[0] != null
            ? (r.Contains(mouse) ? frames[1] ?? frames[0] : frames[0]) : null;
        if (texture != null) GUI.DrawTexture(r, texture);
    }
    internal static Rect SplitRectForIndependentRuntime(float scale) => new Rect(splitWin.x, splitWin.y, 147 * scale, 41 * scale);
    internal static bool SplitDraggingForIndependentRuntime => splitDrag;
    internal static int SplitMaximumForIndependentRuntime => splitMax;
    internal static int SplitValueForIndependentRuntime => splitVal;
    internal static string SplitTextForIndependentRuntime => splitText;
    internal void SetSplitValueForIndependentRuntime(int value) => SetSplitVal(value);
    internal static void SetSplitTextForIndependentRuntime(string value)
    {
        splitText = value ?? "";
        if (int.TryParse(splitText, out int n)) { splitVal = Mathf.Clamp(n, 0, splitMax); if (n > splitMax) splitText = splitMax.ToString(); }
        else splitVal = 0;
    }
    internal void SetSplitFromXForIndependentRuntime(float x, float scale) => SetSplitFromX(x, 80 * scale, 24 * scale, splitWin.x + 21 * scale);
    internal static void CommitSplitForIndependentRuntime() => CommitSplit();
    // Host supplies lifecycle/assets only; both bag and warehouse execute the same native methods.
    internal void OpenSplitForIndependentRuntime(byte tab, int slot, int count, int itemId, Texture2D icon, Vector2 mouse, float scale)
    { s = scale; OpenSplit(tab, slot, count, itemId, icon, mouse); }
    internal void SplitInputForIndependentRuntime(Event ev, float scale)
    { s = scale; SplitInput(ev); }
    internal void SplitDrawForIndependentRuntime(float scale)
    { s = scale; SplitDraw(); }
}
}
