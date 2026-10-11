using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private static readonly HashSet<int> missingEndgameIcons = new HashSet<int>();

    // Classic and current package pixels were both reviewed for these small
    // sprites. Keep a bounded semantic fallback rather than a shared generic icon.
    // Primary, mansion-specific sprite IDs remain authoritative server mappings.
    internal static int EndgameFallbackIconID(int itemID)
    {
        int rune = itemID - 1900028001;
        if (rune >= 0 && rune < 28)
        {
            switch (rune % 7)
            {
                case 0: return 3608; // wood: green engraved emblem
                case 1: return 3573; // metal: golden engraved emblem
                case 2: return 4312; // earth: ochre inscribed tablet
                case 3: return 4094; // sun: golden star
                case 4: return 4024; // moon: crescent jade
                case 5: return 3607; // fire: red flame emblem
                case 6: return 3606; // water: blue swirling emblem
            }
        }
        switch (itemID)
        {
            case 1900028101: return 3303; // key
            case 1900028201: return 3559; // star crystal
            case 1900028202: return 4019; // fine powder
            case 1900028203: return 3619; // luminous essence
            default: return 0;
        }
    }

    private static Texture2D LoadEndgameIconFallback(int itemID, ResLoader loader)
    {
        int fallback = EndgameFallbackIconID(itemID);
        if (fallback == 0) return null;
        // Load sprite paths directly: a sprite ID is not an item-table lookup ID.
        Texture2D texture = loader.LoadTex(string.Format("item/item/item{0:D4}.gso", fallback), 0);
        if (texture == null)
            texture = loader.LoadTex(string.Format("item/itempic/itempic{0:D4}.gso", fallback), 0);
        if (missingEndgameIcons.Add(itemID))
            Debug.LogWarning("Endgame item icon " + itemID + " is unavailable; " +
                (texture != null ? "using semantic fallback sprite " + fallback :
                 "fallback sprite " + fallback + " is also unavailable. Check the client resource installation."));
        return texture;
    }
}
}
