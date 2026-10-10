using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{
    internal static void ScanOpaqueForNpcAnchor(
        Texture2D texture, out float cx, out float topf) =>
        ScanOpaque(texture, out cx, out topf);
}

}
