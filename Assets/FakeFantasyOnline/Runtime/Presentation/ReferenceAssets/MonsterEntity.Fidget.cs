using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official MonsterEntity standing and hand-fidget lifecycle.
/// </summary>
public partial class MonsterEntity
{
    private const float FidgetCheck = 0.5f;
    private float fidgetFrameTimer;

    private void UpdateFidget()
    {
        // Official 1.5.8 UpdateFidget (06000656) has no body gate of its own: arm64 0x492660 and
        // x86_64 0x4a9d20 both reach the first body use after StandSprite and take the IL2CPP null
        // helper (arm64 FUN_00177a74 / x86_64 FUN_0016c1b0), i.e. a NullReferenceException. The
        // `body` gate belongs to the caller Update (06000640), which calls this only when body is
        // live; adding a silent early return here would replace official throw semantics.
        if (fidgetFrame >= 0 && handFrames != null &&
            curDir < handFrames.GetLength(0) && handFrames.GetLength(1) > 0)
        {
            int frameCount = handFrames.GetLength(1);
            fidgetFrameTimer += Time.deltaTime;
            if (fidgetFrameTimer >= handFrameSec)
            {
                fidgetFrameTimer = 0f;
                fidgetFrame++;
            }

            if (fidgetFrame < frameCount)
            {
                // The fallback step is a RAW reference null test on both architectures of the
                // frozen 1.5.8 export (arm64 0x492660 export line 161 `lVar8 == 0`, x86_64
                // 0x4a9d20 export line 62 `lVar8 == 0`, no UnityEngine.Object call), while the
                // gate that precedes set_sprite is UnityEngine.Object.op_Inequality (arm64 lines
                // 167/173, x86_64 FUN_0103dde0). `??` reproduces the raw test: a destroyed
                // (fake-null) hand frame keeps its slot and reaches the Unity gate instead of
                // being replaced by RowAny. This restores the compiled null semantics; it is not
                // a claim that the official C# source text used ?? or any other spelling.
                Sprite sprite = handFrames[curDir, fidgetFrame] ??
                    RowAny(handFrames, curDir);
                if (sprite != null)
                {
                    body.sprite = sprite;
                    body.flipX = flip;
                }
                return;
            }

            fidgetFrame = -1;
            body.sprite = StandSprite(curDir);
            return;
        }

        fidgetFrame = -1;
        if (standFrames != null)
        {
            int frameCount = standFrames.GetLength(1);
            if (frameCount < 2 || standIntervalMs <= 0f)
            {
                standFrame = 0;
            }
            else
            {
                standFrameTimer += Time.deltaTime * 1000f;
                if (standFrameTimer >= standIntervalMs)
                {
                    standFrameTimer = Mathf.Min(
                        standFrameTimer - standIntervalMs,
                        standIntervalMs);
                    standFrame = (standFrame + 1) % frameCount;
                }
            }
        }

        body.sprite = StandSprite(curDir);
        body.flipX = flip;
        if (handAct < 1)
        {
            return;
        }

        fidgetTimer += Time.deltaTime;
        if (fidgetTimer < FidgetCheck)
        {
            return;
        }

        fidgetTimer = 0f;
        if (fidgetRng.Next(5) != 0)
        {
            return;
        }

        EnsureHand();
        if (handFrames == null || curDir >= handFrames.GetLength(0) ||
            handFrames.GetLength(1) < 1)
        {
            return;
        }

        fidgetFrame = 0;
        fidgetFrameTimer = 0f;
    }
}

}
