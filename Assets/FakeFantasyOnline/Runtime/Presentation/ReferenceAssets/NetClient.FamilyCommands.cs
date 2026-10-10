using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// This existing Presentation facet carries the original UI/gameplay members. The official
// FamilyDlg calls NetClient.FamilyList()/FamilyApply(name), FamilyDetailDlg calls
// FamilyRefresh()/FamilyManage(byte,string,int,string)/FamilyLeave() and JobRightDlg calls
// FamilyPositions() on open and FamilyMailDlg calls FamilyMail(title, body) on 发送; the
// Presentation facet routes those through the Application family commands (bound by
// ReferenceCharacterSkillUsePort), keeping the official call shape without the Presentation layer
// importing Protocol.
internal partial class NetClient
{
    private Action familyListWriter;
    private Action<string> familyApplyWriter;
    private Action familyRefreshWriter;
    private Action<byte, string, int, string> familyManageWriter;
    private Action familyLeaveWriter;
    private Action familyPositionsWriter;
    private Action<int, string> familyPosNameWriter;
    private Action<string, string> familyMailWriter;
    private Action<int, int, int, int> familyStashWriter;
    private Action<int> familyStashPermissionWriter;

    internal void BindFamilyCommandsForIndependentRuntime(
        Action listWriter,
        Action<string> applyWriter,
        Action refreshWriter,
        Action<byte, string, int, string> manageWriter,
        Action leaveWriter,
        Action positionsWriter,
        Action<int, string> posNameWriter,
        Action<string, string> mailWriter)
    {
        familyListWriter = listWriter;
        familyApplyWriter = applyWriter;
        familyRefreshWriter = refreshWriter;
        familyManageWriter = manageWriter;
        familyLeaveWriter = leaveWriter;
        familyPositionsWriter = positionsWriter;
        familyPosNameWriter = posNameWriter;
        familyMailWriter = mailWriter;
    }

    public void FamilyListForIndependentRuntime()
    {
        familyListWriter?.Invoke();
    }

    public void FamilyApplyForIndependentRuntime(string name)
    {
        familyApplyWriter?.Invoke(name ?? "");
    }

    /// <summary>Official NetClient.FamilyRefresh (0x0600076d).</summary>
    public void FamilyRefreshForIndependentRuntime()
    {
        familyRefreshWriter?.Invoke();
    }

    /// <summary>Official NetClient.FamilyManage (0x06000768): op, member name, permission/id slot
    /// and the proclamation text, in that byte/string/int/string order.</summary>
    public void FamilyManageForIndependentRuntime(byte op, string name, int id, string text)
    {
        familyManageWriter?.Invoke(op, name ?? "", id, text ?? "");
    }

    /// <summary>Official NetClient.FamilyLeave (0x06000764).</summary>
    public void FamilyLeaveForIndependentRuntime()
    {
        familyLeaveWriter?.Invoke();
    }

    /// <summary>Official NetClient.FamilyPositions (0x0600076a): JobRightDlg.OnOpen and
    /// JobCustomDlg.Toggle send this whenever the window becomes visible to load the assignable
    /// position list.</summary>
    public void FamilyPositionsForIndependentRuntime()
    {
        familyPositionsWriter?.Invoke();
    }

    /// <summary>Official NetClient.FamilyPosName (0x0600076c): JobCustomDlg's 设定 renames the
    /// selected position and 恢复 clears the name back to the default (empty string).</summary>
    public void FamilyPosNameForIndependentRuntime(int positionId, string name)
    {
        familyPosNameWriter?.Invoke(positionId, name ?? "");
    }

    /// <summary>Official NetClient.FamilyMail (0x06000769): FamilyMailDlg's 发送 button sends the
    /// composed title and body to the whole family.</summary>
    public void FamilyMailForIndependentRuntime(string title, string body)
    {
        familyMailWriter?.Invoke(title ?? "", body ?? "");
    }

    internal void BindFamilyStashCommandsForIndependentRuntime(
        Action<int, int, int, int> stashWriter, Action<int> permissionWriter)
    {
        familyStashWriter = stashWriter;
        familyStashPermissionWriter = permissionWriter;
    }

    public void FamilyStashForIndependentRuntime(int op, int bagTab, int bagSlot, int index) =>
        familyStashWriter?.Invoke(op, bagTab, bagSlot, index);

    public void FamilyStashPermForIndependentRuntime(int pos) => familyStashPermissionWriter?.Invoke(pos);
}
}
