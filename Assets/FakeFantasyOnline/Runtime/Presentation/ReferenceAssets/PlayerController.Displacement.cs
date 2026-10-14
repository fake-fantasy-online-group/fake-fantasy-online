using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    public List<Rect> bridgeWalk;
    private float nextFollowPathT;
    public bool IsMoving { get; private set; }

    private bool IsWalkable(float wx, float wy)
    {
        if (DuelHud.Blocks(wx, wy)) return false;
        if (bridgeWalk != null)
        {
            for (int i = 0; i < bridgeWalk.Count; i++)
            {
                Rect area = bridgeWalk[i];
                if (wx >= area.xMin && wx <= area.xMax && wy >= area.yMin && wy <= area.yMax) return true;
            }
        }
        if (maskBytes == null || maskW <= 0 || worldW <= 0f || worldH <= 0f) return true;
        int x = unchecked((int)(wx / maskCW));
        if (x < 0 || x >= maskW) return false;
        int y = unchecked((int)(wy / maskCH));
        if (y < 0 || y >= maskH) return false;
        return (maskBytes[unchecked(maskOff + x + maskW * y)] & 0x80) == 0;
    }

    internal readonly struct DisplacementObservation
    {
        internal DisplacementObservation(float dx, float dy, bool moving, bool cleared, bool blocked, bool replanned)
        { X = dx; Y = dy; Moving = moving; PathCleared = cleared; Blocked = blocked; Replanned = replanned; }
        internal float X { get; }
        internal float Y { get; }
        internal bool Moving { get; }
        internal bool PathCleared { get; }
        internal bool Blocked { get; }
        internal bool Replanned { get; }
    }

    // Original Update displacement/collision/clamp segment. Observations never choose behavior.
    internal DisplacementObservation AdvanceDisplacementForIndependentRuntime()
    {
        float dx = 0f, dy = 0f;
        bool moving = false, cleared = false, blocked = false, replanned = ReplanRiftPathIfChanged();
        if (!sitting && moveToTarget)
        {
            if (path.Count > 0)
            {
                Vector2 waypoint = path[pathIdx];
                dx = waypoint.x - worldX;
                dy = waypoint.y - worldY;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float step = MovementStepForIndependentRuntime();
                if (step < distance)
                {
                    dx = (dx / distance) * step;
                    dy = (dy / distance) * step;
                }
                else
                {
                    pathIdx++;
                    if (pathIdx >= path.Count)
                    {
                        moveToTarget = false;
                        path.Clear();
                        cleared = true;
                        if (pendHops > 0)
                        {
                            int hops = pendHops;
                            pendHops = 0;
                            SetPathTo(pendX, pendY, hops);
                            replanned = true;
                        }
                    }
                }
            }
            else
            {
                dx = tgtX - worldX;
                dy = tgtY - worldY;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float step = MovementStepForIndependentRuntime();
                if (step < distance)
                {
                    dx = (dx / distance) * step;
                    dy = (dy / distance) * step;
                }
                else moveToTarget = false;
            }
            moving = dx != 0f || dy != 0f;
        }
        if (dx != 0f || dy != 0f)
        {
            if (MovementSegmentWalkable(worldX + dx, worldY + dy))
            {
                worldX += dx;
                worldY += dy;
            }
            else if (MovementSegmentWalkable(worldX + dx, worldY))
            {
                worldX += dx;
                dy = 0f;
            }
            else if (MovementSegmentWalkable(worldX, worldY + dy))
            {
                worldY += dy;
                dx = 0f;
            }
            else
            {
                moveToTarget = false;
                path.Clear();
                pathIdx = 0;
                nextFollowPathT = 0f;
                dx = dy = 0f;
                moving = false;
                cleared = blocked = true;
            }
        }
        worldX = Mathf.Clamp(worldX, 0f, worldW);
        worldY = Mathf.Clamp(worldY, 0f, worldH);
        return new DisplacementObservation(dx, dy, moving, cleared, blocked, replanned);
    }

    // Called after the original pickup/transfer slot, using live fields at that point.
    internal Vector3 ProjectMovementForIndependentRuntime()
    {
        Transform ownTransform = transform;
        float x = Mathf.Round(worldX - worldW * 0.5f);
        float y = Mathf.Round(worldH * 0.5f - worldY);
        Vector3 position = new Vector3(x / ppu, y / ppu, 0f);
        ownTransform.position = position;
        return position;
    }
    internal void PublishMovingForIndependentRuntime(DisplacementObservation step) =>
        IsMoving = step.Moving && (step.X != 0f || step.Y != 0f);
    internal float NextFollowPathTimeForIndependentRuntime
    {
        get => nextFollowPathT;
        set => nextFollowPathT = value;
    }
}

}
