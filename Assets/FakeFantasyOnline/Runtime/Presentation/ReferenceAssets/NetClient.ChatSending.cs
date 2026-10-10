using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<WorldChatSendRequest> chatWriter;
    internal void BindChatWriterForIndependentRuntime(Action<WorldChatSendRequest> writer) => chatWriter = writer;
    // Official 0x06000771 / 0x060006db capture the caller's list by reference, so this surface hands
    // the very list it was given to the request instead of projecting it into another shape: the
    // triple is already the official (kind, tab, slot) element. A caller-supplied null stays null;
    // NetClient.WriteShares (0x060006dd) answers that input with the count 0.
    public void GossipForIndependentRuntime(string toName, string text, List<(byte, byte, int)> shares)
        => chatWriter?.Invoke(WorldChatSendRequest.ForWhisper(toName, text, shares));
    public void SendChatForIndependentRuntime(byte channel, string text, List<(byte, byte, int)> shares)
        => chatWriter?.Invoke(WorldChatSendRequest.ForChannel(channel, text, shares));
    // Official 0x060006c8 NetClient.GmCmd(cmd) has its real owner in Protocol already - the frozen
    // body is one display class, one captured string and NetClient.Send (0x06000772) with opcode
    // 0x1006 - so this surface adds no second route: it forwards the caller's command through the
    // very chatWriter the other two members use, and PrivateServerGateway.SendWorldChatAsync sends
    // WorldChatSendKind.GmCommand as SendAsync(() => _netClient.GmCmd(request.Text), ct), the same
    // single gateway send gate every chat request leaves through. The command is stored unchanged
    // (WorldChatSendRequest keeps the string it was handed) and the caller owns any prefix or case:
    // PlayerController.Update's transfer arm hands the raw MapProc cmd, the DifficultyBox callbacks
    // hand the two literals "taoyu" / "taoyu hard", and ChatUI.SendNow hands its own text with the
    // slash stripped.
    public void GmCmdForIndependentRuntime(string cmd) => chatWriter?.Invoke(WorldChatSendRequest.ForGmCommand(cmd));
}
}
