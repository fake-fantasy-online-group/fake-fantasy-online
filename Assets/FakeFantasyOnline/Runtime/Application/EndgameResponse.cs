using System;
using System.Collections.Generic;
using System.Text;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Application
{
public sealed class EndgameResponse
{
    public string Kind { get; }
    public string RequestID { get; }
    public bool OK { get; }
    public string Error { get; }
    public string Message { get; }
    public string Revision { get; }
    public string CatalogVersion { get; }
    public IReadOnlyList<string> Capabilities { get; }
    public string Cursor { get; }
    public string NextCursor { get; }
    public string SnapshotID { get; }
    public int Chunk { get; }
    public int Chunks { get; }
    public int TotalBytes { get; }
    public string Data { get; }
    internal DateTime RiftPageReceivedUtc { get; set; }
    internal double RiftPageReceivedMS { get; set; }
    public IReadOnlyList<EndgameItemView> Items { get; }
    public IReadOnlyList<EndgamePowerRecordView> Powers { get; }
    public IReadOnlyList<EndgameRuneView> Runes { get; }
    public IReadOnlyList<EndgameFormationView> Formations { get; }
    public IReadOnlyList<EndgameRecipeView> Recipes { get; }
    public IReadOnlyList<EndgameMaterialView> Materials { get; }
    public EndgameQuoteView Quote { get; }
    public EndgameRiftView Rift { get; }
    public IReadOnlyList<EndgameRiftEntranceView> RiftEntrances { get; }
    public EndgameCombatStatsView CombatStats { get; }
    public IReadOnlyList<EndgameSkillRankView> SkillRanks { get; }
    public IReadOnlyDictionary<string, string> Result { get; }
    private EndgameResponse(IReadOnlyDictionary<string, object> d)
    {
        string messageKind = EndgameData.String(d, "kind", 64);
        // Business rift snapshots allow additive metadata; transport and scene context remain exact.
        if (!messageKind.StartsWith("rift.", StringComparison.Ordinal) || messageKind == "rift.page" || messageKind == "rift.context")
        EndgameData.Keys(d, "kind", "requestId", "ok", "error", "message", "revision", "catalogVersion", "capabilities", "cursor", "nextCursor", "snapshotId", "chunk", "chunks", "items", "runes", "formations", "recipes", "materials", "quote", "rift", "result", "skillRanks", "combatStats", "riftEntrances", "totalBytes", "data", "powers");
        Kind = EndgameData.String(d, "kind", 64); RequestID = EndgameData.String(d, "requestId", 64); OK = EndgameData.Bool(d, "ok");
        Error = EndgameData.String(d, "error", 64); Message = EndgameData.String(d, "message"); Revision = EndgameData.ID(d, "revision"); CatalogVersion = EndgameData.String(d, "catalogVersion", 128);
        Capabilities = EndgameData.List(d, "capabilities", 16, v => EndgameData.Text(v, "capabilities", 64));
        Cursor = EndgameData.String(d, "cursor", 128); NextCursor = EndgameData.String(d, "nextCursor", 128); SnapshotID = EndgameData.String(d, "snapshotId", 128);
        Chunk = EndgameData.Int(d, "chunk"); Chunks = EndgameData.Int(d, "chunks");
        TotalBytes = EndgameData.Int(d, "totalBytes"); Data = EndgameData.String(d, "data", RiftPageBytes / 3 * 4);
        if (Kind == "rift.page")
        {
            EndgameData.Keys(d, "kind", "ok", "snapshotId", "chunk", "chunks", "totalBytes", "data");
            EndgameData.Required(d, "ok", "snapshotId", "chunks", "totalBytes", "data");
            if (!OK || SnapshotID.Length == 0 || TotalBytes <= 0 || TotalBytes > MaximumRiftSnapshotBytes ||
                Chunks != (TotalBytes + RiftPageBytes - 1) / RiftPageBytes || Chunk < 0 || Chunk >= Chunks || Data.Length == 0)
                throw new FormatException("Invalid rift page envelope.");
        }
        else if (d.ContainsKey("totalBytes") || d.ContainsKey("data")) throw new FormatException("Unexpected rift page fields.");
        Powers = EndgameData.List(d, "powers", 50, v => new EndgamePowerRecordView(EndgameData.Object(v)));
        Items = EndgameData.List(d, "items", 32, v => new EndgameItemView(EndgameData.Object(v)));
        Runes = EndgameData.List(d, "runes", 32, v => new EndgameRuneView(EndgameData.Object(v)));
        Formations = EndgameData.List(d, "formations", 32, v => new EndgameFormationView(EndgameData.Object(v)));
        Recipes = EndgameData.List(d, "recipes", 32, v => new EndgameRecipeView(EndgameData.Object(v)));
        Materials = EndgameData.List(d, "materials", 64, v => new EndgameMaterialView(EndgameData.Object(v)));
        Quote = EndgameData.Optional(d, "quote", v => new EndgameQuoteView(EndgameData.Object(v)));
        Rift = EndgameData.Optional(d, "rift", v => new EndgameRiftView(EndgameData.Object(v)));
        RiftEntrances = EndgameData.List(d, "riftEntrances", 32, v => new EndgameRiftEntranceView(EndgameData.Object(v)));
        CombatStats = EndgameData.Optional(d, "combatStats", v => new EndgameCombatStatsView(EndgameData.Object(v)));
        SkillRanks = EndgameData.List(d, "skillRanks", 256, v => new EndgameSkillRankView(EndgameData.Object(v)));
        var skillIDs = new HashSet<int>();
        foreach (var rank in SkillRanks) if (!skillIDs.Add(rank.ID)) throw new FormatException("Duplicate skill rank.");
        Result = EndgameData.StringMap(d, "result");
        if (Kind.Length == 0 || Chunk < 0 || Chunks < 0 || Chunks > 128) throw new FormatException("Invalid endgame envelope.");
    }
    public const int MaximumBytes = 60 * 1024;
    public const int RiftPageBytes = 36 * 1024;
    public const int MaximumRiftSnapshotBytes = 1024 * 1024;
    // Only the negotiated, bounded page assembler may parse a larger logical document.
    // Native frame size, schema, strict UTF-8 and the shared JSON structural limits do not change.
    internal static EndgameResponse ParseAssembledRift(byte[] utf8)
    {
        if (utf8 == null || utf8.Length == 0 || utf8.Length > MaximumRiftSnapshotBytes) throw new FormatException("Rift snapshot exceeds limit.");
        var response = new EndgameResponse(EndgameData.Object(BoundedJson.Parse(new UTF8Encoding(false, true).GetString(utf8))));
        if (response.Rift == null || !response.Kind.StartsWith("rift.", StringComparison.Ordinal) || response.Kind == "rift.page" || response.Kind == "rift.context")
            throw new FormatException("Invalid assembled rift response.");
        return response;
    }
    public static EndgameResponse Parse(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > MaximumBytes) throw new FormatException("Endgame payload exceeds limit.");
        return new EndgameResponse(EndgameData.Object(BoundedJson.Parse(json)));
    }
}
}
