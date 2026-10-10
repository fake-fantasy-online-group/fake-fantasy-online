namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal partial class NetClient
{
    internal static void BeginSelfDataForIndependentRuntime()
    {
        CharData.counterCharges = 0;
        CharData.counterTtlMs = 0;
        CharData.counterSkillId = 0;
    }
}

internal static partial class CharData
{
    internal static int counterCharges;
    internal static int counterTtlMs;
    internal static int counterSkillId;
}
}
