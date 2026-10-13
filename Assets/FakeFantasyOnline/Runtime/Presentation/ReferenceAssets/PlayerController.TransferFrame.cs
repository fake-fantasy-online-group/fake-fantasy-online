using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    // Official 0x06000a27 / 0x06000a28 are the two callbacks the taoyu arm hands to
    // DifficultyBox.Show. They are cached fields of the PlayerController+<>c singleton at +0x10 /
    // +0x18 (usage 3103): each is built once by Action..ctor over the singleton instance, stored
    // through a write barrier and reused for every later frame, so repeated entries into the gate
    // allocate nothing new. Both bodies (arm64 rva 0x2c8a5c / 0x2c8afc, 160 bytes each) are one
    // read of NetClient.I, one zero test and one NetClient.GmCmd (0x060006c8) call with the string
    // literal 16229 / 16230 - nothing else, so a click runs no panel, no tip and no expectation
    // check of its own and the whole product arm for both is one static invocation per click.
    private static Action taoyuNormalCallback;
    private static Action taoyuHardCallback;
    private Func<RiftPortalContinuation> waitingRiftTransfer;
    private Func<RiftPortalContinuation> waitingSeededRiftTransfer;
    private string seededRiftRunID;
    private int seededRiftFloor;
    private long seededRiftGeneration = -1;
    private bool wasInsideSeededRiftTransfer;

    // Shared by the original component frame and the retained explicit movement diagnostic.
    // True is the original Update return after a fired transfer; false continues projection.
    internal bool AdvanceTransferForIndependentRuntime()
    {
        // Exact ordinary PlayerController.Update transfer tail (arm64 :1803-1923,
        // x86_64 :1775-1897 of the frozen 1.5.8 exports): the gate is
        // !transferred (+0x209) && transferProcs (+0x1f0) != null && onTransfer (+0x200) != null,
        // the spawn-inside edge is suppressed by Start, leaving a polygon rearms it, and a normal
        // transfer latches before the callback. Both architectures dispose the List enumerator and
        // then return from Update for the frame the trigger fires, so the transform write, the
        // sorting order, the IsMoving store, UpdateAnim and UpdateBuffAura of that frame never
        // run; the non-trigger path stores whether a polygon contained the position only after
        // enumeration ends. Exceptions dispose the enumerator and propagate without that store.
        if (!transferred && transferProcs != null && onTransfer != null)
        {
            Vector2 position = new Vector2(worldX, worldY);
            bool inside = false;
            foreach (MapLink.MapProc proc in transferProcs)
            {
                if (!MapLink.PointInPoly(position, proc.poly)) continue;

                // The first hit ends traversal even while spawn-inside suppression is active.
                // Keep callback execution inside this List enumerator's try/finally lifetime.
                if (wasInsideTransfer)
                {
                    if (waitingRiftTransfer != null)
                    {
                        RiftPortalContinuation result = waitingRiftTransfer();
                        if (result != RiftPortalContinuation.Waiting) waitingRiftTransfer = null;
                        if (result == RiftPortalContinuation.Sent) return true;
                        if (result == RiftPortalContinuation.Public) wasInsideTransfer = false;
                    }
                    if (wasInsideTransfer)
                    {
                        inside = true;
                        break;
                    }
                }

                // Reused scenery keeps its native gate polygons, but a private rift
                // owns their meaning. Never begin a speculative public-map load or script.
                if (EndgamePanel.TryTransferRiftPortalFromWorld(out waitingRiftTransfer))
                {
                    wasInsideTransfer = true;
                    return true;
                }

                if (string.IsNullOrEmpty(proc.cmd))
                {
                    // Official order: the latch is written first, then the delegate is invoked.
                    // The native body has no wasInsideTransfer write on this path - the latch is
                    // what closes the gate, so re-arming never happens after a real transfer.
                    transferred = true;
                    onTransfer(proc.mapTo, proc.toX, proc.toY);
                }
                else
                {
                    // Official special-command arm, restored in the order both frozen exports
                    // agree on (arm64 :1823-1899, x86_64 :1793-1868 of 0x06000a00):
                    //   1. SocialClient.Party is read first - the static slot +0x48 of the
                    //      SocialClient static block (usage 2421), the same list
                    //      SocialClient.InParty (0x06000d28) iterates. A null list enters the
                    //      non-returning stub at arm64 FUN_00177a74, so this is a thrown
                    //      NullReferenceException and not a skip, an empty string or an early
                    //      return: the distinction between "no party object" and "party with no
                    //      member" is exactly what separates that throw from the line below.
                    //   2. Count (List._size at +0x18) == 0 emits ChatUI.Add (0x060000e9) with
                    //      channel literal 17895 系统, text literal 18247
                    //      需要组队才能进入(一个人也要先创建队伍)。, the empty who literal 10335 and a
                    //      null share list. Nothing else happens on that path - no DifficultyBox,
                    //      no GmCmd, no focus change - and it is the whole arm for this frame.
                    //   3. Otherwise String.op_Equality compares the gate's own cmd with the
                    //      literal 16229 taoyu: an equal cmd opens DifficultyBox.Show (0x0600026b)
                    //      with title 17253 塔狱, normalDesc 17630 普通：现在的难度，一个字都没变。,
                    //      hardDesc 17233 困难：怪物更强、经验更多（头目血量最高约 24 倍）。and the two
                    //      cached callbacks above, so both sends happen only if the user then
                    //      clicks one of the two difficulty buttons; every other cmd goes straight
                    //      to NetClient.GmCmd (0x060006c8) with the cmd as the gate carried it -
                    //      no trim, no case change, no prefix either way.
                    List<(int, string, int, int, int, int, int, int, bool)> party = SocialClient.Party;
                    if ((object)party == null)
                    {
                        throw new NullReferenceException();
                    }

                    if (party.Count == 0)
                    {
                        ChatUI.Add(
                            "系统",
                            "需要组队才能进入(一个人也要先创建队伍)。",
                            new Color(1f, 120f / 255f, 120f / 255f, 1f),
                            string.Empty);
                    }
                    else if (string.Equals(proc.cmd, "taoyu", StringComparison.Ordinal))
                    {
                        if (taoyuNormalCallback == null)
                        {
                            taoyuNormalCallback =
                                () => SendTransferGmCommandForIndependentRuntime("taoyu");
                        }

                        if (taoyuHardCallback == null)
                        {
                            taoyuHardCallback =
                                () => SendTransferGmCommandForIndependentRuntime("taoyu hard");
                        }

                        // Five business arguments, not six. Both frozen exports list a trailing
                        // literal 0 at this call (06000a00.c arm64 :1900 / x86_64 :1873), which
                        // is the IL2CPP MethodInfo* slot every managed call carries: the atlas
                        // signature of 0x0600026b is static Show(String, String, String, Action,
                        // Action) - five parameters - and the same trailing slot is what the
                        // neighbouring GmCmd call site spends its last argument on even though
                        // NetClient.GmCmd (0x060006c8) declares a single String. So no sixth
                        // business argument is invented here and the UI is not re-routed.
                        DifficultyBox.Show(
                            "塔狱",
                            "普通：现在的难度，一个字都没变。",
                            "困难：怪物更强、经验更多（头目血量最高约 24 倍）。",
                            taoyuNormalCallback,
                            taoyuHardCallback);
                    }
                    else
                    {
                        // The direct other-cmd arm reads the singleton twice on both frozen
                        // exports (06000a00.c arm64 :1855 tests *(usage 2073 + 0xb8) and :1857
                        // loads the same static slot again as the GmCmd receiver; x86_64 :1827
                        // / :1829 identical), so the product carries the two reads as two reads
                        // instead of collapsing them into one cached local: a single-thread
                        // argument is not a reason to give up an order the source can carry
                        // directly. The two DifficultyBox callbacks above keep their own single
                        // read, because 0x06000a27 / 0x06000a28 each read the slot once and use
                        // that one read for both the test and the call.
                        if (NetClient.I != null)
                        {
                            NetClient.I.GmCmdForIndependentRuntime(proc.cmd);
                        }
                    }

                    // Only the command arm re-arms: both exports store 1 into +0x208 here and
                    // nowhere else. The write happens after all three sub-branches and before the
                    // shared return, so it also lands on the empty-party path and on the frame the
                    // popup opens - never after a click, which runs long after this frame ended.
                    wasInsideTransfer = true;
                }

                // Official Update returns right after the enumerator disposal, so this frame
                // performs no projection, no sorting order, no IsMoving store and no animation.
                // The frame host reaches this arm only through this result, and it must keep that
                // for the whole rest of the frame: while the local player is riding with no ride
                // anchor, the unmounted branch below already handed the animation to the caller,
                // and official Update skips that UpdateAnim/UpdateBuffAura pair too.
                return true;
            }

            // The non-trigger path writes the hit result only after enumerator disposal.
            wasInsideTransfer = inside;
            if (!inside) waitingRiftTransfer = null;
        }

        return !transferred && AdvanceSeededRiftTransfer();
    }

    private bool AdvanceSeededRiftTransfer()
    {
        if (!EndgamePanel.TryGetRiftPortalFromWorld(out string runID, out int floor,
            out long generation, out float x, out float y))
        {
            seededRiftRunID = null; waitingSeededRiftTransfer = null;
            wasInsideSeededRiftTransfer = false;
            return false;
        }
        float dx = worldX - x, dy = worldY - y;
        bool inside = dx * dx + dy * dy <= 45f * 45f;
        // A new floor/controller first observes its landing. Never auto-chain
        // floors when a reconnect or same-map handoff spawns inside the exit.
        if (seededRiftRunID != runID || seededRiftFloor != floor || seededRiftGeneration != generation)
        {
            seededRiftRunID = runID; seededRiftFloor = floor; seededRiftGeneration = generation;
            wasInsideSeededRiftTransfer = inside; waitingSeededRiftTransfer = null;
            return false;
        }
        if (!inside)
        {
            wasInsideSeededRiftTransfer = false; waitingSeededRiftTransfer = null;
            return false;
        }
        if (wasInsideSeededRiftTransfer)
        {
            if (waitingSeededRiftTransfer != null)
            {
                RiftPortalContinuation result = waitingSeededRiftTransfer();
                if (result != RiftPortalContinuation.Waiting) waitingSeededRiftTransfer = null;
                return result == RiftPortalContinuation.Sent;
            }
            return false;
        }
        wasInsideSeededRiftTransfer = true;
        return EndgamePanel.TryTransferRiftPortalFromWorld(out waitingSeededRiftTransfer);
    }

    private static void SendTransferGmCommandForIndependentRuntime(string cmd)
    {
        NetClient client = NetClient.I;
        if (client == null) return;
        client.GmCmdForIndependentRuntime(cmd);
    }
}

}
