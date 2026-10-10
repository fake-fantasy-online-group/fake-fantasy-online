using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameHud
{
    internal static float BottomHotbarScaleForIndependentRuntime(float scale, float width) =>
        scale * Mathf.Min(1f, width / (800f * scale));

    internal static float BottomHotbarXForIndependentRuntime(float scale, float width) =>
        width / scale * 0.5f - 418f;

    internal static int BottomHotbarRowCountForIndependentRuntime(int row) => row == 1 ? 20 : 10;
    private static float BottomRowX0(int row) => row == 0 ? 41f : 32f;
    private static float BottomRowStep(int row) => row == 0 ? 38f : 39f;
    private static float BottomRowHit(int row) => row == 0 ? 34f : 33f;

    internal static Rect BottomHotbarSlotRectForIndependentRuntime(int row, int column, float x, float y, float scale)
    {
        float size = BottomRowHit(row) * scale;
        return new Rect((x + BottomRowX0(row) + BottomRowStep(row) * column) * scale,
            y + (row == 0 ? 8f : 4f) * scale, size, size);
    }

    internal static Rect BottomHotbarIconRectForIndependentRuntime(int row, Rect hit, float scale)
    {
        float inset = (BottomRowHit(row) - 30f) * 0.5f * scale;
        return new Rect(hit.x + inset, hit.y + inset, 30f * scale, 30f * scale);
    }

    internal void DrawBottomHotbarChromeForIndependentRuntime(int row, float x, float y, float scale)
    {
        if (row == 0)
        {
            Texture2D background = hb24Bk;
            TexScale.Draw(x * scale, y, background, 0f, 0f, s);
            return;
        }
        if (hb24Block == null) return;
        for (int column = 0; column < BottomHotbarRowCountForIndependentRuntime(row); column++)
        {
            // Native DrawRow reloads the receiver texture for each admitted block.
            Texture2D block = hb24Block;
            GUI.DrawTexture(new Rect((x + BottomRowX0(row) - 4f + BottomRowStep(row) * column) * scale,
                y, 41f * scale, 41f * scale), block);
        }
    }

    internal static void DrawBottomHotbarLabelsForIndependentRuntime(int row, float x, float y,
        float scale, Texture2D texture)
    {
        if (texture == null) return;
        GUI.DrawTexture(new Rect((x + BottomRowX0(row)) * scale, y + (row == 0 ? 7f : 3f) * scale,
            BottomRowStep(row) * BottomHotbarRowCountForIndependentRuntime(row) * scale,
            texture.height * scale), texture);
    }

    // Original OnGUI CtrlIcon: crop legacy button art only in bottom mode.
    internal static void DrawControlIconForIndependentRuntime(Rect rect, Texture2D texture, bool bottom)
    {
        if (texture == null) return;
        if (bottom) GUI.DrawTextureWithTexCoords(rect, texture, new Rect(0f, 0.27272728f, 1f, 0.61818182f));
        else GUI.DrawTexture(rect, texture);
    }

    // Called only inside the original visible-body KeyDown gate.
    internal static int HotbarKeySlotForIndependentRuntime(KeyCode key, bool bottom, bool alt)
    {
        if (!bottom) return TopHotbarKeySlotForIndependentRuntime(key);
        int column = (int)key - (int)KeyCode.F1;
        bool functionKey = (uint)column < 10u;
        int index;
        if (functionKey) index = alt ? column + 20 : column;
        else
        {
            if (!alt) return -1;
            index = (int)key >= (int)KeyCode.Alpha1 && (int)key <= (int)KeyCode.Alpha9
                ? (int)key - (int)KeyCode.Alpha1 + 10
                : key == KeyCode.Alpha0 ? 19 : -1;
        }
        if ((uint)index >= 30u) return -1;
        if (hotSkill[index] == 0 && functionKey)
        {
            int alternate = alt ? column : column + 20;
            if ((uint)alternate < 30u && hotSkill[alternate] != 0) index = alternate;
        }
        return hotSkill[index] != 0 ? index : -1;
    }
}

}
