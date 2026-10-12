using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class SkillTargeting
{
    // Official 06000d1e. Time and scale follow the native owner rather than injected substitutes.
    private void UpdateCircle(float gx, float gy, float ppu, float ww, float wh)
    {
        if (aimerSR == null || aimerFrames == null || aimerFrames.Length == 0) return;
        aimerSR.transform.position = new Vector3((gx - ww * 0.5f) / ppu,
            (wh * 0.5f - gy) / ppu, 0f);
        float scale = 1f;
        if (aimerTexW > 0f)
            scale = ((radiusPx + radiusPx) / ppu) / (aimerTexW / (aimerHdScale * 100f));
        aimerSR.transform.localScale = new Vector3(scale, scale, 1f);
        aimerTimer += Time.deltaTime;
        if (aimerTimer >= 1f / AimerFps)
        {
            aimerTimer = 0f;
            aimerIdx = (aimerIdx + 1) % aimerFrames.Length;
        }
        aimerSR.sprite = aimerFrames[aimerIdx];
    }

    // Official 06000d1f. Missing individual arrow frames preserve the previously selected sprite.
    private void UpdateSelector(PlayerController pc, float ppu, float ww, float wh)
    {
        if (arrowSR == null || arrowFrames == null || arrowFrames.Length == 0) return;
        arrowSR.transform.position = new Vector3((pc.worldX - ww * 0.5f) / ppu,
            (wh * 0.5f - pc.worldY) / ppu, 0f);
        arrowSR.transform.rotation = Quaternion.identity;
        int frame = Mathf.Clamp(pc.CurDir, 0, arrowFrames.Length - 1);
        if (arrowFrames[frame] != null) arrowSR.sprite = arrowFrames[frame];
        arrowSR.flipX = pc.Flip;
        arrowSR.transform.localScale = new Vector3(ArrowScale, ArrowScale, ArrowScale);
    }

    // Official 06000d20, including the captured local rotation helper 06000d23.
    private void UpdateOutline(PlayerController pc, float gx, float gy, float ppu, float ww, float wh)
    {
        if (selLine == null || ppu <= 0f) return;
        Vector3 feet = new Vector3((pc.worldX - ww * 0.5f) / ppu,
            (wh * 0.5f - pc.worldY) / ppu, 0f);
        Vector2 delta = new Vector2((gx - ww * 0.5f) / ppu - feet.x,
            (wh * 0.5f - gy) / ppu - feet.y);
        if (delta.sqrMagnitude < 0.000001f) return;
        float angle = Mathf.Atan2(delta.y, delta.x);
        float c = Mathf.Cos(angle);
        float s = Mathf.Sin(angle);
        Vector3 P(float lx, float ly) => feet + new Vector3(c * lx - s * ly, s * lx + c * ly, 0f);

        var positions = new List<Vector3>();
        if (rectLen > 0f)
        {
            float halfWidth = (rectWid / ppu) * 0.5f;
            positions.Add(P(0f, -halfWidth));
            float length = rectLen / ppu;
            positions.Add(P(length, -halfWidth));
            positions.Add(P(length, halfWidth));
            positions.Add(P(0f, halfWidth));
        }
        else
        {
            float radius = AuthoritativeProfessionSkill.Uses(skillId) ? radiusPx : radiusPx > 0f ? radiusPx : DefaultRadius;
            float halfAngle = AuthoritativeProfessionSkill.Uses(skillId) ? fanDeg * Mathf.Deg2Rad * 0.5f : fanDeg > 0f ? fanDeg * Mathf.Deg2Rad * 0.5f : 1.0471976f;
            positions.Add(P(0f, 0f));
            for (int index = 0; index <= FanArcSegs; index++)
            {
                float rotation = ((halfAngle + halfAngle) * index) / FanArcSegs - halfAngle;
                positions.Add(P((radius / ppu) * Mathf.Cos(rotation),
                    (radius / ppu) * Mathf.Sin(rotation)));
            }
        }
        selLine.widthMultiplier = OutlineWidthPx / ppu;
        selLine.positionCount = positions.Count;
        selLine.SetPositions(positions.ToArray());
    }
}
}
