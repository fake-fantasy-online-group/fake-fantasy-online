using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    private static Material DollGlowMat() => EffectPlayer.GlowLayerMat(CharData.glowMode);

    private static Material DollGlowMat(int glowModel)
    {
        return EffectPlayer.GlowLayerMat(CharData.glowMode, SuitFxDB.IsGlowPremulModel(glowModel));
    }

    internal static Material DollGlowMatForIndependentRuntime(int glowModel) => DollGlowMat(glowModel);
}

}
