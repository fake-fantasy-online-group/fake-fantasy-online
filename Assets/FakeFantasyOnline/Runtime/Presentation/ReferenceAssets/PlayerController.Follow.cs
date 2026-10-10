using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    private float nextFollowT;
    private static float followLostT;
    private static float followLastX;
    private static float followLastY;
    private static bool followLastOk;

    public static void ClearFollow()
    {
        followId = 0;
        followLostT = 0f;
        followLastOk = false;
    }

    public void FollowMate(int id)
    {
        followId = followId == id ? 0 : id;
        followLostT = 0f;
        followLastOk = false;
        if (followId != 0)
        {
            sitting = false;
            followMode = false;
        }
    }

    // The original Start slot immediately after LoadMask. The selected ID survives map creation.
    internal void StartFollowForIndependentRuntime()
    {
        if (followId != 0) followLostT = 0f;
        followLastOk = false;
    }

    internal static int FollowIdForIndependentRuntime => followId;

    // Separate original Update writers: neither performs ClearFollow's additional resets.
    internal static void CancelFollowIdForIndependentRuntime() => followId = 0;

    // Original Update region between combat approach and its later stop gate.
    internal void AdvanceFollowForIndependentRuntime()
    {
        if (followId == 0 || sitting || followMode || Time.time < chantUntil) return;

        float wx, wy;
        if (CombatWorld.MatePos(followId, out wx, out wy))
        {
            followLostT = 0f;
            followLastX = wx;
            followLastY = wy;
            followLastOk = true;
            float dx = wx - worldX;
            float dy = wy - worldY;
            if (dx * dx + dy * dy > 12100f && Time.time >= nextFollowT)
            {
                nextFollowT = Time.time + 0.3f;
                SetPathTo(wx, wy, 0);
            }
        }
        else
        {
            if (followLastOk && !moveToTarget)
            {
                followLastOk = false;
                SetPathTo(followLastX, followLastY, 0);
            }
            if (moveToTarget)
            {
                followLostT = 0f;
            }
            else
            {
                float now = Time.time;
                if (followLostT == 0f)
                {
                    followLostT = now;
                }
                else if (now - followLostT > 8f)
                {
                    followId = 0;
                    followLostT = 0f;
                    followLastOk = false;
                    ChatUI.Add("系统", "跟随目标离得太远(或已离开本地图), 已取消跟随。",
                        new Color(1f, 180f / 255f, 120f / 255f, 1f));
                }
            }
        }
    }
}

}
