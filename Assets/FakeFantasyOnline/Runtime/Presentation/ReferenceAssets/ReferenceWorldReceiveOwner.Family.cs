using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class ReferenceWorldReceiveOwner : IFamilyReceiptOwner
{
    public List<(string, int, int, int, string)> FamilyBrowse => SocialClient.FamilyBrowse;
    public List<(int, string, string)> FamilyPositions => SocialClient.FamilyPositions;
    public void ReceiveFamilyState(WorldFamilyState state) =>
        SocialClient.ReceiveFamilyStateForIndependentRuntime(state);
    public void OpenFamilyDetail() => OpenFamilyDetailForIndependentRuntime();

    internal static void OpenFamilyDetailForIndependentRuntime()
    {
        FamilyDlg.Hide();
        FamilyDetailDlg.OpenSelf();
    }
}
}
