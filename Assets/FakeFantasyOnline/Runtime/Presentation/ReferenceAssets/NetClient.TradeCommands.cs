using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Presentation facet for the four official 1.5.8 NetClient trade commands that TradeDlg (and
// DealDlg's own SendOffer) emit. They did not exist in the product before WBB-024:
//
//   NetClient.TradeOffer   0x06000751  instance void TradeOffer(long, List<(byte,int,int)>)
//   NetClient.TradeLock    0x06000752  instance void TradeLock(bool)
//   NetClient.TradeConfirm 0x06000753  instance void TradeConfirm()
//   NetClient.TradeCancel  0x06000754  instance void TradeCancel()
//
// Evidence: the frozen arm64 export in
// Build/ReferenceReverseGhidra-1.5.8/queries/wbb024-nettrade-arm64 and the x86_64 export in
// Build/ReferenceReverseGhidra-1.5.8/queries/wbb025-writer-x86_64-0600075*. All four follow the
// one official shape the neighbouring commands use - capture the arguments, then hand a writer to
// NetClient.Send (0x06000772) with the command's opcode:
//
//   TradeOffer   display class +0x10 = money, +0x18 = the items list (by reference, write
//                barrier) -> Action<Buf> 0x0600084d -> Send 0x1041
//   TradeLock    display class +0x10 = on -> Action<Buf> 0x0600084f -> Send 0x1042
//   TradeConfirm cached Action<Buf> 0x060007aa at NetClient+<>c +0x80 -> Send 0x1043
//   TradeCancel  cached Action<Buf> 0x060007ab at NetClient+<>c +0x88 -> Send 0x1044
//
// These four members do NOT write the wire. Each one only allocates the capture and calls Send;
// the bytes come from the writer lambda, whose tokens are pinned in the atlas metadata usage table
// (ordinal 9632/9633/9556/9555 -> 0x0600084d/0x0600084f/0x060007aa/0x060007ab). The wire they
// produce is registered against those four tokens. Since WBB-026 the product carrier for those
// writers is the Protocol NetClient.TradeCommands facet (NetClient.WriteOffer / WriteLock / the two
// empty writers), reached through PrivateServerGateway; PrivateServerTradeCodec.EncodeOffer /
// EncodeLock only re-run the same bodies into a byte array for the byte-level Editor audit.
// Recording 0x06000751..0x06000754 as complete wire writers would be wrong for that reason.
//
// What this facet does carry is the official call shape and capture:
//   * the money and the offer list are captured at call time, the list by reference and not copied
//     (arm64 stores with a write barrier at +0x18, x86_64 the same field order), so the writer
//     reads the caller's live list when Send runs it;
//   * a null list is the official raise inside Send, not an empty offer - nothing here replaces it
//     with an empty list or skips the call;
//   * Confirm/Cancel reuse one cached writer each (static <>c +0x80 / +0x88) and Send a packet with
//     no payload; the product reaches the same empty payload through the Application commands.
//
// Presentation never imports Protocol: the writer delegates are bound by
// ReferenceCharacterSkillUsePort to the Application trade commands
// ClientSessionCoordinator.SubmitTradeOfferAsync / SetTradeLockAsync / ConfirmTradeAsync /
// CancelTradeAsync, which route to PrivateServerGateway and from there to the Protocol
// NetClient.TradeOffer/TradeLock/TradeConfirm/TradeCancel - the official members, whose
// Action<Buf> writers run inside NetClient.Send (0x06000772) after its connected test. The
// members forward to null writers until a host binds them, so the product sends no trade traffic
// on its own.
internal partial class NetClient
{
    // The offer argument keeps the official List<(byte,int,int)> type end to end, so the Protocol
    // writer enumerates it with the official List<T> struct enumerator instead of an indexer over a
    // read-only view.
    private Action<long, List<(byte, int, int)>> tradeOfferWriter;
    private Action<bool> tradeLockWriter;
    private Action tradeConfirmWriter;
    private Action tradeCancelWriter;

    internal void BindTradeCommandsForIndependentRuntime(
        Action<long, List<(byte, int, int)>> offerWriter,
        Action<bool> lockWriter,
        Action confirmWriter,
        Action cancelWriter)
    {
        tradeOfferWriter = offerWriter;
        tradeLockWriter = lockWriter;
        tradeConfirmWriter = confirmWriter;
        tradeCancelWriter = cancelWriter;
    }

    /// <summary>Official NetClient.TradeOffer (0x06000751): captures the money amount and the live
    /// offer list (by reference, never a copy) and sends opcode 0x1041. The bytes are written by
    /// the writer 0x0600084d, not by this member; the list element order it writes is
    /// I64(money), U8(count), then U8(tab)/I32(slot)/I32(count) per row.</summary>
    public void TradeOfferForIndependentRuntime(long money, List<(byte, int, int)> items)
    {
        tradeOfferWriter?.Invoke(money, items);
    }

    /// <summary>Official NetClient.TradeLock (0x06000752): captures the lock flag and sends opcode
    /// 0x1042; the writer 0x0600084f writes it as one Buf.U8.</summary>
    public void TradeLockForIndependentRuntime(bool on)
    {
        tradeLockWriter?.Invoke(on);
    }

    /// <summary>Official NetClient.TradeConfirm (0x06000753): the body reuses the cached writer
    /// 0x060007aa stored at static +0x80 (or builds it on first use) and sends opcode 0x1043 with
    /// an empty payload.</summary>
    public void TradeConfirmForIndependentRuntime()
    {
        tradeConfirmWriter?.Invoke();
    }

    /// <summary>Official NetClient.TradeCancel (0x06000754): the body reuses the cached writer
    /// 0x060007ab stored at static +0x88 (or builds it on first use) and sends opcode 0x1044 with
    /// an empty payload.</summary>
    public void TradeCancelForIndependentRuntime()
    {
        tradeCancelWriter?.Invoke();
    }
}
}
