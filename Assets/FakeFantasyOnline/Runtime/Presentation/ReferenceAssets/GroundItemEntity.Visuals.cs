using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class GroundItemEntity : MonoBehaviour
{
    public int id, gx, gy;
    private float sparkleT;

    private void Update()
    {
        UpdateUniqueDrop();
        if (uniqueId > 0) return;
        sparkleT += Time.deltaTime;
        if (sparkleT >= 3f)
        {
            sparkleT = 0f;
            float ppu = CombatNumber.Ppu > 0f ? CombatNumber.Ppu : 100f;
            EffectPlayer.PlayNamed(transform.position, "baowupiaoshi", ppu, 1, -1, false, false);
        }
    }
    private void OnMouseEnter()
    {
        if (CursorManager.I != null) CursorManager.I.SetFetchHover(true);
    }
    private void OnMouseExit()
    {
        if (CursorManager.I != null) CursorManager.I.SetFetchHover(false);
    }
    private void OnDisable()
    {
        ReleaseUniqueDrop();
        if (CursorManager.I != null) CursorManager.I.SetFetchHover(false);
    }
}
}
