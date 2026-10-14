using System;
using FakeFantasyOnline.Presentation.ReferenceAssets;
internal static class UniqueLootBeamCProfileContract
{
    private static int checks;
    private static void Check(bool value,string name){checks++;if(!value)throw new Exception(name);}
    private static void Near(double actual,double expected,string name){Check(Math.Abs(actual-expected)<1e-8,name+": "+actual);}
    internal static void Run()
    {
        Near(UniqueLootBeamCProfile.Width(0),50.4,"approved bottom width");
        Near(UniqueLootBeamCProfile.Width(1),1.4,"approved top width");
        Near(UniqueLootBeamCProfile.Pulse(1000),1.035,"approved pulse peak");
        Near(UniqueLootBeamCProfile.Pulse(3000),.965,"approved pulse trough");
        Near(UniqueLootBeamCProfile.Pulse(0),UniqueLootBeamCProfile.Pulse(4000),"pulse loop seam");
        UniqueLootBeamCProfile.Envelope(225,605,out double primary,out double core);
        Near(primary,(.32+.90+.24)*5/12,"approved root intensity");Near(core,.90*5/12,"approved warm center correction");
        UniqueLootBeamCProfile.Envelope(225,65,out primary,out core);Near(primary,0,"tip faded");
        UniqueLootBeamCProfile.Envelope(225,606,out primary,out core);Near(primary,0,"column stops at foot");
        Near(UniqueLootBeamCProfile.Pool(225,605),.48,"ground light intensity");
        Near(UniqueLootBeamCProfile.Filament(225,605,1),.82*.42*5/12,"source filament intensity");
        UniqueLootBeamCProfile.SourceSample(225,605,out int x,out int y);Check(x==54&&y==177,"native foot source sample");
        UniqueLootBeamCProfile.SourceSample(225,65,out x,out y);Check(x==54&&y==15,"native top source sample");
        for(int i=0;i<12;i++)
        {
            UniqueLootBeamCProfile.Spark(i,0,out double x0,out double y0,out double a0);
            UniqueLootBeamCProfile.Spark(i,4000,out double x1,out double y1,out double a1);
            Near(x0,x1,"spark loop x");Near(y0,y1,"spark loop y");Near(a0,a1,"spark loop intensity");
            Check(y0>=0&&y0<405&&a0>=0&&a0<=.55,"bounded spark");
        }
        for(int row=0;row<541;row++)
        {
            UniqueLootBeamCProfile.Envelope(215,65+row,out double a,out double b);
            UniqueLootBeamCProfile.Envelope(235,65+row,out double c,out double d);
            Near(a,c,"symmetric envelope");Near(b,d,"symmetric core");Check(a>=0&&b>=0,"nonnegative light");
        }
        Console.WriteLine("PASS "+checks+" approved C profile checks");
    }
}
