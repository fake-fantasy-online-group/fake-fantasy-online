using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class GroundItems
{
    public static void Spawn(int id, int itemId, int count, int x, int y, string name,
        bool owned, int lockMs, int kind, int quality = 0,
        int uniqueId = 0, bool freshDrop = false, long dropToken = 0)
    {
        Ensure();
        Transform player = CombatNumber.PlayerTransform;
        PlayerController controller = player != null ? player.GetComponent<PlayerController>() : null;
        if (controller != null)
        {
            I.ppu = controller.ppu;
            I.worldW = controller.worldW;
            I.worldH = controller.worldH;
        }
        if (I.items.TryGetValue(id, out Item existing))
        {
            existing.uniqueId = uniqueId;
            existing.dropToken = dropToken;
            if (existing.go != null)
                existing.go.GetComponent<GroundItemEntity>()?.ConfigureUniqueDrop(uniqueId, freshDrop, dropToken);
            return;
        }
        var root = new GameObject(string.Format("Ground:{0}:{1}", name, id));
        root.transform.position = new Vector3((x - I.worldW * 0.5f) / I.ppu,
            (I.worldH * 0.5f - y) / I.ppu, 0f);
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        Texture2D icon = LoadIcon(itemId);
        if (icon != null) renderer.sprite = TexScale.MakeSprite(icon, I.ppu, 0.5f, 0.2f);
        renderer.sortingOrder = Mathf.Clamp(y, 0, 30000);
        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.size = renderer.sprite != null ? (Vector2)renderer.sprite.bounds.size * 2f : new Vector2(0.4f, 0.4f);
        GroundItemEntity entity = root.AddComponent<GroundItemEntity>();
        entity.id = id;
        entity.gx = x;
        entity.gy = y;
        entity.ConfigureUniqueDrop(uniqueId, freshDrop, dropToken);
        I.items[id] = new Item
        {
            go = root,
            sr = renderer,
            name = name + (count > 1 ? " ×" + count.ToString() : ""),
            owned = owned,
            lockEnd = Time.time + (float)lockMs / 1000f,
            kind = kind,
            quality = quality,
            itemId = itemId,
            uniqueId = uniqueId,
            dropToken = dropToken
        };
    }
}
}
