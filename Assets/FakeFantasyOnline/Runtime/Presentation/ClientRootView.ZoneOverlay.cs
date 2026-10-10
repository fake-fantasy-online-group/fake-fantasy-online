using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView : IZoneOverlayReceiveContext
{
    void IZoneOverlayReceiveContext.EnsureArenaRoster() => ArenaRosterDlg.Ensure();

    void IZoneOverlayReceiveContext.ApplyZoneOverlay(bool active, int cx, int cy, int radius,
        int nextCx, int nextCy, int nextRadius, byte phase, int phaseSeconds,
        int ring, int rings, int alive, bool outside, int damagePerSecond,
        bool canSurrender, int cards, int deaths, string reason, int radiusBonus) =>
        ZoneOverlay.Apply(active, cx, cy, radius, nextCx, nextCy, nextRadius,
            phase, phaseSeconds, ring, rings, alive, outside, damagePerSecond,
            canSurrender, cards, deaths, reason, radiusBonus);
}
}
