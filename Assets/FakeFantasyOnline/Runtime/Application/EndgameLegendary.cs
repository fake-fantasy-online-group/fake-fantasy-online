using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{
/// <summary>Optional, server-authored item presentation. IDs are opaque, not a client power whitelist.
/// Unknown optional members are ignored so a newer server can extend presentation independently.
/// Neither definitions nor rolled affixes execute combat or alter native equipment statistics.</summary>
public sealed class EndgameLegendaryItemView
{
    public string PowerID { get; }
    public string Name { get; }
    public string Description { get; }
    public IReadOnlyList<EndgameLegendaryAffixView> Affixes { get; }
    internal EndgameLegendaryItemView(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Required(d, "powerId", "name", "description");
        PowerID = EndgameData.String(d, "powerId");
        Name = EndgameData.String(d, "name");
        Description = EndgameData.String(d, "description");
        if (string.IsNullOrWhiteSpace(PowerID)) throw new FormatException("Missing legendary power identity.");
        // Six independently rolled affixes are the item contract, not native five-line storage.
        Affixes = EndgameData.List(d, "affixes", 6, v => new EndgameLegendaryAffixView(EndgameData.Object(v)));
    }
}
public sealed class EndgameLegendaryAffixView
{
    public int Attr { get; }
    public int Value { get; }
    public int Mode { get; }
    public int Min { get; }
    public int Max { get; }
    internal EndgameLegendaryAffixView(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Required(d, "attr", "value", "mode", "min", "max");
        Attr = EndgameData.Int(d, "attr"); Value = EndgameData.Int(d, "value"); Mode = EndgameData.Int(d, "mode");
        Min = EndgameData.Int(d, "min"); Max = EndgameData.Int(d, "max");
        if (Mode < 0 || Mode > 1 || Min > Max || Value < Min || Value > Max)
            throw new FormatException("Invalid legendary affix roll.");
    }
}
}
