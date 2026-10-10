namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official TargetSelect owner (06000C9C..06000C9E).
/// </summary>
public static class TargetSelect
{
    public static ISelectable Current;

    // Representation-only bridge. Friendship/HP behavior of the legacy view remains explicit
    // recovery work; the native request type branch consumes its real identity component.
    internal static object CurrentSourceForIndependentRuntime =>
        Current is ReferenceWorldRemoteSelectable remote
            ? (object)remote.SourceIdentityForIndependentRuntime : Current;

    public static void Select(ISelectable s)
    {
        if (s != null && s.SelTransform)
        {
            Current = s;
            TargetRing.Ensure().Bind(s);
            return;
        }

        Clear();
    }

    public static void Clear()
    {
        Current = null;
        if (TargetRing.I)
        {
            TargetRing.I.Bind(null);
        }
    }

    public static void OnGone(ISelectable s)
    {
        if (Current != null && Current == s)
        {
            Clear();
        }
    }
}

}
