using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameHud
{
    // Official 1.5.8 0600042d. Each edge obtains whiteTexture separately; color
    // is restored only on normal return, without an added exception policy.
    private static void DrawHudBorder(Rect r, float t, Color c)
    {
        Color saved = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.x, r.height + r.y - t, r.width, t), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.width + r.x - t, r.y, t, r.height), Texture2D.whiteTexture);
        GUI.color = saved;
    }

    // Official 1.5.8 0600044f. Unlike DrawHudBorder, this body captures one
    // whiteTexture reference and leaves the caller's GUI color untouched.
    private static void DrawRectOutline(Rect r, float t)
    {
        Texture2D texture = Texture2D.whiteTexture;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), texture);
        GUI.DrawTexture(new Rect(r.x, r.height + r.y - t, r.width, t), texture);
        GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), texture);
        GUI.DrawTexture(new Rect(r.width + r.x - t, r.y, t, r.height), texture);
    }
}

}
