using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class EndgamePanel
{
    private float cubeWindowX = float.NaN, cubeWindowY, cubeWindowScale;
    private int cubeWindowGeneration = -1;
    private Rect CubeScreenRect(float scale)
    {
        if (WinDrag.Stale(cubeWindowX, ref cubeWindowGeneration) || cubeWindowScale != scale)
        {
            cubeWindowX = (Screen.width - CubeWindowWidth * scale) * .5f;
            cubeWindowY = (Screen.height - CubeWindowHeight * scale) * .5f;
            cubeWindowScale = scale;
        }
        return new Rect(cubeWindowX, cubeWindowY, CubeWindowWidth * scale, CubeWindowHeight * scale);
    }
    private void HandleCubeDrag(Rect window, float scale)
    {
        if (!SysMsgBox.Visible && !InventoryDlg.HasHeld)
            WinDrag.Handle(this, new Rect(window.x + 105 * scale, window.y + 36 * scale, 225 * scale, 27 * scale), ref cubeWindowX, ref cubeWindowY);
    }
    private static readonly Color CubeGold = new Color(1f, .92f, .66f, 1f);
    private const string CubeBackgroundPath = "res/uires/composedlg/composedlg.gsa";
    private const string CubeClosePath = "res/uires/common/miniclose.gsa";
    private static Rect CubeCellRect(int cell) => new Rect(116 + cell % CubeColumns * 43, 81 + cell / CubeColumns * 43, 40, 40);
    private static Texture2D CubeTexture(string path, int frame = 0)
    {
        Texture2D[] frames = ArtworkSkin.Frames(path, frame + 1);
        return frames != null && frames.Length > frame ? frames[frame] ?? frames[0] : null;
    }
    private static void CubePatch(Rect destination, Rect source)
    {
        Texture2D texture = CubeTexture(CubeBackgroundPath);
        if (texture != null) GUI.DrawTextureWithTexCoords(destination, texture,
            new Rect(source.x / CubeWindowWidth, 1f - source.yMax / CubeWindowHeight, source.width / CubeWindowWidth, source.height / CubeWindowHeight));
    }
    private static void DrawCubeArtwork()
    {
        Texture2D background = CubeTexture(CubeBackgroundPath);
        if (background != null) GUI.DrawTexture(new Rect(0, 0, CubeWindowWidth, CubeWindowHeight), background);
        // Keep the original complete furnace silhouette, title strip, outer borders,
        // native close control. Only replace its recipe/material interior
        // using that very panel's own unlabelled surface; never draw a new frame.
        CubePatch(new Rect(97, 73, 258, 342), new Rect(105, 85, 216, 198));
        CubePatch(new Rect(97, 381, 258, 95), new Rect(105, 85, 216, 95));
        CubeText(new Rect(132, 44, 177, 12), "周天鼎", CubeGold);
    }
    private static void DrawCubeCell(Rect rect, bool selected)
    {
        Color old = GUI.color;
        if (selected) GUI.color = new Color(1.25f, 1.18f, .88f, 1f);
        CubePatch(rect, new Rect(116, 329, 40, 40));
        GUI.color = old;
    }
    private static void CubeText(Rect rect, string text, Color color)
    {
        if (BitmapFont.Ready) BitmapFont.CenterOutline(rect, BitmapFont.Elide(text, rect.width, 1f), color, 1f);
    }
    private static bool CubeNativeButton(Rect rect, string path, string text, bool enabled)
    {
        bool hover = enabled && rect.Contains(Event.current.mousePosition);
        Texture2D texture = CubeTexture(path, !enabled ? 3 : hover ? 1 : 0);
        if (texture != null) GUI.DrawTexture(rect, texture);
        // CraftUI.Click includes the native modal blocker and world-click suppression.
        return enabled && CraftUI.Click(rect, Event.current.mousePosition);
    }
}
}
