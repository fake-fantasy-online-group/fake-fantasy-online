using System;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static partial class CharData
{
    // Official CharData +0x19c,+0x1a0,+0x1b0,+0x1b4,+0x1b8,+0x1bc,+0x1a4,+0x1a8,+0x1ac.
    // petLevel (+0x160) already belongs to CharData.PetSnapshotTail.cs.
    internal static int petExp;
    internal static int petExpUp;
    internal static int petCurHp;
    internal static int petMaxHp;
    internal static int petCurSp;
    internal static int petMaxSp;
    internal static int petPoints;

    internal static WorldPetVitalsState ReadPetVitalsForIndependentRuntime()
    {
        return new WorldPetVitalsState(petLevel, petExp, petExpUp, petCurHp, petMaxHp,
            petCurSp, petMaxSp, petStarve, petTrust, petPoints);
    }

    internal static void ReceivePetVitalForIndependentRuntime(WorldPetVital field, int value)
    {
        switch (field)
        {
            case WorldPetVital.Level: petLevel = value; break;
            case WorldPetVital.Experience: petExp = value; break;
            case WorldPetVital.ExperienceToNext: petExpUp = value; break;
            case WorldPetVital.HitPoints: petCurHp = value; break;
            case WorldPetVital.MaximumHitPoints: petMaxHp = value; break;
            case WorldPetVital.MagicPoints: petCurSp = value; break;
            case WorldPetVital.MaximumMagicPoints: petMaxSp = value; break;
            case WorldPetVital.Starve: petStarve = value; break;
            case WorldPetVital.Trust: petTrust = value; break;
            case WorldPetVital.FreePoints: petPoints = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(field));
        }
    }
}
}
