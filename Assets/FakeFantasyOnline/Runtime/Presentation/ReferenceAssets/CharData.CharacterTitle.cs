using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal static partial class CharData
{




    // Product-only projection guard, not an official field: it records that one of the official
    // writes above has reached this owner, so the head name tag is only projected from the owner
    // once the owner really holds a title instead of the empty string CharData..cctor stored.
    // Both official writers now feed the same owner - TitleFlow.DoEnterWorld stores the 0x8003
    // enter title (arm64 0x391ef4) and case 0x804c stores the server snapshot (arm64 0x254650)
    // - so the guard no longer stands in for a second source; it only keeps the pre-entry state
    // from being projected, and it stays true for a packet that failed after the clear or after
    // the k-th append, which is what the official client leaves behind.
    private static bool titleReplaced;

    internal static bool HasReceivedTitleForIndependentRuntime => titleReplaced;

    // Product-only change-detection counter, not an official CharData field. It records only
    // that one of the three official write steps below ran, never what was stored.
    // Why it exists: the official title window does not hold a snapshot. EquipDlg.OnGUI reads
    // these two statics itself - arm64 0x3fa9ec..0x3faa14 loads CharData (usage 0x148a5a8),
    // static_fields (+0xb8), ownedTitles (+0x38) and its inlined _size (+0x18) to decide how
    // many rows to draw, arm64 0x3fab68..0x3fab90 re-reads the same list for each row and
    // arm64 0x3f6294..0x3f62a4 reads CharData.title (+0x30) - so whatever the owner holds when
    // the window renders is what the window shows. The recovered window therefore has to
    // observe the owner again on its next render whenever the owner was written, including the
    // partial state left behind by a packet that failed after step 3 or step 4; a version that
    // only moves when a whole packet was published would keep the old list on screen.
    private static long titleMutationVersion;

    internal static long TitleMutationVersionForIndependentRuntime => titleMutationVersion;

    // The initial values in CharData.Initializers.cs are the ones CharData..cctor stores:
    // 0x06000f2d (arm64 0x395b74 / x86_64 0x3a8ea0) writes the empty-string literal
    // (metadata usage ordinal 10335, arm64 slot 0x149BB98 / x86_64 slot 0x1490010) into +0x30
    // and a freshly allocated List<string> (arm64 bl 0x9caf64 / x86_64 callq 0x9cfa80) into
    // +0x38, so both statics are non-null before the first 0x804c packet: "" and an empty
    // list. Case 0x804c still null-checks the list before both the clear and every add and
    // enters the non-returning IL2CPP throw helper when it is null, which is why the owner
    // below never substitutes a fresh list on the read path.

    /// <summary>
    /// Official 0x804c step 3: the shared <c>List&lt;T&gt;.Clear</c> body (arm64 0x1418 /
    /// x86_64 0x13d0, reached through MethodRef[44370]) reads _size at +0x18, stores 0 there,
    /// increments _version at +0x1c and, when the old size was not zero, clears _items at
    /// +0x10. Same list instance, no new allocation, and it runs before the count test.
    /// </summary>
    internal static void ClearOwnedTitlesForIndependentRuntime()
    {
        ownedTitles.Clear();
        titleMutationVersion++;
    }

    /// <summary>
    /// Official 0x804c step 4: the inlined <c>List&lt;string&gt;.Add</c> on the very same
    /// static list. No empty, duplicate or null filter runs and no owner test against the
    /// current title runs, so empty strings, duplicates and entries unrelated to the current
    /// title are all kept exactly as the server sent them.
    /// </summary>
    internal static void AddOwnedTitleForIndependentRuntime(string value)
    {
        ownedTitles.Add(value);
        titleMutationVersion++;
    }

    /// <summary>
    /// Official 0x804c step 5: the string read in step 1 is stored into <c>title</c> only
    /// after the loop, and also when the count was zero.
    /// </summary>
    internal static void SetCurrentTitleForIndependentRuntime(string value)
    {
        title = value;
        titleReplaced = true;
        titleMutationVersion++;
    }

    /// <summary>
    /// Projects the two statics for the recovered title window. The window reads the owner
    /// rather than a second parallel copy, so a packet that failed after step 3 or step 4 is
    /// visible there exactly as the official client leaves it.
    /// </summary>
    internal static WorldCharacterTitleState CaptureTitleStateForIndependentRuntime()
    {
        return new WorldCharacterTitleState(title, ownedTitles);
    }
}

}
