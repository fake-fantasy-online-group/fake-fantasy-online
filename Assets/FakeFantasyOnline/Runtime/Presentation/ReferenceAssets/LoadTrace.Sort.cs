using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class LoadTrace
{
    // 06000200. Concrete int/reference instantiations and the RGCTX target are
    // recovered independently; the lambda carries 0600020f. Generated 0600020d/0600020e
    // are the noncapturing generic singleton and constructor carriers.
    private static List<KeyValuePair<TK, (int, float, long)>> SortByMs<TK>(
        Dictionary<TK, (int, float, long)> d)
    {
        var list = new List<KeyValuePair<TK, (int, float, long)>>(d);
        list.Sort((a, b) => b.Value.Item2.CompareTo(a.Value.Item2));
        return list;
    }
}
}
