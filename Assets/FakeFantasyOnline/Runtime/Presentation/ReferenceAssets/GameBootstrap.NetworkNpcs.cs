using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{
    private static Transform _netNpcParent;
    private static float _netNpcW;
    private static float _netNpcH;
    private static float _netNpcPpu;

    // Official 1.5.8 060003d7. The row is authoritative even without a live scene.
    public static void OnNetNpcSpawn(int id, NpcEntry e)
    {
        _netNpcs[id] = e;
        GameBootstrap owner = I;
        if (owner != null)
        {
            Transform parent = _netNpcParent;
            if (parent != null)
            {
                owner = I;
                if (ReferenceEquals(owner, null)) throw new NullReferenceException();
                owner.SpawnNetNpc(id, e);
            }
        }
    }

    // Official 1.5.8 060003d8. A missing retained row does not skip object removal.
    public static void OnNetNpcRemove(int id)
    {
        _netNpcs.Remove(id);
        if (_netNpcGo.TryGetValue(id, out GameObject old) && old != null)
            UnityEngine.Object.Destroy(old);
        _netNpcGo.Remove(id);
    }

    // Official 1.5.8 060003d9. Scene destruction belongs to the world lifetime.
    private static void ClearNetNpcs()
    {
        _netNpcs.Clear();
        _netNpcGo.Clear();
        _netNpcParent = null;
    }

    // Official 1.5.8 060003da. Do not remove the old key before attempting creation.
    private void SpawnNetNpc(int id, NpcEntry e)
    {
        Transform parent = _netNpcParent;
        if (parent == null) return;
        if (_netNpcGo.TryGetValue(id, out GameObject old) && old != null)
            UnityEngine.Object.Destroy(old);

        GameObject created = SpawnNpcEntry(e, _netNpcParent, _netNpcW, _netNpcH, _netNpcPpu);
        if (created != null)
        {
            _netNpcGo[id] = created;
            NpcPts.Add(new Vector2(e.x, e.y));
            if (!string.IsNullOrEmpty(e.name) && e.name.Contains("空投宝箱"))
            {
                Transform transform = created.transform;
                transform.localScale = transform.localScale * 1.8f;
            }
        }
    }

    // Bounded BuildWorld suffix: preserve the live dictionary's managed foreach cleanup.
    // Unmatched native unwind differs between original ARM and x86; this is not a claim
    // that a C# finally reproduces every compiler/runtime exception-wrapper path.
    private void PublishNetNpcSceneForIndependentRuntime(float width, float height, float ppu)
    {
        Transform parent = new GameObject("NPCs").transform;
        parent.parent = worldRoot;
        NpcPts.Clear();
        _netNpcParent = parent;
        _netNpcW = width;
        _netNpcH = height;
        _netNpcPpu = ppu;
        foreach (KeyValuePair<int, NpcEntry> pair in _netNpcs)
            SpawnNetNpc(pair.Key, pair.Value);
    }

    internal static IReadOnlyDictionary<int, NpcEntry> NetNpcRowsForIndependentRuntime => _netNpcs;
    internal static IReadOnlyDictionary<int, GameObject> NetNpcObjectsForIndependentRuntime => _netNpcGo;
}

// Decision-free Protocol-to-native argument conversion. This port is available before a
// frame exists; GameBootstrap's original fields and four bodies decide every mutation.
public sealed partial class ReferenceWorldReceiveOwner : INetworkNpcReceiveOwner
{
    public void ReceiveNetworkNpcSpawn(WorldNpcState row) => GameBootstrap.OnNetNpcSpawn(
        row.EntityId, new NpcEntry
        {
            name = row.DisplayName, sprite = row.Sprite, x = row.X, y = row.Y,
            label = row.Label, portrait = row.Portrait, script = row.Script,
            sell = row.SellList, trans = row.TransportList, dir = row.Direction
        });

    public void ReceiveNetworkNpcRemove(int id) => GameBootstrap.OnNetNpcRemove(id);
}

}
