using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class GroundItems : MonoBehaviour
{
    internal class Item
    {
        public GameObject go;
        public SpriteRenderer sr;
        public string name;
        public bool owned;
        public float lockEnd;
        public int kind, quality, itemId;
        public int uniqueId;
        public long dropToken;
    }

    private static void DropSprite(Item it)
    {
        // Cancel before Unity's deferred Destroy, so a completing audio load cannot outlive pickup.
        if (it.go != null) it.go.GetComponent<GroundItemEntity>()?.CancelUniqueDrop();
        if (it.sr != null && it.sr.sprite != null) Destroy(it.sr.sprite);
    }

    internal static void DropSpriteForIndependentRuntime(Item item) => DropSprite(item);
}
}
