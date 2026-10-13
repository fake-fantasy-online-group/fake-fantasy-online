using System;
namespace FakeFantasyOnline.Application
{
// A local reservation. Only a successful server transaction removes bag items.
public sealed class EndgameCubeInput
{
    public const int Capacity = 35;
    public string UID { get; }
    public int ItemID { get; }
    public int Count { get; }
    public EndgameCubeInput(string uid, int itemID, int count)
    {
        UID = uid ?? ""; ItemID = itemID; Count = count;
        if (count < 1 || UID.Length == 0 && itemID <= 0 || UID.Length > 0 && (itemID != 0 || count != 1)) throw new ArgumentException("Invalid cube input.");
        if (UID.Length > 0) { EndgameData.Identity(UID, "uid"); if (UID == "0") throw new ArgumentException("Invalid cube UID."); }
    }
}
}
