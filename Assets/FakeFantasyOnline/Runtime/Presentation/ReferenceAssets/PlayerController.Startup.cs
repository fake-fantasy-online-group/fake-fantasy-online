using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    public List<MapLink.MapProc> transferProcs;
    public Action<int, float, float> onTransfer;
    private bool wasInsideTransfer;
    private bool transferred;

    // Original Unity Start owns this body even when an optional world view was not admitted.
    private void Start()
    {
        LoadMask();
        if (followId != 0) followLostT = 0f;
        followLastOk = false;
        if (transferProcs != null)
        {
            Vector2 point = new Vector2(worldX, worldY);
            foreach (MapLink.MapProc proc in transferProcs)
            {
                if (MapLink.PointInPoly(point, proc.poly))
                {
                    wasInsideTransfer = true;
                    break;
                }
            }
        }
    }

    // Retained explicit diagnostic entry; production invokes the Unity Start message only.
    internal void StartForIndependentRuntime() => Start();

    internal bool WasInsideTransferForIndependentRuntime
    {
        get => wasInsideTransfer;
        set => wasInsideTransfer = value;
    }
    internal bool TransferredForIndependentRuntime
    {
        get => transferred;
        set => transferred = value;
    }
}

}
