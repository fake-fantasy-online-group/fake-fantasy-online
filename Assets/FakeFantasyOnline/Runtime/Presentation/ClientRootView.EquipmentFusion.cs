using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView : IEquipmentFusionReceiveContext
{
    void IEquipmentFusionReceiveContext.ReceiveEquipmentFusionInfo(int mainKind, int mainTab, int mainSlot,
        int subTab, int subSlot, string mainName, int mainQuality, string subName, int subQuality,
        string mainDesc, string subDesc, bool ok, bool post, string tip, string confirm, int sig) =>
        EquipFuseDlg.OnInfo(mainKind, mainTab, mainSlot, subTab, subSlot, mainName, mainQuality,
            subName, subQuality, mainDesc, subDesc, ok, post, tip, confirm, sig);
}
}
