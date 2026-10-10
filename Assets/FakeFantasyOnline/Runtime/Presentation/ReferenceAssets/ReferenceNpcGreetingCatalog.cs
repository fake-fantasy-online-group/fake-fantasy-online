using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Adapter for the recovered NpcChatter data/selection owners; RoleOf and ambient instance migration remain pending.
/// </summary>
public sealed class ReferenceNpcGreetingCatalog
{
    public const float DefaultMinimumGapSeconds = 40f;
    public const float DefaultMaximumGapSeconds = 90f;
    public const float DefaultShowSeconds = 5f;
    public const float FallbackCycleMilliseconds = 20000f;
    public const int OtherNpcSkipLimit = 6;
    public const string DataPath = "npc-greeting-data.json";
    public const string CommonRole = "common";
    public const string VendorRole = "vendor";
    public const string TransportRole = "transport";
    public const string StorageRole = "storage";
    public const string HealRole = "heal";
    public const string QuestRole = "quest";
    public const string GuideRole = "guide";

    private static List<string> serverAll => NpcChatter.ServerAllForIndependentRuntime;
    private static Dictionary<string, List<string>> serverByNpc => NpcChatter.ServerByNpcForIndependentRuntime;
    private static Dictionary<string, List<string>> serverByRole => NpcChatter.ServerByRoleForIndependentRuntime;
    private static Dictionary<string, List<string>> greetingRoles => NpcChatter.GreetingRolesForIndependentRuntime;
    private readonly ISet<string> knownNpcNames;
    private ReferenceNpcGreetingCatalog(ISet<string> processNpcNames)
    {
        knownNpcNames = processNpcNames ??
            new HashSet<string>(StringComparer.Ordinal);
    }

    // Historical diagnostic names; these expose the active recovered tables.
    public int OfflineChatterCount => serverAll.Count;
    public int OfflineGreetingCount => NpcChatter.ServerGreetingsForIndependentRuntime.Count;
    public bool ServerApplied => serverByNpc != null;
    public bool ChatterEnabled => NpcChatter.ChatterEnabledForIndependentRuntime;
    public float MinimumGapSeconds => NpcChatter.MinimumGapForIndependentRuntime;
    public float MaximumGapSeconds => NpcChatter.MaximumGapForIndependentRuntime;
    public float ShowSeconds => NpcChatter.ShowSecondsForIndependentRuntime;
    public float CycleMilliseconds => NpcChatter.CycleForIndependentRuntime;

    public static bool TryCreate(
        ReferenceResourceLoader resources,
        out ReferenceNpcGreetingCatalog owner)
    {
        return TryCreate(resources, null, out owner);
    }

    public static bool TryCreate(
        ReferenceResourceLoader resources,
        ISet<string> processNpcNames,
        out ReferenceNpcGreetingCatalog owner)
    {
        owner = null;
        if (resources == null)
        {
            return false;
        }

        NpcChatter.PrepareDataForIndependentRuntime();
        owner = new ReferenceNpcGreetingCatalog(processNpcNames);
        return true;
    }

    /// <summary>
    /// Applies an actual server replacement. An absent snapshot does not reset process-wide tables.
    /// </summary>
    public bool ApplyServerChatter(WorldNpcChatterState state)
    {
        return ApplyServerChatterNativeProjection(state);
    }

    internal static bool ApplyServerChatterNativeProjection(WorldNpcChatterState state)
    {
        if (state == null) return true;

        var lines = new List<(string npcName, string role, string text)>(state.Lines.Count);
        foreach (WorldNpcChatterLineState row in state.Lines)
        {
            if (row == null) return false;
            lines.Add((row.NpcName, row.Role, row.Text));
        }
        NpcChatter.SetServerChatter(lines, state.Enabled ? 1 : 0,
            state.MinimumGapSeconds, state.MaximumGapSeconds, state.ShowSeconds);
        return true;
    }

    /// <summary>
    /// Mirrors the official process-wide allNpcNames set. Names are intentionally not removed
    /// when one actor is destroyed.
    /// </summary>
    public void RegisterNpcName(string npcName)
    {
        if (!string.IsNullOrEmpty(npcName))
        {
            knownNpcNames.Add(npcName);
        }
    }

    /// <summary>
    /// Source translation of NpcChatter.PickLine. lineIndex is instance-owned by each NPC.
    /// </summary>
    public string PickAmbientLine(string npcName, string role, ref int lineIndex)
    {
        List<string> pool;
        if (serverByNpc != null && !string.IsNullOrEmpty(npcName) &&
            serverByNpc.TryGetValue(npcName, out pool) && pool.Count > 0)
        {
            return SelectSequential(pool, ref lineIndex);
        }

        string own = OwnGreeting(npcName);
        if (own != null)
        {
            return own;
        }

        Dictionary<string, List<string>> roles = serverByRole;
        List<string> all = serverAll;
        if (string.IsNullOrEmpty(role) ||
            !roles.TryGetValue(role, out pool) || pool.Count < 1)
        {
            pool = all;
        }

        if (pool == null || pool.Count < 1)
        {
            return null;
        }

        for (int attempt = 0; attempt < OtherNpcSkipLimit; attempt++)
        {
            string candidate = SelectSequential(pool, ref lineIndex);
            if (!IsOtherNpcGreeting(candidate, npcName))
            {
                return candidate;
            }
        }

        return null;
    }

    public string RoleOf(WorldNpcState npc)
    {
        if (npc == null)
        {
            return CommonRole;
        }

        if (npc.SellList > 0)
        {
            return VendorRole;
        }

        if (npc.TransportList > 0)
        {
            return TransportRole;
        }

        string name = npc.DisplayName ?? string.Empty;
        if (name.Contains("仓库"))
        {
            return StorageRole;
        }

        if (ContainsAny(name, "主持", "仙子", "医", "药", "治疗"))
        {
            return HealRole;
        }

        if (ContainsAny(name, "传令", "任务", "委托"))
        {
            return QuestRole;
        }

        if (ContainsAny(name, "新手", "教头", "向导", "引导", "导师"))
        {
            return GuideRole;
        }

        return CommonRole;
    }

    public string OwnGreeting(string npcName)
    {
        return NpcChatter.OwnGreeting(npcName);
    }

    /// <summary>
    /// Applies the exact DialogUI.Show branch, including its deliberate own-line suppression.
    /// </summary>
    public string DialogGreeting(WorldNpcState npc)
    {
        return DialogGreeting(npc, -1);
    }

    /// <summary>
    /// Deterministic selection seam used by the durable Editor audit. A negative index preserves
    /// the official UnityEngine.Random.Range call.
    /// </summary>
    public string DialogGreeting(WorldNpcState npc, int selectionIndex)
    {
        if (npc == null)
        {
            return ReferenceFirstWorldNpcDialog.FallbackGreeting;
        }

        if (OwnGreeting(npc.DisplayName) != null)
        {
            return ReferenceFirstWorldNpcDialog.FallbackGreeting;
        }

        string greeting = GetGreeting(npc.DisplayName, RoleOf(npc), selectionIndex);
        return string.IsNullOrEmpty(greeting)
            ? ReferenceFirstWorldNpcDialog.FallbackGreeting
            : greeting;
    }

    // NpcChatter.GetLine selects ordinary chatter, without OwnGreeting or greeting pools.
    public string GetLine(string role)
    {
        return NpcChatter.GetLine(role);
    }

    public string GetGreeting(string npcName, string role, int selectionIndex)
    {
        // Ordinary game calls use the recovered owner; retain the pre-existing explicit
        // deterministic selection seam for historical Editor audits until its separate migration.
        if (selectionIndex < 0) return NpcChatter.GetGreeting(npcName, role);
        string own = OwnGreeting(npcName);
        if (own != null)
        {
            return own;
        }

        List<string> pool;
        if (greetingRoles != null &&
            greetingRoles.TryGetValue(role, out pool) && pool.Count > 0)
        {
            return Select(pool, selectionIndex);
        }

        Dictionary<string, List<string>> roles = serverByRole;
        List<string> all = serverAll;
        if (roles != null && roles.TryGetValue(role, out pool) && pool.Count > 0)
        {
            return Select(pool, selectionIndex);
        }

        return all.Count > 0 ? Select(all, selectionIndex) : string.Empty;
    }

    private static string Select(IReadOnlyList<string> source, int selectionIndex)
    {
        int index = selectionIndex < 0
            ? UnityEngine.Random.Range(0, source.Count)
            : selectionIndex;
        if (index < 0 || index >= source.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(selectionIndex));
        }

        return source[index];
    }

    private static string SelectSequential(IReadOnlyList<string> source, ref int lineIndex)
    {
        int current = lineIndex;
        lineIndex = unchecked(lineIndex + 1);
        int index = current % source.Count;
        if (index < 0)
        {
            index += source.Count;
        }

        return source[index];
    }

    private bool IsOtherNpcGreeting(string line, string selfName)
    {
        if (string.IsNullOrEmpty(line))
        {
            return false;
        }

        foreach (string candidate in knownNpcNames)
        {
            if (candidate.Length > 1 &&
                !string.Equals(candidate, selfName, StringComparison.Ordinal) &&
                line.Contains(candidate))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsAny(string value, params string[] candidates)
    {
        for (int index = 0; index < candidates.Length; index++)
        {
            if (value.Contains(candidates[index]))
            {
                return true;
            }
        }

        return false;
    }

}
}
