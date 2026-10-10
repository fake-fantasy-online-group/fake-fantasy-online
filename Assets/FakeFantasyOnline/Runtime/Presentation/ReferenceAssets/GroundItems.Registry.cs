using System.Collections.Generic;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class GroundItems
{
    private static GroundItems I;
    private readonly Dictionary<int, Item> items = new Dictionary<int, Item>();
    private float ppu = 100f, worldW = 18432f, worldH = 10752f;
    private object frameOwnerForIndependentRuntime;

    private static void Ensure()
    {
        if (I != null) return;
        var root = new GameObject("GroundItems");
        I = root.AddComponent<GroundItems>();
        Transform player = CombatNumber.PlayerTransform;
        PlayerController controller = player != null ? player.GetComponent<PlayerController>() : null;
        if (controller != null)
        {
            I.ppu = controller.ppu;
            I.worldW = controller.worldW;
            I.worldH = controller.worldH;
        }
    }

    public static void Remove(int id)
    {
        if (I == null || !I.items.TryGetValue(id, out Item item)) return;
        I.items.Remove(id);
        DropSprite(item);
        if (item.go != null) Destroy(item.go);
    }

    public static void ClearAll()
    {
        if (I == null) return;
        foreach (Item item in I.items.Values)
        {
            DropSprite(item);
            if (item.go != null) Destroy(item.go);
        }
        I.items.Clear();
    }

    internal static bool TryGetForIndependentRuntime(int id, out GroundItems owner, out Item item)
    {
        owner = I;
        item = null;
        return owner != null && owner.items.TryGetValue(id, out item);
    }
    internal void BindExistingFrameForIndependentRuntime(object frame) => frameOwnerForIndependentRuntime = frame;

    internal static GroundItems BindFrameForIndependentRuntime(object frame)
    {
        Ensure();
        I.frameOwnerForIndependentRuntime = frame;
        return I;
    }
    internal void RegisterForIndependentRuntime(int id, Item item) => items[id] = item;
    internal bool ReleaseForIndependentRuntime(int id, Item item)
    {
        if (I != this || !items.TryGetValue(id, out Item current) || !ReferenceEquals(current, item)) return false;
        Remove(id);
        return true;
    }
    internal bool ClearFrameForIndependentRuntime(object frame, bool clearItems = true)
    {
        if (I != this || !ReferenceEquals(frameOwnerForIndependentRuntime, frame)) return false;
        if (clearItems) ClearAll();
        frameOwnerForIndependentRuntime = null;
        return true;
    }
}
}
