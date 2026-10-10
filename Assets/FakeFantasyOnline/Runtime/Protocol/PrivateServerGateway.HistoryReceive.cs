using FakeFantasyOnline.Application;
namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    private static void ReceiveVoteOptions(byte[] payload, IHistoryReceiveContext context)
    {
        var b = new NetClient.Buf(payload);
        int count = b.U16();
        context.VoteOptions.Clear();
        for (int i = 0; i < count && b.Remaining > 0; i++)
        {
            int id = b.I32();
            string text = b.Str();
            int yes = b.I32();
            int no = b.I32();
            byte mine = (byte)b.U8();
            context.VoteOptions.Add((id, text, yes, no, mine));
        }
        context.AdvanceVoteRevision();
    }

    private static void ReceiveGmFeedbacks(byte[] payload, IHistoryReceiveContext context)
    {
        var b = new NetClient.Buf(payload);
        int firstCount = b.U8();
        context.GmFeedbacks.Clear();
        void ReadAndAppend()
        {
            string who = b.Str();
            long time = b.I64();
            string text = b.Str();
            byte status = (byte)b.U8();
            string reply = b.Str();
            context.GmFeedbacks.Add((who, time, text, status, reply, 0));
        }
        for (int i = 0; i < firstCount; i++) ReadAndAppend();
        if (b.Remaining >= 2)
        {
            int extraCount = b.U16();
            for (int i = 0; i < extraCount && b.Remaining > 0; i++) ReadAndAppend();
        }
        if (b.Remaining >= 2)
        {
            int idCount = b.U16();
            for (int i = 0; i < idCount && i < context.GmFeedbacks.Count && b.Remaining >= 4; i++)
            {
                var row = context.GmFeedbacks[i];
                row.Item6 = b.I32();
                context.GmFeedbacks[i] = row;
            }
        }
        context.AdvanceGmFeedbackRevision();
    }
}
}
