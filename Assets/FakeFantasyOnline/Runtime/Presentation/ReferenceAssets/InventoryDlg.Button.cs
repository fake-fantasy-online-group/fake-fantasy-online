using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class InventoryDlg
{
    // Official 0x06000531: physical held-button state also applies during repaint.
    internal bool Btn(Rect r, Texture2D[] fr, Event ev)
    {
        if (fr == null || fr[0] == null) return false;
        int index = r.Contains(ev.mousePosition) ? (Input.GetMouseButton(0) ? 2 : 1) : 0;
        Texture2D texture = fr[index] ?? fr[0];
        GUI.DrawTexture(r, texture);
        return GUI.Button(r, GUIContent.none, GUIStyle.none);
    }
}
}
