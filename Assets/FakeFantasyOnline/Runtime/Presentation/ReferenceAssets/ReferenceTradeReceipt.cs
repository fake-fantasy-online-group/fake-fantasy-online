using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// The three recovered Dispatch trade tails. No retained callbacks, UI snapshot writes,
// Active-edge inference or retry: both receive adapters invoke this same owner sequence.
internal static partial class ReferenceTradeReceipt
{
    internal static void Open(int otherEntityId, string otherName)
    {
        SocialClient.ResetTrade();
        SocialClient.TradeActive = true;
        SocialClient.OtherId = otherEntityId;
        SocialClient.OtherName = otherName;
        DealDlg.Open();
    }

    internal static void State(WorldActiveTradeState state)
    {
        // This receipt does not write partner identity/TradeActive and never opens a window.
        SocialClient.ApplyTradeStateReceiptForIndependentRuntime(state);
    }

    internal static void End(WorldTradeEndState end)
    {
        SocialClient.ResetTrade();
        DealDlg.Hide();
        ChatUI.Add("系统", end.Text, end.ColorSelector == 0
            ? new Color32(255, 180, 120, 255)
            : new Color32(150, 255, 150, 255));
    }
}
}
