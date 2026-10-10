using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Compatibility facade for original NetClient.LoadItemIcon and ItemIconUI.Draw.
/// </summary>
public sealed class ReferenceItemIconCatalog
{
    public const string ItemIconsTablePath = "tables/item_icons.json";
    public const string ItemIconFixTablePath = "tables/item_icon_fix.json";
    public const string MountIconsTablePath = "tables/mount_icons.json";

    private readonly ReferenceResourceLoader resources;
    private readonly Dictionary<int, int> smallIcons = new Dictionary<int, int>();
    private readonly Dictionary<int, int> bigIcons = new Dictionary<int, int>();
    private readonly Dictionary<int, int> mountIcons = new Dictionary<int, int>();
    private bool mapsLoaded;

    public ReferenceItemIconCatalog(ReferenceResourceLoader resources)
    {
        this.resources = resources ?? throw new ArgumentNullException(nameof(resources));
    }

    public Texture2D Load(int lookupId) => NetClient.LoadItemIcon(lookupId);

    public static void Draw(Rect area, Texture2D texture, float scale) =>
        ItemIconUI.Draw(area, texture, scale);

    // Historical diagnostic map view; production loading delegates to NetClient above.
    internal void ResolveForAudit(
        int lookupId,
        out int smallIconId,
        out int bigIconId,
        out int mountModel)
    {
        EnsureMaps();
        if (!smallIcons.TryGetValue(lookupId, out smallIconId))
        {
            smallIconId = lookupId;
        }

        if (!bigIcons.TryGetValue(lookupId, out bigIconId))
        {
            bigIconId = lookupId;
        }

        if (!mountIcons.TryGetValue(lookupId, out mountModel))
        {
            mountModel = 0;
        }
    }

    private void EnsureMaps()
    {
        if (mapsLoaded)
        {
            return;
        }

        mapsLoaded = true;
        ItemIconTable table = ReadJson<ItemIconTable>(ItemIconsTablePath);
        if (table != null && table.id != null && table.sm != null && table.big != null &&
            table.id.Length == table.sm.Length && table.id.Length == table.big.Length)
        {
            for (int index = 0; index < table.id.Length; index++)
            {
                if (table.id[index] > 0)
                {
                    smallIcons[table.id[index]] = table.sm[index];
                    bigIcons[table.id[index]] = table.big[index];
                }
            }
        }

        ItemIconFixTable fixes = ReadJson<ItemIconFixTable>(ItemIconFixTablePath);
        if (fixes != null && fixes.list != null)
        {
            for (int index = 0; index < fixes.list.Length; index++)
            {
                ItemIconFix row = fixes.list[index];
                if (row != null && row.id > 0 && row.icon > 0)
                {
                    smallIcons[row.id] = row.icon;
                    bigIcons[row.id] = row.icon;
                }
            }
        }

        MountIconTable mounts = ReadJson<MountIconTable>(MountIconsTablePath);
        if (mounts != null && mounts.item != null && mounts.model != null &&
            mounts.item.Length == mounts.model.Length)
        {
            for (int index = 0; index < mounts.item.Length; index++)
            {
                if (mounts.item[index] > 0 && mounts.model[index] > 0)
                {
                    mountIcons[mounts.item[index]] = mounts.model[index];
                }
            }
        }
    }

    private T ReadJson<T>(string path) where T : class
    {
        byte[] bytes = resources.ReadSourceOwned(path);
        if (bytes == null || bytes.Length == 0)
        {
            return null;
        }

        try
        {
            return JsonUtility.FromJson<T>(Encoding.UTF8.GetString(bytes));
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    [Serializable]
    private sealed class ItemIconTable
    {
        public int[] id;
        public int[] sm;
        public int[] big;
    }

    [Serializable]
    private sealed class ItemIconFixTable
    {
        public ItemIconFix[] list;
    }

    [Serializable]
    private sealed class ItemIconFix
    {
        public int id;
        public int icon;
    }

    [Serializable]
    private sealed class MountIconTable
    {
        public int[] item;
        public int[] model;
    }
}

}
