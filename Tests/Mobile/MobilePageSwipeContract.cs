using System;
using FakeFantasyOnline.Presentation.ReferenceAssets;
class MobilePageSwipeContract
{
    static int checks;
    static void Check(bool pass,string message) { checks++; if(!pass) throw new Exception(message); }
    static void Main()
    {
        foreach(float scale in new[]{.5f,1f,2f})
        {
            float threshold=24f*scale;
            var g=new MobilePageSwipe();
            g.Begin(1,100,100);
            Check(g.Sample(1,100,100,threshold,true,false)==0,"Tap never cycles page");
            g.Begin(1,100,100);
            for(int i=0;i<100;i++) Check(g.Sample(1,100,100,threshold,false,false)==0,"Hold never cycles page");
            Check(g.Sample(1,100,100,threshold,true,false)==0,"Long hold release never cycles page");
            g.Begin(1,100,100);
            g.Begin(2,200,100);
            Check(g.Pointer==1,"Second finger cannot steal page ownership");
            Check(g.Sample(2,0,100,threshold,true,false)==0&&g.Pointer==1,"Second finger release ignored");
            Check(g.Sample(1,100-threshold,100,threshold,false,false)==0,"Swipe waits for release");
            Check(g.Sample(1,100-threshold,100,threshold,true,false)==1,"Left swipe advances");
            Check(g.Sample(1,100-threshold,100,threshold,true,false)==0,"Release resolves once");
            g.Begin(1,100,100);
            Check(g.Sample(1,100+threshold,100,threshold,true,false)==-1,"Right swipe returns");
            g.Begin(1,100,100);
            Check(g.Sample(1,100-threshold*.9f,100,threshold,true,false)==0,"Small drag ignored");
            g.Begin(1,100,100);
            Check(g.Sample(1,100-threshold,100-threshold,threshold,true,false)==0,"Diagonal ignored");
            g.Begin(1,100,100);
            g.Sample(1,100,100+threshold*2,threshold,false,false);
            Check(g.Sample(1,100-threshold*3,100,threshold,true,false)==0,"Vertical scroll cannot turn into page change");
            g.Begin(1,100,100);
            Check(g.Sample(1,100-threshold*3,100,threshold,true,true)==0,"Canceled swipe never fires");
            Check(g.Pointer==-1,"Cancellation releases owner");
            g.Begin(-2,100,100);
            Check(g.Sample(-2,100-threshold*3,100,threshold,true,false)==1,"Desktop mobile simulation supports mouse owner");
            g.Begin(1,100,100);g.Reset();
            Check(g.Sample(1,0,100,threshold,true,false)==0,"Viewport/focus/modal reset cancels pending swipe");
        }
        Check(MobilePageSwipe.PageAfter(7,1)==0,"Page eight wraps to first");
        Check(MobilePageSwipe.PageAfter(0,-1)==7,"Page one wraps to last");
        Console.WriteLine("PASS mobile page swipe: "+checks+" production gesture assertions.");
    }
}
