namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public static partial class SocialClient
{
    // Official 06000d25: the argument is a party row index, not an entity ID.
    // Persistent identity wins whenever both sides are present, even if it disagrees
    // with a recycled scene entity ID.
    public static bool IsSelf(int index)
    {
        if (index < 0) return false;
        if (index < PartyCharIds.Count && PartyCharIds[index] != 0 && CharData.charId > 0)
            return PartyCharIds[index] == CharData.charId;
        return index < Party.Count && Party[index].Item1 == LocalEntityIdForIndependentRuntime;
    }
    // Official 06000d26 preserves the matching leader row's persistent identity.
    public static bool IsLeaderMe
    {
        get
        {
            if (PartyLeaderId == 0) return false;
            for (int i = 0; i < Party.Count; i++)
                if (Party[i].Item1 == PartyLeaderId) return IsSelf(i);
            return PartyLeaderId == LocalEntityIdForIndependentRuntime;
        }
    }
}
}
