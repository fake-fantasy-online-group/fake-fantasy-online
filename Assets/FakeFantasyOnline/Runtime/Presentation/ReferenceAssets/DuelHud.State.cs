using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class DuelHud
{
    public static DuelHud I;
    public static int countdown = -1;
    public static string aName = "", bName = "", winnerName = "", reason = "", lootName = "";
    public static int mode, entryGold;
    private static float recvTime, fightStartTime = -1f, endTime = -1f;
    private static byte lastPhase;

    public static void Apply(bool act, byte ph, byte rl, int cd, int ccx, int ccy, int rr, int gp,
        string a, string b, string winner, string why, string loot, int md, int entry)
    {
        mode = md; entryGold = entry;
        if (!act) { Clear(); return; }
        if (ph == 2 && lastPhase != 2) fightStartTime = Time.realtimeSinceStartup;
        if (ph == 3 && lastPhase != 3) endTime = Time.realtimeSinceStartup;
        lastPhase = ph;
        active = true; phase = ph; role = rl; countdown = cd;
        cx = ccx; cy = ccy; r = rr; gap = gp;
        aName = a ?? ""; bName = b ?? ""; winnerName = winner ?? "";
        reason = why ?? ""; lootName = loot ?? "";
        recvTime = Time.realtimeSinceStartup;
        recvMapId = GameBootstrap.CurMapId;
        if (I == null) new GameObject("DuelHud").AddComponent<DuelHud>();
    }

    // Official 1.6.0 0x06000294 (ARM64 0x3feaa0). Only spectators on the
    // received duel map lock the camera; phase and PPU do not add further gates.
    public static bool CameraLock(out Vector3 pos)
    {
        pos = Vector3.zero;
        if (!active || role != 0 || GameBootstrap.CurMapId != recvMapId) return false;
        pos = W2U(cx, cy);
        return true;
    }

    public static void Clear()
    {
        active = false;
        lastPhase = 0;
        fightStartTime = endTime = -1f;
        if (I != null) I.SetRing(false);
    }
    private void Awake() => I = this;
}
}
