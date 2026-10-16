using System;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class PlayerController
{
    private void HandlePickupKey()
    {
        if (!Input.GetKeyDown(KeyCode.Space)) return;
        if (ChatUI.TypingNow || MailDlg.Typing || GossipDlg.Typing || Win05.GmTyping || GUIUtility.keyboardControl != 0) return;
        PickupNearestForIndependentRuntime();
    }

    // Space and the mobile pickup control share eligibility, range, navigation and feedback.
    internal void PickupNearestForIndependentRuntime()
    {
        var item = GroundItems.NearestPickable(worldX, worldY, 600f);
        if (item.Item1 >= 0) WalkToPickup(item.Item1, item.Item2, item.Item3);
        else ChatUI.Add("系统", "附近没有可拾取的物品", new Color(200f / 255f, 200f / 255f, 200f / 255f, 1f));
    }
}
}
