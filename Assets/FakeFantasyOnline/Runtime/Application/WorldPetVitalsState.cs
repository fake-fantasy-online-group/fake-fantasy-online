using System;

namespace FakeFantasyOnline.Application
{
// The ordinals follow the ten stores of Dispatch 0x806f, not the 0x800b wire layout.
public enum WorldPetVital
{
    Level,
    Experience,
    ExperienceToNext,
    HitPoints,
    MaximumHitPoints,
    MagicPoints,
    MaximumMagicPoints,
    Starve,
    Trust,
    FreePoints,
}

/// <summary>
/// Immutable projection of reached pet scalar stores. Null means this adapter has not received
/// that field, so binding an optional owner must leave its existing native value untouched.
/// Once bound, the real CharData roots are read directly and no shadow copy is retained.
/// </summary>
public sealed class WorldPetVitalsState
{
    private readonly int?[] values;

    public WorldPetVitalsState() { values = new int?[10]; }
    private WorldPetVitalsState(int?[] values) { this.values = values; }

    public WorldPetVitalsState(int level, int experience, int experienceToNext,
        int hitPoints, int maximumHitPoints, int magicPoints, int maximumMagicPoints,
        int starve, int trust, int freePoints)
    {
        values = new int?[] { level, experience, experienceToNext, hitPoints, maximumHitPoints,
            magicPoints, maximumMagicPoints, starve, trust, freePoints };
    }

    public int? this[WorldPetVital field] => values[(int)field];

    public WorldPetVitalsState WithValue(WorldPetVital field, int value)
    {
        var next = (int?[])values.Clone();
        next[(int)field] = value;
        return new WorldPetVitalsState(next);
    }

    public void ApplyTo(IWorldPetVitalsReceiveOwner owner)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        for (int i = 0; i < values.Length; i++)
            if (values[i].HasValue) owner.ReceivePetVital((WorldPetVital)i, values[i].Value);
    }


}

/// <summary>Host-thread bridge to the ten actual CharData scalars; no wire or UI owner.</summary>
public interface IWorldPetVitalsReceiveOwner
{
    WorldPetVitalsState PetVitals { get; }
    void ReceivePetVital(WorldPetVital field, int value);
}
}
