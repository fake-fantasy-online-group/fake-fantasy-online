using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Domain;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Application
{

public enum ClientSessionState
{
    Offline,
    DiscoveringAddress,
    Connecting,
    Authenticating,
    ServerSelection,
    CharacterSelection,
    CharacterCreation,
    EnteringWorld,
    InWorld,
    Failed
}

public sealed partial class ServerEndpoint
{
    public ServerEndpoint(string host, int port)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("Host cannot be empty.", nameof(host));
        }

        if (port < 1 || port > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port));
        }

        Host = host;
        Port = port;
    }

    public string Host { get; }

    public int Port { get; }
}

public sealed class LoginCredentials
{
    public LoginCredentials(string accountName, string password)
    {
        AccountName = accountName ?? throw new ArgumentNullException(nameof(accountName));
        Password = password ?? throw new ArgumentNullException(nameof(password));
    }

    public string AccountName { get; }

    public string Password { get; }

    public override string ToString()
    {
        return "LoginCredentials(AccountName=[REDACTED], Password=[REDACTED])";
    }
}

public sealed partial class ServerDirectoryEntry
{
    public ServerDirectoryEntry(
        string displayName,
        ServerEndpoint endpoint,
        string zoneName,
        bool isPlayerVersusPlayer,
        int busyLevel)
    {
        DisplayName = displayName ?? string.Empty;
        Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        ZoneName = zoneName ?? string.Empty;
        IsPlayerVersusPlayer = isPlayerVersusPlayer;
        BusyLevel = busyLevel;
    }

    public string DisplayName { get; }

    public ServerEndpoint Endpoint { get; }

    public string ZoneName { get; }

    public bool IsPlayerVersusPlayer { get; }

    public int BusyLevel { get; }
}

public interface IServerAddressDiscovery
{
    Task<Result<IReadOnlyList<ServerDirectoryEntry>>> DiscoverAsync(
        CancellationToken cancellationToken);
}

// Original NetClient.CharBrief value storage. The shared Application name is retained
// so Protocol and Presentation consume the same completed array without projection.
// A row copy copies scalar fields and retains the original strings/attrs references.
public struct ServerCharacterRecord
{
    public string name;
    public int race;
    public int gender;
    public int head;
    public int hair;
    public int level;
    public int mapId;
    public float x;
    public float y;
    public string lastLogin;
    public int apBody;
    public int apCap;
    public int apBackpack;
    public int apWeaponR;
    public int apWeaponL;
    public int apFace;
    public int atkVariant;
    public int weaponCType;
    public int atkDist;
    public int[] attrs;
    public long honor;

    // Existing host-facing names are views of the same native fields, never extra storage.
    public string ServerId => name;
    public string CharacterId => name;
    public string DisplayName => name;
    public int Race => race;
    public int Gender => gender;
    public int Head => head;
    public int Hair => hair;
    public int Level => level;
    public int MapId => mapId;
    public float X => x;
    public float Y => y;
    public string LastLogin => lastLogin;
    public int[] Attributes => attrs;
    public long Honor => honor;
}

public sealed class CharacterAppearanceRecord
{
    public CharacterAppearanceRecord(
        int body,
        int cap,
        int backpack,
        int rightWeapon,
        int leftWeapon,
        int face,
        int attackVariant,
        int weaponCategory,
        int attackDistance)
    {
        Body = body;
        Cap = cap;
        Backpack = backpack;
        RightWeapon = rightWeapon;
        LeftWeapon = leftWeapon;
        Face = face;
        AttackVariant = attackVariant;
        WeaponCategory = weaponCategory;
        AttackDistance = attackDistance;
    }

    public int Body { get; }

    public int Cap { get; }

    public int Backpack { get; }

    public int RightWeapon { get; }

    public int LeftWeapon { get; }

    public int Face { get; }

    public int AttackVariant { get; }

    public int WeaponCategory { get; }

    public int AttackDistance { get; }
}

public sealed class WorldEntryRecord
{
    public WorldEntryRecord(
        int entityId,
        string characterId,
        string displayName,
        string title,
        int race,
        int gender,
        int head,
        int hair,
        int mapId,
        float x,
        float y,
        IReadOnlyList<int> attributes,
        CharacterAppearanceRecord appearance,
        WorldPetRecord pet,
        long? persistentCharacterId,
        int? glowMode,
        bool? smartCast,
        int? glowUnlock,
        int? petViewMask,
        int level = 0)
    {
        EntityId = entityId;
        CharacterId = characterId ?? throw new ArgumentNullException(nameof(characterId));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Title = title ?? string.Empty;
        Race = race;
        Gender = gender;
        Head = head;
        Hair = hair;
        MapId = mapId;
        X = x;
        Y = y;
        Attributes = CopyAttributes(attributes);
        Appearance = appearance ?? throw new ArgumentNullException(nameof(appearance));
        Pet = pet ?? throw new ArgumentNullException(nameof(pet));
        PersistentCharacterId = persistentCharacterId;
        GlowMode = glowMode;
        SmartCast = smartCast;
        GlowUnlock = glowUnlock;
        PetViewMask = petViewMask;
        Level = level;
    }

    public int EntityId { get; }

    public string CharacterId { get; }

    public string DisplayName { get; }

    public string Title { get; }

    public int Race { get; }

    public int Gender { get; }

    public int Head { get; }

    public int Hair { get; }

    public int MapId { get; }

    public float X { get; }

    public float Y { get; }

    public IReadOnlyList<int> Attributes { get; }

    public CharacterAppearanceRecord Appearance { get; }

    public WorldPetRecord Pet { get; }

    public long? PersistentCharacterId { get; }

    public int? GlowMode { get; }

    public bool? SmartCast { get; }

    public int? GlowUnlock { get; }

    public int? PetViewMask { get; }

    /// <summary>
    /// Resolved level carried by the selected enter-world record.
    /// </summary>
    /// <remarks>
    /// The gateway initially supplies the roster fallback, then resolves this entry through
    /// <see cref="WorldPlayerStatusState.FromEntry"/> and carries that level before returning:
    /// element zero when present, otherwise the existing native scalar read through
    /// <see cref="IWorldReceiveOwner.ReadEntryRetainedScalars"/>. An absent receive owner keeps
    /// the roster fallback. Later startup/status projections do not rewrite this selected record.
    /// </remarks>
    public int Level { get; }

    public WorldEntryRecord WithHair(int head, int hair) =>
        new WorldEntryRecord(EntityId, CharacterId, DisplayName, Title, Race, Gender,
            head, hair, MapId, X, Y, Attributes, Appearance, Pet, PersistentCharacterId,
            GlowMode, SmartCast, GlowUnlock, PetViewMask, Level);

    public WorldEntryRecord WithLevel(int level)
    {
        return new WorldEntryRecord(
            EntityId,
            CharacterId,
            DisplayName,
            Title,
            Race,
            Gender,
            Head,
            Hair,
            MapId,
            X,
            Y,
            Attributes,
            Appearance,
            Pet,
            PersistentCharacterId,
            GlowMode,
            SmartCast,
            GlowUnlock,
            PetViewMask,
            level);
    }

    /// <summary>
    /// Independent-runtime DTO projection used after the recovered GameBootstrap has selected
    /// the official target map and spawn. It carries no map-selection or gameplay decision.
    /// </summary>
    public WorldEntryRecord WithMapPosition(int mapId, float x, float y)
    {
        return new WorldEntryRecord(
            EntityId,
            CharacterId,
            DisplayName,
            Title,
            Race,
            Gender,
            Head,
            Hair,
            mapId,
            x,
            y,
            Attributes,
            Appearance,
            Pet,
            PersistentCharacterId,
            GlowMode,
            SmartCast,
            GlowUnlock,
            PetViewMask,
            Level);
    }

    /// <summary>
    /// Decision-free projection used after the authoritative player-appearance edge. The wire
    /// owner supplies the complete appearance block; absent compatibility tails retain their
    /// already authoritative entry values.
    /// </summary>
    public WorldEntryRecord WithAppearance(
        CharacterAppearanceRecord appearance,
        int? glowMode,
        int? glowUnlock)
    {
        if (appearance == null)
        {
            throw new ArgumentNullException(nameof(appearance));
        }

        return new WorldEntryRecord(
            EntityId,
            CharacterId,
            DisplayName,
            Title,
            Race,
            Gender,
            Head,
            Hair,
            MapId,
            X,
            Y,
            Attributes,
            appearance,
            Pet,
            PersistentCharacterId,
            glowMode ?? GlowMode,
            SmartCast,
            glowUnlock ?? GlowUnlock,
            PetViewMask,
            Level);
    }

    private static IReadOnlyList<int> CopyAttributes(IReadOnlyList<int> source)
    {
        if (source == null || source.Count == 0)
        {
            return Array.Empty<int>();
        }

        List<int> copy = new List<int>(source.Count);
        for (int index = 0; index < source.Count; index++)
        {
            copy.Add(source[index]);
        }

        return new ReadOnlyCollection<int>(copy);
    }
}

/// <summary>
/// Complete immutable player-appearance edge after Protocol has consumed every mandatory
/// field and ordered compatibility tail.
/// </summary>
public sealed class WorldPlayerAppearanceState
{
    public WorldPlayerAppearanceState(
        int entityId,
        CharacterAppearanceRecord appearance,
        string avatarAura,
        string soulEffects,
        string titleEffect,
        int? glowMode,
        int? glowUnlock)
    {
        EntityId = entityId;
        Appearance = appearance ?? throw new ArgumentNullException(nameof(appearance));
        AvatarAura = avatarAura ?? throw new ArgumentNullException(nameof(avatarAura));
        SoulEffects = soulEffects ?? throw new ArgumentNullException(nameof(soulEffects));
        TitleEffect = titleEffect ?? throw new ArgumentNullException(nameof(titleEffect));
        GlowMode = glowMode;
        GlowUnlock = glowUnlock;
    }

    public int EntityId { get; }

    public CharacterAppearanceRecord Appearance { get; }

    public string AvatarAura { get; }

    public string SoulEffects { get; }

    public string TitleEffect { get; }

    public int? GlowMode { get; }

    public int? GlowUnlock { get; }
}

/// <summary>
/// Immutable transport projection of the official NetClient.OnTeleport callback arguments.
/// Protocol publishes this edge without choosing a map or changing world state; the recovered
/// GameBootstrap callback remains the sole presentation owner of the transition decision.
/// </summary>
public sealed class WorldTeleportState
{
    public WorldTeleportState(long sequence, int mapId, float x, float y)
    {
        Sequence = sequence;
        MapId = mapId;
        X = x;
        Y = y;
    }

    /// <summary>
    /// Independent-runtime ordering metadata; it does not exist in or alter the wire root.
    /// </summary>
    public long Sequence { get; }

    public int MapId { get; }

    public float X { get; }

    public float Y { get; }
}

public sealed class WorldPetRecord
{
    public WorldPetRecord(int model, string displayName, int level)
    {
        Model = model;
        DisplayName = displayName ?? string.Empty;
        Level = level;
    }

    public int Model { get; }

    public string DisplayName { get; }

    public int Level { get; }
}

/// <summary>
/// The two complete positional attribute arrays published by the server's attribute packet.
/// </summary>
/// <remarks>
/// Application deliberately preserves every positional value. Only positions proven against the
/// locked 1.3.4 consumer are projected onto named player-status fields; Protocol must not rename,
/// pad or reorder unknown positions.
/// </remarks>
public sealed class WorldPlayerAttributeSnapshot
{
    public WorldPlayerAttributeSnapshot(
        IReadOnlyList<int> current,
        IReadOnlyList<int> total)
    {
        Current = CopyValues(current);
        Total = CopyValues(total);
    }

    public IReadOnlyList<int> Current { get; }

    public IReadOnlyList<int> Total { get; }

    private static IReadOnlyList<int> CopyValues(IReadOnlyList<int> source)
    {
        if (source == null || source.Count == 0)
        {
            return Array.Empty<int>();
        }

        List<int> copy = new List<int>(source.Count);
        for (int index = 0; index < source.Count; index++)
        {
            copy.Add(source[index]);
        }

        return new ReadOnlyCollection<int>(copy);
    }
}

/// <summary>
/// The server-authored current-pet fields consumed by the recovered first-world HUD and pet panel.
/// </summary>
/// <remarks>
/// Protocol validates and consumes the complete pet-status packet before publishing this focused
/// projection. The optional current-pet bound bit is also exposed because the recovered inventory
/// confirmation consumes it. Complete current-pet skills are retained for GameHud.HotSlotTip and
/// the pet-panel branch; the complete pet-slot book and present compatibility patches are retained
/// for that same immutable panel projection.
/// </remarks>
public sealed partial class WorldPlayerPetSkillState
{
    public WorldPlayerPetSkillState(
        int id,
        string displayName,
        int iconId,
        string description,
        bool fight,
        bool active,
        int level,
        int maximumLevel)
    {
        if (id <= 0 || iconId < 0 || level < 0 || maximumLevel < 0)
        {
            throw new ArgumentOutOfRangeException(
                id <= 0 ? nameof(id) : iconId < 0 ? nameof(iconId) :
                level < 0 ? nameof(level) : nameof(maximumLevel));
        }

        Id = id;
        DisplayName = displayName ?? string.Empty;
        IconId = iconId;
        Description = description ?? string.Empty;
        Fight = fight;
        Active = active;
        Level = level;
        MaximumLevel = maximumLevel;
    }

    public int Id { get; }
    public string DisplayName { get; }
    public int IconId { get; }
    public string Description { get; }
    public bool Fight { get; }
    public bool Active { get; }
    public int Level { get; }
    public int MaximumLevel { get; }
}

public sealed class WorldPlayerPetBriefSkillState
{
    public WorldPlayerPetBriefSkillState(string displayName, int level)
    {
        DisplayName = displayName ?? string.Empty;
        Level = level;
    }

    public string DisplayName { get; }

    public int Level { get; }
}

/// <summary>
/// The six source-owned attributes that accept one current-pet free point.
/// Protocol maps these semantic values to the official PetCmd attribute codes.
/// </summary>
public enum WorldPetAttributeKind : byte
{
    Strength = 0,
    Vitality = 1,
    Agility = 2,
    Intelligence = 3,
    Dexterity = 4,
    Spirit = 5
}

public sealed class WorldPlayerPetSlotState
{
    public WorldPlayerPetSlotState(
        int model,
        int slot,
        string nickname,
        string prename,
        string species,
        byte gender,
        byte habit,
        byte opened,
        int level,
        int strength,
        int intelligence,
        int vitality,
        int agility,
        int dexterity,
        int spirit,
        int starve,
        int trust,
        int freePoints,
        int usedPoints,
        IReadOnlyList<WorldPlayerPetBriefSkillState> skills,
        bool bound,
        bool locked = false,
        int ppAiUsed = 0,
        int ppAiCapacity = 0,
        int ppShengUsed = 0,
        int ppShengCapacity = 0,
        int growthTier = 0)
    {
        Model = model;
        Slot = slot;
        Nickname = nickname ?? string.Empty;
        Prename = prename ?? string.Empty;
        Species = species ?? string.Empty;
        Gender = gender;
        Habit = habit;
        Opened = opened;
        Level = level;
        Strength = strength;
        Intelligence = intelligence;
        Vitality = vitality;
        Agility = agility;
        Dexterity = dexterity;
        Spirit = spirit;
        Starve = starve;
        Trust = trust;
        FreePoints = freePoints;
        UsedPoints = usedPoints;
        Skills = CopyBriefSkills(skills);
        Bound = bound;
        Locked = locked;
        PpAiUsed = ppAiUsed;
        PpAiCapacity = ppAiCapacity;
        PpShengUsed = ppShengUsed;
        PpShengCapacity = ppShengCapacity;
        GrowthTier = growthTier;
    }

    public int Model { get; }
    public int Slot { get; }
    public string Nickname { get; }
    public string Prename { get; }
    public string Species { get; }
    public byte Gender { get; }
    public byte Habit { get; }
    public byte Opened { get; }
    public int Level { get; }
    public int Strength { get; }
    public int Intelligence { get; }
    public int Vitality { get; }
    public int Agility { get; }
    public int Dexterity { get; }
    public int Spirit { get; }
    public int Starve { get; }
    public int Trust { get; }
    public int FreePoints { get; }
    public int UsedPoints { get; }
    public IReadOnlyList<WorldPlayerPetBriefSkillState> Skills { get; }
    public bool Bound { get; }
    public bool Locked { get; }
    public int PpAiUsed { get; }
    public int PpAiCapacity { get; }
    public int PpShengUsed { get; }
    public int PpShengCapacity { get; }
    public int GrowthTier { get; }

    public string DisplayName => string.IsNullOrEmpty(Nickname) ? Prename : Nickname;

    public WorldPlayerPetSlotState WithCompatibilityPatch(
        bool locked,
        int ppAiUsed,
        int ppAiCapacity,
        int ppShengUsed,
        int ppShengCapacity)
    {
        return new WorldPlayerPetSlotState(
            Model, Slot, Nickname, Prename, Species, Gender, Habit, Opened, Level,
            Strength, Intelligence, Vitality, Agility, Dexterity, Spirit,
            Starve, Trust, FreePoints, UsedPoints, Skills, Bound, locked,
            ppAiUsed, ppAiCapacity, ppShengUsed, ppShengCapacity, GrowthTier);
    }

    public WorldPlayerPetSlotState WithGrowthTier(int tier)
    {
        return new WorldPlayerPetSlotState(
            Model, Slot, Nickname, Prename, Species, Gender, Habit, Opened, Level,
            Strength, Intelligence, Vitality, Agility, Dexterity, Spirit,
            Starve, Trust, FreePoints, UsedPoints, Skills, Bound, Locked,
            PpAiUsed, PpAiCapacity, PpShengUsed, PpShengCapacity, tier);
    }

    private static IReadOnlyList<WorldPlayerPetBriefSkillState> CopyBriefSkills(
        IReadOnlyList<WorldPlayerPetBriefSkillState> source)
    {
        if (source == null || source.Count == 0)
        {
            return Array.Empty<WorldPlayerPetBriefSkillState>();
        }

        return new ReadOnlyCollection<WorldPlayerPetBriefSkillState>(
            new List<WorldPlayerPetBriefSkillState>(source));
    }
}

public sealed class WorldPlayerPetDetailState
{
    public static readonly WorldPlayerPetDetailState Empty =
        new WorldPlayerPetDetailState(
            string.Empty, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, false);

    public WorldPlayerPetDetailState(
        string prename,
        byte gender,
        byte habit,
        int experience,
        int experienceToNext,
        int starve,
        int trust,
        int freePoints,
        int strength,
        int vitality,
        int agility,
        int intelligence,
        int dexterity,
        int spirit,
        int minimumAttack,
        int maximumAttack,
        int defense,
        int magicAttack,
        int magicDefense,
        int hit,
        int activeSkillId,
        int activeSlot,
        int arousal,
        int ppAi,
        int ppSheng,
        bool sanctified)
    {
        Prename = prename ?? string.Empty;
        Gender = gender;
        Habit = habit;
        Experience = experience;
        ExperienceToNext = experienceToNext;
        Starve = starve;
        Trust = trust;
        FreePoints = freePoints;
        Strength = strength;
        Vitality = vitality;
        Agility = agility;
        Intelligence = intelligence;
        Dexterity = dexterity;
        Spirit = spirit;
        MinimumAttack = minimumAttack;
        MaximumAttack = maximumAttack;
        Defense = defense;
        MagicAttack = magicAttack;
        MagicDefense = magicDefense;
        Hit = hit;
        ActiveSkillId = activeSkillId;
        ActiveSlot = activeSlot;
        Arousal = arousal;
        PpAi = ppAi;
        PpSheng = ppSheng;
        Sanctified = sanctified;
    }

    public string Prename { get; }
    public byte Gender { get; }
    public byte Habit { get; }
    public int Experience { get; }
    public int ExperienceToNext { get; }
    public int Starve { get; }
    public int Trust { get; }
    public int FreePoints { get; }
    public int Strength { get; }
    public int Vitality { get; }
    public int Agility { get; }
    public int Intelligence { get; }
    public int Dexterity { get; }
    public int Spirit { get; }
    public int MinimumAttack { get; }
    public int MaximumAttack { get; }
    public int Defense { get; }
    public int MagicAttack { get; }
    public int MagicDefense { get; }
    public int Hit { get; }
    public int ActiveSkillId { get; }
    public int ActiveSlot { get; }
    public int Arousal { get; }
    public int PpAi { get; }
    public int PpSheng { get; }
    public bool Sanctified { get; }
}

public sealed class WorldPlayerPetTailState
{
    public WorldPlayerPetTailState(int? pickQuality, int? fightCapacity, int? lifeCapacity, int? growthTier)
    {
        PickQuality = pickQuality;
        FightCapacity = fightCapacity;
        LifeCapacity = lifeCapacity;
        GrowthTier = growthTier;
    }
    public int? PickQuality { get; }
    public int? FightCapacity { get; }
    public int? LifeCapacity { get; }
    public int? GrowthTier { get; }
}

public sealed partial class WorldPlayerPetStatusState
{
    public WorldPlayerPetStatusState(
        int model,
        string displayName,
        int level,
        int hitPoints,
        int maximumHitPoints,
        int magicPoints,
        int maximumMagicPoints,
        bool bound,
        bool show = false,
        bool showKnown = false,
        bool ridable = false,
        bool tianmo = false,
        bool showTianmo = false,
        IReadOnlyList<WorldPlayerPetSkillState> skills = null,
        WorldPlayerPetDetailState detail = null,
        IReadOnlyList<WorldPlayerPetSlotState> slots = null,
        int? pickQuality = null,
        int? fightCapacity = null,
        int? lifeCapacity = null,
        int? growthTier = null)
    {
        Model = model;
        DisplayName = displayName ?? string.Empty;
        Level = level;
        HitPoints = hitPoints;
        MaximumHitPoints = maximumHitPoints;
        MagicPoints = magicPoints;
        MaximumMagicPoints = maximumMagicPoints;
        Bound = bound;
        Show = show;
        ShowKnown = showKnown;
        Ridable = ridable;
        Tianmo = tianmo;
        ShowTianmo = showTianmo;
        Skills = CopySkills(skills);
        Detail = detail ?? WorldPlayerPetDetailState.Empty;
        Slots = CopySlots(slots);
        if (pickQuality.HasValue || fightCapacity.HasValue || lifeCapacity.HasValue || growthTier.HasValue)
            OptionalTail = new WorldPlayerPetTailState(pickQuality, fightCapacity, lifeCapacity, growthTier);
    }

    public int Model { get; }

    public string DisplayName { get; }

    public int Level { get; }

    public int HitPoints { get; }

    public int MaximumHitPoints { get; }

    public int MagicPoints { get; }

    public int MaximumMagicPoints { get; }

    /// <summary>
    /// Server-authored current-pet binding state from the optional pet-snapshot tail.
    /// </summary>
    public bool Bound { get; }

    public bool Show { get; }

    public bool ShowKnown { get; }

    public bool Ridable { get; }

    public bool Tianmo { get; }

    public bool ShowTianmo { get; }

    public IReadOnlyList<WorldPlayerPetSkillState> Skills { get; }

    public WorldPlayerPetDetailState Detail { get; }

    public IReadOnlyList<WorldPlayerPetSlotState> Slots { get; }

    // Null means no value was supplied or retained for this optional state.
    public WorldPlayerPetTailState OptionalTail { get; private set; }
    public int? PickQuality => OptionalTail?.PickQuality;
    public int? FightCapacity => OptionalTail?.FightCapacity;
    public int? LifeCapacity => OptionalTail?.LifeCapacity;
    public int? GrowthTier => OptionalTail?.GrowthTier;

    public WorldPlayerPetStatusState WithRetainedOptionalState(WorldPlayerPetStatusState previous)
    {
        if (previous == null || previous.OptionalTail == null ||
            (PickQuality.HasValue && FightCapacity.HasValue && LifeCapacity.HasValue && GrowthTier.HasValue))
            return this;
        var result = new WorldPlayerPetStatusState(
            Model, DisplayName, Level, HitPoints, MaximumHitPoints, MagicPoints, MaximumMagicPoints,
            Bound, Show, ShowKnown, Ridable, Tianmo, ShowTianmo, Skills, Detail, Slots,
            PickQuality ?? previous.PickQuality, FightCapacity ?? previous.FightCapacity,
            LifeCapacity ?? previous.LifeCapacity, GrowthTier ?? previous.GrowthTier);
        // Keep receipt identity when this packet supplied no tail; a later actual tail,
        // even with identical values, must still override local optimistic settings.
        if (OptionalTail == null) result.OptionalTail = previous.OptionalTail;
        return result;
    }

    public static WorldPlayerPetStatusState FromEntry(WorldPetRecord pet)
    {
        if (pet == null)
        {
            throw new ArgumentNullException(nameof(pet));
        }

        return new WorldPlayerPetStatusState(
            pet.Model,
            pet.DisplayName,
            pet.Level,
            0,
            0,
            0,
            0,
            false,
            pet.Model > 0,
            false,
            false,
            false,
            false);
    }

    private static IReadOnlyList<WorldPlayerPetSkillState> CopySkills(
        IReadOnlyList<WorldPlayerPetSkillState> source)
    {
        if (source == null || source.Count == 0)
        {
            return Array.Empty<WorldPlayerPetSkillState>();
        }

        List<WorldPlayerPetSkillState> copy =
            new List<WorldPlayerPetSkillState>(source.Count);
        for (int index = 0; index < source.Count; index++)
        {
            WorldPlayerPetSkillState skill = source[index];
            if (skill == null)
            {
                throw new ArgumentException(
                    "Current-pet skills must be non-null.",
                    nameof(source));
            }

            copy.Add(skill);
        }

        return new ReadOnlyCollection<WorldPlayerPetSkillState>(copy);
    }

    private static IReadOnlyList<WorldPlayerPetSlotState> CopySlots(
        IReadOnlyList<WorldPlayerPetSlotState> source)
    {
        if (source == null || source.Count == 0)
        {
            return Array.Empty<WorldPlayerPetSlotState>();
        }

        return new ReadOnlyCollection<WorldPlayerPetSlotState>(
            new List<WorldPlayerPetSlotState>(source));
    }
}

/// <summary>
/// Immutable self-status root published across the Protocol/Application boundary.
/// </summary>
/// <remarks>
/// The attribute arrays are replaced wholesale by the packet that carried them - a short array is
/// never padded to the length a scalar gate asks for. The named scalars are a separate write with
/// its own length gate, and a skipped scalar store keeps the value already held instead of
/// inventing a zero: the enter callback stores nothing at all for a null or empty array and only
/// element 0 for a length of one to six (0x06000efd, arm64 0x391cf4 / 0x391d04 / 0x391d0c-0x391d28,
/// x86_64 0x3a5095 / 0x3a50aa / 0x3a50b4-0x3a50d5), and the 0x8007 case performs none of its seven
/// stores below a length of seven (arm64 0x250c8c-0x250c98, x86_64 0x25f47c). Short compatibility
/// packets therefore preserve the previous named scalars.
/// </remarks>
public sealed partial class WorldPlayerStatusState
{
    private WorldPlayerStatusState(
        IReadOnlyList<int> attributes,
        IReadOnlyList<int> attributeTotals,
        WorldPlayerPetStatusState pet,
        bool hasAttributeSnapshot,
        bool hasPetSnapshot,
        int level,
        int experience,
        int experienceToNext,
        int hitPoints,
        int maximumHitPoints,
        int magicPoints,
        int maximumMagicPoints,
        int nianli,
        int maximumNianli,
        int naili,
        int maximumNaili)
    {
        Attributes = CopyValues(attributes);
        // The preferred array keeps null as null. Official 0x8003 never writes CharData.attrsTotal
        // (+0xb8); only 0x8007 does, and only when its second count is at least one. An absent
        // totals array is therefore a real official state - the readers then fall back to
        // CharData.attrs (+0xb0) - so it must not be turned into an empty array here.
        AttributeTotals = attributeTotals == null ? null : CopyValues(attributeTotals);
        Pet = pet ?? throw new ArgumentNullException(nameof(pet));
        HasAttributeSnapshot = hasAttributeSnapshot;
        HasPetSnapshot = hasPetSnapshot;
        Level = level;
        Experience = experience;
        ExperienceToNext = experienceToNext;
        HitPoints = hitPoints;
        MaximumHitPoints = maximumHitPoints;
        MagicPoints = magicPoints;
        MaximumMagicPoints = maximumMagicPoints;
        Nianli = nianli;
        MaximumNianli = maximumNianli;
        Naili = naili;
        MaximumNaili = maximumNaili;
    }

    public IReadOnlyList<int> Attributes { get; }

    public IReadOnlyList<int> AttributeTotals { get; }

    public WorldPlayerPetStatusState Pet { get; }

    public bool HasAttributeSnapshot { get; }

    public bool HasPetSnapshot { get; }

    public int Level { get; }

    public int Experience { get; }

    public int ExperienceToNext { get; }

    public int HitPoints { get; }

    public int MaximumHitPoints { get; }

    public int MagicPoints { get; }

    public int MaximumMagicPoints { get; }

    public int Nianli { get; }

    public int MaximumNianli { get; }

    public int Naili { get; }

    public int MaximumNaili { get; }

    /// <summary>
    /// The 0x8003 self-data root. Official case 0x8003 (arm64 0x250698 / x86_64 0x25ed8d) never
    /// stores into CharData.attrs (+0xb0) or CharData.attrsTotal (+0xb8): it only clears
    /// counterCharges (+0xc8) and counterSkillId (+0xd0), builds the NetClient.EnterInfo record and
    /// hands it to TitleFlow.DoEnterWorld (0x06000efd, arm64 0x391b34), whose own store at
    /// arm64 0x391ce8 `str x21, [x0, #0xb0]!` writes CharData.attrs from EnterInfo.attrs
    /// (by-value offset +0x30) and writes nothing to +0xb8. The preferred array is therefore
    /// absent until the first 0x8007 that carries a non-empty second array, which is exactly the
    /// state the official readers fall back from.
    /// </summary>
    public static WorldPlayerStatusState FromEntry(WorldEntryRecord entry)
    {
        return FromEntry(entry, null);
    }

    /// <summary>
    /// The same 0x8003 self-data root, carrying the seven named scalars the enter gate may skip as
    /// they stand in the official statics at gate time.
    /// </summary>
    /// <remarks>
    /// Official DoEnterWorld gates the seven stores on the enter array length: a null or empty
    /// array performs none of them (arm64 0x391cf4 <c>cbz x21</c> / 0x391d04 <c>cbz x8</c>, x86_64
    /// 0x3a5095 / 0x3a50aa), a length of one to six stores element 0 into level only and then leaves
    /// the block (arm64 0x391d0c-0x391d28, x86_64 0x3a50b4-0x3a50d5) and only seven or more
    /// elements run all seven stores (arm64 0x391dcc-0x391e4c, x86_64 0x3a55a5-0x3a562b). The array
    /// itself is still stored whole at arm64 0x391ce8 <c>str x21, [x0, #0xb0]!</c>, so a skipped
    /// store never changes the array's length and the array is not padded to seven here. The level
    /// gate is the odd one out and the two shapes must not be mixed: only a null or empty array
    /// skips the level store, while a length of one to six still writes element 0 into it, so
    /// "short" alone never means "skip" for level. A skipped store keeps the content the official
    /// statics hold at that moment, read through
    /// <see cref="IWorldReceiveOwner.ReadEntryRetainedScalars"/> rather than copied from the
    /// previously published root: <c>CharData.level</c> (+0x20) for level and <c>CharData.exp</c>
    /// (+0x68), <c>expToNext</c> (+0x6c), <c>hp</c> (+0x58), <c>maxHp</c> (+0x5c), <c>mp</c>
    /// (+0x60) and <c>maxMp</c> (+0x64) for the other six. The official skip retains nothing more
    /// than what is already in the static, and that content has the lifetime of the static, not of
    /// a connection. WBB-076 showed why the two lifetimes are not interchangeable -
    /// <c>PrivateServerGateway.DisposeConnectionCore</c> discards the published root, while the
    /// frozen teardown bodies <c>GameBootstrap.TeardownWorldUi</c> (0x060003f3), its local purge
    /// (0x060003f7), <c>NetClient.Close</c> (0x060006be) and <c>NetClient.OnDestroy</c>
    /// (0x060006bf) write none of these seven offsets on either architecture, even though the very
    /// same body zeroes the mount statics it does own
    /// (CharData +0x231/+0x24c/+0x260/+0x264/+0x268). Level is read from the same static rather
    /// than the initial roster fallback in <see cref="WorldEntryRecord.Level"/>: a startup
    /// 0x8007 with at least seven elements may already have overwritten <c>CharData.level</c> at
    /// arm64 0x250c8c / x86_64 0x25f47c. The gateway carries this resolved level into the selected
    /// entry record immediately after this call, before any separate startup/status snapshot
    /// merge, and the coordinator preserves it when the enter array is empty. A null
    /// <paramref name="retained"/> means the read could not be performed; the six then keep the
    /// zero those statics start from - CharData..cctor (0x06000f2d) writes <c>level</c> (+0x20,
    /// the value 1 in both exports) and none of the six, so zero is their official initial value -
    /// and level keeps the roster carrier, which is the value the mirror wrote and the value the
    /// record already carried. No other value is invented here. The four ring scalars have no
    /// recovered enter gate and stay at zero.
    /// </remarks>
    public static WorldPlayerStatusState FromEntry(
        WorldEntryRecord entry,
        WorldEntryRetainedScalars retained)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        IReadOnlyList<int> attributes = entry.Attributes;
        bool hasLevelStore = attributes != null && attributes.Count >= 1;
        bool hasSevenStores = attributes != null && attributes.Count >= 7;
        return new WorldPlayerStatusState(
            entry.Attributes,
            null,
            WorldPlayerPetStatusState.FromEntry(entry.Pet),
            false,
            false,
            hasLevelStore ? attributes[0] : (retained?.Level ?? entry.Level),
            hasSevenStores ? attributes[1] : (retained?.Experience ?? 0),
            hasSevenStores ? attributes[2] : (retained?.ExperienceToNext ?? 0),
            hasSevenStores ? attributes[3] : (retained?.HitPoints ?? 0),
            hasSevenStores ? attributes[4] : (retained?.MaximumHitPoints ?? 0),
            hasSevenStores ? attributes[5] : (retained?.MagicPoints ?? 0),
            hasSevenStores ? attributes[6] : (retained?.MaximumMagicPoints ?? 0),
            0,
            0,
            0,
            0);
    }

    public WorldPlayerStatusState WithAttributeSnapshot(
        WorldPlayerAttributeSnapshot snapshot)
    {
        if (snapshot == null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        IReadOnlyList<int> current = snapshot.Current;
        bool hasCoreScalars = current.Count >= 7;
        bool hasRingScalars = current.Count >= 29;
        // Official case 0x8007 stores the second array into CharData.attrsTotal (+0xb8) only after
        // its own loop has run at least once: arm64 0x250bec `cmp w19, #0x1` / 0x250bf0
        // `b.lt 0x250c5c` and x86_64 0x25f3cf `testl %r15d, %r15d` / 0x25f3d2 `jle 0x25f441`
        // both jump past `str x21, [x0, #0xb8]!` (arm64 0x250c50) / `movq %rbx, 0xb8(%rdi)`
        // (x86_64 0x25f42b) when the second count is zero, while the first array is stored
        // unconditionally (arm64 0x250c7c / x86_64 0x25f467). A zero-length second array
        // therefore leaves the previous totals array in place instead of replacing it.
        IReadOnlyList<int> totals = snapshot.Total != null && snapshot.Total.Count > 0
            ? snapshot.Total
            : AttributeTotals;
        return new WorldPlayerStatusState(
            current,
            totals,
            Pet,
            true,
            HasPetSnapshot,
            hasCoreScalars ? current[0] : Level,
            hasCoreScalars ? current[1] : Experience,
            hasCoreScalars ? current[2] : ExperienceToNext,
            hasCoreScalars ? current[3] : HitPoints,
            hasCoreScalars ? current[4] : MaximumHitPoints,
            hasCoreScalars ? current[5] : MagicPoints,
            hasCoreScalars ? current[6] : MaximumMagicPoints,
            hasRingScalars ? current[25] : Nianli,
            hasRingScalars ? current[26] : MaximumNianli,
            hasRingScalars ? current[27] : Naili,
            hasRingScalars ? current[28] : MaximumNaili);
    }

    public WorldPlayerStatusState WithPetSnapshot(WorldPlayerPetStatusState pet)
    {
        if (pet == null)
        {
            throw new ArgumentNullException(nameof(pet));
        }

        return new WorldPlayerStatusState(
            Attributes,
            AttributeTotals,
            pet,
            HasAttributeSnapshot,
            true,
            Level,
            Experience,
            ExperienceToNext,
            HitPoints,
            MaximumHitPoints,
            MagicPoints,
            MaximumMagicPoints,
            Nianli,
            MaximumNianli,
            Naili,
            MaximumNaili);
    }

    private static IReadOnlyList<int> CopyValues(IReadOnlyList<int> source)
    {
        if (source == null || source.Count == 0)
        {
            return Array.Empty<int>();
        }

        List<int> copy = new List<int>(source.Count);
        for (int index = 0; index < source.Count; index++)
        {
            copy.Add(source[index]);
        }

        return new ReadOnlyCollection<int>(copy);
    }
}

/// <summary>
/// The seven named entry scalars as they stand in the official statics, read at the moment the
/// enter gate decides which stores to skip.
/// </summary>
/// <remarks>
/// The value the official skip leaves behind is not a value the callback computed - it is the
/// content already present in <c>CharData.level</c> (+0x20), <c>CharData.exp</c> (+0x68),
/// <c>CharData.expToNext</c> (+0x6c), <c>CharData.hp</c> (+0x58), <c>CharData.maxHp</c> (+0x5c),
/// <c>CharData.mp</c> (+0x60) and <c>CharData.maxMp</c> (+0x64). Those statics are owned by
/// Presentation, so Application sees them through
/// <see cref="IWorldReceiveOwner.ReadEntryRetainedScalars"/> and never caches a copy of its own:
/// one read at one gate decision, no second authority for these numbers and nothing carried
/// across a connection disposal on its own. Level is part of the same read even though its gate
/// differs - a length of one to six still stores element 0 into it - because the value a skipped
/// store keeps is the static's content either way.
/// </remarks>
public sealed class WorldEntryRetainedScalars
{
    public WorldEntryRetainedScalars(
        int level,
        int experience,
        int experienceToNext,
        int hitPoints,
        int maximumHitPoints,
        int magicPoints,
        int maximumMagicPoints)
    {
        Level = level;
        Experience = experience;
        ExperienceToNext = experienceToNext;
        HitPoints = hitPoints;
        MaximumHitPoints = maximumHitPoints;
        MagicPoints = magicPoints;
        MaximumMagicPoints = maximumMagicPoints;
    }

    public int Level { get; }

    public int Experience { get; }

    public int ExperienceToNext { get; }

    public int HitPoints { get; }

    public int MaximumHitPoints { get; }

    public int MagicPoints { get; }

    public int MaximumMagicPoints { get; }
}

/// <summary>
/// Exact option byte accepted by the official ordinary revive-choice request.
/// </summary>
public enum WorldReviveOption : byte
{
    RespawnCharacter = 0,
    SubstituteDoll = 1,
    SoulReturnDoll = 2,
}

/// <summary>
/// Latest immutable server-authored life edge for the local world entity.
/// </summary>
/// <remarks>
/// This state is distinct from the attribute snapshot: hit points remain owned by the exact
/// attribute packet, while the life packet owns PlayDie/Revive side effects and revive flags.
/// </remarks>
public sealed class WorldLocalPlayerLifeState
{
    public WorldLocalPlayerLifeState(
        int entityId,
        bool alive,
        int reviveFlags,
        long revision,
        bool nativeActionsHandled = false)
    {
        if (entityId == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(entityId));
        }

        if (reviveFlags < 0 || reviveFlags > byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(reviveFlags));
        }

        if (revision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(revision));
        }

        EntityId = entityId;
        Alive = alive;
        ReviveFlags = reviveFlags;
        Revision = revision;
        NativeActionsHandled = nativeActionsHandled;
    }

    public int EntityId { get; }

    public bool Alive { get; }

    public int ReviveFlags { get; }

    public long Revision { get; }

    // The native receive owner handled this receipt, possibly by storing pending state.
    // Snapshot consumers must not invoke the native actions again.
    public bool NativeActionsHandled { get; }

    /// <summary>
    /// Applies the official ReviveDlg flag map without changing the authoritative life state.
    /// </summary>
    public bool Allows(WorldReviveOption option)
    {
        switch (option)
        {
            case WorldReviveOption.RespawnCharacter:
                return true;
            case WorldReviveOption.SubstituteDoll:
                return (ReviveFlags & 1) != 0;
            case WorldReviveOption.SoulReturnDoll:
                return (ReviveFlags & 2) != 0;
            default:
                return false;
        }
    }
}

/// <summary>
/// The complete server-authored self PK state published by Protocol.
/// </summary>
public sealed class WorldSelfPkState
{
    public WorldSelfPkState(
        byte mode,
        long karma,
        byte state,
        byte mapSafe,
        int protectionLevel,
        byte mapFree = 0)
    {
        Mode = mode;
        Karma = karma;
        State = state;
        MapSafe = mapSafe;
        ProtectionLevel = protectionLevel;
        MapFree = mapFree;
    }

    public byte Mode { get; }

    public long Karma { get; }

    public byte State { get; }

    public byte MapSafe { get; }

    public int ProtectionLevel { get; }

    public byte MapFree { get; }
}

/// <summary>
/// The exact server-authored LocalFaction root consumed by the official CombatWorld.
/// </summary>
public sealed class WorldLocalFactionState
{
    public WorldLocalFactionState(int faction)
    {
        Faction = faction;
    }

    public int Faction { get; }
}

/// <summary>
/// One complete server-authored static NPC spawn snapshot.
/// </summary>
/// <remarks>
/// These are the exact fields consumed by the locked 1.3.4 NpcEntry handler. The sprite remains
/// a logical resource name because choosing or decoding its art belongs to Presentation. Commands
/// must still enter through Application; this snapshot grants no direct network authority.
/// No field is validated here. Official 1.5.8 NetClient.Dispatch case 0x8046 (arm64 0x252ca0 /
/// x86_64 0x261e3e) builds the NpcEntry from its eleven reads and hands it to
/// GameBootstrap.OnNetNpcSpawn(int, NpcEntry) (arm64 b 0x423a68 / x86_64 jmp 0x438d50) without an
/// entity-id test, a sprite whitelist, a trim or any other gate: the entry is stored into the
/// authoritative dictionary first, and only afterwards GameBootstrap.SpawnNetNpc
/// (0x060003da, arm64 0x422c9c) may fail to produce a GameObject. A zero id and an empty or
/// whitespace sprite are therefore legal rows: they stay in the snapshot verbatim and simply own
/// no actor. Rejecting them here was a product-only rule that turned those two packet shapes into
/// whole-packet failures and dropped rows the official client keeps.
/// </remarks>
public sealed class WorldNpcState
{
    public WorldNpcState(
        int entityId,
        int x,
        int y,
        string sprite,
        string displayName,
        string label,
        string portrait,
        string script,
        int sellList,
        int transportList,
        int direction)
    {
        EntityId = entityId;
        X = x;
        Y = y;
        Sprite = sprite;
        DisplayName = displayName ?? string.Empty;
        Label = label ?? string.Empty;
        Portrait = portrait ?? string.Empty;
        Script = script ?? string.Empty;
        SellList = sellList;
        TransportList = transportList;
        Direction = direction;
    }

    public int EntityId { get; }

    public int X { get; }

    public int Y { get; }

    public string Sprite { get; }

    public string DisplayName { get; }

    public string Label { get; }

    public string Portrait { get; }

    public string Script { get; }

    public int SellList { get; }

    public int TransportList { get; }

    /// <summary>
    /// Raw wire octant. Presentation applies the recovered PlayerController.OctToStored mapping.
    /// </summary>
    public int Direction { get; }
}

/// <summary>
/// One exact server-authored row consumed by the recovered NpcChatter.SetServerChatter boundary.
/// </summary>
public sealed class WorldNpcChatterLineState
{
    public WorldNpcChatterLineState(string npcName, string role, string text)
    {
        NpcName = npcName ?? string.Empty;
        Role = role ?? string.Empty;
        Text = text ?? string.Empty;
    }

    public string NpcName { get; }

    public string Role { get; }

    public string Text { get; }
}

/// <summary>
/// Complete NPC-chatter replacement published by Protocol.
/// </summary>
public sealed class WorldNpcChatterState
{
    public WorldNpcChatterState(
        bool enabled,
        int minimumGapSeconds,
        int maximumGapSeconds,
        int showSeconds,
        IReadOnlyList<WorldNpcChatterLineState> lines)
    {
        if (lines == null)
        {
            throw new ArgumentNullException(nameof(lines));
        }

        Enabled = enabled;
        MinimumGapSeconds = minimumGapSeconds;
        MaximumGapSeconds = maximumGapSeconds;
        ShowSeconds = showSeconds;
        Lines = new ReadOnlyCollection<WorldNpcChatterLineState>(
            new List<WorldNpcChatterLineState>(lines));
    }

    public bool Enabled { get; }

    public int MinimumGapSeconds { get; }

    public int MaximumGapSeconds { get; }

    public int ShowSeconds { get; }

    public IReadOnlyList<WorldNpcChatterLineState> Lines { get; }
}

/// <summary>
/// One server-authored task row offered by the currently opened NPC.
/// </summary>
/// <remarks>
/// State is the exact wire byte consumed by the official TaskClient. Presentation translates it
/// into receive/in-progress/complete/locked text and must not infer a different task transition.
/// <para>
/// No field is validated. Official 1.5.8 NetClient.Dispatch case 0x8028 reads the row id with a
/// single <c>Buf.I32</c> and hands it straight to <c>TaskClient+Offer.id</c>: the frozen dual
/// architecture bodies carry no positive test, no duplicate test, no sort and no default id, so a
/// zero or negative id is a legal row that the official client stores, renders with the same
/// state prefix and would send back verbatim from <c>TaskClient.Accept/Complete</c>
/// (0x06000e6d/0x06000e6f). Rejecting it here was a product-only rule that turned one legal row
/// into a whole-packet failure inside the decoder's try, so the server's other rows were dropped
/// as well.
/// </para>
/// </remarks>
public sealed class WorldNpcTaskOfferState
{
    public WorldNpcTaskOfferState(
        int taskId,
        byte state,
        string title,
        string description,
        string award)
    {
        TaskId = taskId;
        State = state;
        Title = title ?? string.Empty;
        Description = description ?? string.Empty;
        Award = award ?? string.Empty;
    }

    public int TaskId { get; }

    public byte State { get; }

    public string Title { get; }

    public string Description { get; }

    public string Award { get; }
}

/// <summary>
/// Complete replacement list published by the private server for one NPC name.
/// </summary>
/// <remarks>
/// The name is carried verbatim. Official 1.5.8 case 0x8028 reads it with one <c>Buf.Str</c> and
/// tail-calls <c>TaskClient.SetOffers</c> (0x06000e5e), whose first step stores that reference
/// into <c>TaskClient.OfferNpc</c> (static +0x10) with a write barrier and nothing else: there is
/// no <c>IsNullOrEmpty</c>, no trim and no fallback name in either frozen body. <c>Buf.Str</c>
/// itself can only yield the empty string, never null, so an empty name is a legal replacement
/// that simply equals no real NPC name except an NPC whose own <c>DisplayName</c> is empty;
/// <c>DialogUI.BuildMenu</c> (0x06000215) compares the two with
/// <c>System.String.op_Equality</c> and shows the rows in exactly that case. Rejecting the empty
/// name here was a product-only rule that made a legal packet fail as a whole.
/// </remarks>
public sealed class WorldNpcTaskOffersState
{
    public WorldNpcTaskOffersState(
        string npcName,
        IReadOnlyList<WorldNpcTaskOfferState> offers)
    {
        if (offers == null)
        {
            throw new ArgumentNullException(nameof(offers));
        }

        NpcName = npcName;
        Offers = new ReadOnlyCollection<WorldNpcTaskOfferState>(
            new List<WorldNpcTaskOfferState>(offers));
    }

    public string NpcName { get; }

    public IReadOnlyList<WorldNpcTaskOfferState> Offers { get; }
}

/// <summary>
/// One authoritative row from the official ShopDlg item replacement.
/// </summary>
public sealed class WorldShopItemState
{
    public WorldShopItemState(int itemId, int price, string name, string description, bool blocked)
        : this(itemId, price, name, description, blocked ? (byte)1 : (byte)0)
    {
        if (itemId <= 0 || price <= 0)
            throw new ArgumentOutOfRangeException(nameof(itemId));
    }

    // Native 1.5.8 shop rows retain signed values and the raw blocked byte.
    public WorldShopItemState(int itemId, int price, string name, string description, byte blocked)
    {
        ItemId = itemId;
        Price = price;
        Name = name ?? string.Empty;
        Description = description ?? string.Empty;
        BlockedValue = blocked;
    }

    public int ItemId { get; }
    public int Price { get; }
    public string Name { get; }
    public string Description { get; }
    public byte BlockedValue { get; }
    public bool Blocked => BlockedValue != 0;
}

/// <summary>Immutable projection of the current shop-item replacement.</summary>
public sealed class WorldShopState
{
    public static readonly WorldShopState Empty = new WorldShopState(0, Array.Empty<WorldShopItemState>());

    public WorldShopState(int shopId, IReadOnlyList<WorldShopItemState> items)
        : this(shopId, items, true) { }

    private WorldShopState(int shopId, IReadOnlyList<WorldShopItemState> items, bool validateIdentity)
    {
        if (validateIdentity && shopId < 0)
            throw new ArgumentOutOfRangeException(nameof(shopId));
        ShopId = shopId;
        Items = new ReadOnlyCollection<WorldShopItemState>(
            new List<WorldShopItemState>(items ?? throw new ArgumentNullException(nameof(items))));
    }

    public static WorldShopState FromNative(int shopId, IReadOnlyList<WorldShopItemState> items) =>
        new WorldShopState(shopId, items, false);

    public int ShopId { get; }
    public IReadOnlyList<WorldShopItemState> Items { get; }
}

public sealed class WorldTransportDestinationState
{
    public WorldTransportDestinationState(string name, int mapId, int x, int y, int price)
    {
        if (string.IsNullOrEmpty(name) || mapId <= 0 || price < 0)
        {
            throw new ArgumentException("The transport destination is invalid.");
        }

        Name = name;
        MapId = mapId;
        X = x;
        Y = y;
        Price = price;
    }

    public string Name { get; }
    public int MapId { get; }
    public int X { get; }
    public int Y { get; }
    public int Price { get; }
}

public sealed class WorldTransportState
{
    public static readonly WorldTransportState Empty = new WorldTransportState(
        0,
        Array.Empty<WorldTransportDestinationState>());

    public WorldTransportState(
        int transportList,
        IReadOnlyList<WorldTransportDestinationState> destinations)
    {
        if (transportList < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(transportList));
        }

        TransportList = transportList;
        Destinations = new ReadOnlyCollection<WorldTransportDestinationState>(
            new List<WorldTransportDestinationState>(
                destinations ?? throw new ArgumentNullException(nameof(destinations))));
    }

    public int TransportList { get; }
    public IReadOnlyList<WorldTransportDestinationState> Destinations { get; }
}

public sealed class WorldWarehouseItemState
{
    public WorldWarehouseItemState(
        byte tab,
        int slot,
        int itemId,
        int count,
        string name,
        string description,
        int currentDurability,
        int maximumDurability,
        bool blocked,
        byte quality)
        : this(tab, slot, itemId, count, name, description, currentDurability,
            maximumDurability, blocked ? (byte)1 : (byte)0, quality)
    {
    }

    public WorldWarehouseItemState(
        byte tab, int slot, int itemId, int count, string name, string description,
        int currentDurability, int maximumDurability, byte blocked, byte quality)
    {
        Tab = tab;
        Slot = slot;
        ItemId = itemId;
        Count = count;
        Name = name ?? string.Empty;
        Description = description ?? string.Empty;
        CurrentDurability = currentDurability;
        MaximumDurability = maximumDurability;
        BlockedByte = blocked;
        Quality = quality;
    }

    public byte Tab { get; }
    public int Slot { get; }
    public int ItemId { get; }
    public int Count { get; }
    public string Name { get; }
    public string Description { get; }
    public int CurrentDurability { get; }
    public int MaximumDurability { get; }
    public byte BlockedByte { get; }
    public bool Blocked => BlockedByte != 0;
    public byte Quality { get; }
}

public sealed class WorldWarehouseState
{
    public static readonly WorldWarehouseState Empty = new WorldWarehouseState(
        0,
        0L,
        Array.Empty<WorldWarehouseItemState>());

    public WorldWarehouseState(
        byte pageCount,
        long money,
        IReadOnlyList<WorldWarehouseItemState> items)
    {
        PageCount = pageCount;
        Money = money;
        Items = new ReadOnlyCollection<WorldWarehouseItemState>(
            new List<WorldWarehouseItemState>(items ?? throw new ArgumentNullException(nameof(items))));
    }

    public byte PageCount { get; }
    public long Money { get; }
    public IReadOnlyList<WorldWarehouseItemState> Items { get; }
}

/// <summary>
/// One server-authored active-task row from the official TaskClient state replacement.
/// </summary>
public sealed class WorldActiveTaskState
{
    public WorldActiveTaskState(
        int taskId,
        int remainingSeconds,
        int step,
        int total,
        string text,
        string progress,
        bool ready,
        string turnInNpc, byte? readyByte = null)
    {
        TaskId = taskId;
        RemainingSeconds = remainingSeconds;
        Step = step;
        Total = total;
        Text = text ?? string.Empty;
        Progress = progress ?? string.Empty;
        ReadyByte = readyByte ?? (ready ? (byte)1 : (byte)0);
        Ready = ReadyByte != 0;
        TurnInNpc = turnInNpc ?? string.Empty;
    }

    public int TaskId { get; }

    public int RemainingSeconds { get; }

    public int Step { get; }

    public int Total { get; }

    public string Text { get; }

    public string Progress { get; }

    public bool Ready { get; }
    public byte ReadyByte { get; }

    public string TurnInNpc { get; }

    /// <summary>
    /// Exact source-recovered TaskClient.IsTerminal rule.
    /// </summary>
    public bool IsTerminal => Total > 0 && Step >= Total - 1;
}

/// <summary>
/// Complete immutable replacement of the official TaskClient Active and Done collections.
/// </summary>
public sealed class WorldTaskLogState
{
    public static readonly WorldTaskLogState Empty = new WorldTaskLogState(
        Array.Empty<WorldActiveTaskState>(),
        Array.Empty<int>());

    public WorldTaskLogState(
        IReadOnlyList<WorldActiveTaskState> active,
        IReadOnlyList<int> done)
    {
        if (active == null)
        {
            throw new ArgumentNullException(nameof(active));
        }

        if (done == null)
        {
            throw new ArgumentNullException(nameof(done));
        }

        Active = new ReadOnlyCollection<WorldActiveTaskState>(
            new List<WorldActiveTaskState>(active));
        Done = new ReadOnlyCollection<int>(new List<int>(done));
    }

    public IReadOnlyList<WorldActiveTaskState> Active { get; }

    public IReadOnlyList<int> Done { get; }

    public WorldActiveTaskState FindActive(int taskId)
    {
        for (int index = 0; index < Active.Count; index++)
        {
            if (Active[index].TaskId == taskId)
            {
                return Active[index];
            }
        }

        return null;
    }

    public bool IsTerminal(int taskId)
    {
        WorldActiveTaskState active = FindActive(taskId);
        return active != null && active.IsTerminal;
    }
}

/// <summary>
/// Latest exact server text plus its official display-mode byte.
/// </summary>
public sealed class WorldNpcDialogTextState
{
    public WorldNpcDialogTextState(string text, byte displayMode)
    {
        Text = text ?? string.Empty;
        DisplayMode = displayMode;
    }

    public string Text { get; }

    public byte DisplayMode { get; }
}

/// <summary>
/// One observation of a completed NPC/dialog notice case, in receipt order.
/// Gameplay effects have already run; this value is never a replay command.
/// </summary>
public sealed class WorldNpcDialogTextEdge
{
    public WorldNpcDialogTextEdge(long revision, string text, byte displayMode,
        WorldInvitationCodeResult invitationCodeResult = null)
    {
        if (revision < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(revision));
        }

        Revision = revision;
        Text = text ?? string.Empty;
        DisplayMode = displayMode;
        InvitationCodeResult = invitationCodeResult;
    }

    public long Revision { get; }

    public string Text { get; }

    public byte DisplayMode { get; }

    public WorldInvitationCodeResult InvitationCodeResult { get; }
}

/// <summary>
/// One atomic immutable retained window of ordered, already-delivered notice observations.
/// </summary>
/// <remarks>
/// Version, overflow boundary and retained edges are published as one reference so consumers do
/// not reconstruct a torn tuple. Revisions through DroppedThroughRevision are no longer retained.
/// Overflow or a newly built world may skip history without dropping any gameplay effect.
/// </remarks>
public sealed class WorldNpcDialogTextEdgeWindow
{
    public static readonly WorldNpcDialogTextEdgeWindow Empty =
        new WorldNpcDialogTextEdgeWindow(
            0,
            0,
            Array.Empty<WorldNpcDialogTextEdge>());

    public WorldNpcDialogTextEdgeWindow(
        long version,
        long droppedThroughRevision,
        IReadOnlyList<WorldNpcDialogTextEdge> edges)
    {
        if (version < 0 || droppedThroughRevision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }
        if (edges == null)
        {
            throw new ArgumentNullException(nameof(edges));
        }

        List<WorldNpcDialogTextEdge> copy =
            new List<WorldNpcDialogTextEdge>(edges.Count);
        long expectedRevision = droppedThroughRevision + 1;
        for (int index = 0; index < edges.Count; index++)
        {
            WorldNpcDialogTextEdge edge = edges[index];
            if (edge == null || edge.Revision != expectedRevision)
            {
                throw new ArgumentException(
                    "Dialog-text edges must be non-null and revision-contiguous.",
                    nameof(edges));
            }

            copy.Add(edge);
            expectedRevision++;
        }

        long lastRevision = copy.Count == 0
            ? droppedThroughRevision
            : copy[copy.Count - 1].Revision;
        if (version < lastRevision ||
            version == 0 && (droppedThroughRevision != 0 || copy.Count != 0))
        {
            throw new ArgumentException(
                "The dialog-text window version cannot precede its retained revisions.",
                nameof(version));
        }

        Version = version;
        DroppedThroughRevision = droppedThroughRevision;
        Edges = new ReadOnlyCollection<WorldNpcDialogTextEdge>(copy);
        LastRevision = lastRevision;
    }

    public long Version { get; }

    public long DroppedThroughRevision { get; }

    public IReadOnlyList<WorldNpcDialogTextEdge> Edges { get; }

    public long LastRevision { get; }
}

/// <summary>
/// Entity kinds admitted by the locked 1.3.4 CombatWorld spawn consumer.
/// </summary>
public enum WorldEntityKind : byte
{
    Monster = 3,
    Pet = 4,
    Trap = 5
}

/// <summary>
/// One optional status-effect row carried on the generic entity spawn tail.
/// </summary>
public sealed class WorldEntityStatusEffect
{
    public WorldEntityStatusEffect(
        string effect,
        byte positionMode,
        int durationMilliseconds,
        long revision = 0)
    {
        if (revision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(revision));
        }

        Effect = effect ?? string.Empty;
        PositionMode = positionMode;
        DurationMilliseconds = durationMilliseconds;
        Revision = revision;
    }

    public string Effect { get; }

    public byte PositionMode { get; }

    public int DurationMilliseconds { get; }

    /// <summary>
    /// Client-local monotonic receive revision. It distinguishes an identical refresh packet
    /// from an unrelated immutable entity revision without changing the official wire fields.
    /// </summary>
    public long Revision { get; }

    public WorldEntityStatusEffect WithRevision(long revision)
    {
        return new WorldEntityStatusEffect(
            Effect,
            PositionMode,
            DurationMilliseconds,
            revision);
    }
}

/// <summary>
/// One server-authored CombatWorld.PlayEntityEffect edge.
/// </summary>
/// <remarks>
/// The id and the name are the official wire fields and are validated by the owner, not here:
/// CombatWorld.PlayEntityEffect drops an empty name itself and only admits a zero id or the self
/// id through its local arm, so a zero, negative or unnamed edge is a legal packet that resolves
/// to no effect rather than a contract error.
/// </remarks>
public sealed class WorldSkillHitEffect
{
    public WorldSkillHitEffect(int entityId, string effect, long revision = 0)
    {
        if (revision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(revision));
        }

        EntityId = entityId;
        Effect = effect;
        Revision = revision;
    }

    public int EntityId { get; }

    public string Effect { get; }

    public long Revision { get; }

    public WorldSkillHitEffect WithRevision(long revision)
    {
        return new WorldSkillHitEffect(EntityId, Effect, revision);
    }
}

/// <summary>
/// One optional status-icon row carried on the generic entity spawn tail.
/// </summary>
public sealed class WorldEntityStatusIcon
{
    public WorldEntityStatusIcon(
        int statusId,
        int iconId,
        byte beneficial,
        int remainingMilliseconds)
    {
        StatusId = statusId;
        IconId = iconId;
        Beneficial = beneficial;
        RemainingMilliseconds = remainingMilliseconds;
    }

    public int StatusId { get; }

    public int IconId { get; }

    public byte Beneficial { get; }

    public int RemainingMilliseconds { get; }
}

/// <summary>
/// One exact GameHud.TgSt row returned by the target-status query.
/// </summary>
public sealed class WorldTargetStatusRow
{
    public WorldTargetStatusRow(
        int iconId,
        bool beneficial,
        int remainingMilliseconds,
        string displayName,
        string description)
    {
        IconId = iconId;
        Beneficial = beneficial;
        RemainingMilliseconds = remainingMilliseconds;
        DisplayName = displayName ?? string.Empty;
        Description = description ?? string.Empty;
    }

    public int IconId { get; }

    public bool Beneficial { get; }

    /// <summary>
    /// A negative value is the official permanent-status sentinel.
    /// </summary>
    public int RemainingMilliseconds { get; }

    public string DisplayName { get; }

    public string Description { get; }
}

/// <summary>
/// Immutable server-authored response to one target-status query.
/// </summary>
public sealed class WorldTargetStatusState
{
    private readonly IReadOnlyList<WorldTargetStatusRow> rows;

    public WorldTargetStatusState(
        int entityId,
        IReadOnlyList<WorldTargetStatusRow> rows)
    {
        EntityId = entityId;
        List<WorldTargetStatusRow> copy = new List<WorldTargetStatusRow>();
        if (rows != null)
        {
            for (int index = 0; index < rows.Count; index++)
            {
                WorldTargetStatusRow row = rows[index];
                if (row == null)
                {
                    throw new ArgumentException(
                        "Target-status rows cannot contain null values.",
                        nameof(rows));
                }

                copy.Add(row);
            }
        }

        this.rows = new ReadOnlyCollection<WorldTargetStatusRow>(copy);
    }

    public int EntityId { get; }

    public IReadOnlyList<WorldTargetStatusRow> Rows => rows;
}

/// <summary>
/// One exact BuffView row from the authoritative local-player Buff snapshot.
/// </summary>
public sealed class WorldBuffRow
{
    public WorldBuffRow(
        int skillId,
        int iconId,
        int remainingSeconds,
        string displayName,
        string description,
        bool beneficial,
        int positionMode,
        bool invisible,
        byte immobile)
    {
        SkillId = skillId;
        IconId = iconId;
        RemainingSeconds = remainingSeconds;
        DisplayName = displayName ?? string.Empty;
        Description = description ?? string.Empty;
        Beneficial = beneficial;
        PositionMode = positionMode;
        Invisible = invisible;
        Immobile = immobile;
    }

    public int SkillId { get; }

    public int IconId { get; }

    /// <summary>
    /// A negative value is the official permanent-status sentinel.
    /// </summary>
    public int RemainingSeconds { get; }

    public string DisplayName { get; }

    public string Description { get; }

    public bool Beneficial { get; }

    /// <summary>
    /// The official consumer sign-extends the raw wire byte into BuffView.posMode.
    /// </summary>
    public int PositionMode { get; }

    public bool Invisible { get; }

    /// <summary>
    /// The official consumer preserves the maximum raw immobile byte rather than normalizing it.
    /// </summary>
    public byte Immobile { get; }
}

/// <summary>
/// Immutable complete local-player Buff/Debuff replacement.
/// </summary>
public sealed class WorldBuffState
{
    private readonly IReadOnlyList<WorldBuffRow> rows;

    public WorldBuffState(IReadOnlyList<WorldBuffRow> rows, bool doubleExperience)
    {
        List<WorldBuffRow> copy = new List<WorldBuffRow>();
        if (rows != null)
        {
            for (int index = 0; index < rows.Count; index++)
            {
                WorldBuffRow row = rows[index];
                if (row == null)
                {
                    throw new ArgumentException(
                        "Buff rows cannot contain null values.",
                        nameof(rows));
                }

                copy.Add(row);
            }
        }

        this.rows = new ReadOnlyCollection<WorldBuffRow>(copy);
        DoubleExperience = doubleExperience;
    }

    public static WorldBuffState Empty { get; } =
        new WorldBuffState(Array.Empty<WorldBuffRow>(), false);

    public IReadOnlyList<WorldBuffRow> Rows => rows;

    public bool DoubleExperience { get; }

    /// <summary>
    /// Exact CharData.invisible projection: the official Buff snapshot reader ORs every row's
    /// invisible byte while building the complete replacement snapshot.
    /// </summary>
    public bool Invisible
    {
        get
        {
            for (int index = 0; index < rows.Count; index++)
            {
                if (rows[index].Invisible)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

/// <summary>
/// One immutable equipped-item row from the official inspected-player snapshot.
/// </summary>
public sealed class WorldInspectedEquipmentItem
{
    public WorldInspectedEquipmentItem(
        int slot,
        int itemId,
        string name,
        string description,
        int currentDurability,
        int maximumDurability,
        int avatar,
        int soulAvatar,
        int quality)
    {
        Slot = slot;
        ItemId = itemId;
        Name = name ?? string.Empty;
        Description = description ?? string.Empty;
        CurrentDurability = currentDurability;
        MaximumDurability = maximumDurability;
        Avatar = avatar;
        SoulAvatar = soulAvatar;
        Quality = quality;
    }

    public int Slot { get; }
    public int ItemId { get; }
    public string Name { get; }
    public string Description { get; }
    public int CurrentDurability { get; }
    public int MaximumDurability { get; }
    public int Avatar { get; }
    public int SoulAvatar { get; }
    public int Quality { get; }
}

/// <summary>
/// Immutable server-authored payload consumed by the recovered ExEquipDlg owner.
/// </summary>
public sealed class WorldInspectedPlayerEquipmentState
{
    private readonly IReadOnlyList<int> attributes;
    private readonly IReadOnlyList<WorldInspectedEquipmentItem> equipment;

    public WorldInspectedPlayerEquipmentState(
        int ignoredCompatibilityValue,
        string name,
        int level,
        int race,
        int gender,
        int head,
        int hair,
        string title,
        long honor,
        CharacterAppearanceRecord appearance,
        IReadOnlyList<int> attributes,
        int maximumAttack,
        int minimumAttack,
        int defense,
        int magicAttack,
        int magicDefense,
        int hit,
        IReadOnlyList<WorldInspectedEquipmentItem> equipment)
    {
        IgnoredCompatibilityValue = ignoredCompatibilityValue;
        Name = name ?? string.Empty;
        Level = level;
        Race = race;
        Gender = gender;
        Head = head;
        Hair = hair;
        Title = title ?? string.Empty;
        Honor = honor;
        Appearance = appearance ?? throw new ArgumentNullException(nameof(appearance));
        this.attributes = new ReadOnlyCollection<int>(
            new List<int>(attributes ?? Array.Empty<int>()));
        MaximumAttack = maximumAttack;
        MinimumAttack = minimumAttack;
        Defense = defense;
        MagicAttack = magicAttack;
        MagicDefense = magicDefense;
        Hit = hit;
        this.equipment = new ReadOnlyCollection<WorldInspectedEquipmentItem>(
            new List<WorldInspectedEquipmentItem>(
                equipment ?? Array.Empty<WorldInspectedEquipmentItem>()));
    }

    public int IgnoredCompatibilityValue { get; }
    public string Name { get; }
    public int Level { get; }
    public int Race { get; }
    public int Gender { get; }
    public int Head { get; }
    public int Hair { get; }
    public string Title { get; }
    public long Honor { get; }
    public CharacterAppearanceRecord Appearance { get; }
    public IReadOnlyList<int> Attributes => attributes;
    public int MaximumAttack { get; }
    public int MinimumAttack { get; }
    public int Defense { get; }
    public int MagicAttack { get; }
    public int MagicDefense { get; }
    public int Hit { get; }
    public IReadOnlyList<WorldInspectedEquipmentItem> Equipment => equipment;
}

/// <summary>
/// One complete server-authored generic monster, pet or trap spawn snapshot.
/// </summary>
/// <remarks>
/// The field order mirrors the locked 1.3.4 generic-entity spawn consumer. ModelOrSkillId and
/// HitPointsOrRadiusPixels retain their wire dual meanings for traps. Optional tail rows are
/// copied into immutable collections so Protocol can publish revisions without sharing mutable
/// buffers with Presentation.
/// </remarks>
public sealed class WorldEntityState
{
    public WorldEntityState(
        int entityId,
        WorldEntityKind kind,
        int modelOrSkillId,
        string displayName,
        int level,
        int x,
        int y,
        int hitPointsOrRadiusPixels,
        int maximumHitPoints,
        int ownerEntityId,
        byte eliteFlags,
        int mapIdGuard,
        byte friendlyFlag,
        IReadOnlyList<WorldEntityStatusEffect> statusEffects,
        IReadOnlyList<WorldEntityStatusIcon> statusIcons,
        long statusEffectsRevision = 0,
        long statusIconsRevision = 0,
        bool? hasStatusIconsUpdate = null)
    {
        // Raw 0x800f arguments are not an entity-admission policy. Native Dispatch
        // forwards every id/model and routes only kind 5 specially; the owner decides creation.
        if (statusEffectsRevision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(statusEffectsRevision));
        }

        if (statusIconsRevision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(statusIconsRevision));
        }

        EntityId = entityId;
        Kind = kind;
        ModelOrSkillId = modelOrSkillId;
        DisplayName = displayName ?? string.Empty;
        Level = level;
        X = x;
        Y = y;
        HitPointsOrRadiusPixels = hitPointsOrRadiusPixels;
        MaximumHitPoints = maximumHitPoints;
        OwnerEntityId = ownerEntityId;
        EliteFlags = eliteFlags;
        MapIdGuard = mapIdGuard;
        FriendlyFlag = friendlyFlag;
        StatusEffects = CopyStatusEffects(statusEffects);
        HasStatusIconsUpdate = hasStatusIconsUpdate ?? (statusIcons != null);
        StatusIcons = CopyStatusIcons(statusIcons);
        StatusEffectsRevision = statusEffectsRevision;
        StatusIconsRevision = statusIconsRevision;
    }

    public int EntityId { get; }

    public WorldEntityKind Kind { get; }

    public int ModelOrSkillId { get; }

    public string DisplayName { get; }

    public int Level { get; }

    public int X { get; }

    public int Y { get; }

    public int HitPointsOrRadiusPixels { get; }

    public int MaximumHitPoints { get; }

    public int OwnerEntityId { get; }

    public byte EliteFlags { get; }

    public int EliteClass => EliteFlags & 0x7f;

    public bool EliteFresh => (EliteFlags & 0x80) != 0;

    /// <summary>
    /// Optional startup-map guard. Zero means the older root omitted the guard.
    /// </summary>
    public int MapIdGuard { get; }

    public byte FriendlyFlag { get; }

    public IReadOnlyList<WorldEntityStatusEffect> StatusEffects { get; }

    public IReadOnlyList<WorldEntityStatusIcon> StatusIcons { get; }

    // An omitted/zero spawn count must not be confused with an explicit empty replacement.
    public bool HasStatusIconsUpdate { get; }

    public long StatusEffectsRevision { get; }

    public long StatusIconsRevision { get; }

    /// <summary>
    /// Returns the same immutable spawn root with a newer server-authored hit-point value.
    /// </summary>
    public WorldEntityState WithHitPoints(int hitPoints)
    {
        return new WorldEntityState(
            EntityId,
            Kind,
            ModelOrSkillId,
            DisplayName,
            Level,
            X,
            Y,
            hitPoints,
            MaximumHitPoints,
            OwnerEntityId,
            EliteFlags,
            MapIdGuard,
            FriendlyFlag,
            StatusEffects,
            StatusIcons,
            StatusEffectsRevision,
            StatusIconsRevision,
            HasStatusIconsUpdate);
    }

    /// <summary>
    /// Returns the same immutable entity root with a newer server-authored body-effect state.
    /// </summary>
    public WorldEntityState WithStatusEffects(
        IReadOnlyList<WorldEntityStatusEffect> statusEffects)
    {
        return WithStatusEffects(
            statusEffects,
            NextRevision(StatusEffectsRevision));
    }

    public WorldEntityState WithStatusEffects(
        IReadOnlyList<WorldEntityStatusEffect> statusEffects,
        long statusEffectsRevision)
    {
        return new WorldEntityState(
            EntityId,
            Kind,
            ModelOrSkillId,
            DisplayName,
            Level,
            X,
            Y,
            HitPointsOrRadiusPixels,
            MaximumHitPoints,
            OwnerEntityId,
            EliteFlags,
            MapIdGuard,
            FriendlyFlag,
            statusEffects,
            StatusIcons,
            statusEffectsRevision,
            StatusIconsRevision,
            HasStatusIconsUpdate);
    }

    /// <summary>
    /// Returns the same immutable entity root with the latest server-authored head-icon snapshot.
    /// </summary>
    public WorldEntityState WithStatusIcons(
        IReadOnlyList<WorldEntityStatusIcon> statusIcons)
    {
        return WithStatusIcons(
            statusIcons,
            NextRevision(StatusIconsRevision));
    }

    public WorldEntityState WithStatusIcons(
        IReadOnlyList<WorldEntityStatusIcon> statusIcons,
        long statusIconsRevision)
    {
        return new WorldEntityState(
            EntityId,
            Kind,
            ModelOrSkillId,
            DisplayName,
            Level,
            X,
            Y,
            HitPointsOrRadiusPixels,
            MaximumHitPoints,
            OwnerEntityId,
            EliteFlags,
            MapIdGuard,
            FriendlyFlag,
            StatusEffects,
            statusIcons,
            StatusEffectsRevision,
            statusIconsRevision);
    }

    private static long NextRevision(long revision)
    {
        return revision == long.MaxValue ? long.MaxValue : revision + 1;
    }

    private static IReadOnlyList<WorldEntityStatusEffect> CopyStatusEffects(
        IReadOnlyList<WorldEntityStatusEffect> source)
    {
        List<WorldEntityStatusEffect> copy = new List<WorldEntityStatusEffect>();
        if (source != null)
        {
            for (int index = 0; index < source.Count; index++)
            {
                WorldEntityStatusEffect value = source[index];
                if (value == null)
                {
                    throw new ArgumentException(
                        "Status-effect rows cannot contain null values.",
                        nameof(source));
                }

                copy.Add(value);
            }
        }

        return new ReadOnlyCollection<WorldEntityStatusEffect>(copy);
    }

    private static IReadOnlyList<WorldEntityStatusIcon> CopyStatusIcons(
        IReadOnlyList<WorldEntityStatusIcon> source)
    {
        List<WorldEntityStatusIcon> copy = new List<WorldEntityStatusIcon>();
        if (source != null)
        {
            for (int index = 0; index < source.Count; index++)
            {
                WorldEntityStatusIcon value = source[index];
                if (value == null)
                {
                    throw new ArgumentException(
                        "Status-icon rows cannot contain null values.",
                        nameof(source));
                }

                copy.Add(value);
            }
        }

        return new ReadOnlyCollection<WorldEntityStatusIcon>(copy);
    }
}

/// <summary>
/// Bounded immutable diagnostics for the current Protocol world-entity publication generation.
/// </summary>
/// <remarks>
/// This projection carries only aggregate receive/publication facts. It cannot create an entity,
/// select a map, or authorize gameplay state.
/// </remarks>
public sealed class WorldEntityPublicationState
{
    public static readonly WorldEntityPublicationState Empty =
        new WorldEntityPublicationState(0, 0, 0, 0, 0, 0, 0, 0);

    public WorldEntityPublicationState(
        long sceneGeneration,
        int mapId,
        long decodedSpawnFrameCount,
        long admittedSpawnFrameCount,
        long rejectedMapGuardSpawnFrameCount,
        long receivedRemovalFrameCount,
        long snapshotVersion,
        int entityCount)
    {
        if (sceneGeneration < 0 || mapId < 0 || decodedSpawnFrameCount < 0 ||
            admittedSpawnFrameCount < 0 || rejectedMapGuardSpawnFrameCount < 0 ||
            receivedRemovalFrameCount < 0 || snapshotVersion < 0 || entityCount < 0 ||
            admittedSpawnFrameCount + rejectedMapGuardSpawnFrameCount >
                decodedSpawnFrameCount)
        {
            throw new ArgumentOutOfRangeException(nameof(sceneGeneration));
        }

        SceneGeneration = sceneGeneration;
        MapId = mapId;
        DecodedSpawnFrameCount = decodedSpawnFrameCount;
        AdmittedSpawnFrameCount = admittedSpawnFrameCount;
        RejectedMapGuardSpawnFrameCount = rejectedMapGuardSpawnFrameCount;
        ReceivedRemovalFrameCount = receivedRemovalFrameCount;
        SnapshotVersion = snapshotVersion;
        EntityCount = entityCount;
    }

    public long SceneGeneration { get; }

    public int MapId { get; }

    public long DecodedSpawnFrameCount { get; }

    public long AdmittedSpawnFrameCount { get; }

    public long RejectedMapGuardSpawnFrameCount { get; }

    public long ReceivedRemovalFrameCount { get; }

    public long SnapshotVersion { get; }

    public int EntityCount { get; }
}

/// <summary>
/// One atomically published world-entity scene view for Application consumers.
/// </summary>
/// <remarks>
/// The scene generation, guard map, immutable entity dictionary, snapshot version and bounded
/// publication facts are one reference. Consumers must not reconstruct this tuple from separate
/// properties because Protocol publication can run concurrently with a Unity frame.
/// </remarks>
public sealed class WorldEntitySceneSnapshot
{
    private static readonly IReadOnlyDictionary<int, WorldEntityState> EmptyEntities =
        new ReadOnlyDictionary<int, WorldEntityState>(
            new Dictionary<int, WorldEntityState>());

    public static readonly WorldEntitySceneSnapshot Empty =
        new WorldEntitySceneSnapshot(
            0,
            0,
            0,
            EmptyEntities,
            WorldEntityPublicationState.Empty);

    public WorldEntitySceneSnapshot(
        long sceneGeneration,
        int mapId,
        long snapshotVersion,
        IReadOnlyDictionary<int, WorldEntityState> entities,
        WorldEntityPublicationState publication)
    {
        if (sceneGeneration < 0 || mapId < 0 || snapshotVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sceneGeneration));
        }
        Entities = entities ?? throw new ArgumentNullException(nameof(entities));
        Publication = publication ??
            throw new ArgumentNullException(nameof(publication));
        if (publication.SceneGeneration != sceneGeneration ||
            publication.MapId != mapId ||
            publication.SnapshotVersion != snapshotVersion ||
            publication.EntityCount != entities.Count)
        {
            throw new ArgumentException(
                "The publication facts do not describe the supplied scene snapshot.",
                nameof(publication));
        }

        SceneGeneration = sceneGeneration;
        MapId = mapId;
        SnapshotVersion = snapshotVersion;
    }

    public long SceneGeneration { get; }

    public int MapId { get; }

    public long SnapshotVersion { get; }

    public IReadOnlyDictionary<int, WorldEntityState> Entities { get; }

    public WorldEntityPublicationState Publication { get; }
}

/// <summary>
/// One complete server-authored ground-item spawn snapshot.
/// </summary>
/// <remarks>
/// LockMilliseconds is the remaining owner-protection duration carried by the spawn packet.
/// Presentation converts it to its own monotonic receive-time deadline. Removal and inventory
/// mutation remain server-authored; issuing a pickup intent must not mutate this snapshot.
/// </remarks>
public sealed class WorldGroundItemState
{
    public WorldGroundItemState(
        int groundId,
        int itemId,
        int count,
        int x,
        int y,
        string displayName,
        bool owned,
        int lockMilliseconds,
        byte kind,
        byte quality, int uniqueId = 0, bool freshDrop = false, long dropToken = 0)
        : this(groundId, itemId, count, x, y, displayName, owned, lockMilliseconds, kind, quality, false, uniqueId, freshDrop, dropToken) { }

    public static WorldGroundItemState FromNative(int groundId, int itemId, int count, int x, int y,
        string displayName, bool owned, int lockMilliseconds, byte kind, byte quality, int uniqueId = 0, bool freshDrop = false, long dropToken = 0) =>
        new WorldGroundItemState(groundId, itemId, count, x, y, displayName, owned, lockMilliseconds, kind, quality, true, uniqueId, freshDrop, dropToken);

    private WorldGroundItemState(int groundId, int itemId, int count, int x, int y,
        string displayName, bool owned, int lockMilliseconds, byte kind, byte quality, bool native, int uniqueId, bool freshDrop, long dropToken)
    {
        if (!native && groundId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(groundId));
        }

        if (!native && itemId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemId));
        }

        if (!native && count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (!native && lockMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lockMilliseconds));
        }

        GroundId = groundId;
        ItemId = itemId;
        Count = count;
        X = x;
        Y = y;
        DisplayName = displayName ?? string.Empty;
        Owned = owned;
        LockMilliseconds = lockMilliseconds;
        Kind = kind;
        Quality = quality;
        UniqueId = uniqueId;
        FreshDrop = freshDrop;
        DropToken = dropToken;
    }

    public int GroundId { get; }
    public int ItemId { get; }
    public int Count { get; }
    public int X { get; }
    public int Y { get; }
    public string DisplayName { get; }
    public bool Owned { get; }
    public int LockMilliseconds { get; }
    public byte Kind { get; }
    public byte Quality { get; }
    public int UniqueId { get; }
    public bool FreshDrop { get; }
    public long DropToken { get; }
    public bool IsUnique => UniqueId > 0;
}

/// <summary>
/// Immutable receive-order observation for the authoritative ground-loot settlement boundary.
/// </summary>
/// <remarks>
/// This is diagnostic ordering state, not gameplay authority. Protocol advances the shared
/// sequence only after it has accepted a ground spawn, ground removal, or inventory snapshot.
/// Presentation may observe the order but cannot use it to create loot or mutate inventory.
/// </remarks>
public sealed class WorldGroundLootAuthorityState
{
    public static readonly WorldGroundLootAuthorityState Empty =
        new WorldGroundLootAuthorityState(0, 0, 0, 0, 0, 0);

    public WorldGroundLootAuthorityState(
        long sequence,
        long lastSpawnSequence,
        int lastSpawnGroundId,
        long lastRemoveSequence,
        int lastRemoveGroundId,
        long lastInventorySequence)
    {
        if (sequence < 0 || lastSpawnSequence < 0 || lastRemoveSequence < 0 ||
            lastInventorySequence < 0 || lastSpawnSequence > sequence ||
            lastRemoveSequence > sequence || lastInventorySequence > sequence)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence));
        }

        Sequence = sequence;
        LastSpawnSequence = lastSpawnSequence;
        LastSpawnGroundId = lastSpawnGroundId;
        LastRemoveSequence = lastRemoveSequence;
        LastRemoveGroundId = lastRemoveGroundId;
        LastInventorySequence = lastInventorySequence;
    }

    public long Sequence { get; }
    public long LastSpawnSequence { get; }
    public int LastSpawnGroundId { get; }
    public long LastRemoveSequence { get; }
    public int LastRemoveGroundId { get; }
    public long LastInventorySequence { get; }
}

/// <summary>
/// One ordered, server-authored combat-result edge.
/// </summary>
/// <remarks>
/// Flags retain the reviewed official wire bits: bit 0 critical, bit 1 miss, bit 2 fatal and
/// bit 3 suppresses the ordinary hit sound. Presentation projects this immutable edge; it must
/// never calculate damage or infer acceptance locally.
/// </remarks>
public sealed class WorldCombatResultState
{
    public const byte CriticalFlag = 1;
    public const byte MissFlag = 2;
    public const byte FatalFlag = 4;
    public const byte SuppressHitSoundFlag = 8;
    public const byte HealingFlag = 16;

    public WorldCombatResultState(
        int attackerEntityId,
        int targetEntityId,
        int damage,
        byte flags,
        int targetHitPoints,
        long revision = 0, long attackerIncarnation = 0, long targetIncarnation = 0, long receiveSequence = 0,
        bool nativeCombatHandled = false)
    {
        AttackerEntityId = attackerEntityId;
        TargetEntityId = targetEntityId;
        Damage = damage;
        Flags = flags;
        RawTargetHitPoints = targetHitPoints;
        TargetHitPoints = Math.Max(0, targetHitPoints);
        Revision = revision;
        AttackerIncarnation = attackerIncarnation;
        TargetIncarnation = targetIncarnation;
        ReceiveSequence = receiveSequence;
        NativeCombatHandled = nativeCombatHandled;
    }

    public int AttackerEntityId { get; }
    public int TargetEntityId { get; }
    public int Damage { get; }
    public byte Flags { get; }
    public int TargetHitPoints { get; }
    /// <summary>The unmodified receive parameter for the original target SetHp owner.</summary>
    public int RawTargetHitPoints { get; }
    public long Revision { get; }
    // Receive correlation metadata; never serialized into the combat wire payload.
    public long AttackerIncarnation { get; }
    public long TargetIncarnation { get; }
    public long ReceiveSequence { get; }
    public bool NativeCombatHandled { get; }
    public bool Healing => (Flags & HealingFlag) != 0;
    public bool Critical => (Flags & CriticalFlag) != 0;
    public bool Missed => (Flags & MissFlag) != 0;
    public bool Fatal => (Flags & FatalFlag) != 0 || TargetHitPoints <= 0;
    public bool SuppressHitSound => (Flags & SuppressHitSoundFlag) != 0;

    public WorldCombatResultState WithEntityIncarnations(long attacker, long target, long receiveSequence) =>
        new WorldCombatResultState(AttackerEntityId, TargetEntityId, Damage, Flags,
            RawTargetHitPoints, Revision, attacker, target, receiveSequence, NativeCombatHandled);

    public WorldCombatResultState WithRevision(long revision)
    {
        return new WorldCombatResultState(
            AttackerEntityId,
            TargetEntityId,
            Damage,
            Flags,
            RawTargetHitPoints,
            revision, AttackerIncarnation, TargetIncarnation, ReceiveSequence, NativeCombatHandled);
    }
}

/// <summary>
/// One ordered world-number request owned by the server.
/// </summary>
/// <remarks>
/// Official 1.5.8 CombatNumber.SpawnOn (0x06000117, arm64 0x23a734) receives the value as a
/// plain System.Int32 and contains no sign test at all - the only gates are type == 5 and
/// target == 1 - so a negative value is a real official input and this record keeps it instead
/// of failing the decode.
/// </remarks>
public sealed class WorldCombatNumberState
{
    // Official 1.5.8 CombatNumber.SpawnOn receives type 4 exactly like every other type: this
    // names the experience kind for decode fixtures only and is not a spawn gate.
    public const byte ExperienceType = 4;

    public WorldCombatNumberState(
        byte target,
        int value,
        byte type,
        long revision = 0)
    {
        Target = target;
        Value = value;
        Type = type;
        Revision = revision;
    }

    public byte Target { get; }
    public int Value { get; }
    public byte Type { get; }
    public long Revision { get; }

    public WorldCombatNumberState WithRevision(long revision)
    {
        return new WorldCombatNumberState(Target, Value, Type, revision);
    }
}

/// <summary>
/// One complete server-authored remote-player spawn snapshot.
/// </summary>
/// <remarks>
/// The official 1.3.4 consumer truncates the wire F32 position to signed integer map pixels before
/// constructing RemotePlayer, so X/Y intentionally expose that consumed value. Compatibility
/// race byte is consumed by Protocol; the weapon category is retained for the 1.5.8 attack-pose
/// owner. Riding updates replace RidingState without inventing another
/// player root.
/// </remarks>
public sealed class WorldRemotePlayerState
{
    public WorldRemotePlayerState(
        int entityId,
        string displayName,
        int x,
        int y,
        int level,
        int gender,
        int head,
        int hair,
        string title,
        CharacterAppearanceRecord appearance,
        bool dead,
        string avatarAura,
        string soulEffects,
        int faction,
        int hitPoints,
        int maximumHitPoints,
        string titleEffect,
        int glowMode,
        bool invisible,
        WorldRemoteRidingState ridingState,
        long lifeStateRevision = 0,
        byte pkState = 0,
        long pkStateRevision = 0)
    {
        EntityId = entityId;
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        X = x;
        Y = y;
        Level = level;
        Gender = gender;
        Head = head;
        Hair = hair;
        Title = title ?? string.Empty;
        Appearance = appearance ?? throw new ArgumentNullException(nameof(appearance));
        Dead = dead;
        AvatarAura = avatarAura ?? string.Empty;
        SoulEffects = soulEffects ?? string.Empty;
        Faction = faction;
        HitPoints = hitPoints;
        MaximumHitPoints = maximumHitPoints;
        TitleEffect = titleEffect ?? string.Empty;
        GlowMode = glowMode;
        Invisible = invisible;
        RidingState = ridingState ?? throw new ArgumentNullException(nameof(ridingState));
        LifeStateRevision = lifeStateRevision;
        PkState = pkState;
        PkStateRevision = pkStateRevision;
        if (ridingState.EntityId != entityId)
        {
            throw new ArgumentException(
                "The remote-player and riding entity ids must match.",
                nameof(ridingState));
        }

        if (lifeStateRevision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lifeStateRevision));
        }

        if (pkStateRevision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pkStateRevision));
        }
    }

    public WorldRemotePlayerState WithHair(int head, int hair) =>
        new WorldRemotePlayerState(EntityId, DisplayName, X, Y, Level, Gender,
            head, hair, Title, Appearance, Dead, AvatarAura, SoulEffects, Faction,
            HitPoints, MaximumHitPoints, TitleEffect, GlowMode, Invisible, RidingState,
            LifeStateRevision, PkState, PkStateRevision);

    public WorldRemotePlayerState WithDisplayName(string name) =>
        new WorldRemotePlayerState(EntityId, name ?? string.Empty, X, Y, Level, Gender,
            Head, Hair, Title, Appearance, Dead, AvatarAura, SoulEffects, Faction,
            HitPoints, MaximumHitPoints, TitleEffect, GlowMode, Invisible, RidingState,
            LifeStateRevision, PkState, PkStateRevision);

    public int EntityId { get; }

    public string DisplayName { get; }

    public int X { get; }

    public int Y { get; }

    public int Level { get; }

    public int Gender { get; }

    public int Head { get; }

    public int Hair { get; }

    public string Title { get; }

    public CharacterAppearanceRecord Appearance { get; }

    public bool Dead { get; }

    public string AvatarAura { get; }

    public string SoulEffects { get; }

    public int Faction { get; }

    public int HitPoints { get; }

    public int MaximumHitPoints { get; }

    public string TitleEffect { get; }

    public int GlowMode { get; }

    public bool Invisible { get; }

    public WorldRemoteRidingState RidingState { get; }

    /// <summary>
    /// Monotonic receive revision for explicit life-state packets and refreshed spawn roots.
    /// </summary>
    public long LifeStateRevision { get; }

    /// <summary>
    /// The last server-authored PK state for this remote player. The official name tag maps
    /// state 1 to grey, state 2 to red, and leaves every other wire value white.
    /// </summary>
    public byte PkState { get; }

    /// <summary>
    /// Monotonic receive revision for dedicated remote PK-state updates.
    /// </summary>
    public long PkStateRevision { get; }

    public WorldRemotePlayerState WithVitals(int hitPoints, int maximumHitPoints)
    {
        return new WorldRemotePlayerState(EntityId, DisplayName, X, Y, Level, Gender,
            Head, Hair, Title, Appearance, Dead, AvatarAura, SoulEffects, Faction,
            hitPoints, maximumHitPoints, TitleEffect, GlowMode, Invisible, RidingState,
            LifeStateRevision, PkState, PkStateRevision);
    }

    public WorldRemotePlayerState WithRidingState(WorldRemoteRidingState value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return new WorldRemotePlayerState(
            EntityId,
            DisplayName,
            X,
            Y,
            Level,
            Gender,
            Head,
            Hair,
            Title,
            Appearance,
            Dead,
            AvatarAura,
            SoulEffects,
            Faction,
            HitPoints,
            MaximumHitPoints,
            TitleEffect,
            GlowMode,
            Invisible,
            value,
            LifeStateRevision,
            PkState,
            PkStateRevision);
    }

    public WorldRemotePlayerState WithDeadState(bool value)
    {
        return WithLifeState(
            value,
            checked(LifeStateRevision + 1));
    }

    public WorldRemotePlayerState WithLifeState(bool dead, long revision)
    {
        return new WorldRemotePlayerState(
            EntityId,
            DisplayName,
            X,
            Y,
            Level,
            Gender,
            Head,
            Hair,
            Title,
            Appearance,
            dead,
            AvatarAura,
            SoulEffects,
            Faction,
            HitPoints,
            MaximumHitPoints,
            TitleEffect,
            GlowMode,
            Invisible,
            RidingState,
            revision,
            PkState,
            PkStateRevision);
    }

    public WorldRemotePlayerState WithPkState(byte value, long revision)
    {
        return new WorldRemotePlayerState(
            EntityId,
            DisplayName,
            X,
            Y,
            Level,
            Gender,
            Head,
            Hair,
            Title,
            Appearance,
            Dead,
            AvatarAura,
            SoulEffects,
            Faction,
            HitPoints,
            MaximumHitPoints,
            TitleEffect,
            GlowMode,
            Invisible,
            RidingState,
            LifeStateRevision,
            value,
            revision);
    }
}

/// <summary>
/// One integer map-pixel point in a server-authored entity movement path.
/// </summary>
public struct WorldPathPoint
{
    public WorldPathPoint(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; }

    public int Y { get; }
}

/// <summary>
/// One complete server-authored skill-cast edge.
/// </summary>
/// <remarks>
/// The optional-tail presence is retained instead of being collapsed into zero sentinels.
/// Presentation can therefore reproduce the official local/remote branches without learning
/// Protocol byte layout. Revision zero is reserved for a decoded value that has not yet passed
/// through the live gateway.
/// </remarks>
public sealed class WorldSkillCastState
{
    public WorldSkillCastState(
        int skillId,
        int chantHoldMilliseconds,
        byte phase,
        int cooldownMilliseconds,
        int casterEntityId,
        int? targetEntityId,
        byte? aimFlag,
        int aimX,
        int aimY,
        byte? scalePercent,
        long revision = 0)
    {
        if (revision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(revision));
        }

        SkillId = skillId;
        ChantHoldMilliseconds = chantHoldMilliseconds;
        Phase = phase;
        CooldownMilliseconds = cooldownMilliseconds;
        CasterEntityId = casterEntityId;
        TargetEntityId = targetEntityId;
        AimFlag = aimFlag;
        AimX = aimX;
        AimY = aimY;
        ScalePercent = scalePercent;
        Revision = revision;
    }

    public int SkillId { get; }

    public int ChantHoldMilliseconds { get; }

    public byte Phase { get; }

    public int CooldownMilliseconds { get; }

    public int CasterEntityId { get; }

    public int? TargetEntityId { get; }

    /// <summary>
    /// Exact optional wire byte, nullable because an absent tail reads no wire byte at all. The
    /// official body compares this raw against zero and inverts the result into its no-aim
    /// boolean register, so zero means "no aim" and every nonzero byte means "aim present".
    /// </summary>
    public byte? AimFlag { get; }

    /// <summary>
    /// Product expression of the official no-aim boolean register: absent tail and raw == 0 are
    /// both "no aim", every nonzero raw is "aim present" even when both coordinates are 0.
    /// </summary>
    public bool HasAim => AimFlag.HasValue && AimFlag.Value != 0;

    public int AimX { get; }

    public int AimY { get; }

    public byte? ScalePercent { get; }

    public long Revision { get; }

    // Only the one-shot application provenance is shared/mutable. Wire fields and revision
    // remain immutable; revising an observation must never create another effect authority.
    public WorldSkillCastReceipt Receipt { get; } = new WorldSkillCastReceipt();

    private WorldSkillCastState(WorldSkillCastState state, long revision)
        : this(state.SkillId, state.ChantHoldMilliseconds, state.Phase,
            state.CooldownMilliseconds, state.CasterEntityId, state.TargetEntityId,
            state.AimFlag, state.AimX, state.AimY, state.ScalePercent, revision)
    {
        Receipt = state.Receipt;
    }

    public WorldSkillCastState WithRevision(long revision)
    {
        return new WorldSkillCastState(this, revision);
    }
}

/// <summary>
/// The remote-player action pose selected by the official world dispatcher.
/// </summary>
public enum WorldRemoteActionKind
{
    Attack = 1,
    Magic = 2,
}

/// <summary>
/// One server-authored remote-player action edge.
/// </summary>
/// <remarks>
/// Repeated actions are distinct even when their kind and target match, so Revision is part of
/// the immutable value. TargetEntityId is retained for the attack-facing seam; Presentation may
/// resolve it to a currently materialized player or world object and otherwise preserves the
/// official nullable-target behavior.
/// </remarks>
public sealed class WorldRemoteActionState
{
    public WorldRemoteActionState(
        int entityId,
        WorldRemoteActionKind kind,
        int targetEntityId,
        long revision)
    {
        if (kind != WorldRemoteActionKind.Attack &&
            kind != WorldRemoteActionKind.Magic)
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (revision < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(revision));
        }

        EntityId = entityId;
        Kind = kind;
        TargetEntityId = targetEntityId;
        Revision = revision;
    }

    public int EntityId { get; }

    public WorldRemoteActionKind Kind { get; }

    public int TargetEntityId { get; }

    /// <summary>
    /// Monotonic receive order shared with remote movement paths.
    /// </summary>
    public long Revision { get; }
}

/// <summary>
/// One server-authored channel-chat edge that the official client projects above a remote player.
/// </summary>
/// <remarks>
/// Only channel zero reaches this boundary. ThemeSelector is retained verbatim because the locked
/// The 1.5.8 dispatcher maps selector 1 to red and other values to huang.
/// Empty text preserves the prior bubble. Revision distinguishes repeated
/// identical messages without exposing Protocol or Unity types to Application.
/// </remarks>
public sealed class WorldRemoteChatBubbleState
{
    public WorldRemoteChatBubbleState(
        int entityId,
        string text,
        byte themeSelector,
        long revision)
    {
        if (text == null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        if (revision < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(revision));
        }

        EntityId = entityId;
        Text = text;
        ThemeSelector = themeSelector;
        Revision = revision;
    }

    public int EntityId { get; }

    public string Text { get; }

    public byte ThemeSelector { get; }

    public long Revision { get; }
}

/// <summary>
/// The three source-recovered ChatUI.SendNow routes that cross into Protocol.
/// </summary>
public enum WorldChatSendKind
{
    Channel = 0,
    Whisper = 1,
    GmCommand = 2
}

/// <summary>
/// A source-recovered outbound chat write request, not a claim of server acceptance.
/// </summary>
/// <remarks>
/// <see cref="Shares"/> holds the very list instance the official flush point built, never
/// a copy of it. Official ChatUI.TakeOutgoing (0x060000de) sorts the located tokens by token start,
/// replaces the public static <c>ChatUI.OutgoingShares</c> with a new
/// <c>List&lt;(byte, byte, int)&gt;</c> and only then clears the caller's token list; the official
/// commands NetClient.Gossip (0x06000771), NetClient.SendChat (0x060006db) and NetClient.SendHorn
/// (0x060006dc) capture that list reference in their display class, so NetClient.WriteShares
/// (0x060006dd) re-indexes it through the List indexer while the frame is serialized. The value
/// tuple is the official element type, so nothing is projected, re-ordered or re-allocated between
/// the flush point and the writer, and a null list keeps its native meaning: WriteShares writes the
/// count 0 for it instead of throwing.
/// No count bound, truncation or sort is applied on this side either. The single official limit is
/// the four-entry clamp inside WriteShares, so an over-long list crosses this boundary unchanged
/// and the writer - not this request - decides how many entries reach the frame.
/// Only the (kind, tab, slot) triple is wire content. The token text that edits the input line - the
/// 【name】 string ChatShareTokens.Locate matches on - belongs to the Presentation edit list and
/// never crosses this boundary, so no edit-list body is carried into Protocol here.
/// </remarks>
public sealed class WorldChatSendRequest
{
    // Four is not a bound this type enforces. It mirrors the clamp NetClient.WriteShares
    // (0x060006dd) applies per write, and is only needed by the non-wire shape checks that read
    // the official triple order; the request carries whatever list it is given, long or null, and
    // neither rejects it nor truncates a copy of it.
    public const int MaximumShares = 4;

    private WorldChatSendRequest(
        WorldChatSendKind kind,
        byte channel,
        string targetName,
        string text,
        List<(byte, byte, int)> shares)
    {
        if (string.IsNullOrEmpty(text) && kind != WorldChatSendKind.GmCommand)
        {
            throw new ArgumentException("Chat text cannot be empty.", nameof(text));
        }

        // No count bound belongs here. The only official four-entry limit is the clamp inside
        // NetClient.WriteShares (0x060006dd): arm64 tests `3 < count` and x86_64 tests `count < 4`,
        // so a longer list is written down to four entries silently, per write, while the frame is
        // serialized. Rejecting one here turned an official write into a whole-request failure, and
        // truncating, copying or re-ordering the list here would move that clamp away from its
        // owner, so the reference is stored exactly as it arrived - including a fifth entry and a
        // null list, which WriteShares answers with the count 0. WriteShares re-indexes this list
        // through the List indexer once per field, so the entries are read at serialization time
        // and no snapshot sits between the flush point and the writer.

        Kind = kind;
        Channel = channel;
        TargetName = targetName ?? string.Empty;
        Text = text ?? string.Empty;
        Shares = shares;
    }

    public WorldChatSendKind Kind { get; }

    public byte Channel { get; }

    public string TargetName { get; }

    public string Text { get; }

    public List<(byte, byte, int)> Shares { get; }

    public static WorldChatSendRequest ForChannel(
        byte channel,
        string text,
        List<(byte, byte, int)> shares)
    {
        if (channel > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(channel));
        }

        return new WorldChatSendRequest(
            WorldChatSendKind.Channel,
            channel,
            string.Empty,
            text,
            shares);
    }

    public static WorldChatSendRequest ForWhisper(
        string targetName,
        string text,
        List<(byte, byte, int)> shares)
    {
        if (string.IsNullOrEmpty(targetName))
        {
            throw new ArgumentException(
                "A whisper target is required.",
                nameof(targetName));
        }

        return new WorldChatSendRequest(
            WorldChatSendKind.Whisper,
            0,
            targetName,
            text,
            shares);
    }

    public static WorldChatSendRequest ForGmCommand(string command)
    {
        return new WorldChatSendRequest(
            WorldChatSendKind.GmCommand,
            0,
            string.Empty,
            command,
            null);
    }
}

public sealed class WorldChatSharedPetSkillState
{
    public WorldChatSharedPetSkillState(string name, int level)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Level = level;
    }

    public string Name { get; }

    public int Level { get; }
}

/// <summary>
/// Immutable projection of the official PetBrief nested in a received chat share.
/// </summary>
public sealed class WorldChatSharedPetState
{
    public WorldChatSharedPetState(
        int model,
        int slot,
        string nickname,
        string prename,
        string species,
        byte gender,
        byte habit,
        byte opened,
        int level,
        int strength,
        int intelligence,
        int vitality,
        int agility,
        int dexterity,
        int spirit,
        int starve,
        int trust,
        int freePoints,
        int freeUsed,
        IReadOnlyList<WorldChatSharedPetSkillState> skills,
        byte bound)
    {
        Nickname = nickname ?? throw new ArgumentNullException(nameof(nickname));
        Prename = prename ?? throw new ArgumentNullException(nameof(prename));
        Species = species ?? throw new ArgumentNullException(nameof(species));
        if (skills == null)
        {
            throw new ArgumentNullException(nameof(skills));
        }

        Model = model;
        Slot = slot;
        Gender = gender;
        Habit = habit;
        Opened = opened;
        Level = level;
        Strength = strength;
        Intelligence = intelligence;
        Vitality = vitality;
        Agility = agility;
        Dexterity = dexterity;
        Spirit = spirit;
        Starve = starve;
        Trust = trust;
        FreePoints = freePoints;
        FreeUsed = freeUsed;
        Skills = new ReadOnlyCollection<WorldChatSharedPetSkillState>(
            new List<WorldChatSharedPetSkillState>(skills));
        Bound = bound;
    }

    public int Model { get; }
    public int Slot { get; }
    public string Nickname { get; }
    public string Prename { get; }
    public string Species { get; }
    public byte Gender { get; }
    public byte Habit { get; }
    public byte Opened { get; }
    public int Level { get; }
    public int Strength { get; }
    public int Intelligence { get; }
    public int Vitality { get; }
    public int Agility { get; }
    public int Dexterity { get; }
    public int Spirit { get; }
    public int Starve { get; }
    public int Trust { get; }
    public int FreePoints { get; }
    public int FreeUsed { get; }
    public IReadOnlyList<WorldChatSharedPetSkillState> Skills { get; }
    public byte Bound { get; }
}

/// <summary>
/// Original ChatUI.Share value shared by the reader and both native chat owners.
/// The nullable PetBrief is inline; its one skill list keeps the reader's identity.
/// </summary>
public struct WorldChatSharedEntryState
{
    public byte kind;
    public int itemId;
    public int quality;
    public string name;
    public string desc;
    public PetBrief? pet;

    // Compatibility construction from an application DTO is outside the native reader path.
    public WorldChatSharedEntryState(byte kind, int itemId, byte quality, string name,
        string description, WorldChatSharedPetState pet, int ppAiUsed, int ppAiCap,
        int ppShUsed, int ppShCap)
    {
        this.kind = kind;
        this.itemId = itemId;
        this.quality = quality;
        this.name = name ?? throw new ArgumentNullException(nameof(name));
        desc = description ?? throw new ArgumentNullException(nameof(description));
        this.pet = null;
        if (pet != null)
        {
            var skills = new List<(string, int)>(pet.Skills.Count);
            foreach (WorldChatSharedPetSkillState skill in pet.Skills)
                skills.Add((skill.Name, skill.Level));
            this.pet = new PetBrief
            {
                model = pet.Model, slot = pet.Slot, nickname = pet.Nickname,
                prename = pet.Prename, species = pet.Species, gender = pet.Gender,
                habit = pet.Habit, opened = pet.Opened, level = pet.Level,
                str = pet.Strength, intel = pet.Intelligence, vit = pet.Vitality,
                agi = pet.Agility, dex = pet.Dexterity, spi = pet.Spirit,
                starve = pet.Starve, trust = pet.Trust, freePts = pet.FreePoints,
                freeUsed = pet.FreeUsed, skills = skills, bound = pet.Bound != 0,
                ppAiUsed = ppAiUsed, ppAiCap = ppAiCap,
                ppShUsed = ppShUsed, ppShCap = ppShCap
            };
        }
    }

    public byte Kind => kind;
    public int ItemId => itemId;
    public byte Quality => (byte)quality;
    public string Name => name;
    public string Description => desc;
    public PetBrief? Pet => pet;
    public int PpAiUsed => pet.GetValueOrDefault().ppAiUsed;
    public int PpAiCap => pet.GetValueOrDefault().ppAiCap;
    public int PpShUsed => pet.GetValueOrDefault().ppShUsed;
    public int PpShCap => pet.GetValueOrDefault().ppShCap;
}

/// <summary>
/// One server-authored channel-chat row in receive order.
/// </summary>
/// <remarks>
/// Shares keeps the null that official 0x06000711 NetClient.ReadShares returns for an absent or
/// zero count, exactly like WorldPrivateChatState, because native ChatUI.Add receives that null
/// reference. The null never reaches a consumer unchecked: ChatUI.MapIncomingShares forwards null
/// and ChatUI.DrawShareRow/ShareHover treat it as "no share rows".
/// </remarks>
public sealed class WorldChannelChatState
{
    public WorldChannelChatState(
        int senderEntityId,
        string senderName,
        byte bubbleThemeSelector,
        byte channelCode,
        string text,
        long revision)
        : this(
            senderEntityId,
            senderName,
            bubbleThemeSelector,
            channelCode,
            text,
            Array.Empty<WorldChatSharedEntryState>(),
            revision)
    {
    }

    public WorldChannelChatState(
        int senderEntityId,
        string senderName,
        byte bubbleThemeSelector,
        byte channelCode,
        string text,
        IReadOnlyList<WorldChatSharedEntryState> shares,
        long revision, byte senderLevel = 0, byte senderFlag = 0)
    {
        if (senderName == null)
        {
            throw new ArgumentNullException(nameof(senderName));
        }

        if (text == null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        if (revision < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(revision));
        }

        SenderEntityId = senderEntityId;
        SenderName = senderName;
        BubbleThemeSelector = bubbleThemeSelector;
        ChannelCode = channelCode;
        Text = text;
        Shares = shares == null ? null : new ReadOnlyCollection<WorldChatSharedEntryState>(
            new List<WorldChatSharedEntryState>(shares));
        Revision = revision;
        SenderLevel = senderLevel; SenderFlag = senderFlag;
    }

    public int SenderEntityId { get; }

    public string SenderName { get; }

    public byte BubbleThemeSelector { get; }

    public byte ChannelCode { get; }

    public string Text { get; }

    public IReadOnlyList<WorldChatSharedEntryState> Shares { get; }

    public long Revision { get; }
    public byte SenderLevel { get; }
    public byte SenderFlag { get; }
    // Original 0x8039 broadcast tail. Keep the older property names for compatibility.
    public byte BroadcastTier => SenderLevel;
    public byte BroadcastSkin => SenderFlag;
}

/// <summary>
/// One server-authored private-chat row in exact receive order.
/// </summary>
/// <remarks>
/// The locked 1.3.4 private-chat dispatch branch reads sender, text and the same bounded
/// share list used by channel chat before invoking GossipDlg.Receive. This immutable projection
/// keeps that wire result outside Presentation and does not imply that any local send succeeded.
/// </remarks>
public sealed class WorldPrivateChatState
{
    public WorldPrivateChatState(
        string senderName,
        string text,
        IReadOnlyList<WorldChatSharedEntryState> shares,
        long revision)
    {
        SenderName = senderName ?? string.Empty;
        Text = text;
        if (revision < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(revision));
        }

        Shares = shares == null ? null : new ReadOnlyCollection<WorldChatSharedEntryState>(
            new List<WorldChatSharedEntryState>(shares));
        Revision = revision;
    }

    public string SenderName { get; }

    public string Text { get; }

    public IReadOnlyList<WorldChatSharedEntryState> Shares { get; }

    public long Revision { get; }
}

/// <summary>
/// The latest complete movement path authored by the server for one non-local world entity.
/// </summary>
/// <remarks>
/// Protocol publishes the same immutable value through a type-specific remote-player or monster
/// snapshot only after the entity id resolves. Local-player warp remains a separate consumer of
/// the shared wire packet. Points are copied so the receive loop cannot mutate Presentation state
/// after publish. An empty point list is a valid wire value: the official 0x8018 branch allocates
/// two `int[]` of the byte-sized count and calls CombatWorld.MovePath even when the count is
/// zero, so the only bound kept here is the one the single count byte imposes.
/// </remarks>
public sealed class WorldRemotePathState
{
    public WorldRemotePathState(
        int entityId,
        int speed,
        IReadOnlyList<WorldPathPoint> points,
        long revision = 0, bool nativeMovementHandled = false)
    {
        if (points == null)
        {
            throw new ArgumentNullException(nameof(points));
        }

        if (points.Count > byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(points));
        }

        if (revision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(revision));
        }

        List<WorldPathPoint> copy = new List<WorldPathPoint>(points.Count);
        for (int index = 0; index < points.Count; index++)
        {
            copy.Add(points[index]);
        }

        EntityId = entityId;
        Speed = speed;
        Points = new ReadOnlyCollection<WorldPathPoint>(copy);
        Revision = revision;
        NativeMovementHandled = nativeMovementHandled;
    }

    public int EntityId { get; }

    public int Speed { get; }

    public IReadOnlyList<WorldPathPoint> Points { get; }

    /// <summary>
    /// Monotonic receive order shared with remote actions. Zero is reserved for isolated audit
    /// values that have not passed through a live gateway.
    /// </summary>
    public long Revision { get; }

    public bool NativeMovementHandled { get; }

    public WorldRemotePathState WithRevision(long revision)
    {
        return new WorldRemotePathState(EntityId, Speed, Points, revision, NativeMovementHandled);
    }
}

/// <summary>
/// The latest complete local-player riding state received from the authoritative server.
/// </summary>
/// <remarks>
/// This state is not part of the enter-world pet summary. Protocol builds it only from the
/// dedicated self-riding packet and merges that packet's optional compatibility tails with the
/// previous authoritative state.
/// </remarks>
public sealed class WorldRidingState
{
    public WorldRidingState(
        bool riding,
        int mountModel,
        int riderSeat,
        int rideAnchorId,
        bool mountTianmo,
        int rideSeats = 0)
    {
        Riding = riding;
        MountModel = mountModel;
        RiderSeat = riderSeat;
        RideAnchorId = rideAnchorId;
        MountTianmo = mountTianmo;
        RideSeats = rideSeats;
    }

    public bool Riding { get; }

    public int MountModel { get; }

    public int RiderSeat { get; }

    public int RideAnchorId { get; }

    public bool MountTianmo { get; }

    public int RideSeats { get; }
}

/// <summary>
/// One server-authored remote-player riding state keyed by the remote entity id.
/// </summary>
/// <remarks>
/// Initial remote-player spawn and later remote-riding updates converge on this immutable value.
/// The seat count is server-authored; it is not inferred from local mount art or client-side UI state.
/// </remarks>
public sealed class WorldRemoteRidingState
{
    public WorldRemoteRidingState(
        int entityId,
        bool riding,
        int mountModel,
        bool mountTianmo,
        int riderSeat,
        int rideAnchorId,
        int mountSeats)
    {
        EntityId = entityId;
        Riding = riding;
        MountModel = mountModel;
        MountTianmo = mountTianmo;
        RiderSeat = riderSeat;
        RideAnchorId = rideAnchorId;
        MountSeats = mountSeats;
    }

    public int EntityId { get; }

    public bool Riding { get; }

    public int MountModel { get; }

    public bool MountTianmo { get; }

    public int RiderSeat { get; }

    public int RideAnchorId { get; }

    public int MountSeats { get; }
}

/// <summary>
/// One server-authored member row in the current party snapshot.
/// </summary>
public sealed class WorldPartyMemberState
{
    public WorldPartyMemberState(
        int entityId,
        string displayName,
        int level,
        int hitPoints,
        int maximumHitPoints,
        int gender,
        int hairId,
        int hairColor,
        bool online)
    {
        EntityId = entityId;
        DisplayName = displayName ?? string.Empty;
        Level = level;
        HitPoints = hitPoints;
        MaximumHitPoints = maximumHitPoints;
        Gender = gender;
        HairId = hairId;
        HairColor = hairColor;
        Online = online;
    }

    public int EntityId { get; }

    public string DisplayName { get; }

    public int Level { get; }

    public int HitPoints { get; }

    public int MaximumHitPoints { get; }

    public int Gender { get; }

    public int HairId { get; }

    public int HairColor { get; }

    public bool Online { get; }

    public WorldPartyMemberState WithOnline(bool online)
    {
        return new WorldPartyMemberState(
            EntityId,
            DisplayName,
            Level,
            HitPoints,
            MaximumHitPoints,
            Gender,
            HairId,
            HairColor,
            online);
    }
}

/// <summary>
/// The latest complete party snapshot consumed by the official social client.
/// </summary>
public sealed class WorldPartyState
{
    public static readonly WorldPartyState Empty = new WorldPartyState(
        0,
        string.Empty,
        Array.Empty<WorldPartyMemberState>(),
        Array.Empty<long>());

    public WorldPartyState(
        int leaderEntityId,
        string partyName,
        IReadOnlyList<WorldPartyMemberState> members,
        IReadOnlyList<long> characterIds,
        IReadOnlyList<byte> professions = null)
    {
        LeaderEntityId = leaderEntityId;
        PartyName = partyName ?? string.Empty;
        Members = CopyMembers(members);
        CharacterIds = CopyCharacterIds(characterIds);
        Professions = professions == null || professions.Count == 0
            ? Array.Empty<byte>()
            : new ReadOnlyCollection<byte>(new List<byte>(professions));
    }

    public int LeaderEntityId { get; }

    public string PartyName { get; }

    public IReadOnlyList<WorldPartyMemberState> Members { get; }

    public IReadOnlyList<long> CharacterIds { get; }

    public IReadOnlyList<byte> Professions { get; }

    public bool ContainsEntity(int entityId)
    {
        if (entityId == 0)
        {
            return false;
        }

        for (int index = 0; index < Members.Count; index++)
        {
            if (Members[index].EntityId == entityId)
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyList<WorldPartyMemberState> CopyMembers(
        IReadOnlyList<WorldPartyMemberState> source)
    {
        if (source == null || source.Count == 0)
        {
            return Array.Empty<WorldPartyMemberState>();
        }

        List<WorldPartyMemberState> copy = new List<WorldPartyMemberState>(source.Count);
        for (int index = 0; index < source.Count; index++)
        {
            WorldPartyMemberState member = source[index];
            if (member == null)
            {
                throw new ArgumentException("Party member rows cannot be null.", nameof(source));
            }

            copy.Add(member);
        }

        return new ReadOnlyCollection<WorldPartyMemberState>(copy);
    }

    private static IReadOnlyList<long> CopyCharacterIds(IReadOnlyList<long> source)
    {
        if (source == null || source.Count == 0)
        {
            return Array.Empty<long>();
        }

        List<long> copy = new List<long>(source.Count);
        for (int index = 0; index < source.Count; index++)
        {
            copy.Add(source[index]);
        }

        return new ReadOnlyCollection<long>(copy);
    }
}

/// <summary>
/// One live stall-owner row published by the server's stall compatibility packets.
/// </summary>
public sealed class WorldStallOwnerState
{
    public WorldStallOwnerState(int entityId, int stallType, string displayName)
    {
        EntityId = entityId;
        StallType = stallType;
        DisplayName = displayName ?? string.Empty;
    }

    public int EntityId { get; }

    public int StallType { get; }

    public string DisplayName { get; }
}

/// <summary>
/// One immutable learned character-skill row from the complete server replacement.
/// </summary>
public sealed class WorldLearnedSkillState
{
    public WorldLearnedSkillState(int id, int level)
    {
        Id = id;
        Level = level;
    }

    public int Id { get; }
    public int Level { get; }
}

/// <summary>
/// Exact immutable replacement projected from the official character-skill snapshot.
/// </summary>
public sealed class WorldSkillBookState
{
    public static readonly WorldSkillBookState Empty =
        new WorldSkillBookState(0, Array.Empty<WorldLearnedSkillState>());

    public WorldSkillBookState(
        int skillPoints,
        IReadOnlyList<WorldLearnedSkillState> skills,
        IReadOnlyDictionary<int, int> damageBonuses = null)
    {
        SkillPoints = skillPoints;
        Skills = CopySkills(skills);
        var copiedBonuses = new Dictionary<int, int>();
        if (damageBonuses != null)
            foreach (var pair in damageBonuses) copiedBonuses[pair.Key] = pair.Value;
        DamageBonuses = new ReadOnlyDictionary<int, int>(copiedBonuses);
    }

    public IReadOnlyDictionary<int, int> DamageBonuses { get; }
    public int SkillPoints { get; }
    public IReadOnlyList<WorldLearnedSkillState> Skills { get; }

    public int LevelOf(int id)
    {
        for (int index = 0; index < Skills.Count; index++)
        {
            if (Skills[index].Id == id)
            {
                return Skills[index].Level;
            }
        }

        return 0;
    }

    private static IReadOnlyList<WorldLearnedSkillState> CopySkills(
        IReadOnlyList<WorldLearnedSkillState> source)
    {
        if (source == null || source.Count == 0)
        {
            return Array.Empty<WorldLearnedSkillState>();
        }

        List<WorldLearnedSkillState> copy =
            new List<WorldLearnedSkillState>(source.Count);
        HashSet<int> ids = new HashSet<int>();
        for (int index = 0; index < source.Count; index++)
        {
            WorldLearnedSkillState row = source[index];
            if (row == null || !ids.Add(row.Id))
            {
                throw new ArgumentException(
                    "Learned skill rows must be non-null with unique ids.",
                    nameof(source));
            }

            copy.Add(row);
        }

        return new ReadOnlyCollection<WorldLearnedSkillState>(copy);
    }
}

/// <summary>
/// One immutable life-skill row from Win05.SetLifeSkills.
/// </summary>
public sealed class WorldLifeSkillState
{
    public WorldLifeSkillState(int id, int level, string displayName, bool canLearn)
    {
        Id = id;
        Level = level;
        DisplayName = displayName ?? string.Empty;
        CanLearn = canLearn;
    }

    public int Id { get; }
    public int Level { get; }
    public string DisplayName { get; }
    public bool CanLearn { get; }
    public bool Learned => Level >= 0;
}

/// <summary>
/// Exact immutable replacement projected from the official life-skill list.
/// </summary>
public sealed class WorldLifeSkillBookState
{
    public static readonly WorldLifeSkillBookState Empty =
        new WorldLifeSkillBookState(Array.Empty<WorldLifeSkillState>());

    public WorldLifeSkillBookState(IReadOnlyList<WorldLifeSkillState> skills,
        IReadOnlyCollection<int> learnableIds = null)
    {
        var eligible = new HashSet<int>();
        if (learnableIds != null)
            foreach (int id in learnableIds) eligible.Add(id);
        else if (skills != null)
            foreach (WorldLifeSkillState row in skills)
                if (row != null && row.CanLearn) eligible.Add(row.Id);
        LearnableIds = new ReadOnlyCollection<int>(new List<int>(eligible));
        if (skills == null || skills.Count == 0)
        {
            Skills = Array.Empty<WorldLifeSkillState>();
            return;
        }

        List<WorldLifeSkillState> copy = new List<WorldLifeSkillState>(skills.Count);
        HashSet<int> ids = new HashSet<int>();
        for (int index = 0; index < skills.Count; index++)
        {
            WorldLifeSkillState row = skills[index];
            if (row == null || !ids.Add(row.Id))
            {
                throw new ArgumentException(
                    "Life-skill rows must be non-null with unique ids.",
                    nameof(skills));
            }

            copy.Add(row);
        }

        Skills = new ReadOnlyCollection<WorldLifeSkillState>(copy);
    }

    public IReadOnlyList<int> LearnableIds { get; }
    public bool CanLearn(int id)
    {
        for (int i = 0; i < LearnableIds.Count; i++) if (LearnableIds[i] == id) return true;
        return false;
    }

    public IReadOnlyList<WorldLifeSkillState> Skills { get; }

    public WorldLifeSkillState Find(int id)
    {
        for (int index = 0; index < Skills.Count; index++)
        {
            if (Skills[index].Id == id)
            {
                return Skills[index];
            }
        }

        return null;
    }
}

/// <summary>
/// Stable wire identities used by the official twenty-slot world hotbar.
/// </summary>
public enum WorldHotbarSlotKind : byte
{
    CharacterSkill = 0,
    PetSkill = 1,
    Item = 2
}

/// <summary>
/// One immutable server-authored hotbar slot.
/// </summary>
public sealed class WorldHotbarSlotState
{
    public WorldHotbarSlotState(
        int index,
        int id,
        WorldHotbarSlotKind kind,
        int iconId)
    {
        if (index < 0 || index >= WorldHotbarState.SlotCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        Index = index;
        Id = id;
        Kind = kind;
        IconId = iconId;
    }

    public int Index { get; }

    public int Id { get; }

    public WorldHotbarSlotKind Kind { get; }

    public int IconId { get; }

    public bool Occupied => Id != 0;
}

/// <summary>
/// Immutable thirty-slot projection of the original primary rows and optional extension.
/// </summary>
public sealed class WorldHotbarState
{
    public const int PrimarySlotCount = 20;
    public const int SlotCount = 30;

    public static readonly WorldHotbarState Empty = new WorldHotbarState(
        CreateEmptySlots(),
        false);

    public WorldHotbarState(
        IReadOnlyList<WorldHotbarSlotState> slots,
        bool expanded)
    {
        Slots = CopySlots(slots);
        Expanded = expanded;
    }

    public IReadOnlyList<WorldHotbarSlotState> Slots { get; }

    public bool Expanded { get; }

    public WorldHotbarState WithExpanded(bool expanded)
    {
        return expanded == Expanded
            ? this
            : new WorldHotbarState(Slots, expanded);
    }

    public WorldHotbarState WithSlot(
        int index,
        int id,
        WorldHotbarSlotKind kind,
        int iconId)
    {
        if (index < 0 || index >= SlotCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        List<WorldHotbarSlotState> next =
            new List<WorldHotbarSlotState>(Slots);
        next[index] = new WorldHotbarSlotState(index, id, kind, iconId);
        return new WorldHotbarState(next, Expanded);
    }

    private static IReadOnlyList<WorldHotbarSlotState> CopySlots(
        IReadOnlyList<WorldHotbarSlotState> source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (source.Count != SlotCount)
        {
            throw new ArgumentException(
                "The world hotbar must contain exactly thirty slots.",
                nameof(source));
        }

        List<WorldHotbarSlotState> copy =
            new List<WorldHotbarSlotState>(SlotCount);
        for (int index = 0; index < SlotCount; index++)
        {
            WorldHotbarSlotState slot = source[index];
            if (slot == null || slot.Index != index)
            {
                throw new ArgumentException(
                    "World hotbar slots must be non-null and ordered by their fixed index.",
                    nameof(source));
            }

            copy.Add(slot);
        }

        return new ReadOnlyCollection<WorldHotbarSlotState>(copy);
    }

    private static IReadOnlyList<WorldHotbarSlotState> CreateEmptySlots()
    {
        WorldHotbarSlotState[] slots = new WorldHotbarSlotState[SlotCount];
        for (int index = 0; index < slots.Length; index++)
        {
            slots[index] = new WorldHotbarSlotState(
                index,
                0,
                WorldHotbarSlotKind.CharacterSkill,
                0);
        }

        return Array.AsReadOnly(slots);
    }
}

/// <summary>
/// One skill row embedded in an inventory pet brief.
/// </summary>
/// <summary>
/// One immutable row from the official Win05.SetWardrobe replacement list.
/// </summary>
public sealed class WorldWardrobeItemState
{
    public WorldWardrobeItemState(
        int itemId,
        byte category,
        bool worn,
        bool blocked,
        string displayName,
        string description)
    {
        ItemId = itemId;
        Category = category;
        Worn = worn;
        Blocked = blocked;
        DisplayName = displayName ?? string.Empty;
        Description = description ?? string.Empty;
    }

    public int ItemId { get; }
    public byte Category { get; }
    public bool Worn { get; }
    public bool Blocked { get; }
    public string DisplayName { get; }
    public string Description { get; }
}

/// <summary>
/// Complete server-authored magic-wardrobe replacement in wire order.
/// </summary>
public sealed class WorldWardrobeState : IWorldWardrobeObservation
{
    public static readonly WorldWardrobeState Empty = new WorldWardrobeState(
        0,
        Array.Empty<WorldWardrobeItemState>());

    public WorldWardrobeState(
        int capacity,
        IReadOnlyList<WorldWardrobeItemState> items)
    {
        Capacity = capacity;
        Items = CopyItems(items, capacity);
    }

    public int Capacity { get; }
    public IReadOnlyList<WorldWardrobeItemState> Items { get; }

    WorldWardrobeState IWorldWardrobeObservation.State => this;

    public WorldWardrobeItemState Find(int itemId)
    {
        for (int index = 0; index < Items.Count; index++)
        {
            if (Items[index].ItemId == itemId)
            {
                return Items[index];
            }
        }

        return null;
    }

    private static IReadOnlyList<WorldWardrobeItemState> CopyItems(
        IReadOnlyList<WorldWardrobeItemState> source,
        int capacity)
    {
        if (source == null || source.Count == 0)
        {
            return Array.Empty<WorldWardrobeItemState>();
        }
        List<WorldWardrobeItemState> copy =
            new List<WorldWardrobeItemState>(source.Count);
        for (int index = 0; index < source.Count; index++)
        {
            WorldWardrobeItemState item = source[index];
            if (item == null)
            {
                throw new ArgumentException(
                    "Wardrobe replacement contains an invalid row.",
                    nameof(source));
            }

            copy.Add(item);
        }

        return new ReadOnlyCollection<WorldWardrobeItemState>(copy);
    }
}

public sealed class WorldInventoryPetSkillState
{
    public WorldInventoryPetSkillState(string displayName, int level)
    {
        DisplayName = displayName ?? string.Empty;
        Level = level;
    }

    public string DisplayName { get; }

    public int Level { get; }
}

/// <summary>
/// Immutable PetBrief carried by an inventory item row.
/// </summary>
public sealed class WorldInventoryPetState
{
    public static WorldInventoryPetState FromPetBrief(PetBrief pet)
    {
        var skills = new List<WorldInventoryPetSkillState>(pet.skills?.Count ?? 0);
        if (pet.skills != null)
            foreach (var skill in pet.skills)
                skills.Add(new WorldInventoryPetSkillState(skill.Item1, skill.Item2));
        return new WorldInventoryPetState(pet.model, pet.slot, pet.nickname, pet.prename,
            pet.species, pet.gender, pet.habit, pet.opened, pet.level, pet.str, pet.intel,
            pet.vit, pet.agi, pet.dex, pet.spi, pet.starve, pet.trust, pet.freePts,
            pet.freeUsed, skills, pet.bound);
    }

    public WorldInventoryPetState(
        int model,
        int slot,
        string nickname,
        string prename,
        string species,
        byte gender,
        byte habit,
        byte opened,
        int level,
        int strength,
        int intelligence,
        int vitality,
        int agility,
        int dexterity,
        int spirit,
        int starvation,
        int trust,
        int freePoints,
        int usedFreePoints,
        IReadOnlyList<WorldInventoryPetSkillState> skills,
        bool bound)
    {
        Model = model;
        Slot = slot;
        Nickname = nickname ?? string.Empty;
        Prename = prename ?? string.Empty;
        Species = species ?? string.Empty;
        Gender = gender;
        Habit = habit;
        OpenedValue = opened;
        Level = level;
        Strength = strength;
        Intelligence = intelligence;
        Vitality = vitality;
        Agility = agility;
        Dexterity = dexterity;
        Spirit = spirit;
        Starvation = starvation;
        Trust = trust;
        FreePoints = freePoints;
        UsedFreePoints = usedFreePoints;
        Skills = CopySkills(skills);
        Bound = bound;
    }

    public int Model { get; }
    public int Slot { get; }
    public string Nickname { get; }
    public string Prename { get; }
    public string Species { get; }
    public byte Gender { get; }
    public byte Habit { get; }
    public byte OpenedValue { get; }
    public bool Opened => OpenedValue != 0;
    public int Level { get; }
    public int Strength { get; }
    public int Intelligence { get; }
    public int Vitality { get; }
    public int Agility { get; }
    public int Dexterity { get; }
    public int Spirit { get; }
    public int Starvation { get; }
    public int Trust { get; }
    public int FreePoints { get; }
    public int UsedFreePoints { get; }
    public IReadOnlyList<WorldInventoryPetSkillState> Skills { get; }
    public bool Bound { get; }

    private static IReadOnlyList<WorldInventoryPetSkillState> CopySkills(
        IReadOnlyList<WorldInventoryPetSkillState> source)
    {
        if (source == null || source.Count == 0)
        {
            return Array.Empty<WorldInventoryPetSkillState>();
        }

        List<WorldInventoryPetSkillState> copy =
            new List<WorldInventoryPetSkillState>(source.Count);
        for (int index = 0; index < source.Count; index++)
        {
            WorldInventoryPetSkillState skill = source[index];
            if (skill == null)
            {
                throw new ArgumentException("Inventory pet skills cannot be null.", nameof(source));
            }

            copy.Add(skill);
        }

        return new ReadOnlyCollection<WorldInventoryPetSkillState>(copy);
    }
}

/// <summary>
/// One server-authored inventory row. BagIndex is the official per-tab slot identity.
/// </summary>
public sealed class WorldInventoryItemState
{
    public WorldInventoryItemState(
        int bagIndex,
        int itemId,
        int count,
        byte tab,
        string displayName,
        string description,
        int sellPrice,
        int currentDurability,
        int maximumDurability,
        int avatar,
        bool blocked,
        bool bound,
        byte quality,
        bool cosmetic,
        int equipmentSlot,
        WorldInventoryPetState pet,
        bool locked,
        int petAiPointsUsed,
        int petAiPointsCapacity,
        int petShengPointsUsed,
        int petShengPointsCapacity,
        string fusedDescription,
        int petGrowthTier = 0,
        byte? blockedValue = null)
    {
        BagIndex = bagIndex;
        ItemId = itemId;
        Count = count;
        Tab = tab;
        DisplayName = displayName ?? string.Empty;
        Description = description ?? string.Empty;
        SellPrice = sellPrice;
        CurrentDurability = currentDurability;
        MaximumDurability = maximumDurability;
        Avatar = avatar;
        BlockedValue = blockedValue ?? (blocked ? (byte)1 : (byte)0);
        Bound = bound;
        Quality = quality;
        Cosmetic = cosmetic;
        EquipmentSlot = equipmentSlot;
        Pet = pet;
        Locked = locked;
        PetAiPointsUsed = petAiPointsUsed;
        PetAiPointsCapacity = petAiPointsCapacity;
        PetShengPointsUsed = petShengPointsUsed;
        PetShengPointsCapacity = petShengPointsCapacity;
        FusedDescription = fusedDescription ?? string.Empty;
        PetGrowthTier = petGrowthTier;
    }

    public int BagIndex { get; }
    public int ItemId { get; }
    public int Count { get; }
    public byte Tab { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public int SellPrice { get; }
    public int CurrentDurability { get; }
    public int MaximumDurability { get; }
    public int Avatar { get; }
    public int IconLookupId => Avatar > 0 ? Avatar : ItemId;
    public byte BlockedValue { get; }
    public bool Blocked => BlockedValue != 0;
    public bool Bound { get; }
    public byte Quality { get; }
    public bool Cosmetic { get; }
    public int EquipmentSlot { get; }
    public WorldInventoryPetState Pet { get; }
    public bool Locked { get; }
    public int PetAiPointsUsed { get; }
    public int PetAiPointsCapacity { get; }
    public int PetShengPointsUsed { get; }
    public int PetShengPointsCapacity { get; }
    public string FusedDescription { get; }
    public int PetGrowthTier { get; }

    public WorldInventoryItemState WithCompatibilityPatch(
        bool locked,
        int petAiPointsUsed,
        int petAiPointsCapacity,
        int petShengPointsUsed,
        int petShengPointsCapacity,
        string fusedDescription,
        int? petGrowthTier = null)
    {
        return new WorldInventoryItemState(
            BagIndex,
            ItemId,
            Count,
            Tab,
            DisplayName,
            Description,
            SellPrice,
            CurrentDurability,
            MaximumDurability,
            Avatar,
            Blocked,
            Bound,
            Quality,
            Cosmetic,
            EquipmentSlot,
            Pet,
            locked,
            petAiPointsUsed,
            petAiPointsCapacity,
            petShengPointsUsed,
            petShengPointsCapacity,
            fusedDescription,
            petGrowthTier ?? PetGrowthTier,
            BlockedValue);
    }
}

/// <summary>
/// One equipped row paired with the optional sequential quality tail.
/// </summary>
public sealed class WorldInventoryEquippedItemState
{
    public WorldInventoryEquippedItemState(
        byte slot,
        int itemId,
        string displayName,
        string description,
        int currentDurability,
        int maximumDurability,
        int avatar,
        int soulAvatar,
        byte quality)
    {
        Slot = slot;
        ItemId = itemId;
        DisplayName = displayName ?? string.Empty;
        Description = description ?? string.Empty;
        CurrentDurability = currentDurability;
        MaximumDurability = maximumDurability;
        Avatar = avatar;
        SoulAvatar = soulAvatar;
        Quality = quality;
    }

    public byte Slot { get; }
    public int ItemId { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public int CurrentDurability { get; }
    public int MaximumDurability { get; }
    public int Avatar { get; }
    public int SoulAvatar { get; }
    public byte Quality { get; }
    public int IconLookupId => Avatar > 0 ? Avatar : ItemId;

    public WorldInventoryEquippedItemState WithQuality(byte quality)
    {
        return new WorldInventoryEquippedItemState(
            Slot,
            ItemId,
            DisplayName,
            Description,
            CurrentDurability,
            MaximumDurability,
            Avatar,
            SoulAvatar,
            quality);
    }
}

/// <summary>
/// Complete immutable replacement snapshot from the official inventory downlink.
/// </summary>
public sealed class WorldInventoryItemPatch
{
    public WorldInventoryItemPatch(WorldInventoryItemState item, bool lockAuthoritative)
    {
        Item = item ?? throw new ArgumentNullException(nameof(item));
        LockAuthoritative = lockAuthoritative;
    }

    public WorldInventoryItemState Item { get; }
    public bool LockAuthoritative { get; }
}

public sealed class WorldInventoryState
{
    private readonly object publicationIdentity = new object();
    private readonly object inventoryPatchBaseIdentity;
    private readonly bool hasInventoryPatches;
    private readonly bool hasEquipmentPatches;

    public static readonly WorldInventoryState Empty = new WorldInventoryState(
        0,
        0,
        0,
        0,
        0,
        Array.Empty<WorldInventoryItemState>(),
        Array.Empty<WorldInventoryEquippedItemState>());

    public WorldInventoryState(
        long money,
        long caiYu,
        long honor,
        int weight,
        int maximumWeight,
        IReadOnlyList<WorldInventoryItemState> items,
        IReadOnlyList<WorldInventoryEquippedItemState> equippedItems)
        : this(money, caiYu, honor, weight, maximumWeight, items, equippedItems, null, null, null, null, null) { }

    private WorldInventoryState(
        long money, long caiYu, long honor, int weight, int maximumWeight,
        IReadOnlyList<WorldInventoryItemState> items,
        IReadOnlyList<WorldInventoryEquippedItemState> equippedItems,
        IReadOnlyList<WorldInventoryEquippedItemState> equipmentSnapshotItems,
        IReadOnlyList<WorldInventoryItemState> inventorySnapshotItems,
        IReadOnlyList<WorldInventoryItemPatch> inventoryPatches,
        IReadOnlyList<WorldInventoryEquippedItemState> equipmentPatches,
        object inventoryPatchBaseIdentity)
    {
        Money = money;
        CaiYu = caiYu;
        Honor = honor;
        Weight = weight;
        MaximumWeight = maximumWeight;
        Items = CopyItems(items);
        EquippedItems = CopyEquipped(equippedItems);
        InventorySnapshotItems = inventorySnapshotItems ?? Items;
        EquipmentSnapshotItems = equipmentSnapshotItems ??
            new ReadOnlyCollection<WorldInventoryEquippedItemState>(new List<WorldInventoryEquippedItemState>(EquippedItems));
        hasInventoryPatches = inventoryPatches != null;
        InventoryPatches = CopyInventoryPatches(inventoryPatches);
        hasEquipmentPatches = equipmentPatches != null;
        EquipmentPatches = CopyEquipped(equipmentPatches);
        this.inventoryPatchBaseIdentity = inventoryPatchBaseIdentity;
    }

    public long Money { get; }
    public long CaiYu { get; }
    public long Honor { get; }
    public int Weight { get; }
    public int MaximumWeight { get; }
    public IReadOnlyList<WorldInventoryItemState> Items { get; }
    // Original bag contents of the last full message, not the subsequent merged delta view.
    public IReadOnlyList<WorldInventoryItemState> InventorySnapshotItems { get; }
    public IReadOnlyList<WorldInventoryItemPatch> InventoryPatches { get; }
    public IReadOnlyList<WorldInventoryEquippedItemState> EquipmentPatches { get; }
    public IReadOnlyList<WorldInventoryEquippedItemState> EquippedItems { get; }
    // The last full equipment message is retained through incremental UI patches.
    public IReadOnlyList<WorldInventoryEquippedItemState> EquipmentSnapshotItems { get; }

    public WorldInventoryState WithDelta(
        long money, long caiYu, long honor, int weight, int maximumWeight,
        IReadOnlyList<WorldInventoryItemState> items,
        IReadOnlyList<WorldInventoryEquippedItemState> equippedItems,
        IReadOnlyList<WorldInventoryItemPatch> inventoryPatches = null,
        IReadOnlyList<WorldInventoryEquippedItemState> equipmentPatches = null) =>
        new WorldInventoryState(money, caiYu, honor, weight, maximumWeight, items, equippedItems,
            EquipmentSnapshotItems, InventorySnapshotItems, inventoryPatches, equipmentPatches, publicationIdentity);

    // Identity objects retain no preceding snapshot/history. Missing patch provenance is
    // different from a decoded delta containing zero rows.
    public bool IsInventoryDeltaAfter(WorldInventoryState previous) =>
        hasInventoryPatches && previous != null &&
        ReferenceEquals(inventoryPatchBaseIdentity, previous.publicationIdentity);

    public bool IsEquipmentDeltaAfter(WorldInventoryState previous) =>
        hasEquipmentPatches && previous != null &&
        ReferenceEquals(inventoryPatchBaseIdentity, previous.publicationIdentity);

    private static IReadOnlyList<WorldInventoryItemPatch> CopyInventoryPatches(
        IReadOnlyList<WorldInventoryItemPatch> patches)
    {
        if (patches == null || patches.Count == 0) return Array.Empty<WorldInventoryItemPatch>();
        var copy = new List<WorldInventoryItemPatch>(patches.Count);
        foreach (WorldInventoryItemPatch patch in patches)
        {
            if (patch == null) throw new ArgumentException("Inventory patches cannot be null.", nameof(patches));
            copy.Add(patch);
        }
        return new ReadOnlyCollection<WorldInventoryItemPatch>(copy);
    }

    public WorldInventoryItemState ItemAt(byte tab, int bagIndex)
    {
        for (int index = 0; index < Items.Count; index++)
        {
            WorldInventoryItemState item = Items[index];
            if (item.Tab == tab && item.BagIndex == bagIndex)
            {
                return item;
            }
        }

        return null;
    }

    private static IReadOnlyList<WorldInventoryItemState> CopyItems(
        IReadOnlyList<WorldInventoryItemState> source)
    {
        if (source == null || source.Count == 0)
        {
            return Array.Empty<WorldInventoryItemState>();
        }

        List<WorldInventoryItemState> copy = new List<WorldInventoryItemState>(source.Count);
        for (int index = 0; index < source.Count; index++)
        {
            WorldInventoryItemState item = source[index];
            if (item == null)
            {
                throw new ArgumentException("Inventory items cannot be null.", nameof(source));
            }

            copy.Add(item);
        }

        return new ReadOnlyCollection<WorldInventoryItemState>(copy);
    }

    private static IReadOnlyList<WorldInventoryEquippedItemState> CopyEquipped(
        IReadOnlyList<WorldInventoryEquippedItemState> source)
    {
        if (source == null || source.Count == 0)
        {
            return Array.Empty<WorldInventoryEquippedItemState>();
        }

        List<WorldInventoryEquippedItemState> copy =
            new List<WorldInventoryEquippedItemState>(source.Count);
        for (int index = 0; index < source.Count; index++)
        {
            WorldInventoryEquippedItemState item = source[index];
            if (item == null)
            {
                throw new ArgumentException(
                    "Equipped inventory items cannot be null.",
                    nameof(source));
            }

            copy.Add(item);
        }

        return new ReadOnlyCollection<WorldInventoryEquippedItemState>(copy);
    }
}

/// <summary>
/// One immutable row from the server-owned in-world system-request queue.
/// </summary>
public sealed class WorldSystemRequestState
{
    public WorldSystemRequestState(
        int requestId,
        byte kind,
        int fromEntityId,
        string fromName,
        string text)
    {
        RequestId = requestId;
        Kind = kind;
        FromEntityId = fromEntityId;
        FromName = fromName ?? string.Empty;
        Text = text ?? string.Empty;
    }

    public int RequestId { get; }

    public byte Kind { get; }

    public int FromEntityId { get; }

    public string FromName { get; }

    public string Text { get; }
}

/// <summary>
/// One server-authored row in either half of the active trade offer.
/// </summary>
public sealed class WorldTradeItemState
{
    public WorldTradeItemState(int itemId, int count, string displayName)
    {
        ItemId = itemId;
        Count = count;
        DisplayName = displayName ?? string.Empty;
    }

    public int ItemId { get; }

    public int Count { get; }

    public string DisplayName { get; }
}

/// <summary>
/// Optional display tail paired by index with a trade item row.
/// </summary>
public sealed class WorldTradeItemInfoState
{
    public WorldTradeItemInfoState(string text, byte quality)
    {
        Text = text ?? string.Empty;
        Quality = quality;
    }

    public string Text { get; }

    public byte Quality { get; }
}

/// <summary>Pet detail and potential points for one indexed trade offer row.</summary>
public sealed class WorldTradePetState
{
    public WorldTradePetState(WorldInventoryPetState pet, int aiUsed = 0, int aiCapacity = 0,
        int shengUsed = 0, int shengCapacity = 0)
    {
        Pet = pet ?? throw new ArgumentNullException(nameof(pet));
        AiUsed = aiUsed;
        AiCapacity = aiCapacity;
        ShengUsed = shengUsed;
        ShengCapacity = shengCapacity;
    }

    public WorldInventoryPetState Pet { get; }
    public int AiUsed { get; }
    public int AiCapacity { get; }
    public int ShengUsed { get; }
    public int ShengCapacity { get; }
}

/// <summary>
/// Immutable replacement snapshot owned by the official trade-open/state/end downlinks.
/// </summary>
public sealed class WorldActiveTradeState
{
    public static readonly WorldActiveTradeState Empty = new WorldActiveTradeState(
        false,
        0,
        string.Empty,
        false,
        false,
        false,
        false,
        0,
        0,
        Array.Empty<WorldTradeItemState>(),
        Array.Empty<WorldTradeItemState>(),
        Array.Empty<WorldTradeItemInfoState>(),
        Array.Empty<WorldTradeItemInfoState>());

    public WorldActiveTradeState(
        bool active,
        int otherEntityId,
        string otherName,
        bool myLocked,
        bool otherLocked,
        bool myConfirmed,
        bool otherConfirmed,
        long myMoney,
        long otherMoney,
        IReadOnlyList<WorldTradeItemState> myItems,
        IReadOnlyList<WorldTradeItemState> otherItems,
        IReadOnlyList<WorldTradeItemInfoState> myItemInfo,
        IReadOnlyList<WorldTradeItemInfoState> otherItemInfo,
        IReadOnlyDictionary<int, WorldTradePetState> myPets = null,
        IReadOnlyDictionary<int, WorldTradePetState> otherPets = null)
    {
        Active = active;
        OtherEntityId = otherEntityId;
        OtherName = otherName ?? string.Empty;
        MyLocked = myLocked;
        OtherLocked = otherLocked;
        MyConfirmed = myConfirmed;
        OtherConfirmed = otherConfirmed;
        MyMoney = myMoney;
        OtherMoney = otherMoney;
        MyItems = CopyRows(myItems, nameof(myItems));
        OtherItems = CopyRows(otherItems, nameof(otherItems));
        MyItemInfo = CopyInfo(myItemInfo, nameof(myItemInfo));
        OtherItemInfo = CopyInfo(otherItemInfo, nameof(otherItemInfo));
        MyPets = CopyPets(myPets);
        OtherPets = CopyPets(otherPets);
    }

    public bool Active { get; }

    public int OtherEntityId { get; }

    public string OtherName { get; }

    public bool MyLocked { get; }

    public bool OtherLocked { get; }

    public bool MyConfirmed { get; }

    public bool OtherConfirmed { get; }

    public long MyMoney { get; }

    public long OtherMoney { get; }

    public IReadOnlyList<WorldTradeItemState> MyItems { get; }

    public IReadOnlyList<WorldTradeItemState> OtherItems { get; }

    public IReadOnlyList<WorldTradeItemInfoState> MyItemInfo { get; }

    public IReadOnlyList<WorldTradeItemInfoState> OtherItemInfo { get; }

    public IReadOnlyDictionary<int, WorldTradePetState> MyPets { get; }

    public IReadOnlyDictionary<int, WorldTradePetState> OtherPets { get; }

    private static IReadOnlyDictionary<int, WorldTradePetState> CopyPets(
        IReadOnlyDictionary<int, WorldTradePetState> source)
    {
        var copy = new Dictionary<int, WorldTradePetState>();
        if (source != null)
            foreach (var row in source)
                copy.Add(row.Key, row.Value ?? throw new ArgumentException("Trade pet cannot be null."));
        return new ReadOnlyDictionary<int, WorldTradePetState>(copy);
    }

    private static IReadOnlyList<WorldTradeItemState> CopyRows(
        IReadOnlyList<WorldTradeItemState> source,
        string parameterName)
    {
        if (source == null || source.Count == 0)
        {
            return Array.Empty<WorldTradeItemState>();
        }

        List<WorldTradeItemState> copy = new List<WorldTradeItemState>(source.Count);
        for (int index = 0; index < source.Count; index++)
        {
            WorldTradeItemState row = source[index];
            if (row == null)
            {
                throw new ArgumentException("Trade item rows cannot be null.", parameterName);
            }

            copy.Add(row);
        }

        return new ReadOnlyCollection<WorldTradeItemState>(copy);
    }

    private static IReadOnlyList<WorldTradeItemInfoState> CopyInfo(
        IReadOnlyList<WorldTradeItemInfoState> source,
        string parameterName)
    {
        if (source == null || source.Count == 0)
        {
            return Array.Empty<WorldTradeItemInfoState>();
        }

        List<WorldTradeItemInfoState> copy =
            new List<WorldTradeItemInfoState>(source.Count);
        for (int index = 0; index < source.Count; index++)
        {
            WorldTradeItemInfoState row = source[index];
            if (row == null)
            {
                throw new ArgumentException("Trade item-info rows cannot be null.", parameterName);
            }

            copy.Add(row);
        }

        return new ReadOnlyCollection<WorldTradeItemInfoState>(copy);
    }
}

/// <summary>
/// Immutable server-authored trade-end feedback edge routed to the official chat surface.
/// </summary>
public sealed class WorldTradeEndState
{
    public WorldTradeEndState(byte colorSelector, string text)
    {
        ColorSelector = colorSelector;
        Text = text ?? string.Empty;
    }

    public byte ColorSelector { get; }

    public string Text { get; }
}

public sealed class CharacterCreationRequest
{
    public CharacterCreationRequest(
        string displayName,
        int race,
        int gender,
        int head,
        int hair)
    {
        string normalizedName = (displayName ?? string.Empty).Trim();
        if (normalizedName.Length == 0 || normalizedName.Length > 30)
        {
            throw new ArgumentException(
                "Character name must contain between 1 and 30 characters.",
                nameof(displayName));
        }

        if (race < 0 || race > 4)
        {
            throw new ArgumentOutOfRangeException(nameof(race));
        }

        if (gender < 0 || gender > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(gender));
        }

        if (head < 1 || head > 8)
        {
            throw new ArgumentOutOfRangeException(nameof(head));
        }

        if (hair < 1 || hair > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(hair));
        }

        DisplayName = normalizedName;
        Race = race;
        Gender = gender;
        Head = head;
        Hair = hair;
    }

    public string DisplayName { get; }

    public int Race { get; }

    public int Gender { get; }

    public int Head { get; }

    public int Hair { get; }
}

/// <summary>
/// Complete server-authored CharData title replacement from the official title-snapshot edge.
/// </summary>
public sealed class WorldCharacterTitleState
{
    public static readonly WorldCharacterTitleState Empty =
        new WorldCharacterTitleState(string.Empty, Array.Empty<string>());

    public WorldCharacterTitleState(
        string currentTitle,
        IReadOnlyList<string> ownedTitles)
    {
        CurrentTitle = currentTitle ?? string.Empty;
        if (ownedTitles == null || ownedTitles.Count == 0)
        {
            OwnedTitles = Array.Empty<string>();
            return;
        }

        string[] copy = new string[ownedTitles.Count];
        for (int index = 0; index < copy.Length; index++)
        {
            copy[index] = ownedTitles[index] ?? string.Empty;
        }
        OwnedTitles = Array.AsReadOnly(copy);
    }

    public string CurrentTitle { get; }

    public IReadOnlyList<string> OwnedTitles { get; }

    public bool Owns(string title)
    {
        if (string.IsNullOrEmpty(title))
        {
            return true;
        }
        for (int index = 0; index < OwnedTitles.Count; index++)
        {
            if (string.Equals(OwnedTitles[index], title, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }
}

public interface IGameServerGateway : IDisposable, IPartyCommands, IFamilyCommands, IFriendCommands, IWarehouseCommands, IMailCommands, IDuelCommands, IBlockCommands, IRackCommands, IStallCommands, IShopCommands, IRepairCommands, IHistoryCommands, IItemUseCommands, IItemToolCommands, IItemLockCommands, ICaptureCommands, IFurnaceCommands, IEquipmentFusionCommands, IAvatarFusionCommands, IAttributeWashCommands, IHotbarWriteCommands, IPetForgeCommands, IGroundPickupCommands, INewbieTipCommands, IWorldReturnCommands, ICharacterWorkCommands, ICharacterReviveCommands, ICharacterSettingsCommands, IRidingCommands, IApprenticeCommands, IArenaReviveCommands, IArenaDraftCommands, IAccountRegistrationCommands, IAccountRecoveryCommands, IAccountAppealCommands
{
    bool Connected { get; }

    void Pump();

    IReadOnlyList<WorldTeleportState> TeleportStates { get; }

    long TeleportStatesVersion { get; }

    WorldPlayerStatusState PlayerStatusState { get; }

    long PlayerStatusStateVersion { get; }

    WorldPlayerAppearanceState PlayerAppearanceState { get; }

    long PlayerAppearanceStateVersion { get; }

    WorldCharacterTitleState CharacterTitleState { get; }

    long CharacterTitleStateVersion { get; }

    WorldLocalPlayerLifeState LocalPlayerLifeState { get; }

    long LocalPlayerLifeStateVersion { get; }

    IReadOnlyList<WorldEntityStatusEffect> LocalPlayerStatusEffects { get; }

    long LocalPlayerStatusEffectsVersion { get; }

    IReadOnlyList<WorldSkillHitEffect> SkillHitEffects { get; }

    long SkillHitEffectsVersion { get; }

    WorldSelfPkState SelfPkState { get; }

    long SelfPkStateVersion { get; }

    WorldLocalFactionState LocalFactionState { get; }

    long LocalFactionStateVersion { get; }

    WorldRidingState LocalRidingState { get; }

    long LocalRidingStateVersion { get; }

    IReadOnlyDictionary<int, WorldRemoteRidingState> RemoteRidingStates { get; }

    long RemoteRidingStatesVersion { get; }

    IReadOnlyDictionary<int, WorldRemotePlayerState> RemotePlayers { get; }

    long RemotePlayersVersion { get; }

    IReadOnlyDictionary<int, WorldNpcState> Npcs { get; }

    long NpcsVersion { get; }

    WorldNpcChatterState NpcChatterState { get; }

    long NpcChatterStateVersion { get; }

    WorldNpcTaskOffersState NpcTaskOffersState { get; }

    long NpcTaskOffersStateVersion { get; }

    WorldShopState ShopState { get; }

    long ShopStateVersion { get; }

    WorldTransportState TransportState { get; }

    long TransportStateVersion { get; }

    WorldWarehouseState WarehouseState { get; }

    long WarehouseStateVersion { get; }

    WorldTaskLogState TaskLogState { get; }

    long TaskLogStateVersion { get; }

    WorldNpcDialogTextState NpcDialogTextState { get; }

    long NpcDialogTextStateVersion { get; }

    WorldNpcDialogTextEdgeWindow NpcDialogTextEdgeWindow { get; }

    IReadOnlyDictionary<int, WorldEntityState> WorldEntities { get; }

    long WorldEntitiesVersion { get; }

    WorldEntityPublicationState WorldEntityPublicationState { get; }

    WorldEntitySceneSnapshot WorldEntitySceneSnapshot { get; }

    IReadOnlyDictionary<int, WorldGroundItemState> GroundItems { get; }

    long GroundItemsVersion { get; }

    WorldGroundLootAuthorityState GroundLootAuthorityState { get; }

    IReadOnlyList<WorldCombatResultState> CombatResults { get; }

    long CombatResultsVersion { get; }

    IReadOnlyList<WorldCombatNumberState> CombatNumbers { get; }

    long CombatNumbersVersion { get; }

    WorldTargetStatusState TargetStatusState { get; }

    long TargetStatusStateVersion { get; }

    WorldBuffState BuffState { get; }

    long BuffStateVersion { get; }

    WorldInspectedPlayerEquipmentState InspectedPlayerEquipmentState { get; }

    long InspectedPlayerEquipmentStateVersion { get; }

    IReadOnlyDictionary<int, WorldRemotePathState> RemotePaths { get; }

    long RemotePathsVersion { get; }

    IReadOnlyDictionary<int, WorldRemotePathState> MonsterPaths { get; }

    long MonsterPathsVersion { get; }

    IReadOnlyDictionary<int, WorldRemoteActionState> RemoteActions { get; }

    long RemoteActionsVersion { get; }

    WorldSkillCastState SkillCastState { get; }

    IReadOnlyList<WorldSkillCastState> SkillCastEvents { get; }

    long SkillCastStateVersion { get; }

    IReadOnlyDictionary<int, WorldRemoteChatBubbleState> RemoteChatBubbles { get; }

    long RemoteChatBubblesVersion { get; }

    IReadOnlyList<WorldChannelChatState> ChannelChatRows { get; }

    long ChannelChatRowsVersion { get; }

    IReadOnlyList<WorldPrivateChatState> PrivateChatRows { get; }

    long PrivateChatRowsVersion { get; }

    WorldPartyState PartyState { get; }

    long PartyStateVersion { get; }

    IReadOnlyDictionary<int, WorldStallOwnerState> StallOwners { get; }

    long StallOwnersVersion { get; }

    IReadOnlyList<WorldSystemRequestState> SystemRequests { get; }

    long SystemRequestsVersion { get; }

    WorldInventoryState InventoryState { get; }

    long InventoryStateVersion { get; }

    WorldWardrobeState WardrobeState { get; }

    long WardrobeStateVersion { get; }

    WorldHotbarState HotbarState { get; }

    long HotbarStateVersion { get; }

    WorldSkillBookState SkillBookState { get; }

    long SkillBookStateVersion { get; }

    WorldLifeSkillBookState LifeSkillBookState { get; }

    long LifeSkillBookStateVersion { get; }

    WorldActiveTradeState ActiveTradeState { get; }

    long ActiveTradeStateVersion { get; }

    WorldTradeEndState TradeEndState { get; }

    long TradeEndStateVersion { get; }

    // A successful value identifies the admitted gateway connection's existing lifetime.
    // Login carries it across awaits; cancellation is host lifetime, never a native success.
    Task<Result<CancellationToken>> ConnectAsync(ServerEndpoint endpoint, CancellationToken cancellationToken);

    Task<Result> AuthenticateAsync(LoginCredentials credentials, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<ServerCharacterRecord>>> GetCharacterRosterAsync(
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<ServerCharacterRecord>>> CreateCharacterAsync(
        CharacterCreationRequest request,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<ServerCharacterRecord>>> DeleteCharacterAsync(
        CharacterId characterId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Calls the existing void 0x107d <c>SrvStatReq</c> sender on the title host stack.
    /// Return is not delivery or reply success: native Send/PaceTx own connected/failure and
    /// queueing behavior. The 0x805b dispatch case independently publishes OnlineCount.
    /// </summary>
    void RequestServerOnlineCount();

    Task<Result> QueryTargetStatusAsync(
        int entityId,
        CancellationToken cancellationToken);

    Task<Result> RequestNpcTaskOffersAsync(
        string npcName,
        CancellationToken cancellationToken);

    Task<Result> OpenShopAsync(int shopId, CancellationToken cancellationToken);

    Task<Result> BuyShopItemAsync(
        int itemId,
        int count,
        CancellationToken cancellationToken);

    Task<Result> SellInventoryItemAsync(
        byte tab,
        int bagIndex,
        int count,
        int itemId,
        bool confirmed,
        CancellationToken cancellationToken);

    Task<Result> OpenTransportAsync(
        int transportList,
        CancellationToken cancellationToken);

    Task<Result> ChooseTransportAsync(
        int transportList,
        int destinationIndex,
        CancellationToken cancellationToken);

    Task<Result> OpenWarehouseAsync(CancellationToken cancellationToken);

    Task<Result> MoveWarehouseItemAsync(
        byte direction,
        byte fromTab,
        int fromSlot,
        byte warehouseTab,
        int count,
        int toSlot,
        CancellationToken cancellationToken);

    Task<Result> AcceptTaskAsync(
        int taskId,
        CancellationToken cancellationToken);

    Task<Result> AbandonTaskAsync(
        int taskId,
        CancellationToken cancellationToken);

    Task<Result> CompleteTaskAsync(
        int taskId,
        CancellationToken cancellationToken);

    Task<Result> InspectPlayerAsync(
        int entityId,
        CancellationToken cancellationToken);

    Task<Result> RequestTradeAsync(
        int entityId,
        CancellationToken cancellationToken);

    Task<Result> RespondSystemRequestAsync(
        int requestId,
        bool accept,
        CancellationToken cancellationToken);

    Task<Result> RespondSystemRequestAsync(
        int requestId,
        byte accept,
        CancellationToken cancellationToken);

    Task<Result> UseInventoryItemAsync(
        int itemId,
        byte tab,
        int bagIndex,
        CancellationToken cancellationToken);

    Task<Result> SaveHotbarAsync(
        WorldHotbarState state,
        CancellationToken cancellationToken);

    Task<Result> AttackAsync(
        int targetEntityId,
        CancellationToken cancellationToken);

    Task<Result> PickUpGroundItemAsync(
        int groundId,
        CancellationToken cancellationToken);

    Task<Result> ReportPlayerMoveAsync(
        int x,
        int y,
        CancellationToken cancellationToken);

    Task<Result> SavePlayerPositionAsync(
        int mapId,
        int x,
        int y,
        bool entered,
        CancellationToken cancellationToken);

    Task<Result> RequestReviveAsync(
        WorldReviveOption option,
        CancellationToken cancellationToken);

    Task<Result> SetPlayerSittingAsync(bool sitting, CancellationToken cancellationToken);

    Task<Result> EndCharacterWorkAsync(CancellationToken cancellationToken);

    Task<Result> UseCharacterSkillAsync(
        int skillId,
        int targetEntityId,
        CancellationToken cancellationToken);

    Task<Result> UseCharacterSkillAtAsync(
        int skillId,
        int x,
        int y,
        int targetEntityId,
        CancellationToken cancellationToken);

    Task<Result> UsePetSkillAsync(
        int skillId,
        CancellationToken cancellationToken);

    // The argument is the official attribute code itself (1 STR, 3 INT, 5 VIT, 7 AGI, 9 DEX,
    // 61 SPI), i.e. the first Int32 field of the six rows Win05.DrawPet 0x06001043 builds for its
    // six "+" buttons (arm64 0x3b9dc0-0x3b9e10); NetClient.PetAddPoint 0x06000734 forwards it to
    // PetCmd unchanged, so no product enum stands between the hit site and the wire.
    Task<Result> AllocatePetPointAsync(
        int attributeCode,
        CancellationToken cancellationToken);

    Task<Result> RenamePetAsync(
        string name,
        CancellationToken cancellationToken);

    Task<Result> SetPetSkillAsync(
        int skillId,
        CancellationToken cancellationToken);

    Task<Result> SwitchPetAsync(
        int slot,
        CancellationToken cancellationToken);

    Task<Result> RecallPetAsync(
        int slot,
        CancellationToken cancellationToken);

    Task<Result> DeployPetAsync(
        int slot,
        CancellationToken cancellationToken);

    Task<Result> HatchPetAsync(
        int slot,
        CancellationToken cancellationToken);

    Task<Result> SetPetShownAsync(
        bool show,
        CancellationToken cancellationToken);

    Task<Result> SwapPetAsync(
        int fromSlot,
        int toSlot,
        CancellationToken cancellationToken);

    Task<Result> SetPetViewAsync(
        int mask,
        CancellationToken cancellationToken);

    Task<Result> SetCharacterTitleAsync(
        string title,
        CancellationToken cancellationToken);

    Task<Result> SetEquipmentEffectsMaskAsync(
        int hideMask,
        CancellationToken cancellationToken);

    // The native callback reads the live mask only when its supplied send buffer is written.
    Task<Result> SetEquipmentEffectsMaskAsync(
        Func<int> readHideMask,
        CancellationToken cancellationToken);

    Task<Result> StoreWardrobeItemAsync(
        byte bagTab,
        int bagSlot,
        CancellationToken cancellationToken);

    Task<Result> ToggleWardrobeItemAsync(
        int itemId,
        CancellationToken cancellationToken);

    Task<Result> ExtractWardrobeItemAsync(
        int itemId,
        CancellationToken cancellationToken);

    Task<Result> MoveWardrobeItemAsync(
        int itemId,
        int categoryIndex,
        CancellationToken cancellationToken);

    Task<Result> UpgradeSkillAsync(
        int skillId,
        CancellationToken cancellationToken);

    Task<Result> LearnLifeSkillAsync(
        int skillId,
        CancellationToken cancellationToken);

    Task<Result> UseCaptureInventoryItemAsync(
        int targetEntityId,
        int itemId,
        CancellationToken cancellationToken);

    Task<Result> UseInventoryToolOnAsync(
        int toolId,
        byte targetKind,
        byte tab,
        int slot,
        CancellationToken cancellationToken);

    Task<Result> SplitInventoryItemAsync(
        int tab,
        int bagIndex,
        int count,
        CancellationToken cancellationToken);

    Task<Result> EquipInventoryItemAsync(
        int bagIndex,
        CancellationToken cancellationToken);

    Task<Result> UnequipInventoryItemAsync(
        int slot,
        CancellationToken cancellationToken);

    // The seven deltas travel as one int[] because that is the official
    // NetClient.AllocPoints 0x0600071a argument type: the recovered member captures the array by
    // reference, so the writer reads the caller's own array inside NetClient.Send, after the
    // connection gate, with no read-only view in between.
    Task<Result> AllocateAttributePointsAsync(
        int[] points,
        CancellationToken cancellationToken);

    Task<Result> SortInventoryAsync(
        int tab,
        CancellationToken cancellationToken);

    // The parameter is the official NetClient.TradeOffer (0x06000751) argument type
    // List<ValueTuple<byte,int,int>> (tab, slot, count). It stays a real List on this path so the
    // official writer 0x0600084d enumerates it with the List<T> struct enumerator; the window's
    // own projection is the only copy and no ReadOnly view is introduced here.
    Task<Result> SubmitTradeOfferAsync(
        long money,
        List<(byte Tab, int Slot, int Count)> items,
        CancellationToken cancellationToken);

    Task<Result> SetTradeLockAsync(bool locked, CancellationToken cancellationToken);

    Task<Result> ConfirmTradeAsync(CancellationToken cancellationToken);

    Task<Result> CancelTradeAsync(CancellationToken cancellationToken);

    Task<Result> SendWorldChatAsync(
        WorldChatSendRequest request,
        CancellationToken cancellationToken);

    Task<Result<WorldEntryRecord>> EnterWorldAsync(
        CharacterId characterId,
        int rosterLevel,
        CancellationToken cancellationToken);
}

public sealed class SessionStateChanged : IApplicationEvent
{
    public SessionStateChanged(ClientSessionState previous, ClientSessionState current, string reasonCode)
    {
        Previous = previous;
        Current = current;
        ReasonCode = reasonCode ?? string.Empty;
    }

    public ClientSessionState Previous { get; }

    public ClientSessionState Current { get; }

    public string ReasonCode { get; }
}
}
