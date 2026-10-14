using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    public float worldX;
    public float worldY;
    private float tgtX;
    private float tgtY;
    private float pendX;
    private float pendY;
    private byte[] maskBytes;
    private int maskOff;
    private int maskW;
    private int maskH;
    private int maskCW = 10;
    private int maskCH = 10;


    // 0x060009ef
    private bool CellWalkable(int cx, int cy)
    {
        if (cx < 0 || cy < 0 || cx >= maskW || cy >= maskH) return false;
        return (maskBytes[unchecked(maskOff + cx + maskW * cy)] & 0x80) == 0 && RiftCellWalkable(cx, cy);
    }

    // 0x060009f0
    private bool LineWalkable(int x0, int y0, int x1, int y1)
    {
        if (!RiftCellEdgeWalkable(x0, y0, x1, y1)) return false;
        int dx = unchecked(x1 - x0);
        int dy = unchecked(y1 - y0);
        if (dx < 0) dx = unchecked(-dx);
        if (dy < 0) dy = unchecked(-dy);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int error = unchecked(dx - dy);
        while (true)
        {
            if (!CellWalkable(x0, y0)) return false;
            if (x0 == x1 && y0 == y1) return true;
            int doubled = unchecked(error * 2);
            if (doubled > unchecked(-dy))
            {
                error = unchecked(error - dy);
                x0 = unchecked(x0 + sx);
            }
            if (doubled < dx)
            {
                error = unchecked(error + dx);
                y0 = unchecked(y0 + sy);
            }
        }
    }

    // 0x060009f1
    private bool SnapWalkable(ref int cx, ref int cy)
    {
        if (CellWalkable(cx, cy)) return true;
        for (int radius = 1; radius <= 50; radius++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (Math.Abs(dx) != radius && Math.Abs(dy) != radius) continue;
                    int x = unchecked(cx + dx);
                    int y = unchecked(cy + dy);
                    if (!CellWalkable(x, y)) continue;
                    cx = x;
                    cy = y;
                    return true;
                }
            }
        }
        return false;
    }

    // 0x060009f2
    private static int Heur(int x, int y, int tx, int ty)
    {
        int dx = unchecked(tx - x);
        int dy = unchecked(ty - y);
        if (dx < 0) dx = unchecked(-dx);
        if (dy < 0) dy = unchecked(-dy);
        int diagonal = dx < dy ? dx : dy;
        return unchecked(14 * diagonal + 10 * (dx + dy - 2 * diagonal));
    }

    // 0x060009f6
    private void SetPathTo(float tx, float ty) => SetPathTo(tx, ty, 0);

    // 0x060009f7
    private void SetPathTo(float tx, float ty, int hops)
    {
        RefreshRiftCollision();
        if (hops == 0) { pluginNavigationRevision++; riftPathGoalX = tx; riftPathGoalY = ty; }
        path.Clear();
        pathIdx = 0;
        moveToTarget = false;
        pendHops = 0;
        if (maskBytes == null || maskW <= 0)
        {
            if (!RiftPathWalkable(worldX, worldY, tx, ty)) return;
            tgtX = tx;
            tgtY = ty;
            moveToTarget = true;
            return;
        }

        int sx = unchecked((int)(worldX / maskCW));
        int sy = unchecked((int)(worldY / maskCH));
        int gx = unchecked((int)(tx / maskCW));
        int gy = unchecked((int)(ty / maskCH));
        if (!CellWalkable(sx, sy)) return;
        int requestedX = gx;
        int requestedY = gy;
        if (!SnapWalkable(ref gx, ref gy)) return;
        bool exactEnd = gx == requestedX && gy == requestedY;
        if (sx == gx && sy == gy)
        {
            if (exactEnd && RiftPathWalkable(worldX, worldY, tx, ty))
            {
                tgtX = tx;
                tgtY = ty;
                moveToTarget = true;
            }
            return;
        }

        List<int> cells = new List<int>();
        if (AStarCells(sx, sy, gx, gy, cells, 30000))
        {
            BuildPath(cells, tx, ty, exactEnd);
            return;
        }
        if (hops > 15) return;

        List<(long Score, int X, int Y)> waypoints = new List<(long, int, int)>();
        float dirx = tx - worldX;
        float diry = ty - worldY;
        for (int dy = -4; dy <= 4; dy++)
        {
            AddWaypoint(waypoints, unchecked(sx - 4), unchecked(sy + dy), dirx, diry);
            AddWaypoint(waypoints, unchecked(sx + 4), unchecked(sy + dy), dirx, diry);
        }
        for (int dx = -3; dx <= 3; dx++)
        {
            AddWaypoint(waypoints, unchecked(sx + dx), unchecked(sy - 3), dirx, diry);
            AddWaypoint(waypoints, unchecked(sx + dx), unchecked(sy + 3), dirx, diry);
        }
        // Official SetPathTo iterates the whole descending-score ring with no cut-off on the
        // sign of the score: backward candidates stay eligible for the 2,000-expansion retry.
        waypoints.Sort((a, b) => b.Score.CompareTo(a.Score));
        foreach ((long Score, int X, int Y) waypoint in waypoints)
        {
            cells.Clear();
            if (!AStarCells(sx, sy, waypoint.X, waypoint.Y, cells, 2000)) continue;
            BuildPath(cells, tx, ty, false);
            pendX = tx;
            pendY = ty;
            pendHops = unchecked(hops + 1);
            break;
        }
    }

    // 0x060009f8
    private void AddWaypoint(List<(long Score, int X, int Y)> wps,
        int cx, int cy, float dirx, float diry)
    {
        if (!CellWalkable(cx, cy)) return;
        float dx = (cx + 0.5f) * maskCW - worldX;
        float dy = (cy + 0.5f) * maskCH - worldY;
        long score = unchecked((long)(dirx * dx + diry * dy));
        wps.Add((score, cx, cy));
    }

    // 0x060009f9
    private bool AStarCells(int sx, int sy, int gx, int gy, List<int> cells, int cap = 30000)
    {
        Dictionary<int, int> parents = new Dictionary<int, int>();
        Dictionary<int, int> costs = new Dictionary<int, int>();
        HashSet<int> closed = new HashSet<int>();
        SortedSet<(int Priority, int Cell)> open = new SortedSet<(int, int)>();
        int start = unchecked(sx + maskW * sy);
        costs[start] = 0;
        open.Add((Heur(sx, sy, gx, gy), start));
        int goal = unchecked(gx + maskW * gy);
        int expanded = 0;
        while (open.Count > 0)
        {
            (int Priority, int Cell) entry = open.Min;
            open.Remove(entry);
            int current = entry.Cell;
            if (current == goal)
            {
                while (true)
                {
                    cells.Add(current);
                    if (current == start) break;
                    current = parents[current];
                }
                cells.Reverse();
                return true;
            }
            if (!closed.Add(current)) continue;
            expanded = unchecked(expanded + 1);
            if (expanded > cap) return false;
            int x = current % maskW;
            int y = current / maskW;
            int cost = costs[current];
            for (int direction = 0; direction < 8; direction++)
            {
                int nx = unchecked(x + DX8[direction]);
                int ny = unchecked(y + DY8[direction]);
                if (!CellWalkable(nx, ny) || !RiftCellEdgeWalkable(x, y, nx, ny)) continue;
                int nextCost;
                if (direction < 4)
                {
                    nextCost = unchecked(cost + 10);
                }
                else
                {
                    if (!CellWalkable(unchecked(x + DX8[direction]), y) ||
                        !CellWalkable(x, unchecked(y + DY8[direction]))) continue;
                    nextCost = unchecked(cost + 14);
                }
                int next = unchecked(nx + maskW * ny);
                int previous;
                if (costs.TryGetValue(next, out previous) && nextCost >= previous) continue;
                costs[next] = nextCost;
                parents[next] = current;
                open.Add((unchecked(nextCost + Heur(nx, ny, gx, gy)), next));
            }
        }
        return false;
    }

    // 0x060009fa
    private void BuildPath(List<int> cells, float tx, float ty, bool exactEnd)
    {
        int last = cells.Count - 1;
        int current = 0;
        while (current < last)
        {
            int x = cells[current] % maskW;
            int y = cells[current] / maskW;
            int next = current + 1;
            int selected = last;
            while (selected > next && !LineWalkable(x, y,
                cells[selected] % maskW, cells[selected] / maskW)) selected--;
            path.Add(new Vector2((cells[selected] % maskW + 0.5f) * maskCW,
                (cells[selected] / maskW + 0.5f) * maskCH));
            current = selected;
            last = cells.Count - 1;
        }
        if (exactEnd && path.Count > 0)
        {
            Vector2 from = path.Count > 1 ? path[path.Count - 2] : new Vector2(worldX, worldY);
            if (RiftPathWalkable(from.x, from.y, tx, ty)) path[path.Count - 1] = new Vector2(tx, ty);
        }
        moveToTarget = path.Count > 0;
    }

    // Representation/lifecycle seams; navigation decisions stay in the original methods above.
    internal void SetPathToForIndependentRuntime(float tx, float ty, int hops = 0) =>
        SetPathTo(tx, ty, hops);

    internal float MovementTargetXForIndependentRuntime
    {
        get => tgtX;
        set => tgtX = value;
    }
    internal float MovementTargetYForIndependentRuntime
    {
        get => tgtY;
        set => tgtY = value;
    }
    internal float PendingTargetXForIndependentRuntime
    {
        get => pendX;
        set => pendX = value;
    }
    internal float PendingTargetYForIndependentRuntime
    {
        get => pendY;
        set => pendY = value;
    }
}

}
