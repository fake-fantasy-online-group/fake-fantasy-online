using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class RemotePlayer
{
    private SpriteRenderer body;
    private PlayerAvatar avatar;
    private bool acting;
    private bool flip;
    private int[] pathX;
    private int[] pathY;
    private int pathIdx;
    private bool moving;

    private Vector3 WorldToLocal(float wx, float wy)
    {
        return new Vector3((wx - worldW * 0.5f) / ppu, (worldH * 0.5f - wy) / ppu, 0f);
    }

    private int SortFor(float wy) => Mathf.Clamp((int)wy, 0, 30000);

    private PlayerAvatar.Pose StandPose(int dir)
    {
        return avatar != null && avatar.ok ? avatar.StandAt(Mathf.Clamp(dir, 0, 4), Time.time) : null;
    }

    public void SetPath(int[] xs, int[] ys, int speed)
    {
        if (xs == null || ys == null || xs.Length == 0 || ys.Length != xs.Length) return;
        if (speed < 0)
        {
            worldX = xs[0];
            worldY = ys[0];
            pathX = null;
            pathY = null;
            pathIdx = 0;
            moving = false;
            catchUp = 1f;
            transform.position = WorldToLocal(Mathf.Round(worldX), Mathf.Round(worldY));
            if (body)
            {
                body.sortingOrder = SortFor(worldY);
                ApplyPose(StandPose(curDir), flip, SortFor(worldY));
            }
            return;
        }

        if (speed != 0) moveSpeed = speed;
        float now = Time.time;
        float interval = lastPathTime < 0f ? 0.3f : Mathf.Clamp(now - lastPathTime, 0.05f, 2f);
        lastPathTime = now;
        float dx = xs[xs.Length - 1] - worldX;
        float dy = ys[ys.Length - 1] - worldY;
        float distance = Mathf.Sqrt(dx * dx + dy * dy);
        if (distance > 260f)
        {
            worldX = xs[xs.Length - 1];
            worldY = ys[ys.Length - 1];
            pathX = null;
            pathY = null;
            pathIdx = 0;
            moving = false;
            catchUp = 1f;
            transform.position = WorldToLocal(Mathf.Round(worldX), Mathf.Round(worldY));
            if (body) body.sortingOrder = SortFor(worldY);
            return;
        }

        catchUp = moveSpeed > 0f && distance > interval * moveSpeed + 40f ? 1.6f : 1f;
        pathX = xs;
        pathY = ys;
        pathIdx = 0;
        moving = false;
        while (pathIdx < xs.Length)
        {
            dx = xs[pathIdx] - worldX;
            dy = ys[pathIdx] - worldY;
            if (dx * dx + dy * dy > 4f)
            {
                moving = true;
                acting = false;
                break;
            }
            pathIdx++;
        }
    }

    private void AdvancePath()
    {
        long stamp = LoadTrace.On ? LoadTrace.SegStamp() : 0L;
        try
        {
            if (!moving || body == null) return;
            float oldX = worldX;
            float oldY = worldY;
            float remaining = moveSpeed * catchUp * Time.deltaTime;
            while (remaining > 0f && pathX != null && pathIdx < pathX.Length)
            {
                float tx = pathX[pathIdx];
                float ty = pathY[pathIdx];
                float dx = tx - worldX;
                float dy = ty - worldY;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                if (!(remaining >= distance) && !(0.01f >= distance))
                {
                    worldX += (dx / distance) * remaining;
                    worldY += (dy / distance) * remaining;
                    break;
                }
                remaining -= distance;
                pathIdx++;
                worldX = tx;
                worldY = ty;
            }
            if (pathX == null || pathIdx >= pathX.Length) moving = false;
            transform.position = WorldToLocal(Mathf.Round(worldX), Mathf.Round(worldY));
            body.sortingOrder = SortFor(worldY);
            float movedX = worldX - oldX;
            float movedY = worldY - oldY;
            if (movedX * movedX + movedY * movedY > 1f)
            {
                int oct = (Mathf.RoundToInt(Mathf.Atan2(movedY, movedX) * Mathf.Rad2Deg / 45f) + 8) % 8;
                curDir = dirByOct[oct];
                flip = flipByOct[oct];
            }
        }
        finally
        {
            if (LoadTrace.On) LoadTrace.AddSeg((LoadTrace.Seg)7, stamp);
        }
    }

    internal SpriteRenderer BodyForIndependentRuntime => body;

    // No avatar may be handed in from outside. RemotePlayer.Init (0x06000af6) produces the spawn-time
    // avatar through the official ShouldHideNow/TakeBuildBudget gate into Rebuild (0x06000b06), the
    // owner's own missing-avatar update branch re-tries it later, and RemotePlayer.OnDestroy
    // (0x06000b37) is the only disposer. A host-supplied avatar would either replace the gated build
    // (disposing it) or bypass the gate entirely, so callers read AvatarForIndependentRuntime instead.

    internal void AdvancePathForIndependentRuntime() => AdvancePath();
    internal float MoveSpeedForIndependentRuntime => moveSpeed;
    internal float CatchUpForIndependentRuntime => catchUp;
    internal int PathIndexForIndependentRuntime => pathIdx;
    internal int PathCountForIndependentRuntime => pathX == null ? 0 : pathX.Length;
    internal bool MovingForIndependentRuntime => moving;
    internal void StopMovingForIndependentRuntime() => moving = false;
    internal bool ActingForIndependentRuntime { get => acting; set => acting = value; }
    internal int DirectionForIndependentRuntime { get => curDir; set => curDir = value; }
    internal bool FlipForIndependentRuntime { get => flip; set => flip = value; }
}

}
