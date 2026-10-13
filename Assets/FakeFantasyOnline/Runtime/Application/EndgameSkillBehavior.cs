using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{
/// <summary>Server skill identity and behavior. Independent of optional local presentation resources.</summary>
public sealed class EndgameSkillContractView
{
    public string Name { get; }
    public int Icon { get; }
    public int Profession { get; }
    public string Kind { get; }
    public int UnlockLevel { get; }
    public IReadOnlyList<int> PrerequisiteSkillIDs { get; }
    public EndgameSkillBehaviorView Current { get; }
    public EndgameSkillBehaviorView Next { get; }

    internal EndgameSkillContractView(IReadOnlyDictionary<string, object> d)
    {
        EndgameSkillContractData.Fields(d, "name", "icon", "profession", "kind", "unlockLevel", "prerequisiteSkillIds", "current", "next");
        Name = EndgameData.String(d, "name", 128);
        if (string.IsNullOrWhiteSpace(Name)) throw new FormatException("Invalid skill name.");
        Icon = EndgameSkillContractData.Nonnegative(d, "icon");
        Profession = EndgameSkillContractData.Nonnegative(d, "profession");
        if (Profession > 5) throw new FormatException("Invalid skill profession.");
        Kind = EndgameSkillContractData.Enum(d, "kind", "single", "area", "passive", "special");
        UnlockLevel = EndgameSkillContractData.Nonnegative(d, "unlockLevel");
        if (d["prerequisiteSkillIds"] == null) throw new FormatException("Missing skill prerequisites.");
        PrerequisiteSkillIDs = EndgameData.List(d, "prerequisiteSkillIds", 256, v => EndgameData.Integer(v, "prerequisiteSkillIds"));
        var ids = new HashSet<int>();
        foreach (int id in PrerequisiteSkillIDs)
            if (id <= 0 || !ids.Add(id)) throw new FormatException("Invalid skill prerequisite.");
        Current = EndgameData.Optional(d, "current", v => new EndgameSkillBehaviorView(EndgameData.Object(v)));
        Next = EndgameData.Optional(d, "next", v => new EndgameSkillBehaviorView(EndgameData.Object(v)));
    }
}

/// <summary>Resolved server behavior for one effective rank. Usable describes a closed active effect,
/// not live MP, cooldown, control or target admission. Zero cast distance imposes no extra bound.</summary>
public sealed class EndgameSkillBehaviorView
{
    public int MPCost { get; }
    public int PrepareMilliseconds { get; }
    public int CooldownMilliseconds { get; }
    public int CastDistance { get; }
    public bool Usable { get; }
    public bool RequiresInvisible { get; }
    public int RequiredWeaponType { get; }
    public string InputMode { get; }
    public bool TargetSelf { get; }
    public bool TargetEnemy { get; }
    public bool TargetTeam { get; }
    public bool AllowsDeadTarget { get; }
    public bool DeadTargetOnly { get; }
    public EndgameSkillAreaView Area { get; }
    public bool GroundEffect { get; }
    public bool PointRequired { get; }
    public bool SelfFallback { get; }
    public EndgameSkillVariantView Variant { get; }

    internal EndgameSkillBehaviorView(IReadOnlyDictionary<string, object> d)
    {
        EndgameSkillContractData.FieldsWithOptional(d, "variant", "mpCost", "prepareMs", "cooldownMs", "castDistance", "usable", "requiresInvisible", "requiredWeaponType", "inputMode", "targetSelf", "targetEnemy", "targetTeam", "allowsDeadTarget", "deadTargetOnly", "area", "groundEffect", "pointRequired", "selfFallback");
        MPCost = EndgameSkillContractData.Nonnegative(d, "mpCost");
        PrepareMilliseconds = EndgameSkillContractData.Nonnegative(d, "prepareMs");
        CooldownMilliseconds = EndgameSkillContractData.Nonnegative(d, "cooldownMs");
        CastDistance = EndgameSkillContractData.Nonnegative(d, "castDistance");
        Usable = EndgameData.Bool(d, "usable");
        RequiresInvisible = EndgameData.Bool(d, "requiresInvisible");
        RequiredWeaponType = EndgameSkillContractData.Nonnegative(d, "requiredWeaponType");
        InputMode = EndgameSkillContractData.Enum(d, "inputMode", "none", "self", "entity", "point", "direction", "trapEntity");
        TargetSelf = EndgameData.Bool(d, "targetSelf");
        TargetEnemy = EndgameData.Bool(d, "targetEnemy");
        TargetTeam = EndgameData.Bool(d, "targetTeam");
        AllowsDeadTarget = EndgameData.Bool(d, "allowsDeadTarget");
        DeadTargetOnly = EndgameData.Bool(d, "deadTargetOnly");
        Area = new EndgameSkillAreaView(EndgameData.Object(d["area"]));
        GroundEffect = EndgameData.Bool(d, "groundEffect");
        PointRequired = EndgameData.Bool(d, "pointRequired");
        SelfFallback = EndgameData.Bool(d, "selfFallback");
        Variant = EndgameData.Optional(d, "variant", v => new EndgameSkillVariantView(EndgameData.Object(v)));
        if (DeadTargetOnly && !AllowsDeadTarget) throw new FormatException("Dead-only skill must allow dead targets.");
    }
}

/// <summary>Equipment form metadata; execution remains entirely server-owned.</summary>
public sealed class EndgameSkillVariantView
{
    public string ID { get; }
    public string Name { get; }
    public string SourceUID { get; }
    public int Item { get; }
    public string Description { get; }
    public int MaxTargets { get; }

    internal EndgameSkillVariantView(IReadOnlyDictionary<string, object> d)
    {
        EndgameSkillContractData.Fields(d, "id", "name", "sourceUID", "item", "description", "maxTargets");
        ID = EndgameData.String(d, "id", 64);
        Name = EndgameData.String(d, "name", 128);
        SourceUID = EndgameData.ID(d, "sourceUID");
        Item = EndgameData.Int(d, "item");
        Description = EndgameData.String(d, "description", 2048);
        MaxTargets = EndgameData.Int(d, "maxTargets");
        if (string.IsNullOrWhiteSpace(ID) || string.IsNullOrWhiteSpace(Name) ||
            string.IsNullOrWhiteSpace(Description) || SourceUID.Length == 0 || SourceUID == "0" || Item <= 0 || MaxTargets <= 0)
            throw new FormatException("Invalid equipment skill variant.");
    }
}

public sealed class EndgameSkillAreaView
{
    public string Shape { get; }
    public string Center { get; }
    public int Radius { get; }
    public int Length { get; }
    public int Width { get; }
    public int Angle { get; }

    internal EndgameSkillAreaView(IReadOnlyDictionary<string, object> d)
    {
        EndgameSkillContractData.Fields(d, "shape", "center", "radius", "length", "width", "angle");
        Shape = EndgameSkillContractData.Enum(d, "shape", "none", "rectangle", "circle", "cone");
        Center = EndgameSkillContractData.Enum(d, "center", "none", "caster", "target");
        Radius = EndgameSkillContractData.Nonnegative(d, "radius");
        Length = EndgameSkillContractData.Nonnegative(d, "length");
        Width = EndgameSkillContractData.Nonnegative(d, "width");
        Angle = EndgameSkillContractData.Nonnegative(d, "angle");
    }
}

internal static class EndgameSkillContractData
{
    // An omitted legacy contract is valid. A present versioned contract is exact and complete;
    // missing numeric/boolean members must not silently become authoritative zero/false values.
    public static void Fields(IReadOnlyDictionary<string, object> d, params string[] fields)
    {
        EndgameData.Keys(d, fields);
        foreach (string field in fields)
            if (!d.ContainsKey(field)) throw new FormatException("Missing skill behavior field: " + field);
    }
    public static void FieldsWithOptional(IReadOnlyDictionary<string, object> d, string optional, params string[] required)
    {
        var allowed = new string[required.Length + 1];
        Array.Copy(required, allowed, required.Length); allowed[required.Length] = optional;
        EndgameData.Keys(d, allowed);
        foreach (string field in required)
            if (!d.ContainsKey(field)) throw new FormatException("Missing skill behavior field: " + field);
    }
    public static int Nonnegative(IReadOnlyDictionary<string, object> d, string field)
    {
        int value = EndgameData.Int(d, field);
        if (value < 0) throw new FormatException("Negative skill behavior field: " + field);
        return value;
    }
    public static string Enum(IReadOnlyDictionary<string, object> d, string field, params string[] values)
    {
        string value = EndgameData.String(d, field, 32);
        if (Array.IndexOf(values, value) < 0) throw new FormatException("Invalid skill behavior enum: " + field);
        return value;
    }
}
}
