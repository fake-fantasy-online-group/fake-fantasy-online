using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// User-selected C, 2026-10-14. Engine-free math shared by generation and contract tests.
internal static class UniqueLootBeamCProfile
{
    internal const int Height = 540, Center = 225, Foot = 605, Frames = 20, Sparks = 12;
    internal const float CycleMilliseconds = 4000f;
    internal static double Clamp(double x, double lo, double hi) => Math.Max(lo, Math.Min(hi, x));
    internal static double Width(double z) => 1.4 + 49 * Math.Pow(1 - Clamp(z, 0, 1), 1.65);
    internal static double Pulse(double milliseconds) => 1 + .035 * Math.Sin(milliseconds * Math.PI * 2 / CycleMilliseconds);
    internal static void Envelope(double x, double y, out double primary, out double correction)
    {
        double z = Clamp((Foot - y) / Height, 0, 1), w = Width(z), dx = x - Center;
        double gate = y <= Foot && y >= Foot - Height ? Clamp((1-z)*12,0,1)*Clamp((Foot-y+5)/12,0,1) : 0;
        double halo = Math.Exp(-Math.Pow(dx/(w+2),2)*1.7)*.32*(1-.55*z);
        double core = Math.Exp(-Math.Pow(dx/(1.1+w*.13),2)*2)*.90*(.68+.32*(1-z));
        double shoulder = Math.Exp(-Math.Pow(dx/(w*.44+1),2)*2.1)*.24;
        primary = (halo+core+shoulder)*gate;
        correction = core*gate;
    }
    internal static double Pool(double x, double y) => .48*Math.Exp(-1.7*(Math.Pow((x-Center)/52,2)+Math.Pow((y-Foot)/15,2)));
    internal static double Filament(double x, double y, double sourceLuminance)
    {
        if(y>Foot || y<Foot-Height)return 0;
        double z=Clamp((Foot-y)/Height,0,1),w=Width(z);
        return Math.Max(sourceLuminance-.18,0)*Math.Exp(-Math.Pow((x-Center)/(w+2),2)*1.2)*.42*Clamp((1-z)*12,0,1)*Clamp((Foot-y+5)/12,0,1);
    }
    internal static void SourceSample(double x,double y,out int sx,out int sy)
    {
        double z=Clamp((Foot-y)/Height,0,1);
        sx=(int)Clamp((x-Center)/(Width(z)+1)*38+54,0,107);
        sy=(int)Clamp(177-z*162,0,203);
    }
    internal static void Spark(int index,double milliseconds,out double x,out double y,out double intensity)
    {
        double u=milliseconds/CycleMilliseconds+index*.137;u-=Math.Floor(u);
        x=Math.Sin(index*4.7+u*4)*49*(1-u)*.60;
        y=u*Height*.75;
        intensity=Math.Pow(Math.Sin(Math.PI*u),2)*.55;
    }
    internal static byte Byte(double value) => (byte)Clamp(value,0,255);
}
}
