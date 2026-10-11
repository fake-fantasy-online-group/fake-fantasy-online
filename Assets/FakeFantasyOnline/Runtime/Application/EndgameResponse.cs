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
    public IReadOnlyList<EndgameItemView> Items { get; }
    public IReadOnlyList<EndgameRuneView> Runes { get; }
    public IReadOnlyList<EndgameFormationView> Formations { get; }
    public IReadOnlyList<EndgameRecipeView> Recipes { get; }
    public IReadOnlyList<EndgameMaterialView> Materials { get; }
    public EndgameQuoteView Quote { get; }
    public EndgameRiftView Rift { get; }
    public IReadOnlyDictionary<string, string> Result { get; }
    private EndgameResponse(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Keys(d, "kind", "requestId", "ok", "error", "message", "revision", "catalogVersion", "capabilities", "cursor", "nextCursor", "snapshotId", "chunk", "chunks", "items", "runes", "formations", "recipes", "materials", "quote", "rift", "result");
        Kind = EndgameData.String(d, "kind", 64); RequestID = EndgameData.String(d, "requestId", 64); OK = EndgameData.Bool(d, "ok");
        Error = EndgameData.String(d, "error", 64); Message = EndgameData.String(d, "message"); Revision = EndgameData.ID(d, "revision"); CatalogVersion = EndgameData.String(d, "catalogVersion", 128);
        Capabilities = EndgameData.List(d, "capabilities", 16, v => EndgameData.Text(v, "capabilities", 64));
        Cursor = EndgameData.String(d, "cursor", 128); NextCursor = EndgameData.String(d, "nextCursor", 128); SnapshotID = EndgameData.String(d, "snapshotId", 128);
        Chunk = EndgameData.Int(d, "chunk"); Chunks = EndgameData.Int(d, "chunks");
        Items = EndgameData.List(d, "items", 32, v => new EndgameItemView(EndgameData.Object(v)));
        Runes = EndgameData.List(d, "runes", 32, v => new EndgameRuneView(EndgameData.Object(v)));
        Formations = EndgameData.List(d, "formations", 32, v => new EndgameFormationView(EndgameData.Object(v)));
        Recipes = EndgameData.List(d, "recipes", 32, v => new EndgameRecipeView(EndgameData.Object(v)));
        Materials = EndgameData.List(d, "materials", 64, v => new EndgameMaterialView(EndgameData.Object(v)));
        Quote = EndgameData.Optional(d, "quote", v => new EndgameQuoteView(EndgameData.Object(v)));
        Rift = EndgameData.Optional(d, "rift", v => new EndgameRiftView(EndgameData.Object(v)));
        Result = EndgameData.StringMap(d, "result");
        if (Kind.Length == 0 || Chunk < 0 || Chunks < 0 || Chunks > 128) throw new FormatException("Invalid endgame envelope.");
    }
    public const int MaximumBytes = 60 * 1024;
    public static EndgameResponse Parse(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > MaximumBytes) throw new FormatException("Endgame payload exceeds limit.");
        return new EndgameResponse(EndgameData.Object(BoundedJson.Parse(json)));
    }
}
}
