using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class SocialClient
{
    // Official 1.5.8 layout: Friends is List<(string,bool)> at +0xd8.
    public static List<(string, bool)> Friends = new List<(string, bool)>();

    // Official: MasterName / MasterLevel / MasterOnline at +0xa8, Peers at +0xb8,
    // Apprentices at +0xc0. The element shape below follows the 0x805a wire
    // (Str name, I32 level, U8 online) - the official element type was not
    // individually frozen in the SocialClient evidence packet.
    public static string MasterName = string.Empty;
    public static int MasterLevel;
    public static bool MasterOnline;
    public static List<(string, int, bool)> Peers = new List<(string, int, bool)>();
    public static List<(string, int, bool)> Apprentices = new List<(string, int, bool)>();

    // Official: Blocks is List<(long charId, string name, bool scope)> at +0xe0. scope 0 means the
    // single character, 1 means every character on that account.
    public static List<(long, string, bool)> Blocks = new List<(long, string, bool)>();

    /// <summary>
    /// Official 1.6.0 SocialClient.IsBlocked(string). The native implementation builds a captured
    /// Predicate&lt;ValueTuple&lt;long,string,bool&gt;&gt; whose body is exactly
    /// String.op_Equality(element.Item2, capturedName) - see &lt;IsBlocked&gt;b__0 at 0x06000d3c,
    /// which is 20 bytes and touches only Item2. So this is a plain name match and the scope flag
    /// is deliberately not consulted.
    /// </summary>
    public static bool IsBlocked(string name)
    {
        return Blocks.Exists(row => row.Item2 == name);
    }

    internal static void BindFriendStateForIndependentRuntime(WorldFriendState state)
    {
        Friends.Clear();
        foreach ((string name, bool online) in state.Entries)
            Friends.Add((name, online));
    }

    internal static void BindApprenticeStateForIndependentRuntime(WorldApprenticeState state)
    {
        MasterName = state.MasterName;
        MasterLevel = state.MasterLevel;
        MasterOnline = state.MasterOnline;
        Peers.Clear();
        foreach ((string name, int level, bool online) in state.Peers)
            Peers.Add((name, level, online));
        Apprentices.Clear();
        foreach ((string name, int level, bool online) in state.Apprentices)
            Apprentices.Add((name, level, online));
    }

    internal static void BindBlockStateForIndependentRuntime(WorldBlockState state)
    {
        Blocks.Clear();
        foreach ((long charId, string name, bool scope) in state.Entries)
            Blocks.Add((charId, name, scope));
    }
}

}
