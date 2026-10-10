using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{
    private static Material isoMat;
    private static Material isoYChar;
    private static Material isoYObj;

    // Official 1.5.8 token 060003ea. The original factory remains dormant.
    public static Material IsoMat()
    {
        if (!UseIsoDepth) return null;
        if (isoMat == null)
        {
            Shader shader = Shader.Find("Custom/IsoDepth");
            if (shader != null)
            {
                Material material = new Material(shader);
                material.name = "IsoDepth(shared)";
                isoMat = material;
                bool reversed = SystemInfo.usesReversedZBuffer;
                isoMat.SetFloat("_ZTestMode", reversed ? 7f : 4f);
                isoMat.SetFloat("_Reversed", reversed ? 1f : 0f);
            }
        }
        return isoMat;
    }

    // Official 1.5.8 token 060003eb.
    public static Material IsoYObjMat()
    {
        if (!UseIsoY) return null;
        if (isoYObj == null)
        {
            Shader shader = Shader.Find("Custom/IsoDepthY");
            if (shader != null)
            {
                Material material = new Material(shader);
                material.name = "IsoYObj";
                isoYObj = material;
                isoYObj.SetFloat("_Bias", 0f);
                isoYObj.SetFloat("_ZTestMode", SystemInfo.usesReversedZBuffer ? 7f : 4f);
            }
        }
        return isoYObj;
    }

    public static Material IsoYCharMat()
    {
        if (!UseIsoY) return null;
        if (!isoYChar)
        {
            Shader shader = Shader.Find("Custom/IsoDepthY");
            if (shader)
            {
                Material material = new Material(shader);
                material.name = "IsoYChar";
                isoYChar = material;
                isoYChar.SetFloat("_Bias", -0.4f);
                isoYChar.SetFloat("_Cutoff", 0.3f);
                isoYChar.SetFloat("_ZTestMode", SystemInfo.usesReversedZBuffer ? 7f : 4f);
            }
        }
        return isoYChar;
    }
}

}
