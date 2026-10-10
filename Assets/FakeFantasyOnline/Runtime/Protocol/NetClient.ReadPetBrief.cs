using System.Collections.Generic;
using FakeFantasyOnline.Application;
namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // The recovered reader returns a Unity-free representation across the protocol boundary.
    internal static PetBrief ReadPetBrief(Buf reader)
    {
        int model = reader.I32();
        int slot = reader.I32();
        string nickname = reader.Str();
        string prename = reader.Str();
        string species = reader.Str();
        byte gender = (byte)reader.U8();
        byte habit = (byte)reader.U8();
        byte opened = (byte)reader.U8();
        int level = reader.I32();
        int strength = reader.I32();
        int intelligence = reader.I32();
        int vitality = reader.I32();
        int agility = reader.I32();
        int dexterity = reader.I32();
        int spirit = reader.I32();
        int starvation = reader.I32();
        int trust = reader.I32();
        int freePoints = reader.I32();
        int usedFreePoints = reader.I32();
        int count = reader.U8();
        var skills = new List<(string, int)>();
        for (int i = 0; i < count; i++) skills.Add((reader.Str(), reader.I32()));
        bool bound = reader.Remaining > 0 && reader.U8() != 0;
        return new PetBrief
        {
            model = model, slot = slot, nickname = nickname, prename = prename,
            species = species, gender = gender, habit = habit, opened = opened,
            level = level, str = strength, intel = intelligence, vit = vitality,
            agi = agility, dex = dexterity, spi = spirit, starve = starvation,
            trust = trust, freePts = freePoints, freeUsed = usedFreePoints,
            skills = skills, bound = bound
        };
    }
}
}
