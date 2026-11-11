using System.Collections.Generic;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static partial class WinZ
{
    internal static bool AnyLayoutBlockingWindow
    {
        get
        {
            int frame=Time.frameCount;
            foreach (var pair in reg)
                if (unchecked(frame-pair.Value.frame)<=2 && !AddonUiHost.IsMobilePassiveLauncher(pair.Key)) return true;
            return false;
        }
    }

    // Mobile chrome/scroll helpers: the freshest rectangle of one registered owner.
    internal static bool TryGetRect(object owner, out Rect rect)
    {
        rect = default;
        if (owner == null || !reg.TryGetValue(owner, out Entry entry) || unchecked(Time.frameCount - entry.frame) > 2) return false;
        rect = entry.rect; return true;
    }

    // Windows drawn in the last two frames, back to front.
    internal static void CollectFresh(List<object> owners, List<Rect> rects)
    {
        owners.Clear(); rects.Clear();
        int frame = Time.frameCount;
        for (int i = 0; i < order.Count; i++)
        {
            object candidate = order[i];
            if (candidate == null || !reg.TryGetValue(candidate, out Entry entry) || unchecked(frame - entry.frame) > 2) continue;
            owners.Add(candidate); rects.Add(entry.rect);
        }
    }
}
}
