using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class SoundManager
{
    internal static void PlayGuarded(int id, Func<bool> stillValid)
    {
        if (stillValid == null || !stillValid()) return;
        Ensure();
        if (I == null) return;
        if (I.cache.TryGetValue(id, out AudioClip clip) && clip != null)
        {
            if (stillValid()) I.Emit(clip);
            return;
        }
        I.StartCoroutine(I.LoadAndPlay(id, false, stillValid));
    }
}
}
