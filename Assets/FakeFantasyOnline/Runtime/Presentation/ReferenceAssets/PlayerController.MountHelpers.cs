using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    private float rideAnchorLastX;
    private float rideAnchorLastY;
    private bool rideAnchorMoving;
    private Sprite[,] ringCxGrid;
    private float[] ringCxAvg;
    private int ringCxFill = -1;

    private void FollowRideAnchor()
    {
        RemotePlayer player = CombatWorld.FindPlayer(CharData.rideAnchorId);
        if (!player)
        {
            rideAnchorMoving = false;
            return;
        }
        float x = player.worldX, y = player.worldY;
        rideAnchorMoving = Mathf.Abs(x - rideAnchorLastX) > 0.01f || Mathf.Abs(y - rideAnchorLastY) > 0.01f;
        rideAnchorLastX = x;
        rideAnchorLastY = y;
        worldX = x < 0f ? 0f : x > worldW ? worldW : x;
        worldY = y < 0f ? 0f : y > worldH ? worldH : y;
        Transform target = transform;
        float rx = (float)System.Math.Round(worldX - worldW * 0.5f);
        float ry = (float)System.Math.Round(worldH * 0.5f - worldY);
        target.position = new Vector3(rx / ppu, ry / ppu, 0f);
    }

    private float MountRingCx(Sprite[,] grid, int dd)
    {
        if (grid == null) return 0f;
        int fill = mountData == null ? 0 : mountData.FillVersion;
        if (!ReferenceEquals(ringCxGrid, grid) || ringCxFill != fill)
        {
            ringCxGrid = grid;
            ringCxFill = fill;
            ringCxAvg = null;
        }
        if (ringCxAvg == null)
        {
            int dirs = grid.GetLength(0), frames = grid.GetLength(1);
            ringCxAvg = new float[dirs];
            for (int d = 0; d < dirs; d++)
            {
                float sum = 0f;
                int count = 0;
                for (int f = 0; f < frames; f++)
                    if (grid[d, f])
                    {
                        sum += grid[d, f].bounds.center.x;
                        count = unchecked(count + 1);
                    }
                ringCxAvg[d] = count > 0 ? sum / count : 0f;
            }
        }
        return dd >= 0 && dd < ringCxAvg.Length ? ringCxAvg[dd] : 0f;
    }
}

internal static partial class CharData
{
}

}
