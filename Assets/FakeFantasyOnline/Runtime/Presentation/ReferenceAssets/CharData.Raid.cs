using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal static partial class CharData
{
    // Official CharData static slots behind the raid (天神考验) marquee row. Every offset is the
    // displacement from the CharData static blob at TypeInfo +0xb8, pinned by
    // metadata_usage_01441_kind1 in both slices; the type of each slot is the atlas field type at
    // that offset, not a name chosen here:
    //   +0x91 System.Boolean  raid active
    //   +0x94 System.Int32    current wave
    //   +0x98 System.Int32    total waves
    //   +0x9c System.Int32    seconds to the next wave, negative for "no more waves"
    //   +0xa0 System.Int32    seconds left of the whole trial
    //   +0xa4 System.Int32    monsters alive
    //   +0xa8 System.Single   Time.realtimeSinceStartup stamp of the packet
    internal static bool raidActive;

    internal static int raidWave;

    internal static int raidTotal;

    internal static int raidNextSec;

    internal static int raidRemainSec;

    internal static int raidAlive;

    internal static float raidRecvTime;

    // NetClient.Dispatch (0600077f) case 0x805c — jump-table index 0x805c - 0x8001 = 91 of the 118
    // entry table at arm64 file offset 0x1126fc4 (arm64 handler 0x24dae0, x86_64 0x25b5cd; frozen
    // contract VERSION_UPGRADE_1_5_8_DISPATCH_SWITCH_ARM64_V1.json). Native order of the arm64
    // body: Buf.U8 -> +0x91, Buf.I16 -> +0x94, Buf.I16 -> +0x98, Buf.I32 -> +0x9c, Buf.I32 ->
    // +0xa0, Buf.I16 -> +0xa4 (0x24db70..0x24db7c), then the clock into +0xa8 (0x24db84 ->
    // 0x24db90). x86_64 repeats the same six reads and seven stores from 0x25b5cd.
    //
    // The active byte is normalised inside the case, not at the draw: arm64 0x24db64 cmp w20,#0 /
    // 0x24db6c cset w9,ne and x86_64 0x25b644 cmp / 0x25b652 setne both store 0/1, so the draw
    // side test against literal 1 (arm64 0x4398d4 cmp w8,#1) is the ordinary bool read and no
    // other byte value can reach the row.
    //
    // Sole writer of all seven slots in the frozen managed image: a register-tracked scan of the
    // arm64 il2cpp section for stores against a CharData statics base finds only these, while the
    // same scan does find the sibling counter slots written by case 0x8071 and cleared by the
    // 0x8003 SelfData prefix. No official body clears +0x91..+0xa8, so the row keeps the last
    // 0x805c payload it was given.
    internal static void ReceiveRaidStateForIndependentRuntime(
        bool active,
        int wave,
        int total,
        int nextSec,
        int remainSec,
        int alive)
    {
        raidActive = active;
        raidWave = wave;
        raidTotal = total;
        raidNextSec = nextSec;
        raidRemainSec = remainSec;
        raidAlive = alive;
        raidRecvTime = Time.realtimeSinceStartup;
    }
}
}
