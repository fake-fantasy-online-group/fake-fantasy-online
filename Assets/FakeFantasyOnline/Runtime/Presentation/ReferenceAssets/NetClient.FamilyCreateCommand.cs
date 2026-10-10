using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Presentation facet for the family-creation command FamilyCreateDlg's 确定 button sends. The
// product already had the Application entry (ClientSessionCoordinator.RequestFamilyCreateAsync)
// but no NetClient member, so the dialog did not compile. Follows the shape of the other family
// facets: a writer delegate bound by ReferenceCharacterSkillUsePort, keeping the Presentation
// layer from importing Protocol.
internal partial class NetClient
{
    private Action<string, string> familyCreateWriter;

    internal void BindFamilyCreateForIndependentRuntime(Action<string, string> writer)
    {
        familyCreateWriter = writer;
    }

    /// <summary>FamilyCreateDlg's 确定 button: the trimmed family name plus the proclamation.</summary>
    public void FamilyCreateForIndependentRuntime(string name, string proclaim)
    {
        familyCreateWriter?.Invoke(name ?? "", proclaim ?? "");
    }
}
}
