using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatNumber
{
    private static float lastSpawnT = -9f;
    private static int spawnStack;
    private static int healFrame = -1;
    private static int healThisFrame;

    public static void SpawnOn(int target, int value, int type)
    {
        TrySpawnOnForIndependentRuntime(target, value, type);
    }

    // The retained experience journal needs to distinguish a temporarily missing anchor or
    // glyph/font from a completed presentation. The public native entry point still ignores
    // the result, including its ordinary heal-rate and missing-target drops.
    internal static bool TrySpawnOnForIndependentRuntime(int target, int value, int type)
    {
        if (type == 5)
        {
            if (Time.frameCount != healFrame)
            {
                healFrame = Time.frameCount;
                healThisFrame = 0;
            }
            if (++healThisFrame > 2) return true;
        }
        Transform targetTransform;
        float headY;
        if (target == 1 && !CharData.riding)
        {
            targetTransform = petT;
            headY = petHeadY;
        }
        else
        {
            targetTransform = playerT;
            headY = playerHeadY;
        }
        if (!targetTransform) return false;
        int nextStack = Time.time - lastSpawnT < 0.3f ? spawnStack + 1 : 0;
        float offset = (14f / regPpu) * nextStack;
        Vector3 position = targetTransform.position + new Vector3(0f, headY + offset, 0f);
        if (!TrySpawnForIndependentRuntime(position, value, type, regPpu, targetTransform))
            return false;
        // Failed retries must not accumulate offsets while assets are unavailable.
        spawnStack = nextStack;
        lastSpawnT = Time.time;
        return true;
    }
}

}
