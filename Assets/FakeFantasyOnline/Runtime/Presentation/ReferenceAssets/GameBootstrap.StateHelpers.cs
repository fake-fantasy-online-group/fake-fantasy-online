using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{
    // Official 1.5.8 token 060003be. Bounds touching on either axis overlap; Z is ignored.
    private static bool Overlap2D(Bounds a, Bounds b)
    {
        return a.min.x <= b.max.x && a.max.x >= b.min.x &&
            a.min.y <= b.max.y && a.max.y >= b.min.y;
    }

    // Official 1.5.8 token 060003ca deliberately does not use its name argument.
    private static GameObject NewObjGo(string name)
    {
        return new GameObject();
    }
}

}
