using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameHud
{

    public static void StartSkillCooldown(int skill, float seconds)
    {
        if (skill < 1 || seconds <= 0f) return;
        skillCd[skill] = new Vector2(Time.time, seconds);
    }

    private static float SkillCdFrac(int skill)
    {
        if (!skillCd.TryGetValue(skill, out Vector2 state)) return 0f;
        float elapsed = Time.time - state.x;
        if (elapsed >= state.y)
        {
            skillCd.Remove(skill);
            return 0f;
        }
        return 1f - elapsed / state.y;
    }

    // Native overlay body; the existing HUD view supplies its actual frame collection until
    // the original GameHud instance and complete OnGUI lifecycle are integrated.
    internal static bool DrawSkillCooldownForIndependentRuntime(Rect area, int skill, IReadOnlyList<Texture2D> frames)
    {
        if (frames == null) return false;
        float fraction = SkillCdFrac(skill);
        if (fraction <= 0f) return false;
        Texture2D frame = frames[CooldownFrameIndexForIndependentRuntime(fraction)];
        if (frame == null) return false;
        Color saved = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 180f / 255f);
        GUI.DrawTexture(area, frame);
        GUI.color = saved;
        return true;
    }

    internal static int CooldownFrameIndexForIndependentRuntime(float fraction) =>
        Mathf.Clamp(Mathf.FloorToInt((1f - fraction) * 36f), 0, 36);

    internal static int SkillCooldownCountForIndependentRuntime => skillCd.Count;
    internal static float SkillCooldownFractionForIndependentRuntime(int skill) => SkillCdFrac(skill);
}

}
