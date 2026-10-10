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
        if (type == 5)
        {
            if (Time.frameCount != healFrame)
            {
                healFrame = Time.frameCount;
                healThisFrame = 0;
            }
            if (++healThisFrame > 2) return;
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
        if (!targetTransform) return;
        if (Time.time - lastSpawnT < 0.3f) spawnStack++;
        else spawnStack = 0;
        lastSpawnT = Time.time;
        float offset = (14f / regPpu) * spawnStack;
        Vector3 position = targetTransform.position + new Vector3(0f, headY + offset, 0f);
        Spawn(position, value, type, regPpu, targetTransform);
    }
}

}
