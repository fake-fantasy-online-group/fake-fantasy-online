using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal struct ReferenceCombatWorldRemovalObservation
{
    public long Revision;
    public int EntityId;
    public byte Reason;
    public bool MonsterWasPresent;
    public bool SourceOwnerLive;
    public bool Dying;
    public int DeathFrame;
    public float DeathAlpha;
}

/// <summary>
/// Recovered official CombatWorld removal owner.
/// </summary>
/// <remarks>
/// Verified branch by branch against 0x06000144 (arm64 0x241888, x86_64 0x24f0e0). The official
/// order is trapPos.TryGetValue first (only reason == 1 plays an effect - skillId minus 10214 is
/// switched over 0..2 and the name is skipped when null or empty - then trapObj Destroy, then the
/// four trap dictionary removals), then players.TryGetValue with the dictionary removal before
/// Destroy(player.gameObject), then monsters.TryGetValue with target clearing,
/// TargetSelect.OnGone, the dictionary removal and Destroy(monster.gameObject). An id that is in
/// no dictionary returns without any call, and a Unity-null I returns after the three lookups.
/// NoteRemovalForIndependentRuntime is a product-side observation and changes no official
/// decision.
/// </remarks>
public partial class CombatWorld : MonoBehaviour
{
    private static CombatWorld I;
    // Scene adapters borrow the owner created by the original Ensure callers. Reading
    // this slot must not create or replace a CombatWorld for an otherwise empty map.
    internal static CombatWorld CurrentForIndependentRuntime => I;

    private readonly Dictionary<int, MonsterEntity> monsters =
        new Dictionary<int, MonsterEntity>();
    private readonly Dictionary<int, RemotePlayer> players =
        new Dictionary<int, RemotePlayer>();
    // Observation only: SpawnPlayer has already published this exact native identity.
    internal static RemotePlayer SpawnedPlayerForIndependentRuntime(int id) => I.players[id];

    private MonsterEntity target;
    // Keep the official nonzero initializers in one partial declaration: both native
    // constructors store these dimensions after players and before allocating trapPos.
    private float ppu = 100f;
    private float worldW = 18432f;
    private float worldH = 10752f;
    internal static float PixelsPerUnitForIndependentRuntime => I.ppu;
    private readonly Dictionary<int, Vector3> trapPos =
        new Dictionary<int, Vector3>();
    private readonly Dictionary<int, int> trapSkill =
        new Dictionary<int, int>();
    private readonly Dictionary<int, GameObject> trapObj =
        new Dictionary<int, GameObject>();
    private readonly Dictionary<int, float> trapScale =
        new Dictionary<int, float>();
    private static long removalObservationRevision;
    // Product observations must not add an allocation/failure to the original static
    // constructor, whose sole allocation is NoTargetSample. Initialize this adapter
    // storage on its own type, reached only by the existing observation consumers.
    private static Dictionary<int, ReferenceCombatWorldRemovalObservation> removalObservations =>
        RemovalObservationStorageForIndependentRuntime.Items;

    private static class RemovalObservationStorageForIndependentRuntime
    {
        internal static readonly Dictionary<int, ReferenceCombatWorldRemovalObservation> Items =
            new Dictionary<int, ReferenceCombatWorldRemovalObservation>();
    }

    internal void BindForIndependentRuntime(float pixelsPerUnit)
    {
        // A prior Ensure-created owner or retiring frame must not remain a second Unity driver.
        // Disable only that component; its dictionary/resource release stays with its owner.
        if (I != null && I != this) I.enabled = false;
        ppu = pixelsPerUnit;
        I = this;
        enabled = true;
        removalObservationRevision = 0;
        removalObservations.Clear();
    }

    // A retiring frame must not repoint static I or clear a later world's registry. Preserve
    // its existing local teardown using this instance's actual player entries only. Active
    // teardown has already run the unchanged native ClearAll and takes no second destroy path.
    internal void ReleaseRetiredPlayersForIndependentRuntime()
    {
        if (I == this) return;
        foreach (RemotePlayer player in players.Values)
            if (player != null) Destroy(player.gameObject);
        players.Clear();
    }

    internal static void RegisterSnapshotMonsterForIndependentRuntime(MonsterEntity monster)
    {
        if (monster == null) return;
        // Legacy snapshot input supplies an actual entity; ordinary native receive
        // paths already call Spawn, which owns its original Ensure entry.
        Ensure();
        I.RegisterMonsterForIndependentRuntime(monster);
    }

    internal void RegisterMonsterForIndependentRuntime(MonsterEntity monster)
    {
        if (monster != null) monsters[monster.id] = monster;
    }

    internal void UnregisterMonsterForIndependentRuntime(MonsterEntity monster)
    {
        if (monster != null)
        {
            MonsterEntity current;
            if (monsters.TryGetValue(monster.id, out current) && current == monster)
            {
                monsters.Remove(monster.id);
            }
        }
    }

    internal static void SetTargetForIndependentRuntime(MonsterEntity monster)
    {
        if (I)
        {
            I.target = monster;
        }
    }

    internal static void ClearTargetForIndependentRuntime(MonsterEntity monster)
    {
        if (I && I.target == monster)
        {
            I.target = null;
        }
    }

    internal static bool ContainsMonsterForIndependentRuntime(int id)
    {
        return I && I.monsters.ContainsKey(id);
    }

    internal static long RemovalObservationRevisionForIndependentRuntime =>
        removalObservationRevision;

    internal static bool TryGetRemovalObservationForIndependentRuntime(
        int id,
        out ReferenceCombatWorldRemovalObservation observation)
    {
        return removalObservations.TryGetValue(id, out observation);
    }

    private void OnDestroy()
    {
        if (I == this)
        {
            I = null;
        }
    }

    public static void Remove(int id, byte reason = 0)
    {
        NoteRemovalForIndependentRuntime(id, reason);
        Vector3 worldPos;
        if (I && I.trapPos.TryGetValue(id, out worldPos))
        {
            if (reason == 1)
            {
                string effectName = TrapExplodeFx(
                    I.trapSkill.GetValueOrDefault(id));
                if (!string.IsNullOrEmpty(effectName))
                {
                    EffectPlayer.NextScale =
                        I.trapScale.GetValueOrDefault(id, 1f);
                    EffectPlayer.PlayNamed(
                        worldPos,
                        effectName,
                        I.ppu,
                        1,
                        -1,
                        false,
                        false);
                }
            }

            GameObject trapObject;
            if (I.trapObj.TryGetValue(id, out trapObject) && trapObject)
            {
                Destroy(trapObject);
            }

            I.trapPos.Remove(id);
            I.trapSkill.Remove(id);
            I.trapObj.Remove(id);
            I.trapScale.Remove(id);
            return;
        }

        RemotePlayer player;
        if (I && I.players.TryGetValue(id, out player))
        {
            I.players.Remove(id);
            if (player)
            {
                Destroy(player.gameObject);
            }

            return;
        }

        if (!I)
        {
            return;
        }

        MonsterEntity monster;
        if (!I.monsters.TryGetValue(id, out monster))
        {
            return;
        }

        if (I.target == monster)
        {
            I.target = null;
        }

        if (monster)
        {
            TargetSelect.OnGone(monster);
        }

        I.monsters.Remove(id);
        if (monster)
        {
            Destroy(monster.gameObject);
        }
    }

    private static void NoteRemovalForIndependentRuntime(int id, byte reason)
    {
        MonsterEntity monster = null;
        bool present = I && I.monsters.TryGetValue(id, out monster);
        removalObservationRevision++;
        removalObservations[id] = new ReferenceCombatWorldRemovalObservation
        {
            Revision = removalObservationRevision,
            EntityId = id,
            Reason = reason,
            MonsterWasPresent = present,
            SourceOwnerLive = present && monster.UnityLiveForIndependentRuntime,
            Dying = present && monster.DyingForIndependentRuntime,
            DeathFrame = present ? monster.DeathFrameForIndependentRuntime : -1,
            DeathAlpha = present ? monster.DeathAlphaForIndependentRuntime : 0f,
        };
    }

    private static string TrapExplodeFx(int skillId)
    {
        switch (skillId)
        {
            case 10214:
                return "中毒";
            case 10215:
                return "封印";
            case 10216:
                return "冰冻";
            default:
                return "TFX032_30";
        }
    }
}

}
