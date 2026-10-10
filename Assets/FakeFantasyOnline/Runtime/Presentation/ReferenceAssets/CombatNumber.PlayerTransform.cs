using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Original shared player Transform boundary. Number rendering remains a separate owner.
public partial class CombatNumber : MonoBehaviour
{
    private static Transform playerT;
    private static Transform petT;
    private static float playerHeadY;
    private static float petHeadY;
    private static float regPpu = 100f;
    public static float Ppu => regPpu;
    public static void RegisterPlayer(Transform t, float headWorldY, float ppu)
    {
        playerT = t;
        playerHeadY = headWorldY;
        regPpu = ppu;
    }
    public static Transform PlayerTransform => playerT;
    public static Transform PetTransform => petT;
    public static void RegisterPet(Transform t, float headWorldY)
    {
        petT = t;
        petHeadY = headWorldY;
    }
    internal static void BindPlayerTransformForIndependentRuntime(Transform player) => playerT = player;
}

}
