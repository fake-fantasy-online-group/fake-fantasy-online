using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class RemotePlayer
{

    // 06000ae5. A nonpositive maximum leaves the nonnegative incoming HP uncapped.
    public void SetHp(int value)
    {
        hp = value < 0 ? 0 : maxHp > 0 ? Mathf.Min(value, maxHp) : value;
    }
}

}
