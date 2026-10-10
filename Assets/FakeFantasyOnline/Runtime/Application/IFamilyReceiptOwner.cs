using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{
/// <summary>
/// Main-thread access to the three existing family roots. Directory/position lists are the
/// actual native owners, including null; implementations must not repair a missing list.
/// Successful membership replacement and its separate window callback have no world gate.
/// </summary>
public interface IFamilyReceiptOwner
{
    List<(string, int, int, int, string)> FamilyBrowse { get; }
    List<(int, string, string)> FamilyPositions { get; }
    void ReceiveFamilyState(WorldFamilyState state);
    void OpenFamilyDetail();
}
}
