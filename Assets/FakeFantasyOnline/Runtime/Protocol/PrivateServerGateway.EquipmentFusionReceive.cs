using FakeFantasyOnline.Application;
namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    private static void ReceiveEquipmentFusion(byte[] payload, IEquipmentFusionReceiveContext context)
    {
        var reader = new NetClient.Buf(payload);
        int mainKind = reader.U8();
        int mainTab = reader.U8();
        int mainSlot = reader.I32();
        int subTab = reader.U8();
        int subSlot = reader.I32();
        string mainName = reader.Str();
        int mainQuality = reader.U8();
        string subName = reader.Str();
        int subQuality = reader.U8();
        string mainDesc = reader.Str();
        string subDesc = reader.Str();
        bool ok = reader.U8() != 0;
        bool post = reader.U8() != 0;
        string tip = reader.Str();
        string confirm = reader.Str();
        int sig = reader.Remaining >= 4 ? reader.I32() : 0;
        context.ReceiveEquipmentFusionInfo(mainKind, mainTab, mainSlot, subTab, subSlot,
            mainName, mainQuality, subName, subQuality, mainDesc, subDesc, ok, post, tip, confirm, sig);
    }
}
}
