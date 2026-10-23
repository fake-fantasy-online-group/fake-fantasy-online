using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{
public sealed class EndgameStatView
{
    public int Attr { get; }
    public int Value { get; }
    public int Mode { get; }
    internal EndgameStatView(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Keys(d, "attr", "value", "mode");
        Attr = EndgameData.Int(d, "attr"); Value = EndgameData.Int(d, "value"); Mode = EndgameData.Int(d, "mode");
        if (Mode < 0 || Mode > 1) throw new FormatException("Invalid stat mode.");
    }
}
public sealed class EndgameSocketView
{
    public int Index { get; }
    public int ItemID { get; }
    public string UID { get; }
    public string Name { get; }
    internal EndgameSocketView(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Keys(d, "index", "itemId", "uid", "name");
        Index = EndgameData.Int(d, "index"); ItemID = EndgameData.Int(d, "itemId");
        UID = EndgameData.ID(d, "uid"); Name = EndgameData.String(d, "name");
        if (Index < 0 || Index > 4 || ItemID < 0) throw new FormatException("Invalid socket.");
    }
}
public sealed class EndgameItemView
{
    public string UID { get; }
    public int ItemID { get; }
    public string Location { get; }
    public int Tab { get; }
    public int Slot { get; }
    public string Revision { get; }
    public EndgameLegendaryItemView Legendary { get; }
    public int UniqueID { get; }
    public int UniqueRollBP { get; }
    public string Name { get; }
    public string Description { get; }
    public string Trait { get; }
    public int Level { get; }
    public int Profession { get; }
    public int SocketCount { get; }
    public IReadOnlyList<EndgameSocketView> Sockets { get; }
    public IReadOnlyList<EndgameStatView> Stats { get; }
    public int FormationID { get; }
    public bool Active { get; }
    public string InactiveReason { get; }
    public bool Bound { get; }
    public bool Locked { get; }
    internal EndgameItemView(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Keys(d, "uid", "itemId", "location", "tab", "slot", "revision", "uniqueId", "uniqueRollBp", "name", "description", "trait", "level", "profession", "socketCount", "sockets", "stats", "formationId", "active", "inactiveReason", "bound", "locked", "legendary");
        Legendary = EndgameData.Optional(d, "legendary", v => new EndgameLegendaryItemView(EndgameData.Object(v)));
        UID = EndgameData.ID(d, "uid"); ItemID = EndgameData.Int(d, "itemId");
        Location = EndgameData.String(d, "location", 32); Tab = EndgameData.Int(d, "tab"); Slot = EndgameData.Int(d, "slot");
        Revision = EndgameData.ID(d, "revision"); UniqueID = EndgameData.Int(d, "uniqueId"); UniqueRollBP = EndgameData.Int(d, "uniqueRollBp");
        Name = EndgameData.String(d, "name"); Description = EndgameData.String(d, "description"); Trait = EndgameData.String(d, "trait");
        Level = EndgameData.Int(d, "level"); Profession = EndgameData.Int(d, "profession");
        SocketCount = EndgameData.Int(d, "socketCount");
        Sockets = EndgameData.List(d, "sockets", 5, v => new EndgameSocketView(EndgameData.Object(v)));
        Stats = EndgameData.List(d, "stats", 64, v => new EndgameStatView(EndgameData.Object(v)));
        FormationID = EndgameData.Int(d, "formationId"); Active = EndgameData.Bool(d, "active");
        InactiveReason = EndgameData.String(d, "inactiveReason"); Bound = EndgameData.Bool(d, "bound"); Locked = EndgameData.Bool(d, "locked");
        if (ItemID <= 0 || SocketCount < 0 || SocketCount > 5) throw new FormatException("Invalid endgame item.");
        var indexes = new HashSet<int>();
        foreach (var socket in Sockets) if (!indexes.Add(socket.Index)) throw new FormatException("Repeated socket index.");
    }
}
}
