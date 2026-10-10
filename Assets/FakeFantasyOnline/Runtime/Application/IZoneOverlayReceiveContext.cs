namespace FakeFantasyOnline.Application
{
public interface IZoneOverlayReceiveContext
{
    void EnsureArenaRoster();
    void ApplyZoneOverlay(bool active, int cx, int cy, int radius,
        int nextCx, int nextCy, int nextRadius, byte phase, int phaseSeconds,
        int ring, int rings, int alive, bool outside, int damagePerSecond,
        bool canSurrender, int cards, int deaths, string reason, int radiusBonus);
}
}
