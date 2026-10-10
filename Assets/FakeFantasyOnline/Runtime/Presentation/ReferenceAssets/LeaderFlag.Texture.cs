using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class LeaderFlag : MonoBehaviour
{
    private static Texture2D flagTex;
    private static bool flagTried;

    public static Texture2D Tex
    {
        get
        {
            if (flagTried) return flagTex;
            flagTried = true;
            ResLoader loader = ResLoader.I;
            if (loader)
                flagTex = loader.LoadGsa2021First("res/effect/paopao_tleaderflag.gsa");
            else
                flagTried = false;
            return flagTex;
        }
    }
}

}
