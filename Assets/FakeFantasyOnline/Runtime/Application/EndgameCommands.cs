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
    public bool Mutates => Op == "spirit.unseal" || Op == "spirit.process" || Op == "spirit.rollback" || Op == "spirit.reset" || Op == "spirit.fuse" || Op == "spirit.separate" || Op == "spirit.appearance" || Op == "socket.insert" || Op == "socket.remove" || Op == "cube.execute" || Op == "cube.recover" || Op == "rift.start" || Op == "rift.claim" || Op == "rift.leave" || Op == "rift.advance" || Op == "skill.upgrade" || (Op == "powerbook.extract" || Op == "powerbook.apply" || Op == "powerbook.recover");
    public EndgameRequest(string op, string revision = "", string targetUID = "", string runeUID = "", int socket = 0,
        int recipeID = 0, string quoteID = "", string runID = "", int tier = 0,
        IReadOnlyList<string> itemUIDs = null, string catalog = "", string cursor = "", string requestID = null, int floorIndex = 0, int skillID = 0, int npcEntityID = 0, IReadOnlyList<EndgameCubeInput> cubeInputs = null, string powerID = "", EndgameSpiritSelection spiritItem = null, EndgameSpiritSelection spiritMaterial = null)
    {
        if (Array.IndexOf(new[] { "spirit.open", "spirit.preview", "spirit.unseal", "spirit.process", "spirit.rollback", "spirit.reset", "spirit.fuse", "spirit.separate", "spirit.appearance", "powerbook.list", "powerbook.recover", "powerbook.extract", "powerbook.apply", "skill.ranks", "skill.upgrade", "hello", "capabilities", "snapshot", "catalog", "socket.insert", "socket.remove", "cube.quote", "cube.execute", "cube.recover", "rift.start", "rift.state", "rift.claim", "rift.leave", "rift.advance" }, op) < 0) throw new ArgumentException("Unknown endgame operation.");
        Op = op; Catalog = catalog; Cursor = cursor ?? ""; RequestID = requestID ?? Guid.NewGuid().ToString("N");
        if (RequestID.Length < 16 || RequestID.Length > 64) throw new ArgumentException("Invalid request ID.");
        foreach (char c in RequestID) if (c < 33 || c > 126) throw new ArgumentException("Invalid request ID.");
        if (socket < 0 || socket > 4 || tier < 0 || tier > 150 || recipeID < 0 || floorIndex < 0) throw new ArgumentException("Command is out of range.");
        if ((op == "cube.recover" || op == "powerbook.recover") && (npcEntityID == 0 || string.IsNullOrEmpty(revision))) throw new ArgumentException("Recovery requires the current NPC and revision.");
        if (op == "rift.advance" && (string.IsNullOrEmpty(runID) || floorIndex < 1)) throw new ArgumentException("Advancing requires the observed run ID and floor index.");
        var data = new Dictionary<string, object> { { "op", op }, { "requestId", RequestID } };
        Add(data, "expectedRevision", revision); Add(data, "targetUid", targetUID); Add(data, "runeUid", runeUID); Add(data, "quoteId", quoteID); Add(data, "runId", runID);
        if (op == "hello") data.Add("capabilities", new object[] { "endgame.v1", "endgame.native-stats.v1", "endgame.growth.v5", "endgame.skill-behavior.v1", "endgame.skill-variants.v1", "endgame.rift-portals.v1", "endgame.rift-entrance.v1", "endgame.cube-inputs.v1", "endgame.power-codex.v1", "endgame.spirit.v1" });
        if (op == "capabilities") data.Add("capabilities", new object[] { "endgame.v1", "endgame.rift-pages.v1", "endgame.ground-fx.v1" });
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
        if (op == "powerbook.list") { EndgameData.Identity(Cursor, "cursor"); Add(data, "cursor", Cursor); }
        if (op == "powerbook.extract")
        {
            if (string.IsNullOrWhiteSpace(powerID) || string.IsNullOrEmpty(revision) || targetUID.Length != 0 || runeUID.Length != 0) throw new ArgumentException("Extraction requires a recorded power and revision.");
            Add(data, "powerId", powerID);
        }
        else if (powerID.Length != 0) throw new ArgumentException("powerId is only valid for powerbook.extract.");
        if (op == "powerbook.apply" && (string.IsNullOrEmpty(revision) || string.IsNullOrEmpty(targetUID) || targetUID == "0" || string.IsNullOrEmpty(runeUID) || runeUID == "0" || targetUID == runeUID))
            throw new ArgumentException("Applying requires distinct scroll and target identities and revision.");
        if (op.StartsWith("socket.", StringComparison.Ordinal)) data.Add("socket", socket);
        if (recipeID != 0) data.Add("recipeId", recipeID);
        if (tier != 0) data.Add("tier", tier);
        if (op == "rift.advance") data.Add("floorIndex", floorIndex);
        if (npcEntityID != 0)
        {
            if (op != "rift.start" && op != "cube.quote" && op != "cube.execute" && op != "cube.recover" && op != "powerbook.recover" && !op.StartsWith("spirit.", StringComparison.Ordinal)) throw new ArgumentException("npcEntityId is not valid for this operation.");
            data.Add("npcEntityId", (long)unchecked((uint)npcEntityID));
        }
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
        if (cubeInputs != null)
        {
            if (op != "cube.quote" || recipeID != 0 || targetUID.Length != 0 || runeUID.Length != 0 || cubeInputs.Count < 1 || cubeInputs.Count > EndgameCubeInput.Capacity) throw new ArgumentException("Invalid cube input request.");
            var inputs = new List<object>(); var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var input in cubeInputs)
            {
                if (input == null || !seen.Add(input.UID.Length > 0 ? "u" + input.UID : "i" + input.ItemID)) throw new ArgumentException("Duplicate cube input.");
                var row = new Dictionary<string, object> { { "count", input.Count } };
                if (input.UID.Length > 0) row.Add("uid", input.UID); else row.Add("itemId", input.ItemID);
                inputs.Add(row);
            }
            data.Add("cubeInputs", inputs);
        }
        if (op.StartsWith("spirit.", StringComparison.Ordinal) && (runeUID.Length > 0 || socket != 0 || recipeID != 0 ||
            quoteID.Length > 0 || runID.Length > 0 || tier != 0 || itemUIDs != null || catalog.Length > 0 || Cursor.Length > 0 || floorIndex != 0))
            throw new ArgumentException("Unexpected fields in spirit operation.");
        WriteSpiritSelection(data, op, revision, targetUID, npcEntityID, spiritItem, spiritMaterial);
        EndgameData.ID(data, "expectedRevision"); EndgameData.ID(data, "targetUid"); EndgameData.ID(data, "runeUid");
        Json = BoundedJson.Serialize(data);
    }
    // Shared correlation contract for native UI state and plugin receipts.
    public bool MatchesResponse(EndgameResponse response)
    {
        if (response == null || response.RequestID != RequestID) return false;
        if (!response.OK) return response.Kind == Op;
        if (Op == "powerbook.extract" || Op == "powerbook.apply" || Op == "powerbook.recover") return response.Kind == "powerbook.result";
        if (Op.StartsWith("spirit.", StringComparison.Ordinal) && Mutates) return response.Kind == "spirit.result";
        if (Op == "cube.execute") return response.Kind == "cube.result";
        if (Op == "catalog") return (response.Kind == "catalog." + Catalog || response.Kind == "catalog") && response.Cursor == Cursor;
        return response.Kind == Op;
    }
    private static void WriteSpiritSelection(IDictionary<string, object> data, string op, string revision, string targetUID,
        int npcEntityID, EndgameSpiritSelection item, EndgameSpiritSelection material)
    {
        bool spirit = op.StartsWith("spirit.", StringComparison.Ordinal);
        if (!spirit)
        { if (item != null || material != null) throw new ArgumentException("Spirit selections require a spirit operation."); return; }
        bool workbench = op == "spirit.open" || op == "spirit.preview" || op == "spirit.unseal" || op == "spirit.process" || op == "spirit.rollback" || op == "spirit.reset";
        bool read = op == "spirit.open" || op == "spirit.preview";
        if (workbench && npcEntityID == 0 || !workbench && npcEntityID != 0)
            throw new ArgumentException("Invalid spirit NPC context.");
        if (!read && string.IsNullOrEmpty(revision)) throw new ArgumentException("Spirit mutation requires expectedRevision.");
        if (op == "spirit.open")
        { if (item != null || material != null || targetUID.Length > 0) throw new ArgumentException("Open has no item selection."); return; }
        if (item == null) throw new ArgumentException("Spirit operation requires a selected bag item.");
        if (workbench)
        {
            if (targetUID.Length > 0 || !read && material == null || read && material != null)
                throw new ArgumentException("Invalid spirit material selection.");
            if (material != null && item.Tab == material.Tab && item.Slot == material.Slot)
                throw new ArgumentException("Spirit and essence must be distinct items.");
        }
        else if (material != null || targetUID.Length == 0 || targetUID == "0" || item.UID.Length > 0 && item.UID == targetUID)
            throw new ArgumentException("Spirit equipment action requires a distinct target identity.");
        data.Add("spiritItem", item.ToJson());
        if (material != null) data.Add("spiritMaterial", material.ToJson());
    }
    private static void Add(IDictionary<string, object> data, string key, string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        if (value.Length > 128) throw new ArgumentException("Identifier exceeds limit.");
        data.Add(key, value);
    }
}
}
