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
        I.rangePx = range > 0f ? range : DefaultRange;
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
        if (pc == null || Camera.main == null) return;
        if (GameHud.I != null && GameHud.I.MouseOverHud()) return;

        float worldW = pc.worldW;
        float worldH = pc.worldH;
        float ppu = pc.ppu;
        Vector3 pointerWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float gx = Mathf.Clamp(pointerWorld.x * ppu + worldW * 0.5f, 0f, worldW);
        float gy = Mathf.Clamp(worldH * 0.5f - pointerWorld.y * ppu, 0f, worldH);
        float effectiveRange = range > 0f ? range : DefaultRange;
        float playerX = pc.worldX;
        float playerY = pc.worldY;

        if (mode == Aim.Direction)
            pc.AimFace(new Vector3((gx - 0.5f * worldW) / ppu,
                (0.5f * worldH - gy) / ppu, 0f));

        int roundedX = (int)Math.Round((double)gx);
        int roundedY = (int)Math.Round((double)gy);
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
