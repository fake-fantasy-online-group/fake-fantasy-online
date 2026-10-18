using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public sealed partial class RackShopDlg
    {
        // Resolve only rendered rows, never the complete incoming catalog. The resource
        // owner retains texture ownership; this window also remembers missing/bad icons
        // so IMGUI layout/repaint passes do not repeat I/O or decoder failures.
        // SetGoods clears these lookups on each fresh catalog receipt.
        private readonly Dictionary<int, Texture2D> visibleGoodIcons = new Dictionary<int, Texture2D>();

        private void DrawGoodIcon(Rect rect, Good good)
        {
            Texture2D icon = ResolveGoodIcon(good);
            if (icon != null) ItemIconUI.Draw(rect, icon, s);
        }

        private Texture2D ResolveGoodIcon(Good good)
        {
            if (good.icon != null) return good.icon;
            if (good.id <= 0 || ResLoader.I == null) return null;
            int iconId = good.id > 89999999 ? 3385 : good.id;
            Texture2D icon;
            if (visibleGoodIcons.TryGetValue(iconId, out icon)) return icon;
            try { icon = NetClient.LoadItemIcon(iconId); }
            catch (Exception exception)
            {
                // A corrupt optional icon must not abort receipt of the catalog or
                // prevent the rest of the window (including Close) from drawing.
                Debug.LogWarning("Rack item icon unavailable (" + iconId + "): " + exception.Message);
                icon = null;
            }
            visibleGoodIcons[iconId] = icon;
            return icon;
        }
    }
}
