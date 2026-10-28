using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{
/// <summary>Server-authored, character-scoped best power record. No combat arithmetic lives here.</summary>
public sealed class EndgamePowerRecordView
{
    public string PowerID { get; }
    public string Name { get; }
    public string Category { get; }
    public string Class { get; }
    public string Description { get; }
    /// <summary>Server-authored legal parameter bounds, separate from the recorded effect.</summary>
    public string RangeText { get; }
    /// <summary>Actual recorded value of the same primary parameter as RangeText, authored by server.</summary>
    public string CurrentValueText { get; }
    /// <summary>Bounds of that same primary parameter, independent of prose ordering.</summary>
    public string PrimaryRangeText { get; }
    public int RollBP { get; }
    public int MinRollBP { get; }
    public int MaxRollBP { get; }
    public int MinLevel { get; }
    public bool Unlocked { get; }
    public IReadOnlyList<string> Slots { get; }
    internal EndgamePowerRecordView(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Required(d, "powerId", "name", "description");
        PowerID = EndgameData.String(d, "powerId", 128); Name = EndgameData.String(d, "name");
        Category = EndgameData.String(d, "category"); Class = EndgameData.String(d, "class");
        Description = EndgameData.String(d, "description"); RollBP = EndgameData.Int(d, "rollBp");
        RangeText = EndgameData.String(d, "rangeText");
        CurrentValueText = EndgameData.String(d, "currentValueText");
        PrimaryRangeText = EndgameData.String(d, "primaryRangeText");
        MinRollBP = EndgameData.Int(d, "minRollBp"); MaxRollBP = EndgameData.Int(d, "maxRollBp"); MinLevel = EndgameData.Int(d, "minLevel");
        Unlocked = EndgameData.Bool(d, "unlocked");
        Slots = EndgameData.List(d, "slots", 32, v => EndgameData.Text(v, "slots", 64));
        if (string.IsNullOrWhiteSpace(PowerID) || MinRollBP < 0 || MaxRollBP > 10000 || MinRollBP > MaxRollBP || Unlocked && (RollBP < MinRollBP || RollBP > MaxRollBP) || MinLevel < 0)
            throw new FormatException("Invalid power record.");
    }
}
public sealed partial class EndgameClientState
{
    public bool PowerBookAvailable { get; private set; }
    public int PowerBookItemID { get; private set; }
    public int PowerScrollItemID { get; private set; }
    public string PowerBookNPC { get; private set; } = "";
    public string PowerBookName { get; private set; } = "万象秘典";
    public bool PowerBookLoaded { get; private set; }
    public string PowerBookRevision { get; private set; } = "";
    public string PowerBookNextCursor { get; private set; } = "";
    public IReadOnlyList<EndgamePowerRecordView> Powers { get; private set; } = Array.Empty<EndgamePowerRecordView>();
    public bool PowerBookLoading => powerBookRead != null;
    public bool PowerBookCurrent => PowerBookLoaded && PowerBookRevision == Revision;
    private EndgameRequest powerBookRead;
    private DateTime powerBookReadAt;
    private void ResetPowerBookValues()
    {
        PowerBookLoaded = false; PowerBookRevision = PowerBookNextCursor = "";
        Powers = Array.Empty<EndgamePowerRecordView>(); powerBookRead = null;
    }
    private void ResetPowerBook() { PowerBookAvailable = false; PowerBookItemID = PowerScrollItemID = 0; PowerBookNPC = ""; ResetPowerBookValues(); }
    private bool BeginPowerBookRead(EndgameRequest request)
    {
        if (PowerBookLoading) return false;
        if (request.Cursor.Length > 0 && (!PowerBookCurrent || request.Cursor != PowerBookNextCursor)) return false;
        powerBookRead = request; powerBookReadAt = DateTime.UtcNow;
        return true;
    }
    private void ReleasePowerBookRead(string id) { if (powerBookRead?.RequestID == id) powerBookRead = null; }
    private void TickPowerBook(DateTime now)
    {
        if (powerBookRead != null && (now - powerBookReadAt).TotalSeconds >= 15)
        { powerBookRead = null; Error = "timeout"; Message = "秘典暂未响应，请刷新重试。"; Version++; }
    }
    private bool ApplyPowerBook(EndgameResponse response)
    {
        if (!PowerBookAvailable || powerBookRead == null || response.RequestID != powerBookRead.RequestID) return false;
        EndgameRequest request = powerBookRead; powerBookRead = null;
        if (!response.OK)
        { Error = response.Error; Message = response.Message.Length > 0 ? response.Message : ErrorText(response.Error); RememberResponse(response); Version++; return false; }
        if (response.Revision.Length == 0 || CompareRevision(response.Revision, Revision) < 0 ||
            request.Cursor.Length > 0 && response.Revision != PowerBookRevision)
        { PowerBookRevision = ""; Error = "stale_revision"; Message = "秘典已有变化，请刷新重试。"; Version++; return false; }
        EndgameData.Identity(response.NextCursor, "nextCursor");
        int offset = 0;
        if (request.Cursor.Length > 0 && !int.TryParse(request.Cursor, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out offset))
            throw new FormatException("Invalid power record cursor.");
        if (response.NextCursor.Length > 0 && (response.Powers.Count == 0 || response.NextCursor != (offset + response.Powers.Count).ToString(System.Globalization.CultureInfo.InvariantCulture)))
            throw new FormatException("Invalid power record cursor.");
        var rows = request.Cursor.Length == 0 ? new List<EndgamePowerRecordView>() : new List<EndgamePowerRecordView>(Powers);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var p in rows) ids.Add(p.PowerID);
        foreach (var p in response.Powers)
        {
            if (!ids.Add(p.PowerID)) throw new FormatException("Duplicate power record.");
            rows.Add(p);
        }
        Powers = rows.AsReadOnly(); PowerBookLoaded = true; PowerBookRevision = response.Revision;
        Revision = response.Revision; PowerBookNextCursor = response.NextCursor;
        Error = ""; Message = ""; RememberResponse(response); Version++; return true;
    }
}
}
