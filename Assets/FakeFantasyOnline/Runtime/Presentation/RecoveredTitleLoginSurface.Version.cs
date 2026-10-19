using FakeFantasyOnline.Foundation;
using UnityEngine;

namespace FakeFantasyOnline.Presentation
{
internal static partial class RecoveredTitleLoginSurface
{
    // Application display/update version is independent of Protocol.NetClient's authentication marker.
    internal static string ClientVer => UnityEngine.Application.version;
    // Only a validated dispatch minimum may block login. Legacy LatestVer is informational:
    // its publication did not define a minimum and must not compare app 0.1.0 against wire 1.6.0.
    public static bool NeedUpdate => ClientUpdateSession.Plan != null && ClientUpdateSession.Plan.Required;
}
}
