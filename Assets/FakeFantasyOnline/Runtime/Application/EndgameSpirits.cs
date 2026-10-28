using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{
/// <summary>Observed native bag address; optional exact UID prevents slot replacement races.</summary>
public sealed class EndgameSpiritSelection
{
    public int Tab { get; }
    public int Slot { get; }
    public int ItemID { get; }
    public string UID { get; }
    public EndgameSpiritSelection(int tab, int slot, int itemID, string uid = "")
    {
        if (tab < 0 || tab > 3 || slot < 0 || slot > 99 || itemID <= 0) throw new ArgumentException("Invalid spirit bag selection.");
        EndgameData.Identity(uid, "uid");
        if (uid == "0") throw new ArgumentException("Zero is not an item identity.");
        Tab = tab; Slot = slot; ItemID = itemID; UID = uid;
    }
    internal IDictionary<string, object> ToJson()
    {
        var result = new Dictionary<string, object> { { "tab", Tab }, { "slot", Slot }, { "itemId", ItemID } };
        if (UID.Length > 0) result.Add("uid", UID);
        return result;
    }
}
public sealed class EndgameSpiritDefinitionView
{
    public int Kind { get; }
    public string Name { get; }
    public int ItemID { get; }
    public int EssenceID { get; }
    public int SeparatorID { get; }
    public int NpcID { get; }
    public int MapID { get; }
    public string NpcName { get; }
    public string NpcScript { get; }
    public int Slot { get; }
    public string UIKey { get; }
    public string ProcessMoney { get; }
    internal EndgameSpiritDefinitionView(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Keys(d, "kind", "name", "itemId", "essenceId", "separatorId", "npcId", "mapId", "npcName", "npcScript", "slot", "uiKey", "processMoney");
        Kind = EndgameData.Int(d, "kind"); Name = EndgameData.String(d, "name", 64);
        ItemID = EndgameData.Int(d, "itemId"); EssenceID = EndgameData.Int(d, "essenceId"); SeparatorID = EndgameData.Int(d, "separatorId");
        MapID = EndgameData.Int(d, "mapId"); NpcName = EndgameData.String(d, "npcName", 64); NpcScript = EndgameData.String(d, "npcScript", 128);
        NpcID = EndgameData.Int(d, "npcId"); Slot = EndgameData.Int(d, "slot"); UIKey = EndgameData.String(d, "uiKey", 64);
        ProcessMoney = EndgameData.ID(d, "processMoney");
        if (Kind < 1 || Kind > 32 || Name.Length == 0 || ItemID <= 0 || EssenceID <= 0 || SeparatorID <= 0 ||
            NpcID <= 0 || MapID <= 0 || NpcName.Length == 0 || NpcScript.Length == 0 || Slot < 1 || Slot > 25 || UIKey.Length == 0 || ProcessMoney.Length == 0)
            throw new FormatException("Invalid spirit definition.");
    }
}
public sealed class EndgameSpiritAttributeView
{
    public int Attr { get; }
    public int Value { get; }
    public int Permanent { get; }
    public int Level { get; }
    public int PerfectLevel { get; }
    public int Last { get; }
    internal EndgameSpiritAttributeView(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Keys(d, "attr", "value", "permanent", "last", "level", "perfectLevel");
        Attr = EndgameData.Int(d, "attr"); Value = EndgameData.Int(d, "value");
        Level = EndgameData.Int(d, "level"); PerfectLevel = EndgameData.Int(d, "perfectLevel");
        Permanent = EndgameData.Int(d, "permanent"); Last = EndgameData.Int(d, "last");
        if (Attr <= 0 || Value < 0 || Permanent < 0 || Permanent > Value || Last < 0 || Last > 20 || Level < 0 || Level > 15 || PerfectLevel < 0 || PerfectLevel > Level)
            throw new FormatException("Invalid spirit attribute.");
    }
}
/// <summary>Authoritative view. The client never rolls, charges or reconstructs retained history.</summary>
public sealed class EndgameSpiritView
{
    public int Kind { get; }
    public int ItemID { get; }
    public bool Sealed { get; }
    public int Level { get; }
    public IReadOnlyList<EndgameSpiritAttributeView> Attrs { get; }
    public bool CanUnseal { get; }
    public bool CanProcess { get; }
    public bool CanRollback { get; }
    public bool CanReset { get; }
    public int UnsealEssence { get; }
    public int ProcessEssence { get; }
    public int RollbackEssence { get; }
    public int ResetEssence { get; }
    public string ProcessMoney { get; }
    internal EndgameSpiritView(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Keys(d, "kind", "itemId", "sealed", "level", "attrs", "canUnseal", "canProcess", "canRollback", "canReset",
            "unsealEssence", "processEssence", "rollbackEssence", "resetEssence", "processMoney");
        Kind = EndgameData.Int(d, "kind"); ItemID = EndgameData.Int(d, "itemId"); Sealed = EndgameData.Bool(d, "sealed"); Level = EndgameData.Int(d, "level");
        Attrs = EndgameData.List(d, "attrs", 4, v => new EndgameSpiritAttributeView(EndgameData.Object(v)));
        CanUnseal = EndgameData.Bool(d, "canUnseal"); CanProcess = EndgameData.Bool(d, "canProcess");
        CanRollback = EndgameData.Bool(d, "canRollback"); CanReset = EndgameData.Bool(d, "canReset");
        UnsealEssence = EndgameData.Int(d, "unsealEssence"); ProcessEssence = EndgameData.Int(d, "processEssence");
        RollbackEssence = EndgameData.Int(d, "rollbackEssence"); ResetEssence = EndgameData.Int(d, "resetEssence");
        ProcessMoney = EndgameData.ID(d, "processMoney");
        if (Kind < 1 || Kind > 32 || ItemID <= 0 || Level < 0 || Level > 15 ||
            UnsealEssence < 0 || ProcessEssence < 0 || RollbackEssence < 0 || ResetEssence < 0)
            throw new FormatException("Invalid spirit state.");
        var attrs = new HashSet<int>();
        foreach (var attr in Attrs) if (!attrs.Add(attr.Attr)) throw new FormatException("Repeated spirit attribute.");
        if (Sealed && (Level != 0 || Attrs.Count != 0)) throw new FormatException("Sealed spirit has processing state.");
    }
}
public sealed partial class EndgameClientState
{
    public bool SpiritAvailable { get; private set; }
    public IReadOnlyList<EndgameSpiritDefinitionView> Spirits { get; private set; } = Array.Empty<EndgameSpiritDefinitionView>();
    public IReadOnlyList<int> SpiritAppearanceIDs { get; private set; } = Array.Empty<int>();
    public EndgameSpiritView SpiritView { get; private set; }
    public string SpiritRevision { get; private set; } = "";
    public string SpiritRequestID { get; private set; } = "";
    public bool SpiritLoading => spiritRead != null;
    public bool SpiritCurrent => SpiritAvailable && SpiritView != null && SpiritRevision == Revision;
    private EndgameRequest spiritRead;
    private DateTime spiritReadAt;
    public void InvalidateSpiritPreview()
    {
        SpiritView = null; SpiritRevision = SpiritRequestID = ""; spiritRead = null; Version++;
    }
    private void ResetSpirit()
    {
        SpiritAvailable = false; Spirits = Array.Empty<EndgameSpiritDefinitionView>(); SpiritAppearanceIDs = Array.Empty<int>();
        InvalidateSpiritPreview();
    }
    private void ApplySpiritHello(EndgameResponse response, bool accepted)
    {
        bool capable = false;
        foreach (string cap in response.Capabilities) if (cap == "endgame.spirit.v1") capable = true;
        SpiritAvailable = accepted && capable;
        Spirits = SpiritAvailable ? response.Spirits : Array.Empty<EndgameSpiritDefinitionView>();
        SpiritAppearanceIDs = SpiritAvailable ? response.SpiritAppearanceIDs : Array.Empty<int>();
        InvalidateSpiritPreview();
    }
    private bool BeginSpirit(EndgameRequest request)
    {
        if (!SpiritAvailable) return false;
        if (request.Mutates) return !Pending && !SpiritLoading;
        if (Pending || SpiritLoading) return false;
        spiritRead = request; spiritReadAt = DateTime.UtcNow; SpiritRequestID = request.RequestID;
        if (request.Op == "spirit.preview") { SpiritView = null; SpiritRevision = ""; }
        return true;
    }
    private void ReleaseSpiritRead(string id)
    {
        if (spiritRead?.RequestID == id) spiritRead = null;
    }
    private void TickSpirit(DateTime now)
    {
        if (spiritRead == null || (now - spiritReadAt).TotalSeconds < 15) return;
        spiritRead = null; SpiritRequestID = "";
        Error = "timeout"; Message = "灵息尚未应答，请稍后重试。"; Version++;
    }
    private bool ApplySpiritRead(EndgameResponse response)
    {
        if (!SpiritAvailable || spiritRead == null || response.RequestID != spiritRead.RequestID) return false;
        EndgameRequest request = spiritRead;
        if (!request.MatchesResponse(response)) { NoteInvalidResponse("灵息回执与原请求不符。"); return false; }
        spiritRead = null;
        if (!response.OK)
        {
            SpiritView = null; SpiritRevision = ""; Error = response.Error;
            Message = response.Message.Length > 0 ? response.Message : ErrorText(response.Error);
            RememberResponse(response); Version++; return false;
        }
        if (response.Revision.Length == 0 || CompareRevision(response.Revision, Revision) < 0)
        {
            SpiritView = null; SpiritRevision = ""; Error = "stale_revision"; Message = "器物已有变化，请重新置入。";
            RememberResponse(response); Version++; return false;
        }
        if (request.Op == "spirit.open")
        {
            Spirits = response.Spirits;
            SpiritAppearanceIDs = response.SpiritAppearanceIDs;
        }
        else if (response.Spirit == null) throw new FormatException("Missing spirit preview.");
        SpiritView = response.Spirit; SpiritRevision = response.Revision; SpiritRequestID = response.RequestID;
        Revision = response.Revision; Error = Message = ""; RememberResponse(response); Version++; return true;
    }
    private void PublishSpiritResult(EndgameResponse response)
    {
        // Only an exact pending mutation reaches this method. Delayed unsolicited
        // replies cannot replace a more recent selection or free a different action.
        if (response.Revision.Length == 0 || CompareRevision(response.Revision, Revision) < 0) return;
        spiritRead = null;
        SpiritView = response.Spirit; SpiritRevision = response.Revision; SpiritRequestID = response.RequestID;
    }
}
}
