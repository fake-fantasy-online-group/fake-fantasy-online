using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameBootstrap
{
    private bool nativeChatSendBound;
    private void BindNativeChatSendForIndependentRuntime()
    {
        if (nativeChatSendBound) return;
        ChatUI.OnSend += SendNativeChatForIndependentRuntime;
        nativeChatSendBound = true;
    }
    private void ReleaseNativeChatSendForIndependentRuntime()
    {
        if (!nativeChatSendBound) return;
        ChatUI.OnSend -= SendNativeChatForIndependentRuntime;
        nativeChatSendBound = false;
    }
    // Official GameBootstrap.<Start>b__151_0 (060003fe).
    private static void SendNativeChatForIndependentRuntime(string channel, string text)
    {
        List<(byte, byte, int)> shares = ChatUI.OutgoingShares;
        ChatUI.OutgoingShares = new List<(byte, byte, int)>();
        if (text?.Trim() == "/擂台") { DuelDlg.Open(0, "", 1); return; }
        if (!string.IsNullOrEmpty(channel) && (channel.StartsWith("交谈:") || channel.StartsWith("密语:")))
        {
            NetClient.I?.GossipForIndependentRuntime(channel.Substring(3), text, shares);
            return;
        }
        switch (channel)
        {
            case "附近": NetClient.I?.SendChatForIndependentRuntime(0, text, shares); break;
            case "队伍": NetClient.I?.SendChatForIndependentRuntime(1, text, shares); break;
            case "家族": NetClient.I?.SendChatForIndependentRuntime(2, text, shares); break;
            case "世界": NetClient.I?.SendChatForIndependentRuntime(3, text, shares); break;
        }
    }
}
}
