using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal readonly struct ReferenceNpcTaskSymbol
{
    public static readonly ReferenceNpcTaskSymbol None =
        new ReferenceNpcTaskSymbol(-1, 0, 0);

    public ReferenceNpcTaskSymbol(int kind, int colorBucket, int magicId)
    {
        Kind = kind;
        ColorBucket = colorBucket;
        MagicId = magicId;
    }

    public int Kind { get; }
    public int ColorBucket { get; }
    public int MagicId { get; }
    public bool Visible => Kind >= 0 && MagicId >= 5015 && MagicId <= 5020;
}

/// <summary>
/// Rendering bridge for the recovered TaskClient.HeadSymbol and NpcChatter symbol owners.
/// </summary>
internal sealed class ReferenceWorldNpcTaskFeedback
{
    // Historical Editor audit/provenance constants; runtime does not load this normalized table.
    public const string TaskTablePath = "tasks/task-head-symbols.json";
    public const int ExpectedTaskCount = 372;
    public const int AvailableKind = 1;
    public const int ReadyTurnInKind = 0;
    public const int AvailableMagicBase = 5015;
    public const int ReadyTurnInMagicBase = 5018;
    public const float SymbolHeightUnits = 34f;
    public const float SymbolHeadGapUnits = 4f;
    public const float SwingDegrees = 14f;
    public const float SwingRadiansPerSecond = 3f;

    private ReferenceWorldNpcTaskFeedback() { }
    internal int TaskCount => TaskDB.Count;
    internal int TextureCount => NpcChatter.SymbolTextureCountForIndependentRuntime;
    internal string TaskName(int taskId) => TaskDB.Get(taskId)?.name;

    public static bool TryCreate(ReferenceResourceLoader resources, out ReferenceWorldNpcTaskFeedback feedback)
    {
        feedback = resources != null ? new ReferenceWorldNpcTaskFeedback() : null;
        return feedback != null;
    }

    public ReferenceNpcTaskSymbol ResolveCurrent(string npcName)
    {
        var symbol = TaskClient.HeadSymbol(npcName);
        return symbol.kind < 0 ? ReferenceNpcTaskSymbol.None :
            new ReferenceNpcTaskSymbol(symbol.kind, symbol.color, MagicId(symbol.kind, symbol.color));
    }

    // Compatibility surface for historical callers. Native state is published by NetClient;
    // drawing must not fabricate a second task state from the arguments.
    public ReferenceNpcTaskSymbol Resolve(string npcName, int playerLevel, WorldTaskLogState taskLog)
    {
        return ResolveCurrent(npcName);
    }

    public Texture2D TextureFor(ReferenceNpcTaskSymbol symbol)
    {
        return symbol.Visible ? NpcChatter.SymbolTextureForIndependentRuntime(symbol.MagicId) : null;
    }

#if UNITY_EDITOR
    // Historical numeric audit helper, not a production decision path.
    internal static int ColorBucket(int playerLevel, int recommendedLevel)
    {
        int difference = unchecked(playerLevel - recommendedLevel);
        return difference < 4 ? 2 : difference > 10 ? 1 : 0;
    }
#endif

    internal static int MagicId(int kind, int colorBucket)
    {
        return NpcChatter.SymbolIdForIndependentRuntime(kind, colorBucket);
    }

    internal static bool TryResolveDrawRect(
        Vector3 screenPoint,
        int screenWidth,
        int screenHeight,
        float uiScale,
        Texture2D texture,
        out Rect rect)
    {
        rect = default(Rect);
        if (screenWidth < 1 || screenHeight < 1 || uiScale <= 0f || !texture ||
            texture.width < 1 || texture.height < 1 || screenPoint.z <= 0f ||
            screenPoint.x < 0f || screenPoint.x > screenWidth ||
            screenPoint.y < 0f || screenPoint.y > screenHeight)
        {
            return false;
        }

        float height = uiScale * SymbolHeightUnits;
        float width = height * texture.width / Mathf.Max(1f, texture.height);
        float top = screenHeight - screenPoint.y - height - uiScale * SymbolHeadGapUnits;
        rect = new Rect(screenPoint.x - width * 0.5f, top, width, height);
        return true;
    }

}

}
