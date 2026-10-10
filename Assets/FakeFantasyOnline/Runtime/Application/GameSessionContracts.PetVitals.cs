namespace FakeFantasyOnline.Application
{
public sealed partial class WorldPlayerPetStatusState
{
    /// <summary>Overlay only received vitals; preserve the exact optional-tail receipt identity.</summary>
    public WorldPlayerPetStatusState WithPetVitals(WorldPetVitalsState vitals)
    {
        if (vitals == null) return this;
        WorldPlayerPetDetailState detail = Detail;
        var nextDetail = new WorldPlayerPetDetailState(
            detail.Prename, detail.Gender, detail.Habit,
            vitals[WorldPetVital.Experience] ?? detail.Experience,
            vitals[WorldPetVital.ExperienceToNext] ?? detail.ExperienceToNext,
            vitals[WorldPetVital.Starve] ?? detail.Starve,
            vitals[WorldPetVital.Trust] ?? detail.Trust,
            vitals[WorldPetVital.FreePoints] ?? detail.FreePoints,
            detail.Strength, detail.Vitality, detail.Agility, detail.Intelligence,
            detail.Dexterity, detail.Spirit, detail.MinimumAttack, detail.MaximumAttack,
            detail.Defense, detail.MagicAttack, detail.MagicDefense, detail.Hit,
            detail.ActiveSkillId, detail.ActiveSlot, detail.Arousal,
            detail.PpAi, detail.PpSheng, detail.Sanctified);
        var result = new WorldPlayerPetStatusState(Model, DisplayName,
            vitals[WorldPetVital.Level] ?? Level,
            vitals[WorldPetVital.HitPoints] ?? HitPoints,
            vitals[WorldPetVital.MaximumHitPoints] ?? MaximumHitPoints,
            vitals[WorldPetVital.MagicPoints] ?? MagicPoints,
            vitals[WorldPetVital.MaximumMagicPoints] ?? MaximumMagicPoints,
            Bound, Show, ShowKnown, Ridable, Tianmo, ShowTianmo, Skills, nextDetail, Slots);
        result.OptionalTail = OptionalTail;
        return result;
    }
}

public sealed partial class WorldPlayerStatusState
{
    // Vitals do not assert that a complete pet snapshot was received.
    public WorldPlayerStatusState WithPetVitals(WorldPetVitalsState vitals)
    {
        if (vitals == null) return this;
        return new WorldPlayerStatusState(Attributes, AttributeTotals, Pet.WithPetVitals(vitals),
            HasAttributeSnapshot, HasPetSnapshot, Level, Experience, ExperienceToNext,
            HitPoints, MaximumHitPoints, MagicPoints, MaximumMagicPoints,
            Nianli, MaximumNianli, Naili, MaximumNaili);
    }
}
}
