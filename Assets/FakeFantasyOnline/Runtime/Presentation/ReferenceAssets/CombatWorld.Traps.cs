using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatWorld
{
    public static void SpawnTrap(int id, int skillId, int x, int y, int radiusPx)
    {
        Ensure();
        if (I.trapPos.ContainsKey(id)) return;
        Vector3 position = new Vector3((x - I.worldW * 0.5f) / I.ppu,
            (I.worldH * 0.5f - y) / I.ppu, 0f);
        I.trapPos[id] = position;
        I.trapSkill[id] = skillId;
        Win05.SkInfo skill = Win05.SkillInfo(skillId);
        float baseRadius = skill != null && skill.radius > 0 ? skill.radius : 100f;
        float scale = 1f;
        if (radiusPx > 0 && baseRadius > 0f) scale = Mathf.Clamp(radiusPx / baseRadius, 1f, 4f);
        I.trapScale[id] = scale;
        GameObject root = new GameObject(string.Format("Trap:{0}", id));
        root.transform.position = position;
        root.AddComponent<TrapVisual>().Load(581, I.ppu, y);
        if (scale > 1f) root.transform.localScale *= scale;
        I.trapObj[id] = root;
    }

    // The independent scene host owns these otherwise unparented native objects.
    // Release on host teardown without consulting another scene's static I or playing explosions.
    internal void ReleaseTrapsForIndependentRuntime()
    {
        foreach (GameObject value in trapObj.Values)
            if (value) Destroy(value);
        trapObj.Clear();
        trapPos.Clear();
        trapSkill.Clear();
        trapScale.Clear();
    }
}

}
