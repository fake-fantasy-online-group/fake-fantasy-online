using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered PlayerController facing fields/properties consumed by SkillCaster.CasterFacing.
/// Remaining PlayerController ownership is restored in adjacent partials.
/// </summary>
public partial class PlayerController : MonoBehaviour
{
    private int curDir;
    private bool flip;

    public int CurDir => curDir;

    public bool Flip => flip;

    internal void SetSkillFacingForIndependentRuntime(int direction, bool valueFlip)
    {
        curDir = Mathf.Clamp(direction, 0, 4);
        flip = valueFlip;
    }
}

}
