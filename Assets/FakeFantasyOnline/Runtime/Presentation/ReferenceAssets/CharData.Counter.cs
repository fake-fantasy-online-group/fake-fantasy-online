using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal static partial class CharData
{
    // Official static field +0xd4 (212), System.Single, the Time.realtimeSinceStartup stamp the
    // self counter-skill packet was received at.
    internal static float counterRecvTime;

    // Official NetClient.Dispatch (0x0600077f) case 0x8071, the four stores of that case body in
    // their native order: counterCharges (+0xc8) from Buf.U8, counterTtlMs (+0xcc) from the first
    // Buf.I32, counterSkillId (+0xd0) from the second Buf.I32 or from the literal 0 of the
    // Remaining < 1 arm, and only then counterRecvTime (+0xd4) from Time.realtimeSinceStartup -
    // arm64 0x258624 / 0x258628 bl 0x104dde0 / 0x258634 str s0,[x8,#0xd4], x86_64 0x25a233 /
    // 0x25a235 callq 0x10402e0 / 0x25a244 movss %xmm0,0xd4(%rax). The clock is taken at this
    // point of this packet and not restamped per frame. No value is clamped, defaulted or
    // deduplicated: a second packet overwrites all four slots.
    internal static void ReceiveCounterWindowForIndependentRuntime(
        int charges,
        int ttlMilliseconds,
        int skillId)
    {
        counterCharges = charges;
        counterTtlMs = ttlMilliseconds;
        counterSkillId = skillId;
        counterRecvTime = Time.realtimeSinceStartup;
    }

    // Official 0x06000f1b, static System.Int32 CharData.CounterChargesNow().
    //
    // arm64 rva 0x393f48 (276 bytes) and x86_64 rva 0x3a7240 (272 bytes): counterCharges is read
    // first and gates everything; only a positive charge count is checked against the ttl window,
    // so an expired or unarmed window reports zero rather than the stale count.
    public static int CounterChargesNow()
    {
        if (counterCharges > 0 &&
            Time.realtimeSinceStartup - counterRecvTime < counterTtlMs / 1000f)
        {
            return counterCharges;
        }
        return 0;
    }

    // Official 0x06000f1c, static System.Int32 CharData.CounterRemainSec().
    //
    // arm64 rva 0x39405c (240 bytes): scvtf/divss by 1000.0, two subs, then frintp + fcvtps
    // followed by "bic w8, w8, w8, asr #31" and an fcmp against +INFINITY with csel.
    // x86_64 rva 0x3a7350 (256 bytes): cvtsi2ss/divss/subss/subss, then "roundss xmm0, xmm1, 0xa"
    // (round toward plus infinity), cvttss2si, and "test ecx,ecx / cmovg". Both architectures are
    // therefore a single-precision ceiling followed by a clamp of negatives to zero.
    public static int CounterRemainSec()
    {
        float remain = counterTtlMs / 1000f - (Time.realtimeSinceStartup - counterRecvTime);
        int secs = Mathf.CeilToInt(remain);
        return secs > 0 ? secs : 0;
    }
}

}
