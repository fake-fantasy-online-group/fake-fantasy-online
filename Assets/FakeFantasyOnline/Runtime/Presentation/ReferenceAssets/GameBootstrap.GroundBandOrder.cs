using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{
    // Official 1.5.8 GameBootstrap.GroundBandOrder (060003cc): arm64 rva 0x41cc34 (272 bytes),
    // x86_64 rva 0x431900 (304 bytes). PlaceObj (060003cb) inlines the same body inside its
    // sortOrder == int.MinValue automatic branch, so this helper has no direct native caller.
    //
    // Mathf.Max(1f, worldH) is the native unordered result: arm64 fcmp + fcsel mi
    // (0x41cc4c/0x41cc50) and x86_64 maxss (0x431910) both keep the operand that is unordered,
    // so an unordered worldH stays worldH instead of collapsing to 1.
    //
    // System.Math.Round(value, MidpointRounding.ToEven) is the native lowering: the only
    // metadata usage in the body is usage 02013 = TypeInfo:System.Math (exact), and both
    // architectures emit modf + the odd-integral correction + frintm(value + 0.5) for the
    // non-negative side and frintp(value - 0.5) for the negative side, which is round-half-
    // to-even, neither truncation nor round-half-up.
    //
    // Mathf.Clamp(scaled, 0, 3899) - 3900 is the native clamp: both architectures take the
    // maximum with 0 first (arm64 bic w8,w8,w8,asr #31 at 0x41cd10; x86_64 cmovg at 0x431a11)
    // and then the 3900 upper bound (arm64 subs/csinv lo at 0x41cd14/0x41cd18; x86_64
    // add 0xfffff0c4 + cmovae at 0x431a14/0x431a1f), which is a clamp, not an absolute value.
    public static int GroundBandOrder(int footY, float worldH)
    {
        float denominator = Mathf.Max(1f, worldH);
        int scaled = (int)Math.Round((footY * 3899f) / denominator, MidpointRounding.ToEven);
        return Mathf.Clamp(scaled, 0, 3899) - 3900;
    }
}

}
