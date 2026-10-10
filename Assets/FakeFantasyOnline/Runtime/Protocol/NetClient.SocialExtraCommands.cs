using System.Collections.Generic;
namespace FakeFantasyOnline.Protocol
{
    public sealed partial class NetClient
    {
        public void ApprenticeReq(int targetId, byte mode, string targetName) => Send(ProtocolOpcodes.ApprenticeRequest, b =>
        { b.I32(targetId); b.U8(mode); b.Str(targetName ?? ""); });
        public void ApprenticeList() => Send(ProtocolOpcodes.ApprenticeListRequest, _ => { });
        public void DuelChallenge(int targetId, string targetName = "", byte mode = 0) => Send(ProtocolOpcodes.DuelChallengeRequest, b =>
        { b.I32(targetId); b.Str(targetName ?? ""); b.U8(mode); });
        public void RequestDuelList() => Send(ProtocolOpcodes.DuelListRequest, _ => { });
        public void DuelBet(int duelId, byte side, int amount) => Send(ProtocolOpcodes.DuelBetRequest, b =>
        { b.I32(duelId); b.U8(side); b.I32(amount); });
        public void DuelSpectate(int duelId) => Send(ProtocolOpcodes.DuelSpectateRequest, b => b.I32(duelId));
        public void SendBlockAdd(string name, byte scope = 0) => Send(ProtocolOpcodes.BlockAddRequest, b => { b.Str(name ?? ""); b.U8(scope); });
        public void SendBlockDel(long charId) => Send(ProtocolOpcodes.BlockDeleteRequest, b => b.I64(charId));
        public void OpenMail() => Send(ProtocolOpcodes.OpenMailRequest, _ => { });
        public void MailAction(byte action, long mailId) => Send(ProtocolOpcodes.MailActionRequest, b => { b.U8(action); b.I64(mailId); });
        public void OpenRack() => Send(ProtocolOpcodes.OpenRackRequest, _ => { });
        public void BuyRack(int itemId, byte source = 0, int count = 1) => Send(ProtocolOpcodes.BuyRackRequest, b => { b.I32(itemId); b.U8(source); b.I32(count); });
        public void HelpReq() => Send(ProtocolOpcodes.HelpRequest, _ => { });
        public void RequestDeposit(string chain, int amount) => Send(ProtocolOpcodes.RequestDepositRequest, b => { b.Str(chain ?? ""); b.I32(amount); });
        public void RackRefundList() => Send(ProtocolOpcodes.RackRefundListRequest, _ => { });
        public void RackRefund(int itemId, int unitPrice, int bundle, int day, int shares) => Send(ProtocolOpcodes.RackRefundRequest, b => { b.I32(itemId); b.I32(unitPrice); b.I32(bundle); b.I32(day); b.I32(shares); });
        public void OpenTeleport(int npcId) => Send(ProtocolOpcodes.OpenTeleportRequest, b => b.I32(npcId));
        public void RideTogether(int targetId) => Send(ProtocolOpcodes.RideTogetherRequest, b => b.I32(targetId));
        public void RideInvite(int targetId) => Send(ProtocolOpcodes.RideInviteRequest, b => b.I32(targetId));
        // SendMail（0600070e）与其 writer <SendMail>b__0（060007ff）：双架构证据见
        // Build/ReferenceReverseGhidra-1.5.8/queries/WBD-118/{arm64,x86_64}-{070e,7ff}/。
        // 官方形参是 List<ValueTuple<Byte,Int32,Int32>>（元素 12 字节）而不是 IReadOnlyList，按引用
        // 捕获不复制，序列化发生在 NetClient.Send 的连接门之后；只有计数夹到 255，元素循环不截断。
        public void SendMail(string to, string title, string body, long money, long caiyu, byte type,
            List<(byte, int, int)> attach) => Send(ProtocolOpcodes.SendMailRequest, b =>
        {
            b.Str(to);
            b.Str(title);
            b.Str(body);
            b.I64(money);
            b.I64(caiyu);
            b.U8(type);
            b.U8(attach.Count < 255 ? attach.Count : 255);
            foreach ((byte, int, int) item in attach)
            {
                b.U8(item.Item1);
                b.I32(item.Item2);
                b.I32(item.Item3);
            }
        });
    }
}
