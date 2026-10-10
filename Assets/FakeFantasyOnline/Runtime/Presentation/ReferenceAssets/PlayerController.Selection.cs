using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController : ISelectable
{
    public Transform SelTransform => this != null ? transform : null;
    public string SelName => CharData.name;
    public int SelLevel => CharData.level;
    public int SelHp => CharData.hp;
    public int SelMaxHp => CharData.maxHp;
    public bool SelFriendly => true;
    public int SelOwnerId => -1;
    public Vector3 SelFeetPos => transform.position;

    private ISelectable PickSelectable(Vector2 point)
    {
        int count = Physics2D.OverlapPointNonAlloc(point, _overlapBuf);
        ISelectable nearest = null, npc = null;
        float nearestDistance = float.MaxValue, npcDistance = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            Collider2D collider = _overlapBuf[i];
            ISelectable selected = collider.GetComponent<ISelectable>();
            if (selected == null) continue;
            if (selected.SelTransform == null) continue;
            Vector2 delta = (Vector2)collider.bounds.center - point;
            float distance = delta.x * delta.x + delta.y * delta.y;
            if (selected is NpcInteract)
            {
                if (distance < npcDistance) { npcDistance = distance; npc = selected; }
            }
            else if (distance < nearestDistance) { nearestDistance = distance; nearest = selected; }
        }
        if (npc != null) return npc;
        if (nearest != null) return nearest;
        if (dollBodySr != null && dollBodySr.sprite != null && dollBodySr.enabled)
        {
            Bounds bounds = dollBodySr.bounds;
            float width = Mathf.Min(bounds.size.x, 60f / ppu);
            if (Mathf.Abs(point.x - bounds.center.x) <= width * 0.5f &&
                point.y >= bounds.center.y - bounds.extents.y && point.y <= bounds.center.y + bounds.extents.y) return this;
        }
        return null;
    }

    internal ISelectable PickSelectableForIndependentRuntime(Vector2 point) => PickSelectable(point);
}

internal static partial class CharData
{
    internal static int hp;
    internal static int maxHp;
}

}
