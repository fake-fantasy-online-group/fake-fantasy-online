using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FakeFantasyOnline.Presentation.ReferenceAssets;

// Drives the production iterator at host-frame boundaries. The two queues model FIFO
// transport scheduling, not Unity rendering or a replacement implementation of the server.
// Its crossing transcript pairs with the real server session/router portal tests.
static class MapTransitionStressContract
{
    sealed class Receipt { public int Due; public string Kind; public int Number; }
    static int cases, checks, crossingDefects, otherDefects;
    static readonly float[][] Profiles = {
        new[]{ .05f }, new[]{ .016f,.033f,.05f }, new[]{ .5f,.016f,.2f,.033f }
    };
    static void Require(bool value,string reason) { checks++;if(!value)throw new Exception(reason); }
    static GameBootstrap Fresh()
    {
        ResLoader.I=new ResLoader();MapDef.ThrowLoad=false;ArtCaches.ThrowEvict=false;
        Resources.StallUnload=false;Resources.FallbackMaps.Clear();Debug.Errors.Clear();Debug.Logs.Clear();
        LoadTrace.On=false;LoadTrace.Phases.Clear();CombatWorld.Clears=0;CombatWorld.Entities.Clear();
        Time.unscaledDeltaTime=.05f;EndgamePanel.OwnsRift=false;TestSession.Retires=0;CombatNumber.Releases=0;
        return new GameBootstrap();
    }
    static void Crossing(bool localFirst,int teleportDelivery,int uplinkDelay,int snapshotGap,int profile,bool verbose=false)
    {
        var b=Fresh();var up=new Queue<Receipt>();var down=new Queue<Receipt>();
        int frame=0,readySerial=0,lastUp=0,lastDown=teleportDelivery,batches=0,dropped=0,firstReady=-1,firstSnapshot=-1;
        bool firstReadyConsumed=false;
        var transcript=new List<string>();
        Action<string> note=value=>{if(verbose)transcript.Add(frame+":"+value);};
        // The server processed the triggering movement first and has an actual load barrier.
        // Its first 803a is in the downlink; no second physical transfer is requested.
        down.Enqueue(new Receipt{Due=teleportDelivery,Kind="teleport"});
        note("server already emitted first portal 803a; downlink delivery frame="+teleportDelivery);
        b.OnReady=owner=>{
            Require(owner.Built,"ready cannot originate before a complete production world");
            if(firstReady<0)firstReady=frame;
            int due=Math.Max(frame+1+uplinkDelay,lastUp);lastUp=due;
            up.Enqueue(new Receipt{Due=due,Kind="ready",Number=++readySerial});note("client ready queued #"+readySerial);
        };
        if(localFirst)b.SwitchMap(2,200,210);
        for(frame=0;frame<300;frame++)
        {
            Time.unscaledDeltaTime=Profiles[profile][frame%Profiles[profile].Length];
            // StartCoroutine in this host pump executes its first yield immediately;
            // do not resume that newly yielded routine a second time in this same frame.
            CoroutineRun[] scheduled=b.ScheduledBeforeHostPumpForTest();
            // Between host pumps the server consumes only frames the client really emitted.
            // First ready opens the pending map; later ready follows existing ClientSceneReady.
            while(up.Count>0&&up.Peek().Due<=frame)
            {
                Receipt ready=up.Dequeue();
                note("server consumes "+(firstReadyConsumed?"same-scene extra ready":"first ready")+" #"+ready.Number);
                firstReadyConsumed=true;batches++;
                int due=Math.Max(frame+snapshotGap,lastDown);lastDown=due;
                down.Enqueue(new Receipt{Due=due,Kind="snapshot",Number=batches});
            }
            // A whole packet is applied on the host thread before its successor, and no
            // callback re-enters synchronous BuildWorld from inside the ready sender.
            while(down.Count>0&&down.Peek().Due<=frame)
            {
                Receipt packet=down.Dequeue();
                if(packet.Kind=="teleport") {note("receive FIRST server portal 803a");b.SwitchMapAuthoritative(2,200,210);}
                else
                {
                    if(firstSnapshot<0)firstSnapshot=frame;
                    bool admitted=b.Receiving&&b.Built;
                    if(admitted)CombatWorld.Entities.Add(2001);else dropped++;
                    note("receive snapshot #"+packet.Number+" admitted="+admitted);
                }
            }
            b.StepScheduledForTest(scheduled);
            if(readySerial>0&&b.Active==0&&up.Count==0&&down.Count==0)break;
        }
        cases++;
        Require(frame<300&&b.Built&&GameBootstrap.CurMapId==2,"bounded schedule must settle on destination");
        Require(b.X==200&&b.Y==210,"FIFO crossing cannot change landing");
        Require(CombatWorld.Entities.Contains(2001),"eventual ordinary server resnapshot restores the entity");
        if(localFirst&&b.Builds.Count!=1)
        {
            crossingDefects++;
            if(verbose||crossingDefects<=3)
                Console.WriteLine($"DEFECT first-portal-crossing profile={profile} teleportFrame={teleportDelivery} readyUplink={uplinkDelay} snapshotGap={snapshotGap} firstReadyFrame={firstReady} firstSnapshotFrame={firstSnapshot} builds={b.Builds.Count} ready={b.Ready.Count} snapshots={batches} droppedSnapshots={dropped}");
        }
        else Require(b.Builds.Count==1&&b.Ready.Count==1&&batches==1,"one real transfer must need only one build/ready/batch");
        if(verbose)foreach(string line in transcript)Console.WriteLine("TRACE "+line);
    }
    static void Sequential(bool sameMapInstance,int nextDelay,int profile)
    {
        var b=Fresh();var up=new Queue<Receipt>();var down=new Queue<Receipt>();
        int frame=0,readySerial=0,batches=0;bool nextIssued=false;
        down.Enqueue(new Receipt{Due=0,Kind="teleport",Number=1});
        b.OnReady=owner=>up.Enqueue(new Receipt{Due=frame+1,Kind="ready",Number=++readySerial});
        for(frame=0;frame<300;frame++)
        {
            Time.unscaledDeltaTime=Profiles[profile][frame%Profiles[profile].Length];
            CoroutineRun[] scheduled=b.ScheduledBeforeHostPumpForTest();
            while(up.Count>0&&up.Peek().Due<=frame)
            {
                Receipt ready=up.Dequeue();batches++;
                down.Enqueue(new Receipt{Due=frame,Kind="snapshot",Number=ready.Number});
                // A genuine queued second transfer is released only after server consumes
                // the prior ready, and its packet follows that first full snapshot FIFO.
                if(!nextIssued){nextIssued=true;down.Enqueue(new Receipt{Due=frame+nextDelay,Kind="teleport",Number=2});}
            }
            while(down.Count>0&&down.Peek().Due<=frame)
            {
                Receipt packet=down.Dequeue();
                if(packet.Kind=="teleport")
                {
                    int map=packet.Number==1||sameMapInstance?2:3;
                    b.SwitchMapAuthoritative(map,200,210);
                }
                else if(b.Receiving&&b.Built)CombatWorld.Entities.Add(packet.Number==1?2001:3001);
            }
            b.StepScheduledForTest(scheduled);
            if(readySerial==2&&b.Active==0&&up.Count==0&&down.Count==0)break;
        }
        cases++;
        Require(frame<300&&b.Builds.Count==2&&b.Ready.Count==2&&batches==2,"two actual server transfers need exactly two original builds and ready messages");
        Require(GameBootstrap.CurMapId==(sameMapInstance?2:3),"rapid A-B-C/same-map instance settles at the last authoritative destination");
        Require(CombatWorld.Entities.Contains(3001)&&!CombatWorld.Entities.Contains(2001),"second real scene owns its entity set, not prior snapshot entities");
    }
    static void ExitBoundaries()
    {
        for(int boundary=0;boundary<=18;boundary++)
        foreach(bool disposal in new[]{false,true})
        {
            var old=Fresh();old.DisposeOnStop=disposal;old.SwitchMap(2,200,210);
            for(int i=0;i<boundary;i++)old.Step();
            int builds=old.Builds.Count,ready=old.Ready.Count;old.ExitForTest();
            var next=new GameBootstrap();next.SwitchMap(3,300,310);
            for(int i=0;i<60;i++){old.Step();next.Step();}
            cases++;
            Require(old.Active==0&&old.Builds.Count==builds&&old.Ready.Count==ready,"retired connection's iterator cannot mutate new world");
            Require(next.Built&&GameBootstrap.CurMapId==3&&next.Ready.Count==1,"replacement world builds once after any exit boundary");
        }
    }
    static void RetryFailures()
    {
        var b=Fresh();ResLoader.I.Missing.Add("map/qw0002.gsm");b.HitPortalForTest(2,200,210);
        ResLoader.I.Missing.Clear();b.Player.transferProcs[0].poly=false;b.Player.AdvanceTransferForIndependentRuntime();
        b.Player.transferProcs[0].poly=true;b.Player.AdvanceTransferForIndependentRuntime();b.Drain();cases++;
        if(b.Builds.Count==0&&b.Player.TransferredForIndependentRuntime){otherDefects++;Console.WriteLine("DEFECT failed-local-portal-retry: recovered asset plus leave/re-enter still cannot retry because native transferred latch remains true");}
        else Require(b.Builds.Count==1&&b.Ready.Count==1,"resource recovery admits only a fresh original portal edge");
        var io=Fresh();ResLoader.I.ThrowRead=true;bool threw=false;
        try{io.SwitchMap(2,200,210);}catch(InvalidOperationException){threw=true;}
#if ORIGINAL_BASELINE
        Require(threw,"baseline read exception propagated from actual production resource boundary");
#else
        Require(!threw&&io.Receiving,"resource exception safety path restored original receive/transition state");
#endif
        io.SwitchMap(3,300,310);io.Drain();cases++;
        if(!io.Receiving&&io.Builds.Count==0){otherDefects++;Console.WriteLine("DEFECT preflight-exception-retry: original switching stays true after real read exception; later valid SwitchMap is ignored until final exit");}
        else Require(io.Builds.Count==1&&io.Ready.Count==1&&GameBootstrap.CurMapId==3,"synchronous I/O recovery allows a new genuine destination");
    }
    static void Main()
    {
        Crossing(true,8,0,0,0,true);
        foreach(int delay in new[]{0,2,4,6,9,14,22})
        foreach(int up in new[]{0,2,9})
        foreach(int gap in new[]{0,1,3})
        for(int p=0;p<Profiles.Length;p++)Crossing(true,delay,up,gap,p);
        foreach(int delay in new[]{0,4,14})
        foreach(int up in new[]{0,9})
        foreach(int gap in new[]{0,3})
        for(int p=0;p<Profiles.Length;p++)Crossing(false,delay,up,gap,p);
        foreach(bool same in new[]{false,true})
        foreach(int gap in new[]{0,1,3,12})
        for(int p=0;p<Profiles.Length;p++)Sequential(same,gap,p);
#if !ORIGINAL_BASELINE
        ExitBoundaries();
#endif
        RetryFailures();
        Console.WriteLine($"STRESS cases={cases} safetyAssertions={checks} samePhysicalPortalDoubleBuild={crossingDefects} retryDefects={otherDefects}; FIFO queues and actual production iterator, no Unity rendering or live network");
        // Desired one-pass behavior is intentionally not redefined to accept a second
        // build. A nonzero result is a reproducible defect report, not a green suite.
        if(crossingDefects+otherDefects>0)Environment.ExitCode=1;
    }
}
