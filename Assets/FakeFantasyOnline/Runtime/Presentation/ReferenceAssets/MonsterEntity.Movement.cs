using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official MonsterEntity path-state owner.
/// </summary>
public partial class MonsterEntity
{
    public float worldX;
    public float worldY;

    private const float SnapPx = 260f;
    private const float CatchUpMul = 1.6f;
    private const float LagTolPx = 40f;
    private const float DefaultLagSec = 0.25f;
    private const float MinLagSec = 0.05f;
    private const float MaxLagSec = 2f;
    private int[] pathX;
    private int[] pathY;
    private int pathIdx;
    public bool friendly;
    private float fidgetTimer;

    public void SetPath(int[] xs, int[] ys, int speed)
    {
        if (xs == null || ys == null || xs.Length == 0 || ys.Length != xs.Length)
        {
            return;
        }

        if (speed < 0)
        {
            worldX = xs[0];
            worldY = ys[0];
            pathX = null;
            pathY = null;
            pathIdx = 0;
            moving = false;
            catchUp = 1f;
            if (body != null)
            {
                transform.position = new Vector3(
                    (Mathf.Round(worldX) - worldW * 0.5f) / ppu,
                    (worldH * 0.5f - Mathf.Round(worldY)) / ppu,
                    0f);
                body.sortingOrder = Mathf.Clamp((int)worldY, 0, 30000);
                body.sprite = StandSprite(curDir);
            }
            return;
        }

        if (speed != 0)
        {
            moveSpeed = speed;
        }

        // Official 0600063e reads UnityEngine.Time.get_time once, after the entry gate and the
        // speed < 0 warp arm above have both returned (arm64 0x48f7ac, x86_64 0x4a6caa), and
        // derives the catch-up lag window from the gap to the previous accepted path:
        // DefaultLagSec while lastPathTime (0x188) is still negative,
        // otherwise the elapsed time clamped into [MinLagSec, MaxLagSec]. lastPathTime is written
        // with the same reading, including on the greater-than-SnapPx warp below.
        float now = Time.time;
        float lagSec = lastPathTime >= 0f
            ? Mathf.Clamp(now - lastPathTime, MinLagSec, MaxLagSec)
            : DefaultLagSec;
        lastPathTime = now;

        int last = xs.Length - 1;
        float deltaX = xs[last] - worldX;
        float deltaY = ys[last] - worldY;
        float distance = Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);
        if (distance > SnapPx && !friendly)
        {
            worldX = xs[last];
            worldY = ys[last];
            pathX = null;
            pathY = null;
            pathIdx = 0;
            moving = false;
            catchUp = 1f;
            if (body != null)
            {
                transform.position = new Vector3(
                    (Mathf.Round(worldX) - worldW * 0.5f) / ppu,
                    (worldH * 0.5f - Mathf.Round(worldY)) / ppu,
                    0f);
                body.sortingOrder = Mathf.Clamp((int)worldY, 0, 30000);
            }
            return;
        }

        catchUp = !friendly && moveSpeed > 0f &&
                  lagSec * moveSpeed + LagTolPx < distance
            ? CatchUpMul
            : 1f;
        pathX = xs;
        pathY = ys;
        pathIdx = 0;
        moving = false;
        while (pathIdx < pathX.Length)
        {
            deltaX = pathX[pathIdx] - worldX;
            deltaY = pathY[pathIdx] - worldY;
            if (deltaX * deltaX + deltaY * deltaY > 4f)
            {
                moving = true;
                fidgetTimer = 0f;
                fidgetFrame = -1;
                return;
            }

            pathIdx++;
        }
    }

    private void AdvancePath()
    {
        // Official 06000641 tests moving (0x134) first, then the renderer through
        // UnityEngine.Object.op_Equality against null: arm64 calls
        // UnityEngine_CoreModule_dll__UnityEngine_Object__op_Equality__104570c and x86_64
        // calls FUN_01038840 at 0x4a931a. A destroyed renderer therefore leaves the whole
        // member as a no-op.
        if (!moving || body == null)
        {
            return;
        }

        float previousX = worldX;
        float previousY = worldY;
        float remaining = moveSpeed * catchUp * Time.deltaTime;
        if (remaining > 0f && pathX != null)
        {
            while (pathIdx < pathX.Length)
            {
                float deltaX = pathX[pathIdx] - worldX;
                float deltaY = pathY[pathIdx] - worldY;
                float distance = Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);
                if (remaining < distance && distance > 0.01f)
                {
                    // Official 06000641 normalises the leg first and scales afterwards:
                    // x86_64 0x4a9444 issues divps (delta/distance) and 0x4a944b then mulps
                    // that unit vector by the remaining distance; the Cpp2IL body keeps the
                    // same two steps, so the division is the inner operation here.
                    worldX += remaining * (deltaX / distance);
                    worldY += remaining * (deltaY / distance);
                    break;
                }

                remaining -= distance;
                worldX = pathX[pathIdx];
                worldY = pathY[pathIdx];
                pathIdx++;
                if (remaining <= 0f)
                {
                    break;
                }
            }
        }

        if (pathX == null || pathIdx >= pathX.Length)
        {
            moving = false;
        }

        transform.position = new Vector3(
            (Mathf.Round(worldX) - worldW * 0.5f) / ppu,
            (worldH * 0.5f - Mathf.Round(worldY)) / ppu,
            0f);
        body.sortingOrder = Mathf.Clamp((int)worldY, 0, 30000);

        float facingX;
        float facingY;
        if (pathX != null && pathIdx < pathX.Length)
        {
            facingX = pathX[pathIdx] - worldX;
            facingY = pathY[pathIdx] - worldY;
        }
        else
        {
            facingX = worldX - previousX;
            facingY = worldY - previousY;
        }

        if (facingX * facingX + facingY * facingY <= 0.25f)
        {
            return;
        }

        int octant =
            (Mathf.RoundToInt(
                Mathf.Atan2(facingY, facingX) * Mathf.Rad2Deg / 45f) + 8) % 8;
        curDir = dirByOct[octant];
        flip = flipByOct[octant];
    }
}

}
