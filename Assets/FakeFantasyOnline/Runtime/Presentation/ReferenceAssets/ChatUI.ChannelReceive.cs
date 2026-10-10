using Share = FakeFantasyOnline.Application.WorldChatSharedEntryState;
using System;
using System.Collections.Generic;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ChatUI
{
    internal static void ReceiveChannelForIndependentRuntime(byte channelCode, string senderName, string text,
        byte tier, byte skin, List<Share> shares)
    {
        string channel;
        Color color;
        ResolveIncomingChannel(channelCode, out channel, out color);
        if (channelCode == 5) AddBroadcast(senderName, text, Math.Max(1, (int)tier), skin, shares);
        Add(channel, senderName + ": " + text, color, senderName, shares);
    }
    // The official 0x8039 case loads one packed 0xAABBGGRR literal per channel and splits it as
    // r = byte0 / 255, g = byte1 / 255, b = byte2 / 255 with alpha 1, so 家族 is 0xffaaffaa and
    // the unmatched branch is 0xffe6e6e6.
    private static void ResolveIncomingChannel(
        byte channelCode,
        out string channel,
        out Color color)
    {
        if (channelCode == 1)
        {
            channel = "队伍";
            color = new Color(120f / 255f, 200f / 255f, 1f, 1f);
        }
        else if (channelCode == 2)
        {
            channel = "家族";
            color = new Color(170f / 255f, 1f, 170f / 255f, 1f);
        }
        else if (channelCode == 3)
        {
            channel = "世界";
            color = new Color(1f, 150f / 255f, 90f / 255f, 1f);
        }
        else if (channelCode == 5)
        {
            channel = "广播";
            color = new Color(1f, 215f / 255f, 60f / 255f, 1f);
        }
        else
        {
            channel = "附近";
            color = new Color(230f / 255f, 230f / 255f, 230f / 255f, 1f);
        }
    }

 }
}
