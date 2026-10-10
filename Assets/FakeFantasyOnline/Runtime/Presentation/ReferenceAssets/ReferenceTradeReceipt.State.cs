using PetBrief = FakeFantasyOnline.Application.PetBrief;
using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static partial class ReferenceTradeReceipt
{
    internal static void StoreFlag(TradeReceiptFlag field, bool value)
    {
        switch (field)
        {
            case TradeReceiptFlag.MyLocked: SocialClient.MyLocked = value; break;
            case TradeReceiptFlag.OtherLocked: SocialClient.OtherLocked = value; break;
            case TradeReceiptFlag.MyConfirmed: SocialClient.MyConfirmed = value; break;
            case TradeReceiptFlag.OtherConfirmed: SocialClient.OtherConfirmed = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(field));
        }
    }

    internal static void StoreMoney(bool ownSide, long value)
    {
        if (ownSide) SocialClient.MyMoney = value;
        else SocialClient.OtherMoney = value;
    }

    internal static void ClearItems(bool ownSide) =>
        (ownSide ? SocialClient.MyItems : SocialClient.OtherItems).Clear();

    internal static void AddItem(bool ownSide, int itemId, int count, string name) =>
        (ownSide ? SocialClient.MyItems : SocialClient.OtherItems).Add((itemId, count, name));

    internal static void ClearItemInfo(bool ownSide) =>
        (ownSide ? SocialClient.MyItemInfo : SocialClient.OtherItemInfo).Clear();

    internal static void AddItemInfo(bool ownSide, string text, byte quality) =>
        (ownSide ? SocialClient.MyItemInfo : SocialClient.OtherItemInfo).Add((text, quality));

    internal static void ClearPets(bool ownSide) =>
        (ownSide ? SocialClient.MyItemPets : SocialClient.OtherItemPets).Clear();

    internal static void StorePet(bool ownSide, int index, PetBrief pet) =>
        (ownSide ? SocialClient.MyItemPets : SocialClient.OtherItemPets)[index] =
            pet;

    internal static void StorePetPoints(bool ownSide, int index,
        int aiUsed, int aiCapacity, int shengUsed, int shengCapacity)
    {
        var pets = ownSide ? SocialClient.MyItemPets : SocialClient.OtherItemPets;
        if (!pets.TryGetValue(index, out PetBrief pet)) return;
        pet.ppAiUsed = aiUsed;
        pet.ppAiCap = aiCapacity;
        pet.ppShUsed = shengUsed;
        pet.ppShCap = shengCapacity;
        pets[index] = pet;
    }

    // Observation only, requested after native mutation. No receipt allocates a mirror before
    // its next field/list write, and a failed receipt keeps its original exception.
    internal static WorldActiveTradeState CaptureState()
    {
        return new WorldActiveTradeState(SocialClient.TradeActive, SocialClient.OtherId,
            SocialClient.OtherName, SocialClient.MyLocked, SocialClient.OtherLocked,
            SocialClient.MyConfirmed, SocialClient.OtherConfirmed,
            SocialClient.MyMoney, SocialClient.OtherMoney,
            ObserveItems(SocialClient.MyItems), ObserveItems(SocialClient.OtherItems),
            ObserveInfo(SocialClient.MyItemInfo), ObserveInfo(SocialClient.OtherItemInfo),
            ObservePets(SocialClient.MyItemPets), ObservePets(SocialClient.OtherItemPets));
    }

    private static List<WorldTradeItemState> ObserveItems(List<(int, int, string)> source)
    {
        var rows = new List<WorldTradeItemState>(source.Count);
        foreach (var row in source)
            rows.Add(new WorldTradeItemState(row.Item1, row.Item2, row.Item3));
        return rows;
    }

    private static List<WorldTradeItemInfoState> ObserveInfo(List<(string, byte)> source)
    {
        var rows = new List<WorldTradeItemInfoState>(source.Count);
        foreach (var row in source)
            rows.Add(new WorldTradeItemInfoState(row.Item1, row.Item2));
        return rows;
    }

    private static Dictionary<int, WorldTradePetState> ObservePets(Dictionary<int, PetBrief> source)
    {
        var result = new Dictionary<int, WorldTradePetState>(source.Count);
        foreach (var row in source)
        {
            PetBrief p = row.Value;
            var skills = new List<WorldInventoryPetSkillState>(p.skills?.Count ?? 0);
            if (p.skills != null)
                foreach (var skill in p.skills)
                    skills.Add(new WorldInventoryPetSkillState(skill.Item1, skill.Item2));
            var pet = new WorldInventoryPetState(p.model, p.slot, p.nickname, p.prename,
                p.species, p.gender, p.habit, p.opened, p.level, p.str, p.intel,
                p.vit, p.agi, p.dex, p.spi, p.starve, p.trust, p.freePts, p.freeUsed,
                skills, p.bound);
            result.Add(row.Key, new WorldTradePetState(pet, p.ppAiUsed, p.ppAiCap, p.ppShUsed, p.ppShCap));
        }
        return result;
    }
}
}
