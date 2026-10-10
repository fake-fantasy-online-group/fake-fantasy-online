namespace FakeFantasyOnline.Application
{
public interface IEquipmentFusionReceiveContext
{
    void ReceiveEquipmentFusionInfo(int mainKind, int mainTab, int mainSlot, int subTab, int subSlot,
        string mainName, int mainQuality, string subName, int subQuality, string mainDesc, string subDesc,
        bool ok, bool post, string tip, string confirm, int sig);
}
}
