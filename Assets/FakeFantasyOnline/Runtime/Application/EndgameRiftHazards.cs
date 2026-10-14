using System;
using System.Collections.Generic;
using System.Globalization;

namespace FakeFantasyOnline.Application
{
/// <summary>Exact server rectangle. Its parent hazard owns it and supplies the deadline.</summary>
public sealed class EndgameRiftWallView
{
    public string ID { get; }
    public double X { get; }
    public double Y { get; }
    public double X2 { get; }
    public double Y2 { get; }
    public double Width { get; }
    internal EndgameRiftWallView(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Keys(d, "id", "x", "y", "x2", "y2", "width");
        EndgameData.Required(d, "id", "x", "y", "x2", "y2", "width");
        ID = EndgameData.String(d, "id", 128);
        X = EndgameData.Number(d, "x"); Y = EndgameData.Number(d, "y");
        X2 = EndgameData.Number(d, "x2"); Y2 = EndgameData.Number(d, "y2"); Width = EndgameData.Number(d, "width");
        double dx = X2 - X, dy = Y2 - Y, length = EndgameRiftWallCollision.Hypot(dx, dy);
        if (ID.Length == 0 || Math.Abs(X) > 1e7 || Math.Abs(Y) > 1e7 || Math.Abs(X2) > 1e7 || Math.Abs(Y2) > 1e7 ||
            Width <= 0 || Width > 1024 || length <= 0 || length > 2048)
            throw new FormatException("Invalid rift wall.");
    }
}
/// <summary>Additive v6 effect snapshot. Ability IDs, geometry and lifetime never depend on translated names.</summary>
public sealed class EndgameRiftHazardView
{
    public string ID { get; }
    public string SourceID { get; }
    public int AbilityID { get; }
    public string Mechanism { get; }
    public int X { get; }
    public int Y { get; }
    public int Radius { get; }
    public int Width { get; }
    public long BornMS { get; }
    public long ExpiresMS { get; }
    public int RemainingMS { get; }
    public long FirstPulseMS { get; }
    public long PulseMS { get; }
    public long PropagationMS { get; }
    public IReadOnlyList<EndgameRiftWallView> Walls { get; }
    internal EndgameRiftHazardView(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Keys(d, "id", "sourceId", "abilityId", "mechanism", "x", "y", "radius", "width", "bornMS", "expiresMS", "remainingMS", "firstPulseMS", "pulseMS", "propagationMS", "walls");
        EndgameData.Required(d, "id", "sourceId", "abilityId", "mechanism", "x", "y", "radius", "bornMS", "expiresMS", "remainingMS");
        ID = EndgameData.String(d, "id", 128); SourceID = EndgameData.String(d, "sourceId", 20);
        AbilityID = EndgameData.Int(d, "abilityId"); Mechanism = EndgameData.String(d, "mechanism", 32);
        X = EndgameData.Int(d, "x"); Y = EndgameData.Int(d, "y"); Radius = EndgameData.Int(d, "radius"); Width = EndgameData.Int(d, "width");
        BornMS = EndgameData.Long(d, "bornMS"); ExpiresMS = EndgameData.Long(d, "expiresMS"); RemainingMS = EndgameData.Int(d, "remainingMS");
        FirstPulseMS = EndgameData.Long(d, "firstPulseMS"); PulseMS = EndgameData.Long(d, "pulseMS"); PropagationMS = EndgameData.Long(d, "propagationMS");
        Walls = EndgameData.List(d, "walls", 3, v => new EndgameRiftWallView(EndgameData.Object(v)));
        string expected = AbilityID == 2 ? "death_lava" : AbilityID == 3 ? "lightning_orb" : AbilityID == 4 ? "ice_orb" : AbilityID == 6 ? "poison_pool" : AbilityID == 7 ? "walls" : "";
        if (ID.Length == 0 || !ValidEntityID(SourceID) || expected.Length == 0 || Mechanism != expected ||
            Math.Abs((double)X) > 1e7 || Math.Abs((double)Y) > 1e7 || Radius < 0 || Radius > 10000 || Width < 0 || Width > 10000 ||
            BornMS <= 0 || ExpiresMS <= BornMS || ExpiresMS - BornMS > 60000 || RemainingMS < 0 || RemainingMS > ExpiresMS - BornMS ||
            FirstPulseMS < 0 || FirstPulseMS > 60000 || PulseMS < 0 || PulseMS > 60000 || PropagationMS < 0 || PropagationMS > 60000)
            throw new FormatException("Invalid rift hazard.");
        if (Mechanism == "walls" ? Walls.Count != 3 : Walls.Count != 0 || Radius <= 0)
            throw new FormatException("Invalid rift hazard geometry.");
        if (Mechanism == "lightning_orb" && (Width <= 0 || Width > Radius || FirstPulseMS <= 0 || PulseMS <= 0 || PropagationMS <= 0 ||
            PropagationMS > PulseMS || FirstPulseMS + PropagationMS > ExpiresMS - BornMS))
            throw new FormatException("Invalid rift lightning timing.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var wall in Walls) if (!ids.Add(wall.ID)) throw new FormatException("Duplicate rift wall.");
    }
    internal static bool ValidEntityID(string value) => value.Length > 0 && value[0] != '0' &&
        uint.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out uint entityID) && entityID != 0;
}
/// <summary>Short server receipt for a blast that actually resolved; removal alone is never a blast.</summary>
public sealed class EndgameRiftImpactView
{
    public string ID { get; }
    public string Mechanism { get; }
    public int X { get; }
    public int Y { get; }
    public int Radius { get; }
    public long AtMS { get; }
    public long ExpiresMS { get; }
    internal EndgameRiftImpactView(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Keys(d, "id", "mechanism", "x", "y", "radius", "atMS", "expiresMS");
        EndgameData.Required(d, "id", "mechanism", "x", "y", "radius", "atMS", "expiresMS");
        ID = EndgameData.String(d, "id", 128); Mechanism = EndgameData.String(d, "mechanism", 32);
        X = EndgameData.Int(d, "x"); Y = EndgameData.Int(d, "y"); Radius = EndgameData.Int(d, "radius");
        AtMS = EndgameData.Long(d, "atMS"); ExpiresMS = EndgameData.Long(d, "expiresMS");
        if (ID.Length == 0 || (Mechanism != "death_lava" && Mechanism != "ice_orb") || Math.Abs((double)X) > 1e7 || Math.Abs((double)Y) > 1e7 ||
            Radius <= 0 || Radius > 10000 || AtMS <= 0 || AtMS > long.MaxValue - 700 || ExpiresMS != AtMS + 700)
            throw new FormatException("Invalid rift impact.");
    }
}
/// <summary>Shared continuous slab intersection, identical to server temporary_obstacles.go.</summary>
public static class EndgameRiftWallCollision
{
    public const double ActorClearance = 12;
    public static bool Blocks(EndgameRiftWallView wall, double fromX, double fromY, double toX, double toY)
    {
        if (wall == null) throw new ArgumentNullException(nameof(wall));
        if (!Finite(fromX) || !Finite(fromY) || !Finite(toX) || !Finite(toY)) return true;
        double dx = wall.X2 - wall.X, dy = wall.Y2 - wall.Y, length = EndgameRiftWallCollision.Hypot(dx, dy);
        double cx = (wall.X + wall.X2) / 2, cy = (wall.Y + wall.Y2) / 2;
        double x = ((fromX - cx) * dx + (fromY - cy) * dy) / length;
        double y = (-(fromX - cx) * dy + (fromY - cy) * dx) / length;
        double x2 = ((toX - cx) * dx + (toY - cy) * dy) / length;
        double y2 = (-(toX - cx) * dy + (toY - cy) * dx) / length;
        double lo = 0, hi = 1;
        return Clip(x, x2 - x, length / 2 + ActorClearance, ref lo, ref hi) &&
            Clip(y, y2 - y, wall.Width / 2 + ActorClearance, ref lo, ref hi);
    }
    // Match Go math.Hypot's scaled evaluation as well as its formula. This avoids
    // different rounding at an oriented wall's exact actor-clearance boundary.
    internal static double Hypot(double x, double y)
    {
        x = Math.Abs(x); y = Math.Abs(y);
        if (x < y) { double swap = x; x = y; y = swap; }
        if (x == 0) return 0;
        y /= x;
        return x * Math.Sqrt(1 + y * y);
    }
    private static bool Clip(double origin, double delta, double halfExtent, ref double lo, ref double hi)
    {
        if (Math.Abs(delta) < 1e-12) return Math.Abs(origin) <= halfExtent;
        double a = (-halfExtent - origin) / delta, b = (halfExtent - origin) / delta;
        if (a > b) { double swap = a; a = b; b = swap; }
        lo = Math.Max(lo, a); hi = Math.Min(hi, b);
        return lo <= hi;
    }
    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
}
