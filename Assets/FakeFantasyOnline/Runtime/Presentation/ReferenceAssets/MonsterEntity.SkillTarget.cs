using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class MonsterEntity
{
    public bool NoAttack => friendly || ally;
    public Vector3 CenterPos() => transform.position + new Vector3(0f, headTopU * 0.5f, 0f);
    internal void BindHeadTopForIndependentRuntime(float value) => headTopU = value;
}

}
