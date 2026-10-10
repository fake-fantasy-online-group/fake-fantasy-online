using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class NpcChatter
{
    internal Texture2D BakedNameForIndependentRuntime => _bakedName;
    internal string BakedNameKeyForIndependentRuntime => _bakedNameFor;
    internal int BakedNameScaleForIndependentRuntime => _bakedNameScale;

    internal string RoleForIndependentRuntime => role;
    internal string BubbleForIndependentRuntime => bubble;
    internal float BubbleUntilForIndependentRuntime => bubbleUntil;
    internal float AccumulatorForIndependentRuntime => acc;
    internal int LineIndexForIndependentRuntime => lineIdx;
    internal static float CycleForIndependentRuntime => CycleMs();

    internal static Texture2D SymbolTextureForIndependentRuntime(int magicId) => MagicSymTex(magicId);
    internal static int SymbolIdForIndependentRuntime(int kind, int bucket) => SymMagicId(kind, bucket);
    internal static int SymbolTextureCountForIndependentRuntime => symCache.Count;

    internal static void PrepareDataForIndependentRuntime() { EnsureData(); }
    internal static Dictionary<string, List<string>> GreetingRolesForIndependentRuntime => ovByRole;

    // Decision-free bridge for the existing NPC view/dialog catalog. The recovered
    // SetServerChatter member owns replacement, routing and timing policy.
    internal static Dictionary<string, List<string>> ServerByNpcForIndependentRuntime => byNpc;
    internal static Dictionary<string, List<string>> ServerByRoleForIndependentRuntime => byRole;
    internal static List<string> ServerAllForIndependentRuntime => all;
    internal static List<string> ServerGreetingsForIndependentRuntime => ovGreets;
    internal static bool ChatterEnabledForIndependentRuntime => sEnabled;
    internal static float MinimumGapForIndependentRuntime => sMinGap;
    internal static float MaximumGapForIndependentRuntime => sMaxGap;
    internal static float ShowSecondsForIndependentRuntime => sShowSec;
}

}
