using System;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class Win05
{
    private bool positioned;
    public bool HistoryDialogVisible => cur == "historybar";

    private static bool Btn(Rect r, Texture2D[] frames, Event ev)
    {
        if (frames == null || frames[0] == null) return false;
        int index = r.Contains(ev.mousePosition) ? Input.GetMouseButton(0) ? 2 : 1 : 0;
        GUI.DrawTexture(r, frames[index] ?? frames[0]);
        if (!GUI.Button(r, GUIContent.none, GUIStyle.none)) return false;
        PlayerController.SuppressWorldClick();
        return true;
    }
}
}
