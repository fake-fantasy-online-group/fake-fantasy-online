using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class EffectPlayer : MonoBehaviour
{
    internal static Material BorrowAlphaMaterial() { return AlphaMat(); }
    internal static Material BorrowAdditiveMaterial() { return AddMat(); }
}
}
