using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal static partial class CharData
{
    // Official 0x06000f17, static System.String CharData.get_PrefSuffix().
    //
    // arm64 rva 0x393ab4 (228 bytes) and x86_64 rva 0x3a6e40 (192 bytes) both load the CharData
    // static block, read the System.Int64 at +0x08 (charId) and, when it is positive, return
    // Int64.ToString() over that slot; otherwise they return the System.String at +0x00 (name).
    // The name is not null-checked, so an unsuffixed session yields the raw name slot.
    public static string PrefSuffix => charId > 0 ? charId.ToString() : name;

    // Official 0x06000f18, static System.Int32 CharData.GetPref(System.String, System.Int32).
    //
    // arm64 rva 0x393b98 (404 bytes) and x86_64 rva 0x3a6f00 (384 bytes): key = prefix + PrefSuffix.
    // When that key is absent and charId > 0 and name is not null/empty, the legacy key
    // prefix + name is read with the single-argument PlayerPrefs.GetInt overload, copied onto the
    // current key and flushed with Save() before being returned. Any other path falls through to
    // PlayerPrefs.GetInt(key, def).
    public static int GetPref(string prefix, int def)
    {
        string key = prefix + PrefSuffix;
        if (!PlayerPrefs.HasKey(key) && charId > 0 && !string.IsNullOrEmpty(name))
        {
            string legacyKey = prefix + name;
            if (PlayerPrefs.HasKey(legacyKey))
            {
                int legacy = PlayerPrefs.GetInt(legacyKey);
                PlayerPrefs.SetInt(key, legacy);
                PlayerPrefs.Save();
                return legacy;
            }
        }
        return PlayerPrefs.GetInt(key, def);
    }

    // Official 0x06000f19, static System.Void CharData.SetPref(System.String, System.Int32).
    //
    // arm64 rva 0x393d2c (208 bytes) and x86_64 rva 0x3a7080 (176 bytes): an empty suffix returns
    // without touching PlayerPrefs at all, so the empty-string guard precedes the write.
    public static void SetPref(string prefix, int v)
    {
        if (string.IsNullOrEmpty(PrefSuffix)) return;
        PlayerPrefs.SetInt(prefix + PrefSuffix, v);
        PlayerPrefs.Save();
    }

    // Official 0x06000f1a, static System.Boolean CharData.HasPref(System.String).
    //
    // arm64 rva 0x393dfc (332 bytes) and x86_64 rva 0x3a7130 (272 bytes): the current key wins
    // outright; only then is the legacy prefix + name key consulted, and only when charId > 0 and
    // name is neither null nor empty.
    public static bool HasPref(string prefix)
    {
        if (PlayerPrefs.HasKey(prefix + PrefSuffix)) return true;
        if (charId <= 0 || string.IsNullOrEmpty(name)) return false;
        return PlayerPrefs.HasKey(prefix + name);
    }
}

}
