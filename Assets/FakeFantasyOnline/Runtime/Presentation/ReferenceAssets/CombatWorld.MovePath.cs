using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official CombatWorld movement owner for the 0x8018 path message.
/// </summary>
/// <remarks>
/// Verified instruction by instruction against 0x06000149 (arm64 0x242ef4, x86_64 0x250760) and
/// 0x06000148 (arm64 0x242d68, x86_64 0x250620), with the static layout taken from the frozen
/// atlas (MoveRecv +0x10, MoveSelf +0x18, MovePlayer +0x20, MoveMob +0x28, MoveNoTarget +0x30,
/// MoveNoWorld +0x38, NoTargetSample +0x40 as List&lt;int&gt;).
/// The official resolution order is: MoveRecv count, then - only when xs is not null - the local
/// player id held in NetClient.SelfEntityId (static +0x7c of the NetClient type reached through
/// the metadata slot 0x148b968), then the Unity-null test of CombatWorld.I (Object.op_Equality),
/// then I.players (+0x28), then I.monsters (+0x20), then NoteNoTarget.
/// The local branch warps to the last point of each array - xs[xs.Length - 1] and
/// ys[ys.Length - 1] are read with their own lengths at arm64 0x243174..0x2431ac and
/// x86_64 0x250a81..0x250ae2, then tail into PlayerController.Warp (arm64 0x2a9e30, x86_64
/// 0x2ba910) - so it is not the remote negative-speed first-point rule used by
/// RemotePlayer.SetPath and MonsterEntity.SetPath, and a null or empty ys throws exactly like
/// the original instead of being rejected up front.
/// A zero-length xs skips the local branch and MoveSelf entirely (arm64 0x242fec branches to
/// 0x243038) and continues with the dictionary chain, so an empty authoritative path for the
/// local id still reaches the no-target sample.
/// The six counters and the sample list keep the official declarations: the frozen atlas gives
/// flags 22 (Public | Static) for MoveRecv, MoveSelf, MovePlayer, MoveMob, MoveNoTarget and
/// MoveNoWorld and flags 54 (Public | Static | InitOnly) for NoTargetSample, so they are
/// public static and public static readonly rather than narrowed to the current consumer set.
/// Nothing else reads or writes them, and no further diagnostic surface is added here.
/// </remarks>
public partial class CombatWorld : MonoBehaviour
{
    // Assigned by the official .cctor (arm64 0x24a678): the new List<int> is built by the two
    // argument helper at 0x9b37e4 with no capacity argument and stored to the static slot +0x40,
    // so the initial capacity is the BCL default and 12 is a sample limit, not a capacity.
    public static long MoveRecv;
    public static long MoveSelf;
    public static long MovePlayer;
    public static long MoveMob;
    public static long MoveNoTarget;
    public static long MoveNoWorld;
    public static readonly List<int> NoTargetSample = new List<int>();

    public static void MovePath(int id, int[] xs, int[] ys, int speed)
    {
        MoveRecv++;
        if (xs != null && id == NetClient.SelfEntityId && xs.Length != 0)
        {
            MoveSelf++;
            // The original tests the raw reference here (arm64 0x24302c cbnz, x86_64 0x250887
            // test rdi,rdi), not Object.op_Equality, so a destroyed controller is still entered
            // and fails inside Warp exactly like the original.
            if (ReferenceEquals(PlayerController.I, null))
            {
                return;
            }

            PlayerController.I.Warp(xs[xs.Length - 1], ys[ys.Length - 1]);
            return;
        }

        if (!I)
        {
            MoveNoWorld++;
            return;
        }

        RemotePlayer player;
        if (I.players.TryGetValue(id, out player) && player)
        {
            MovePlayer++;
            player.SetPath(xs, ys, speed);
            return;
        }

        MonsterEntity monster;
        if (I.monsters.TryGetValue(id, out monster) && monster)
        {
            MoveMob++;
            monster.SetPath(xs, ys, speed);
            return;
        }

        NoteNoTarget(id);
    }

    /// <summary>
    /// Records one authoritative id that no live player or monster answered.
    /// </summary>
    /// <remarks>
    /// Verified against 0x06000148 (arm64 0x242d68, x86_64 0x250620): MoveNoTarget (+0x30) is
    /// incremented first, then NoTargetSample (+0x40) is used as a List&lt;int&gt;.
    ///   - a null list throws (arm64 0x242e18, x86_64 0x250693);
    ///   - a size above 11 returns without inserting (arm64 0x242e20, x86_64 0x25069c), so 12 is a
    ///     sample limit and not an initial capacity;
    ///   - an id the list already holds returns without inserting: the generic shared body at
    ///     arm64 0x9b4500 / x86_64 0x9ba3b0 receives the list as the first argument, the id as the
    ///     second and the rgctx method slot 0x1491e58, and its bool result is tested at arm64
    ///     0x242e5c / x86_64 0x2506df. Neither body carries a label in the frozen index, so the
    ///     identity rests on the receiver type plus the _items/_size/_version field accesses and
    ///     the identical two-architecture shape, not on a symbol name;
    ///   - otherwise _version (+0x1c) is incremented before the insert, which is exactly
    ///     List&lt;int&gt;.Add: the inline _items[_size++] write at arm64 0x242ea0..0x242ebc with
    ///     the shared growth body arm64 0x9b425c / x86_64 0x9ba120 as the fallback.
    /// No logging, character creation or movement is invented here; the original behavior is a
    /// bounded diagnostic sample of at most 12 distinct ids.
    /// </remarks>
    public static void NoteNoTarget(int id)
    {
        MoveNoTarget++;
        if (NoTargetSample.Count > 11)
        {
            return;
        }

        if (NoTargetSample.Contains(id))
        {
            return;
        }

        NoTargetSample.Add(id);
    }
}

}
