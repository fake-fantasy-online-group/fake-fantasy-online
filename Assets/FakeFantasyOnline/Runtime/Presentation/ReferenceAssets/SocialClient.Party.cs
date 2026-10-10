using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class SocialClient
{
    public static List<(int, string, int, int, int, int, int, int, bool)> Party =
        new List<(int, string, int, int, int, int, int, int, bool)>();
    public static List<long> PartyCharIds = new List<long>();
    internal static int LocalEntityIdForIndependentRuntime => NetClient.SelfEntityId;
    public static int PartyLeaderId;
    public static int SelectedMemberId;
    public static string PartyName = string.Empty;

    public static string TeamName()
    {
        if (Party.Count == 0) return string.Empty;
        if (!string.IsNullOrEmpty(PartyName)) return PartyName;
        foreach (var member in Party)
            if (member.Item1 == PartyLeaderId) return member.Item2 + "的小队";
        return string.Empty;
    }

    public static bool InParty(int id)
    {
        if (id == 0) return false;
        for (int i = 0; i < Party.Count; i++)
            if (Party[i].Item1 == id) return true;
        return false;
    }

    internal static void BindPartyForIndependentRuntime(WorldPartyState state)
    {
        // Native 8033 constructs all temporary lists before touching SocialClient.
        var party = new List<(int, string, int, int, int, int, int, int, bool)>(state.Members.Count);
        foreach (WorldPartyMemberState member in state.Members)
            party.Add((member.EntityId, member.DisplayName, member.Level, member.HitPoints,
                member.MaximumHitPoints, member.Gender, member.HairId, member.HairColor, member.Online));
        var characterIds = new List<long>(state.CharacterIds);
        var professions = new List<byte>(state.Professions);
        PartyName = state.PartyName;
        PartyLeaderId = state.LeaderEntityId;
        Party.Clear();
        Party.AddRange(party);
        PartyCharIds.Clear();
        PartyCharIds.AddRange(characterIds);
        PartyProfs.Clear();
        PartyProfs.AddRange(professions);
    }
}

}
