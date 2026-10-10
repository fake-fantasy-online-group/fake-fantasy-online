using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{

public sealed partial class PrivateServerGateway
{
    // Official 0600077f, case 0x8071. Each store follows its successful read;
    // a later bounds failure must retain the earlier fields and the old timestamp.
    private void ReceiveCounterWindowPacket(byte[] payload)
    {
        var reader = new NetClient.Buf(payload);
        var owner = _worldReceiveOwner as ICounterWindowReceiveOwner;
        int charges = reader.U8();
        owner?.StoreCounterCharges(charges);
        int ttlMilliseconds = reader.I32();
        owner?.StoreCounterTtlMilliseconds(ttlMilliseconds);
        int skillId = reader.Remaining >= 1 ? reader.I32() : 0;
        if (owner != null)
        {
            owner.StoreCounterSkillId(skillId);
            owner.StampCounterWindowReceipt();
        }
        else
        {
            // Keep the existing decoded-state adapter for hosts without the
            // incremental receipt port. The production owner implements it.
            _worldReceiveOwner?.ReceiveCounterWindow(charges, ttlMilliseconds, skillId);
        }
    }
}

}
