using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameHud
{
    private Texture2D hb24Bk;
    internal void BindBottomBackgroundForIndependentRuntime(Texture2D texture) => hb24Bk = texture;
    public static float BottomHotbarHeight(float s, float width)
    {
        if (!GameSettings.HotbarBottom || I == null) return 0f;
        bool missing = I.hb24Bk == null;
        if (width >= s * 1600f || missing) return 0f;
        float sh = s * Mathf.Min(1f, width / (s * 800f));
        return sh * 4f + sh * (hotbarExpanded ? 95f : 50f);
    }
}
}
