using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class SkillTargeting
{
    // Official 1.5.8 06000d15. The renderers belong to the lazily created instance.
    public static void Begin(int skillId, Aim mode, float range, float radius,
        float angle, float rectL, float rectW, bool isTrap)
    {
        if (PlayerController.I == null) return;

        if (I == null)
        {
            var owner = new GameObject("SkillTargeting");
            I = owner.AddComponent<SkillTargeting>();
            I.LoadCircle();
            I.MakeArrow();
            I.MakeSelLine();
        }

        I.skillId = skillId;
        I.mode = mode;
        I.rangePx = AuthoritativeProfessionSkill.Uses(skillId)
            ? (range > 0f ? range : float.PositiveInfinity) : range > 0f ? range : DefaultRange;
        I.radiusPx = radius;
        I.fanDeg = angle;
        I.rectLen = rectL;
        I.rectWid = rectW;
        I.noCircle = isTrap;

        if (I.selLine != null)
        {
            Color color = OutlineColor(skillId);
            I.selLine.startColor = I.selLine.endColor = color;
        }

        I.Show(true);
        if (CursorManager.I != null) CursorManager.I.SetCastMode(true);
        Active = true;
    }

    // Official 1.5.8 06000d16. Immediate casting does not enter targeting mode.
    public static void CastImmediate(int skillId, Aim mode, float range,
        float radius, float angle, float rectL, float rectW, bool isTrap)
    {
        PlayerController pc = PlayerController.I;
        if (AuthoritativeProfessionSkill.Uses(skillId) && !AuthoritativeProfessionSkill.LearnedActive(skillId))
        { AuthoritativeProfessionSkill.Waiting(); return; }
        if (pc == null || Camera.main == null) return;
        if (GameHud.I != null && GameHud.I.MouseOverHud()) return;

        float worldW = pc.worldW;
        float worldH = pc.worldH;
        float ppu = pc.ppu;
        Vector3 pointerWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float gx = Mathf.Clamp(pointerWorld.x * ppu + worldW * 0.5f, 0f, worldW);
        float gy = Mathf.Clamp(worldH * 0.5f - pointerWorld.y * ppu, 0f, worldH);
        float effectiveRange = AuthoritativeProfessionSkill.Uses(skillId)
            ? (range > 0f ? range : float.PositiveInfinity) : range > 0f ? range : DefaultRange;
        float playerX = pc.worldX;
        float playerY = pc.worldY;

        if (mode == Aim.Direction)
            pc.AimFace(new Vector3((gx - 0.5f * worldW) / ppu,
                (0.5f * worldH - gy) / ppu, 0f));

        int roundedX = (int)Math.Round((double)gx);
        int roundedY = (int)Math.Round((double)gy);
        if (mode == Aim.Trap)
        {
            CastTrapAtPointer(skillId, roundedX, roundedY);
            return;
        }
        bool approach = false;
        if (mode == Aim.Circle)
        {
            float dx = gx - playerX;
            float dy = gy - playerY;
            approach = Mathf.Sqrt(dx * dx + dy * dy) > effectiveRange;
        }
        if (approach)
        {
            CombatWorld.RequestGroundCast(skillId, roundedX, roundedY, effectiveRange);
            return;
        }

        NetClient.PendingCastSkill = skillId;
        NetClient.PendingCastGround = new Vector3((gx - 0.5f * worldW) / ppu,
            (0.5f * worldH - gy) / ppu, 0f);
        if (NetClient.I != null)
            NetClient.I.UseSkillAt(skillId, roundedX, roundedY, 0);
    }

    // Official 1.5.8 06000d1d. Unity drives the original scene component independently.
    private void Update()
    {
        if (!Active) return;
        if (AuthoritativeProfessionSkill.Uses(skillId) && !AuthoritativeProfessionSkill.LearnedActive(skillId))
        { AuthoritativeProfessionSkill.Waiting(); End(); return; }

        // Equipment can change after the cursor opens, including a same-revision rank
        // refresh. Re-read the authoritative plan instead of retaining the old footprint.
        if (AuthoritativeProfessionSkill.TryResolve(skillId, out Aim refreshedMode, out float refreshedRange,
            out float refreshedRadius, out float refreshedAngle, out float refreshedLength, out float refreshedWidth, out bool refreshedTrap))
        {
            if (refreshedMode != mode) { End(); return; }
            rangePx = refreshedRange > 0f ? refreshedRange : float.PositiveInfinity;
            radiusPx = refreshedRadius; fanDeg = refreshedAngle; rectLen = refreshedLength; rectWid = refreshedWidth;
            noCircle = refreshedTrap;
        }

        PlayerController pc = PlayerController.I;
        if (pc == null || Camera.main == null)
        {
            End();
            return;
        }

        float worldW = pc.worldW;
        float worldH = pc.worldH;
        float ppu = pc.ppu;
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            End();
            return;
        }

        Vector3 pointerWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float gx = Mathf.Clamp(pointerWorld.x * ppu + worldW * 0.5f, 0f, worldW);
        float gy = Mathf.Clamp(worldH * 0.5f - pointerWorld.y * ppu, 0f, worldH);
        float playerX = pc.worldX;
        float playerY = pc.worldY;
        if (mode == Aim.Direction)
        {
            pc.AimFace(new Vector3((gx - 0.5f * worldW) / ppu,
                (0.5f * worldH - gy) / ppu, 0f));
            UpdateSelector(pc, ppu, worldW, worldH);
            UpdateOutline(pc, gx, gy, ppu, worldW, worldH);
        }
        else if (!noCircle)
        {
            UpdateCircle(gx, gy, ppu, worldW, worldH);
        }

        bool overHud = GameHud.I != null && GameHud.I.MouseOverHud();
        if (!Input.GetMouseButtonDown(0) || overHud) return;

        PlayerController.SuppressWorldClick();
        int roundedX = Mathf.RoundToInt(gx);
        int roundedY = Mathf.RoundToInt(gy);
        if (mode == Aim.Trap)
        {
            if (CastTrapAtPointer(skillId, roundedX, roundedY)) End();
            return;
        }
        bool approach = false;
        if (mode == Aim.Circle)
        {
            float dx = gx - playerX;
            float dy = gy - playerY;
            approach = Mathf.Sqrt(dx * dx + dy * dy) > rangePx;
        }
        if (approach)
        {
            CombatWorld.RequestGroundCast(skillId, roundedX, roundedY, rangePx);
        }
        else
        {
            NetClient.PendingCastSkill = skillId;
            NetClient.PendingCastGround = new Vector3((gx - 0.5f * worldW) / ppu,
                (0.5f * worldH - gy) / ppu, 0f);
            if (NetClient.I != null)
                NetClient.I.UseSkillAt(skillId, roundedX, roundedY, 0);
        }

        End();
    }

    // Official 1.5.8 06000d21. End is an instance member and retains I.
    private void End()
    {
        Active = false;
        Show(false);
        if (CursorManager.I != null) CursorManager.I.SetCastMode(false);
    }
}
}
