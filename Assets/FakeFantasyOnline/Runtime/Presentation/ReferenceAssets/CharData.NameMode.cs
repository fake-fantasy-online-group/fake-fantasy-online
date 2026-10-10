namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal static partial class CharData
{

    // Official static field +0x240 (576), private static System.String, the suffix the cached
    // nameMode was last read for.
    private static string _nameModeChar;

    // Official const field NAMEMODE_PREF. CharData.LoadNameMode passes metadata usage 15444,
    // which is the literal "namemode_"; the const is inlined at the call site.
    private const string NameModePreferencePrefix = "namemode_";

    // Official 0x06000f2a, static System.Int32 CharData.get_NAMEMODE_COUNT().
    //
    // arm64 rva 0x395860 (128 bytes) and x86_64 rva 0x3a8bc0 (144 bytes) dereference the array at
    // +0x238 and return its +0x18 length word; the null array case raises, which the C# property
    // reproduces because the field is never reassigned.
    public static int NAMEMODE_COUNT => NAMEMODE_TIPS.Length;

    // Official 0x06000f2b, static System.Void CharData.LoadNameMode().
    //
    // arm64 rva 0x3958e0 (476 bytes) and x86_64 rva 0x3a8c50 (432 bytes): the suffix is evaluated
    // three times because the compiler does not cache the property. An empty suffix returns
    // without caching anything; an unchanged suffix short-circuits before the preference read is
    // issued; only then is the suffix stored into +0x240 and GetPref("namemode_", 0) consulted.
    // A negative value is clamped without touching the array, and a value at or past
    // NAMEMODE_TIPS.Length is clamped after the array length is read, so the two clamps are a
    // short-circuiting "v < 0 || v >= NAMEMODE_TIPS.Length" and both store 0 into nameMode.
    public static void LoadNameMode()
    {
        if (string.IsNullOrEmpty(PrefSuffix)) return;
        if (_nameModeChar == PrefSuffix) return;
        _nameModeChar = PrefSuffix;
        int mode = GetPref(NameModePreferencePrefix, 0);
        if (mode < 0 || mode >= NAMEMODE_TIPS.Length) mode = 0;
        nameMode = mode;
    }
}

}
