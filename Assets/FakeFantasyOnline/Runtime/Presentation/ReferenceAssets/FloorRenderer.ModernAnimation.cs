using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class FloorRenderer
{
    // Original TickAnim (06000ba8) modern branch; the caller supplies its single Time.time read.
    private void TickModernAnim(float now)
    {
        if (v27Anim.Count <= 0) return;
        foreach (var pair in v27Anim)
        {
            AnimCellV27 cell = pair.Value;
            AnimTil animation = GetAnimTil(cell.name);
            if (animation == null) continue;
            int frame = (int)(now * 1000f / animation.frameMs) % animation.frameNum;
            Sprite sprite = GetV27Cell(cell.lb, cell.lt, cell.rt, cell.rb, cell.name, frame);
            if (sprite != null && active.TryGetValue(pair.Key, out SpriteRenderer renderer))
                renderer.sprite = sprite;
        }
    }
}

}
