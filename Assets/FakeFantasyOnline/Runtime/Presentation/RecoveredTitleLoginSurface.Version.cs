using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

namespace FakeFantasyOnline.Presentation
{
internal static partial class RecoveredTitleLoginSurface
{
    // Official TitleFlow.ClientVer. This is the local build literal, not a LatestVer seed.
    internal const string ClientVer = "1.6.0";

    // Official 06000ee7; keep the editor and empty-publication short circuits in order.
    public static bool NeedUpdate =>
        !UnityEngine.Application.isEditor &&
        !string.IsNullOrEmpty(NetClient.LatestVer) &&
        NetClient.LatestVer != ClientVer;

}
}
