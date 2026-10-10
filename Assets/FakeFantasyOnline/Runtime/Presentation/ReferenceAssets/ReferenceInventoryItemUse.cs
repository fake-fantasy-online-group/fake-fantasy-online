using System;
using System.Collections.Generic;
using System.Text;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Native GameHud cooldown view with explicitly requested legacy catalogue diagnostics.
/// </summary>
/// <remarks>
/// Legacy special-use classification and its catalogue remain separate recovery boundaries.
/// Inventory counts and gameplay outcomes remain server-authored.
/// </remarks>
public sealed class ReferenceGameHudItemCooldowns : IDisposable
{
    public const string RuntimeTablePath = "tables/inventory_item_use.json";
    public const int CooldownFrameCount = 37;
    public const float OverlayAlpha = 180f / 255f;

    private const int ExpectedCooldownRows = 86;
    private const int ExpectedAvatarItemIds = 5740;
    private const int ExpectedSoulItemIds = 255;
    private const int ExpectedDragonItemIds = 315;
    private const int ExpectedCaptureItemIds = 6;
    private const int ExpectedToolTargetItemIds = 2;
    private const int ExpectedModalItemIds = 5;
    private const int ExpectedPetBindItemIds = 35;
    private const int ExpectedSpecialRanges = 1;

    // Runtime item activation belongs to InventoryDlg / ItemActions / GameHud. These legacy
    // catalogue fields are loaded only by their classification/count diagnostics, never by
    // world admission or the shared native cooldown state and drawing.
    private readonly ReferenceResourceLoader resources;
    private Dictionary<int, float> cooldownSeconds;
    private HashSet<int> deferredItemIds;
    private HashSet<int> captureItemIds;
    private HashSet<int> petBindItemIds;
    private RangeRow[] specialRanges;
    private bool disposed;

    private ReferenceGameHudItemCooldowns(ReferenceResourceLoader resources)
    {
        this.resources = resources;
        Ready = true;
    }

    public bool Ready { get; private set; }

    internal int CooldownRowCount => Ready && TryLoadLegacyCatalog() ? cooldownSeconds.Count : 0;

    internal int DeferredItemCount => Ready && TryLoadLegacyCatalog() ? deferredItemIds.Count : 0;

    internal int CaptureItemCount => Ready && TryLoadLegacyCatalog() ? captureItemIds.Count : 0;

    internal int PetBindItemCount => Ready && TryLoadLegacyCatalog() ? petBindItemIds.Count : 0;

    internal int ActiveCount => GameHud.SkillCooldownCountForIndependentRuntime;

    /// <summary>
    /// Creates the borrowed native cooldown view. Ready describes its lifetime; catalogue
    /// availability is checked only when a legacy classification or count is requested.
    /// </summary>
    public static bool TryCreate(
        ReferenceResourceLoader resources,
        out ReferenceGameHudItemCooldowns owner)
    {
        owner = null;
        if (resources == null)
        {
            return false;
        }

        owner = new ReferenceGameHudItemCooldowns(resources);
        return true;
    }

    // The generated table has no live Runtime classification consumer. Keep its qualified
    // legacy queries available without making its presence/counts a GameHud startup rule.
    private bool TryLoadLegacyCatalog()
    {
        if (cooldownSeconds != null)
        {
            return true;
        }

        byte[] encoded = resources.ReadSourceOwned(RuntimeTablePath);
        if (encoded == null || encoded.Length == 0)
        {
            return false;
        }

        RuntimeTable table;
        try
        {
            table = JsonUtility.FromJson<RuntimeTable>(Encoding.UTF8.GetString(encoded));
        }
        catch (ArgumentException)
        {
            return false;
        }

        Dictionary<int, float> durations;
        HashSet<int> deferred;
        HashSet<int> capture;
        HashSet<int> petBind;
        RangeRow[] ranges;
        if (!TryValidate(
                table,
                out durations,
                out deferred,
                out capture,
                out petBind,
                out ranges))
        {
            return false;
        }

        cooldownSeconds = durations;
        deferredItemIds = deferred;
        captureItemIds = capture;
        petBindItemIds = petBind;
        specialRanges = ranges;
        return true;
    }

    /// <summary>
    /// Legacy catalogue diagnostic for the six source-recovered capture-item ids. Runtime
    /// target selection and the special no-target feedback remain owned by the inventory dialog.
    /// </summary>
    public bool CanCaptureUse(WorldInventoryItemState item)
    {
        ThrowIfDisposed();
        return Ready && TryLoadLegacyCatalog() && item != null && item.ItemId > 0 && item.Count > 0 &&
            item.Tab <= 3 && item.BagIndex >= 0 && item.BagIndex <= 99 &&
            item.EquipmentSlot < 0 && !item.Cosmetic && item.Pet == null &&
            captureItemIds.Contains(item.ItemId);
    }

    /// <summary>
    /// Legacy catalogue diagnostic for the generic pet-binding consumable branch. The two pet-reset
    /// items own an earlier destructive confirmation and remain deferred to that separate module.
    /// </summary>
    public bool CanPetBindUse(WorldInventoryItemState item)
    {
        ThrowIfDisposed();
        return Ready && TryLoadLegacyCatalog() && item != null && item.ItemId > 0 && item.Count > 0 &&
            item.Tab <= 3 && item.BagIndex >= 0 && item.BagIndex <= 99 &&
            item.EquipmentSlot < 0 && !item.Cosmetic && item.Pet == null &&
            item.ItemId != 3076 && item.ItemId != 25270 &&
            petBindItemIds.Contains(item.ItemId);
    }

    /// <summary>
    /// Legacy catalogue diagnostic for the ordinary direct-use branch. Special entries fail closed;
    /// the live inventory and hotbar call the original ItemActions owner instead.
    /// </summary>
    public bool CanDirectUse(WorldInventoryItemState item)
    {
        ThrowIfDisposed();
        if (!Ready || !TryLoadLegacyCatalog() || item == null || item.ItemId <= 0 || item.Count <= 0 ||
            item.Tab > 3 || item.BagIndex < 0 || item.BagIndex > 99 ||
            item.EquipmentSlot >= 0 || item.Cosmetic || item.Pet != null ||
            deferredItemIds.Contains(item.ItemId))
        {
            return false;
        }

        for (int index = 0; index < specialRanges.Length; index++)
        {
            RangeRow range = specialRanges[index];
            if (item.ItemId >= range.first && item.ItemId <= range.last)
            {
                return false;
            }
        }

        return true;
    }

    public float Fraction(int itemId, float now)
    {
        ThrowIfDisposed();
        return Ready ? GameHud.CdFrac(itemId) : 0f;
    }

    public bool TryStart(int itemId, float now)
    {
        ThrowIfDisposed();
        return Ready && GameHud.StartItemCooldownForIndependentRuntime(itemId);
    }

    public bool DrawOverlay(Rect area, int itemId, float now, IReadOnlyList<Texture2D> frames)
    {
        ThrowIfDisposed();
        return GameHud.DrawSkillCooldownForIndependentRuntime(area, itemId, frames);
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

        cooldownSeconds?.Clear();
        deferredItemIds?.Clear();
        captureItemIds?.Clear();
        petBindItemIds?.Clear();
        Ready = false;
        disposed = true;
    }

    private static bool TryValidate(
        RuntimeTable table,
        out Dictionary<int, float> durations,
        out HashSet<int> deferred,
        out HashSet<int> capture,
        out HashSet<int> petBind,
        out RangeRow[] ranges)
    {
        durations = null;
        deferred = null;
        capture = null;
        petBind = null;
        ranges = null;
        if (table == null || table.schemaVersion != 1 ||
            table.cooldowns == null || table.cooldowns.Length != ExpectedCooldownRows ||
            table.avatarItemIds == null || table.avatarItemIds.Length != ExpectedAvatarItemIds ||
            table.soulItemIds == null || table.soulItemIds.Length != ExpectedSoulItemIds ||
            table.dragonItemIds == null || table.dragonItemIds.Length != ExpectedDragonItemIds ||
            table.captureItemIds == null ||
                table.captureItemIds.Length != ExpectedCaptureItemIds ||
            table.toolTargetItemIds == null ||
                table.toolTargetItemIds.Length != ExpectedToolTargetItemIds ||
            table.modalItemIds == null || table.modalItemIds.Length != ExpectedModalItemIds ||
            table.petBindItemIds == null ||
                table.petBindItemIds.Length != ExpectedPetBindItemIds ||
            table.specialRanges == null ||
                table.specialRanges.Length != ExpectedSpecialRanges)
        {
            return false;
        }

        Dictionary<int, float> parsedDurations = new Dictionary<int, float>();
        for (int index = 0; index < table.cooldowns.Length; index++)
        {
            CooldownRow row = table.cooldowns[index];
            if (row == null || row.itemId <= 0 || row.seconds < 0 || row.seconds > 600 ||
                row.group <= 0 || parsedDurations.ContainsKey(row.itemId))
            {
                return false;
            }

            parsedDurations.Add(row.itemId, row.seconds);
        }

        HashSet<int> parsedDeferred = new HashSet<int>();
        HashSet<int> parsedCapture = new HashSet<int>();
        HashSet<int> parsedPetBind = new HashSet<int>();
        if (!AddUniquePositive(table.captureItemIds, parsedCapture))
        {
            return false;
        }

        if (!AddUniquePositive(table.avatarItemIds, parsedDeferred) ||
            !AddUniquePositive(table.soulItemIds, parsedDeferred) ||
            !AddUniquePositive(table.dragonItemIds, parsedDeferred) ||
            !AddUniquePositive(table.captureItemIds, parsedDeferred) ||
            !AddUniquePositive(table.toolTargetItemIds, parsedDeferred) ||
            !AddUniquePositive(table.modalItemIds, parsedDeferred) ||
            !AddUniquePositive(table.petBindItemIds, parsedDeferred) ||
            !AddUniquePositive(table.petBindItemIds, parsedPetBind))
        {
            return false;
        }

        RangeRow[] parsedRanges = new RangeRow[table.specialRanges.Length];
        for (int index = 0; index < table.specialRanges.Length; index++)
        {
            RangeRow range = table.specialRanges[index];
            if (range == null || range.first <= 0 || range.last < range.first)
            {
                return false;
            }

            parsedRanges[index] = range;
        }

        durations = parsedDurations;
        deferred = parsedDeferred;
        capture = parsedCapture;
        petBind = parsedPetBind;
        ranges = parsedRanges;
        return true;
    }

    private static bool AddUniquePositive(int[] values, HashSet<int> destination)
    {
        HashSet<int> source = new HashSet<int>();
        for (int index = 0; index < values.Length; index++)
        {
            if (values[index] <= 0 || !source.Add(values[index]))
            {
                return false;
            }

            destination.Add(values[index]);
        }

        return true;
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(ReferenceGameHudItemCooldowns));
        }
    }

    [Serializable]
    private sealed class RuntimeTable
    {
        public int schemaVersion;
        public CooldownRow[] cooldowns;
        public int[] avatarItemIds;
        public int[] soulItemIds;
        public int[] dragonItemIds;
        public int[] captureItemIds;
        public int[] toolTargetItemIds;
        public int[] modalItemIds;
        public int[] petBindItemIds;
        public RangeRow[] specialRanges;
    }

    [Serializable]
    private sealed class CooldownRow
    {
        public int itemId;
        public int seconds;
        public int group;
    }

    [Serializable]
    private sealed class RangeRow
    {
        public int first;
        public int last;
    }
}

}
