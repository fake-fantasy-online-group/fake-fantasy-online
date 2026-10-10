using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameHud
{

    public static bool PosOverHotbar(Vector2 point)
    {
        for (int i = 0; i < 30; i++)
            if (hotSlotRect[i].width > 0f && hotSlotRect[i].Contains(point)) return true;
        return false;
    }

    // GameHud.OnGUI top-mode key branch: ARM 0x42cef0 / x86 0x442a79.
    internal static int TopHotbarKeySlotForIndependentRuntime(KeyCode key)
    {
        int column = (int)key - (int)KeyCode.F1;
        if ((uint)column >= 10u) return -1;
        int index = unchecked(hotbarRow * 10 + column);
        return (uint)index < 30u && hotSkill[index] != 0 ? index : -1;
    }

    internal static float HotbarXForIndependentRuntime(float scale, float width) =>
        Mathf.Max(175f, ((width / scale - 179f + 171f) * 0.5f) - 214f);

    internal static void BeginHotbarRowsForIndependentRuntime() => Array.Clear(hotSlotRect, 0, 30);
    internal static void BindHotbarRectForIndependentRuntime(int index, Rect rect) => hotSlotRect[index] = rect;

    internal static bool HotbarButtonForIndependentRuntime(Texture2D texture, Rect rect)
    {
        if (texture != null) GUI.DrawTexture(rect, texture);
        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    internal void DrawTopHotbarChromeForIndependentRuntime(int row, float x, float y, float scale)
    {
        // HBX/sh are the row capture; the texture and late T scale belong to this receiver.
        Texture2D background = accelbarbk;
        TexScale.Draw(x * scale, y, background, 0f, 0f, s);
        if (hotbarRow == row && accelEffect != null)
            GUI.DrawTexture(new Rect((x - 3f) * scale, y - 2f * scale, 434f * scale, 49f * scale), accelEffect);
    }

    internal static void DrawTopHotbarLabelsForIndependentRuntime(float y, float width, float scale, Texture2D texture)
    {
        if (texture == null) return;
        float x = HotbarXForIndependentRuntime(scale, width);
        GUI.DrawTexture(new Rect((x + 5f) * scale, y, 400f * scale, texture.height * scale), texture);
    }
}

}
