using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{
public sealed class EndgameRuneView
{
    public int ItemID { get; }
    public string Name { get; }
    public string Description { get; }
    public string Quadrant { get; }
    public string Mansion { get; }
    public IReadOnlyList<EndgameStatView> Stats { get; }
    internal EndgameRuneView(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Keys(d, "itemId", "name", "description", "quadrant", "mansion", "stats");
        ItemID = EndgameData.Int(d, "itemId"); Name = EndgameData.String(d, "name"); Description = EndgameData.String(d, "description"); Quadrant = EndgameData.String(d, "quadrant"); Mansion = EndgameData.String(d, "mansion");
        Stats = EndgameData.List(d, "stats", 64, v => new EndgameStatView(EndgameData.Object(v)));
    }
}
public sealed class EndgameFormationView
{
    public int ID { get; }
    public string Name { get; }
    public string Description { get; }
    public string Channel { get; }
    public IReadOnlyList<int> Runes { get; }
    public IReadOnlyList<EndgameStatView> Stats { get; }
    public int MinLevel { get; }
    public int Professions { get; }
    public IReadOnlyList<int> Slots { get; }
    public bool Active { get; }
    public string InactiveReason { get; }
    internal EndgameFormationView(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Keys(d, "id", "name", "description", "channel", "runes", "stats", "minLevel", "professions", "slots", "active", "inactiveReason");
        ID = EndgameData.Int(d, "id"); Name = EndgameData.String(d, "name"); Description = EndgameData.String(d, "description"); Channel = EndgameData.String(d, "channel");
        Runes = EndgameData.List(d, "runes", 5, v => EndgameData.Integer(v, "runes"));
        Stats = EndgameData.List(d, "stats", 64, v => new EndgameStatView(EndgameData.Object(v)));
        MinLevel = EndgameData.Int(d, "minLevel"); Professions = EndgameData.Int(d, "professions");
        Slots = EndgameData.List(d, "slots", 32, v => EndgameData.Integer(v, "slots"));
        Active = EndgameData.Bool(d, "active"); InactiveReason = EndgameData.String(d, "inactiveReason");
    }
}
public sealed class EndgameMaterialView
{
    public int ItemID { get; }
    public int Count { get; }
    public int Owned { get; }
    public string Name { get; }
    internal EndgameMaterialView(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Keys(d, "itemId", "count", "owned", "name");
        ItemID = EndgameData.Int(d, "itemId"); Count = EndgameData.Int(d, "count"); Owned = EndgameData.Int(d, "owned"); Name = EndgameData.String(d, "name");
        if (ItemID <= 0 || Count < 0 || Owned < 0) throw new FormatException("Invalid material count.");
    }
}
public sealed class EndgameRecipeView
{
    public int ID { get; }
    public string Name { get; }
    public string Description { get; }
    public string Operation { get; }
    public string Money { get; }
    public IReadOnlyList<EndgameMaterialView> Materials { get; }
    public int OutputItemID { get; }
    public bool TargetRequired { get; }
    internal EndgameRecipeView(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Keys(d, "id", "name", "description", "operation", "money", "materials", "outputItemId", "targetRequired");
        ID = EndgameData.Int(d, "id"); Name = EndgameData.String(d, "name"); Description = EndgameData.String(d, "description"); Operation = EndgameData.String(d, "operation", 64);
        Money = EndgameData.ID(d, "money"); Materials = EndgameData.List(d, "materials", 64, v => new EndgameMaterialView(EndgameData.Object(v)));
        OutputItemID = EndgameData.Int(d, "outputItemId"); TargetRequired = EndgameData.Bool(d, "targetRequired");
    }
}
public sealed class EndgameQuoteView
{
    public string QuoteID { get; }
    public int RecipeID { get; }
    public string TargetUID { get; }
    public string Revision { get; }
    public string Money { get; }
    public IReadOnlyList<EndgameMaterialView> Materials { get; }
    public string ExpiresAt { get; }
    public string Description { get; }
    internal EndgameQuoteView(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Keys(d, "quoteId", "recipeId", "targetUid", "revision", "money", "materials", "expiresAt", "description");
        QuoteID = EndgameData.String(d, "quoteId", 128); RecipeID = EndgameData.Int(d, "recipeId"); TargetUID = EndgameData.ID(d, "targetUid"); Revision = EndgameData.ID(d, "revision"); Money = EndgameData.ID(d, "money");
        Materials = EndgameData.List(d, "materials", 64, v => new EndgameMaterialView(EndgameData.Object(v)));
        ExpiresAt = EndgameData.String(d, "expiresAt", 64); Description = EndgameData.String(d, "description");
    }
}
}
