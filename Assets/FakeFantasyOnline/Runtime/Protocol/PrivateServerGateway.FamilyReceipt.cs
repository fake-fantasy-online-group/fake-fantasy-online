using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    // Both Pump routes consume these cases here, before stage-dependent retention. No family
    // packet enters startup/world queues, so there is no later decode, state rollback or action.
    private bool TryReceiveFamilyPacket(ushort opcode, byte[] payload, IFamilyReceiptOwner owner)
    {
        if (opcode == ProtocolOpcodes.FamilySnapshot)
        {
            WorldFamilyState state = ReadFamilyState(payload, out byte openFlag);
            owner.ReceiveFamilyState(state);
            PublishFamilyState(state);
            if (openFlag == 1) owner.OpenFamilyDetail();
            return true;
        }
        if (opcode == ProtocolOpcodes.FamilyBrowse)
        {
            var reader = new NetClient.Buf(payload);
            int count = reader.U8();
            var rows = owner.FamilyBrowse;
            rows.Clear();
            try
            {
                for (int i = 0; i < count; i++)
                    rows.Add((reader.Str(), reader.U8(), reader.U8(), reader.U8(), reader.Str()));
            }
            finally
            {
                // Count succeeded and Clear ran. A later short row preserves the clear and
                // each completed tuple; the observation follows that same reached prefix.
                PublishFamilyBrowseState(rows);
            }
            return true;
        }
        if (opcode == ProtocolOpcodes.FamilyPositions)
        {
            var reader = new NetClient.Buf(payload);
            int count = reader.U8();
            var rows = owner.FamilyPositions;
            rows.Clear();
            try
            {
                for (int i = 0; i < count; i++)
                    rows.Add((reader.U8(), reader.Str(), reader.Str()));
            }
            finally
            {
                PublishFamilyPositionsState(rows);
            }
            return true;
        }
        return false;
    }
}
}
