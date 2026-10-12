using System;
using System.Collections.Generic;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Application
{
public interface IEndgameClient
{
    EndgameClientState Endgame { get; }
    bool EndgameInWorld { get; }
    bool SendEndgame(EndgameRequest request);
    bool RetryEndgame();
    bool EquipEndgameItem(EndgameItemView item);
    bool UnequipEndgameItem(EndgameItemView item);
}
/// <summary>An immutable command; a retry sends precisely the same JSON and request ID.</summary>
public sealed class EndgameRequest
{
    public string Op { get; }
    public string RequestID { get; }
    public string Json { get; }
    public string Catalog { get; }
    public string Cursor { get; }
    public bool Mutates => Op == "socket.insert" || Op == "socket.remove" || Op == "cube.execute" || Op == "rift.start" || Op == "rift.claim" || Op == "rift.leave" || Op == "rift.advance" || Op == "skill.upgrade";
    public EndgameRequest(string op, string revision = "", string targetUID = "", string runeUID = "", int socket = 0,
        int recipeID = 0, string quoteID = "", string runID = "", int tier = 0,
        IReadOnlyList<string> itemUIDs = null, string catalog = "", string cursor = "", string requestID = null, int floorIndex = 0, int skillID = 0)
    {
        if (Array.IndexOf(new[] { "skill.ranks", "skill.upgrade", "hello", "snapshot", "catalog", "socket.insert", "socket.remove", "cube.quote", "cube.execute", "rift.start", "rift.state", "rift.claim", "rift.leave", "rift.advance" }, op) < 0) throw new ArgumentException("Unknown endgame operation.");
        Op = op; Catalog = catalog; Cursor = cursor ?? ""; RequestID = requestID ?? Guid.NewGuid().ToString("N");
        if (RequestID.Length < 16 || RequestID.Length > 64) throw new ArgumentException("Invalid request ID.");
        foreach (char c in RequestID) if (c < 33 || c > 126) throw new ArgumentException("Invalid request ID.");
        if (socket < 0 || socket > 4 || tier < 0 || tier > 150 || recipeID < 0 || floorIndex < 0) throw new ArgumentException("Command is out of range.");
        if (op == "rift.advance" && (string.IsNullOrEmpty(runID) || floorIndex < 1)) throw new ArgumentException("Advancing requires the observed run ID and floor index.");
        var data = new Dictionary<string, object> { { "op", op }, { "requestId", RequestID } };
        Add(data, "expectedRevision", revision); Add(data, "targetUid", targetUID); Add(data, "runeUid", runeUID); Add(data, "quoteId", quoteID); Add(data, "runId", runID);
        if (op == "hello") data.Add("capabilities", new object[] { "endgame.v1", "endgame.native-stats.v1", "endgame.growth.v5", "endgame.skill-behavior.v1", "endgame.skill-variants.v1", "endgame.rift-portals.v1" });
        if (op == "skill.upgrade")
        {
            if (skillID <= 0 || string.IsNullOrEmpty(revision)) throw new ArgumentException("Skill investment requires skillId and expectedRevision.");
            data.Add("skillId", skillID);
        }
        else if (skillID != 0) throw new ArgumentException("skillId is only valid for skill.upgrade.");
        if (op == "catalog")
        {
            if (Array.IndexOf(new[] { "uniques", "runes", "formations", "recipes" }, catalog) < 0) throw new ArgumentException("Invalid catalog.");
            data.Add("catalog", catalog); Add(data, "cursor", Cursor);
        }
        if (op.StartsWith("socket.", StringComparison.Ordinal)) data.Add("socket", socket);
        if (recipeID != 0) data.Add("recipeId", recipeID);
        if (tier != 0) data.Add("tier", tier);
        if (op == "rift.advance") data.Add("floorIndex", floorIndex);
        if (itemUIDs != null)
        {
            if (itemUIDs.Count > 5) throw new ArgumentException("Too many rune UIDs.");
            var ids = new List<object>(); var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in itemUIDs)
            {
                EndgameData.Identity(id, "itemUids");
                if (string.IsNullOrEmpty(id) || id == "0" || !seen.Add(id)) throw new ArgumentException("Duplicate or empty rune UID.");
                ids.Add(id);
            }
            data.Add("itemUids", ids);
        }
        EndgameData.ID(data, "expectedRevision"); EndgameData.ID(data, "targetUid"); EndgameData.ID(data, "runeUid");
        Json = BoundedJson.Serialize(data);
    }
    // Shared correlation contract for native UI state and plugin receipts.
    public bool MatchesResponse(EndgameResponse response)
    {
        if (response == null || response.RequestID != RequestID) return false;
        if (!response.OK) return response.Kind == Op;
        if (Op == "cube.execute") return response.Kind == "cube.result";
        if (Op == "catalog") return (response.Kind == "catalog." + Catalog || response.Kind == "catalog") && response.Cursor == Cursor;
        return response.Kind == Op;
    }
    private static void Add(IDictionary<string, object> data, string key, string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        if (value.Length > 128) throw new ArgumentException("Identifier exceeds limit.");
        data.Add(key, value);
    }
}
}
