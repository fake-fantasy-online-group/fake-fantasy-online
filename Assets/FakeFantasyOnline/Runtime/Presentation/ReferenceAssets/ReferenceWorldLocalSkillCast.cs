using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Hash-locked runtime projection of SkillEffectDB.IsPhysical and the narrow Win05 skill fields
/// used by the official local skill-cast facing branch.
/// </summary>
public sealed class ReferenceSkillEffectCatalog : IDisposable
{
    public const string SkillShowRuntimePath = "tables/skillshow.json";
    public const string SkillsRuntimePath = "tables/skills.json";
    public const int ExpectedSkillShowRows = 675;
    public const int ExpectedPhysicalRows = 230;
    public const int ExpectedSkillRows = 222;

    private readonly HashSet<int> physicalSkillIds;
    private readonly Dictionary<int, SkillAimRule> aimRules;
    private bool disposed;

    private ReferenceSkillEffectCatalog(
        HashSet<int> physicalSkillIds,
        Dictionary<int, SkillAimRule> aimRules)
    {
        this.physicalSkillIds = physicalSkillIds;
        this.aimRules = aimRules;
        Ready = true;
    }

    public bool Ready { get; private set; }

    internal int SkillShowRowCount { get; private set; }

    internal int PhysicalRowCount => physicalSkillIds.Count;

    internal int SkillRowCount => aimRules.Count;

    public static bool TryCreate(
        ReferenceResourceLoader resources,
        out ReferenceSkillEffectCatalog owner)
    {
        owner = null;
        if (resources == null)
        {
            return false;
        }

        byte[] skillShowBytes = resources.ReadSourceOwned(SkillShowRuntimePath);
        byte[] skillsBytes = resources.ReadSourceOwned(SkillsRuntimePath);
        if (skillShowBytes == null || skillShowBytes.Length == 0 ||
            skillsBytes == null || skillsBytes.Length == 0)
        {
            return false;
        }

        SkillShowTable skillShow;
        SkillTable skills;
        try
        {
            skillShow = JsonUtility.FromJson<SkillShowTable>(
                Encoding.UTF8.GetString(skillShowBytes));
            skills = JsonUtility.FromJson<SkillTable>(
                Encoding.UTF8.GetString(skillsBytes));
        }
        catch (ArgumentException)
        {
            return false;
        }

        HashSet<int> physical;
        Dictionary<int, SkillAimRule> rules;
        int skillShowRows;
        if (!TryValidate(skillShow, skills, out physical, out rules, out skillShowRows))
        {
            return false;
        }

        owner = new ReferenceSkillEffectCatalog(physical, rules)
        {
            SkillShowRowCount = skillShowRows
        };
        return true;
    }

    /// <summary>
    /// Exact SkillEffectDB.IsPhysical semantics: ids absent from skillshow are non-physical.
    /// </summary>
    public bool IsPhysical(int skillId)
    {
        ThrowIfDisposed();
        return Ready && physicalSkillIds.Contains(skillId);
    }

    /// <summary>
    /// The official preparation branch aims at a resolved target only for an enemy-capable
    /// direct/cast-kind-one skill. Explicit aim coordinates are handled before this rule.
    /// </summary>
    public bool ShouldAimAtResolvedTarget(int skillId)
    {
        ThrowIfDisposed();
        SkillAimRule rule;
        return Ready && aimRules.TryGetValue(skillId, out rule) &&
            rule.CanEnemy != 0 && (rule.TargetKind == 1 || rule.CastKind == 1);
    }

    /// <summary>
    /// Applies the source Win05 target permissions before the mode-zero UseSkill writer receives
    /// an entity id. Zero retains the official self/center meaning.
    /// </summary>
    public int ResolveImmediateTargetEntityId(
        int skillId,
        int selectedEntityId,
        bool selectedMonster,
        bool selectedFriendlyPlayer)
    {
        ThrowIfDisposed();
        SkillAimRule rule;
        if (!Ready || selectedEntityId <= 0 ||
            !aimRules.TryGetValue(skillId, out rule))
        {
            return 0;
        }

        if (selectedMonster)
        {
            return rule.CanEnemy != 0 ? selectedEntityId : 0;
        }

        return selectedFriendlyPlayer
            ? rule.CanFriendly != 0 ? selectedEntityId : 0
            : rule.CanEnemy != 0 ? selectedEntityId : 0;
    }

    internal bool TryGetAimRuleForAudit(
        int skillId,
        out int castKind,
        out int targetKind,
        out int shape,
        out int distance,
        out int canEnemy)
    {
        ThrowIfDisposed();
        SkillAimRule rule;
        if (!Ready || !aimRules.TryGetValue(skillId, out rule))
        {
            castKind = 0;
            targetKind = 0;
            shape = 0;
            distance = 0;
            canEnemy = 0;
            return false;
        }

        castKind = rule.CastKind;
        targetKind = rule.TargetKind;
        shape = rule.Shape;
        distance = rule.Distance;
        canEnemy = rule.CanEnemy;
        return true;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        physicalSkillIds.Clear();
        aimRules.Clear();
        Ready = false;
        disposed = true;
    }

    private static bool TryValidate(
        SkillShowTable skillShow,
        SkillTable skills,
        out HashSet<int> physical,
        out Dictionary<int, SkillAimRule> rules,
        out int skillShowRows)
    {
        physical = null;
        rules = null;
        skillShowRows = 0;
        if (skillShow == null || skillShow.shows == null ||
            skillShow.shows.Length != ExpectedSkillShowRows ||
            skills == null || skills.skills == null ||
            skills.skills.Length != ExpectedSkillRows)
        {
            return false;
        }

        HashSet<int> seenShows = new HashSet<int>();
        HashSet<int> parsedPhysical = new HashSet<int>();
        for (int index = 0; index < skillShow.shows.Length; index++)
        {
            SkillShowRow row = skillShow.shows[index];
            if (row == null || row.id <= 0 || (row.phy != 0 && row.phy != 1) ||
                !seenShows.Add(row.id))
            {
                return false;
            }

            if (row.phy == 1)
            {
                parsedPhysical.Add(row.id);
            }
        }

        if (parsedPhysical.Count != ExpectedPhysicalRows)
        {
            return false;
        }

        Dictionary<int, SkillAimRule> parsedRules =
            new Dictionary<int, SkillAimRule>();
        for (int index = 0; index < skills.skills.Length; index++)
        {
            SkillRow row = skills.skills[index];
            if (row == null || row.id <= 0 || parsedRules.ContainsKey(row.id))
            {
                return false;
            }

            if (NativeSwordSkillTargeting.TryGet(row.id, out var original))
            {
                if (!original.SelfCentered)
                {
                    row.castkind = original.CastKind;
                    if (original.CanEnemy != 0) row.dist = original.Distance;
                }
                row.tgt = original.TargetKind;
                row.shape = original.Shape;
                row.canSelf = original.CanSelf;
                row.canFriendly = 0;
                row.canEnemy = original.CanEnemy;
            }

            parsedRules.Add(
                row.id,
                new SkillAimRule(
                    row.castkind,
                    row.tgt,
                    row.shape,
                    row.dist,
                    row.canSelf,
                    row.canFriendly,
                    row.canEnemy));
        }

        physical = parsedPhysical;
        rules = parsedRules;
        skillShowRows = seenShows.Count;
        return true;
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(ReferenceSkillEffectCatalog));
        }
    }

    private struct SkillAimRule
    {
        public SkillAimRule(
            int castKind,
            int targetKind,
            int shape,
            int distance,
            int canSelf,
            int canFriendly,
            int canEnemy)
        {
            CastKind = castKind;
            TargetKind = targetKind;
            Shape = shape;
            Distance = distance;
            CanSelf = canSelf;
            CanFriendly = canFriendly;
            CanEnemy = canEnemy;
        }

        public int CastKind { get; }
        public int TargetKind { get; }
        public int Shape { get; }
        public int Distance { get; }
        public int CanSelf { get; }
        public int CanFriendly { get; }
        public int CanEnemy { get; }
    }

    [Serializable]
    private sealed class SkillShowTable
    {
        public SkillShowRow[] shows;
    }

    [Serializable]
    private sealed class SkillShowRow
    {
        public int id;
        public int phy;
    }

    [Serializable]
    private sealed class SkillTable
    {
        public SkillRow[] skills;
    }

    [Serializable]
    private sealed class SkillRow
    {
        public int id;
        public int castkind;
        public int tgt;
        public int shape;
        public int dist;
        public int canSelf;
        public int canFriendly;
        public int canEnemy;
    }
}

/// <summary>
/// Existing HUD bridge to native GameHud skill-cooldown state and overlay drawing.
/// </summary>
public sealed class ReferenceGameHudSkillCooldowns : IDisposable
{
    public const int CooldownFrameCount = 37;
    public const float OverlayAlpha = 180f / 255f;

    private bool disposed;

    public bool Ready { get; private set; } = true;

    internal int ActiveCount => GameHud.SkillCooldownCountForIndependentRuntime;

    public bool TryStart(int skillId, float durationSeconds, float now)
    {
        ThrowIfDisposed();
        if (!Ready) return false;
        GameHud.StartSkillCooldown(skillId, durationSeconds);
        return skillId >= 1 && !(durationSeconds <= 0f);
    }

    public float Fraction(int skillId, float now)
    {
        ThrowIfDisposed();
        return Ready ? GameHud.SkillCooldownFractionForIndependentRuntime(skillId) : 0f;
    }

    public bool DrawOverlay(
        Rect area,
        int skillId,
        float now,
        IReadOnlyList<Texture2D> frames)
    {
        ThrowIfDisposed();
        return GameHud.DrawSkillCooldownForIndependentRuntime(area, skillId, frames);
    }

    internal static int FrameIndexForAudit(float fraction)
    {
        return GameHud.CooldownFrameIndexForIndependentRuntime(fraction);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        Ready = false;
        disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(ReferenceGameHudSkillCooldowns));
        }
    }

    // Native skillCd is static. Disposing this view does not clear another view's cooldowns.
}

}
