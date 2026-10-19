using UnityEngine;

namespace FakeFantasyOnline.Presentation
{
/// <summary>
/// Source-owned artwork included in every Player, including the resource-free installer.
/// This must never resolve through the downloaded game-resource root or package loaders.
/// </summary>
public static class EmbeddedClientBackground
{
    public const string ResourcePath = "FakeFantasyOnline/Bootstrap/TaoistCloudSanctuary";
    private static Texture2D background;
    private static bool loadAttempted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForPlaySession()
    {
        background = null;
        loadAttempted = false;
    }

    /// <summary>Load once on the main thread, before resource verification if possible.</summary>
    public static void Preload()
    {
        if (loadAttempted) return;
        loadAttempted = true;
        background = Resources.Load<Texture2D>(ResourcePath);
        if (background == null)
            Debug.LogWarning("The embedded client background is missing; using the independent startup fallback.");
    }

    /// <summary>Draw an opaque, centered aspect-cover background in the caller's GUI coordinates.</summary>
    public static void Draw(Rect viewport)
    {
        if (viewport.width <= 0 || viewport.height <= 0) return;
        Preload();
        Color previousColor = GUI.color;
        try
        {
            GUI.color = background != null ? Color.white : new Color(.035f, .07f, .11f, 1f);
            GUI.DrawTexture(viewport, background != null ? background : Texture2D.whiteTexture,
                ScaleMode.ScaleAndCrop, false);
        }
        finally { GUI.color = previousColor; }
    }
}
}
