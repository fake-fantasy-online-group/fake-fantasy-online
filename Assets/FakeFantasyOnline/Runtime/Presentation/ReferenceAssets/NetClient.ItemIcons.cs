using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal partial class NetClient
{
    private static Dictionary<int, int> _iconSm;
    private static Dictionary<int, int> _iconBig;
    private static Dictionary<int, int> _mountModel;

    [Serializable] private class IconMapJson { public int[] id; public int[] sm; public int[] big; }
    [Serializable] private class MountIconJson { public int[] item; public int[] model; }
    [Serializable] private class IconFixJson { public IconFix[] list; }
    [Serializable] private class IconFix { public int id; public int icon; public string note; }

    internal static void EnsureIconMapForIndependentRuntime() => EnsureIconMap();
    internal static void ApplyIconOverrideForIndependentRuntime(int itemId, int iconId)
    {
        _iconSm[itemId] = iconId;
        _iconBig[itemId] = iconId;
    }

    private static void EnsureIconMap()
    {
        if (_iconSm != null) return;
        _iconSm = new Dictionary<int, int>();
        _iconBig = new Dictionary<int, int>();
        _mountModel = new Dictionary<int, int>();
        string text = GameTables.LoadText("mount_icons");
        if (text != null)
        {
            MountIconJson mounts = JsonUtility.FromJson<MountIconJson>(text);
            if (mounts != null && mounts.item != null)
                for (int i = 0; i < mounts.item.Length; i++) _mountModel[mounts.item[i]] = mounts.model[i];
        }
        text = GameTables.LoadText("item_icons");
        if (text == null) return;
        IconMapJson icons = JsonUtility.FromJson<IconMapJson>(text);
        if (icons == null || icons.id == null) return;
        for (int i = 0; i < icons.id.Length; i++)
        {
            _iconSm[icons.id[i]] = icons.sm[i];
            _iconBig[icons.id[i]] = icons.big[i];
        }
        text = GameTables.LoadText("item_icon_fix");
        if (text != null)
        {
            IconFixJson fixes = JsonUtility.FromJson<IconFixJson>(text);
            if (fixes != null && fixes.list != null)
                foreach (IconFix fix in fixes.list)
                {
                    _iconSm[fix.id] = fix.icon;
                    _iconBig[fix.id] = fix.icon;
                }
        }
    }

    public static Texture2D LoadItemIcon(int itemId)
    {
        if (!PetForgeIds.IsGrayBook(itemId)) return LoadItemIconRaw(itemId);
        if (_grayIcon.TryGetValue(itemId, out Texture2D cached)) return cached;
        Texture2D result = Desaturate(LoadItemIconRaw(itemId));
        _grayIcon[itemId] = result;
        return result;
    }

    private static Texture2D LoadItemIconRaw(int itemId)
    {
        ResLoader loader = ResLoader.I;
        if (loader == null) return null;
        Texture2D texture = loader.LoadUI(string.Format("itemicon/item{0}.png", itemId));
        if (texture != null) return texture;
        EnsureIconMap();
        int small = _iconSm.TryGetValue(itemId, out int sm) ? sm : itemId;
        int big = _iconBig.TryGetValue(itemId, out int bg) ? bg : itemId;
        string[] paths =
        {
            string.Format("item/item/item{0:D4}.gso", small),
            string.Format("item/item/item{0}.gso", small),
            string.Format("item/itempic/itempic{0:D4}.gso", big),
            string.Format("item/itempic/itempic{0}.gso", big)
        };
        foreach (string path in paths)
        {
            texture = loader.LoadTex(path, 0);
            if (texture != null) return texture;
        }
        if (_mountModel.TryGetValue(itemId, out int model) && model > 0)
        {
            texture = loader.LoadGsa(string.Format("res/peticon/M{0}.gsa", model));
            if (texture != null) return texture;
        }
        return null;
    }
}

}
