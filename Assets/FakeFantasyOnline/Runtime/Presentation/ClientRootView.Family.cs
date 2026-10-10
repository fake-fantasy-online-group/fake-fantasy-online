using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView : IFamilyReceiptOwner
{
    List<(string, int, int, int, string)> IFamilyReceiptOwner.FamilyBrowse => SocialClient.FamilyBrowse;
    List<(int, string, string)> IFamilyReceiptOwner.FamilyPositions => SocialClient.FamilyPositions;
    void IFamilyReceiptOwner.ReceiveFamilyState(WorldFamilyState state) =>
        SocialClient.ReceiveFamilyStateForIndependentRuntime(state);
    void IFamilyReceiptOwner.OpenFamilyDetail() =>
        ReferenceWorldReceiveOwner.OpenFamilyDetailForIndependentRuntime();
}
}
