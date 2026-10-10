using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class SocialClient
{
    // Official 1.5.8 family block at +0x78..+0xd0. Names, types and order follow the
    // official fields table. The scalar members carry no initializer, which is why the
    // official static constructor does not touch them.
    public static string FamilyName = string.Empty;          // +0x78
    public static int FamilyLevel;                           // +0x80
    public static int FamilyMemberCap;                       // +0x84
    public static string FamilyProclaim = string.Empty;      // +0x88
    public static bool FamilyResist;                         // +0x90
    public static int MyFamilyPos;                           // +0x94
    public static bool FamilyCanInvite;                      // +0x98
    public static bool FamilyCanMail;                        // +0x99
    public static List<(string, int, string, string, bool)> Family =
        new List<(string, int, string, string, bool)>();     // +0xa0
    public static List<(int, string, string)> FamilyPositions =
        new List<(int, string, string)>();                   // +0xc8
    public static List<(string, int, int, int, string)> FamilyBrowse =
        new List<(string, int, int, int, string)>();         // +0xd0

    // Read projections only. No window/session observer can publish into these native roots.
    internal static WorldFamilyState FamilyStateForIndependentRuntime => new WorldFamilyState(
        FamilyName, FamilyLevel, FamilyMemberCap, FamilyProclaim, FamilyResist, MyFamilyPos,
        FamilyCanInvite, FamilyCanMail, 0, Family);

    internal static void ReceiveFamilyStateForIndependentRuntime(WorldFamilyState state)
    {
        // This adapter admits complete successful 8034 packets only. It does not claim native
        // partial-field writes or list allocation/clear timing for a malformed membership row.
        FamilyName = state.Name;
        FamilyLevel = state.Level;
        FamilyMemberCap = state.MemberCapacity;
        FamilyProclaim = state.Proclaim;
        FamilyResist = state.Resist;
        MyFamilyPos = state.MyPosition;
        FamilyCanInvite = state.CanInvite;
        FamilyCanMail = state.CanMail;
        Family = new List<(string, int, string, string, bool)>(state.Members);
    }

    /// <summary>
    /// Official 1.5.8 token 06000d2a. Walks the official Family list, matches the caller
    /// by CharData.name and returns whether the matched row's fourth element equals the
    /// literal family-leader rank. The third element is not consulted, and a caller who
    /// is absent from the list is not a leader.
    /// </summary>
    public static bool IamFamilyLeader
    {
        get
        {
            foreach (var member in Family)
                if (member.Item1 == CharData.name)
                    return member.Item4 == "族长";
            return false;
        }
    }

    /// <summary>
    /// Official 1.5.8 token 06000d2b. MyFamilyPos 1 is the family leader and 2 is the
    /// officer; every other value grants no family authority.
    /// </summary>
    public static bool IamFamilyOfficer => MyFamilyPos == 1 || MyFamilyPos == 2;
}

}
