using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Official collision predicate/state; Apply and the ring/HUD share these same fields.
public partial class DuelHud : MonoBehaviour
{
    public static bool active;
    public static byte phase;
    public static byte role;
    public static float cx;
    public static float cy;
    public static float r;
    public static float gap;
    private static int recvMapId;

    public static bool Blocks(float wx, float wy)
    {
        if (!active || phase == 0 || phase > 2 || GameBootstrap.CurMapId != recvMapId) return false;
        float dx = wx - cx;
        float dy = wy - cy;
        float distanceSquared = dx * dx + dy * dy;
        if (role == 1) return distanceSquared > r * r;
        float outer = r + gap;
        return distanceSquared < outer * outer;
    }
}

}
