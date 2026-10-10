namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal static partial class CharData
{
    // Official 1.5.8 field token 67114212: System.Int64 CharData.honor, static offset 112
    // (0x70), reached through metadata usage ordinal 1441 TypeInfo:CharData (arm64 0x148a5a8
    // / x86_64 0x147ea20) and its static field area at TypeInfo +0xb8.
    //
    // Initial value 0, not a cctor store: a full disassembly of both CharData..cctor extents
    // (0x06000f2d, arm64 0x395b74 1724 bytes / x86_64 0x3a8ea0 1936 bytes) contains no memory
    // operand at 0x70, and the recovered declaration is a bare "public static long honor;"
    // with no initializer, so the zero-filled static area is what the first packet overwrites.
    //
    // Write-only in 1.5.8. A whole-slice enumeration of every place the CharData slot is
    // materialised - 580 arm64 ADRP+ADD pairs resolving to 0x148a5a8 and 695 x86_64
    // RIP-relative LEA references to 0x147ea20 - finds exactly two accesses through
    // static-area +0x70 and both are stores: the 0x8006 case of NetClient.Dispatch
    // (arm64 0x25f14c / x86_64 0x26e01e) and its 0x8069 case (arm64 0x2557f8 / x86_64
    // 0x264d1d). No load of this slot exists anywhere in either slice, so no official window
    // reads it and no product window may be rewired onto it.
    internal static long honor;
}
}
