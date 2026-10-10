using PetBrief = FakeFantasyOnline.Application.PetBrief;
using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public static partial class SocialClient
{
    public static bool TradeActive, MyLocked, OtherLocked, MyConfirmed, OtherConfirmed;
    public static int OtherId;
    public static string OtherName = string.Empty;
    public static long MyMoney, OtherMoney;
    public static readonly List<(int, int, string)> MyItems = new List<(int, int, string)>();
    public static readonly List<(int, int, string)> OtherItems = new List<(int, int, string)>();
    public static readonly List<(string, byte)> MyItemInfo = new List<(string, byte)>();
    public static readonly List<(string, byte)> OtherItemInfo = new List<(string, byte)>();
    internal static readonly Dictionary<int, PetBrief> MyItemPets = new Dictionary<int, PetBrief>();
    internal static readonly Dictionary<int, PetBrief> OtherItemPets = new Dictionary<int, PetBrief>();

    internal static void ApplyTradeStateReceiptForIndependentRuntime(WorldActiveTradeState state)
    {
        MyLocked = state.MyLocked; OtherLocked = state.OtherLocked; MyConfirmed = state.MyConfirmed; OtherConfirmed = state.OtherConfirmed;
        MyMoney = state.MyMoney; OtherMoney = state.OtherMoney;
        MyItems.Clear(); OtherItems.Clear(); MyItemInfo.Clear(); OtherItemInfo.Clear();
        MyItemPets.Clear(); OtherItemPets.Clear();
        foreach (WorldTradeItemState row in state.MyItems) MyItems.Add((row.ItemId, row.Count, row.DisplayName));
        foreach (WorldTradeItemState row in state.OtherItems) OtherItems.Add((row.ItemId, row.Count, row.DisplayName));
        foreach (WorldTradeItemInfoState row in state.MyItemInfo) MyItemInfo.Add(row == null ? default : (row.Text, row.Quality));
        foreach (WorldTradeItemInfoState row in state.OtherItemInfo) OtherItemInfo.Add(row == null ? default : (row.Text, row.Quality));
        foreach (var entry in state.MyPets) MyItemPets.Add(entry.Key, ProjectTradePet(entry.Value));
        foreach (var entry in state.OtherPets) OtherItemPets.Add(entry.Key, ProjectTradePet(entry.Value));
    }
    private static PetBrief ProjectTradePet(WorldTradePetState row)
    {
        PetBrief pet = InventoryDlg.ProjectPetBriefForIndependentRuntime(row.Pet);
        pet.ppAiUsed = row.AiUsed; pet.ppAiCap = row.AiCapacity;
        pet.ppShUsed = row.ShengUsed; pet.ppShCap = row.ShengCapacity;
        return pet;
    }
}
}
