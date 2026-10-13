using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Protocol;

// Real server bytes + real client map iterators + native ground/NPC receivers.
// Unity objects and combat registry remain substitutes. Raw FIFO order is retained; no synthetic confirmation or
// compensating snapshot is created by this replay.
static class ServerMapTraceContract
{
    sealed class Trace
    {
        public string Name; public bool Local; public int Map, X, Y, Builds, FinalMap;
        public List<byte[]>[] Batches;
    }
    sealed class Packet { public int Due, Batch; public byte[] Bytes; }
    sealed class Ready { public int Due, Number; }
    static readonly HashSet<string> visible = new HashSet<string>();
    static readonly float[][] profiles = { new[]{ .05f }, new[]{ .016f,.033f,.05f }, new[]{ .5f,.016f,.2f,.033f } };
    static int cases, checks;
    static readonly IGroundItemReceiveContext groundContext=new FakeFantasyOnline.Presentation.ClientRootView();
    static readonly ReferenceWorldReceiveOwner npcContext=new ReferenceWorldReceiveOwner();
    static void Require(bool value,string reason) { checks++; if(!value)throw new Exception(reason); }
    public static void ClearCombat() => visible.RemoveWhere(v=>v.StartsWith("32783:")||v.StartsWith("32803:"));
    static ushort Op(byte[] p) => BitConverter.ToUInt16(p,0);
    static int Id(byte[] p) => BitConverter.ToInt32(p,2);
    static bool Spawn(ushort op) => op==0x800f||op==0x8023||op==0x8013||op==0x8046;
    static string Key(ushort op,int id) => op+":"+id;
    static List<Trace> Read()
    {
        var traces=new List<Trace>();Trace current=null;
        foreach(string line in File.ReadAllLines(Environment.GetEnvironmentVariable("FFO_SERVER_MAP_TRACE_TSV")))
        {
            string[] fields=line.Split('\t');
            if(fields[0]=="T")
            {
                current=new Trace{Name=fields[1],Local=fields[2]=="1",Map=int.Parse(fields[3]),X=int.Parse(fields[4]),Y=int.Parse(fields[5]),Builds=int.Parse(fields[6]),FinalMap=int.Parse(fields[7]),Batches=new List<byte[]>[int.Parse(fields[8])]};
                for(int i=0;i<current.Batches.Length;i++)current.Batches[i]=new List<byte[]>();
                traces.Add(current);
            }
            else current.Batches[int.Parse(fields[1])].Add(Convert.FromBase64String(fields[2]));
        }
        return traces;
    }
    static void Run(Trace trace,int initialDelay,int uplinkDelay,int packetGap,int profile)
    {
        visible.Clear();GroundItems.ClearAll();GameBootstrap.ResetNpcReceiptsForTest();ResLoader.I=new ResLoader();MapDef.ThrowLoad=false;ArtCaches.ThrowEvict=false;
        Resources.StallUnload=false;Resources.FallbackMaps.Clear();Time.unscaledDeltaTime=.05f;
        var boot=new GameBootstrap();GameBootstrap.I=boot;var down=new Queue<Packet>();var up=new Queue<Ready>();
        var sourceObjects=new List<GameObject>();
        int frame=0,readyCount=0,lastDown=-1,lastUp=-1,spawnCount=0;
        Action<int,int> enqueue=(batch,earliest)=>{
            foreach(byte[] packet in trace.Batches[batch])
            {
                int due=Math.Max(earliest,lastDown<0?earliest:lastDown+packetGap);lastDown=due;
                down.Enqueue(new Packet{Due=due,Batch=batch,Bytes=packet});
            }
        };
        enqueue(0,initialDelay);
        boot.OnReady=b=>{
            Require(b.Built,trace.Name+": ready before complete build");
            int n=++readyCount;
            Require(n<trace.Batches.Length,trace.Name+": client emitted an unrecorded extra ready/rebuild");
            int due=Math.Max(frame+1+uplinkDelay,lastUp);lastUp=due;up.Enqueue(new Ready{Due=due,Number=n});
        };
        if(trace.Local)boot.HitPortalForTest(trace.Map,trace.X,trace.Y);
        for(frame=0;frame<1000;frame++)
        {
            Time.unscaledDeltaTime=profiles[profile][frame%profiles[profile].Length];
            CoroutineRun[] scheduled=boot.ScheduledBeforeHostPumpForTest();
            while(up.Count>0&&up.Peek().Due<=frame){Ready ready=up.Dequeue();enqueue(ready.Number,frame);}
            while(down.Count>0&&down.Peek().Due<=frame)
            {
                Packet packet=down.Dequeue();ushort op=Op(packet.Bytes);
                if(op==0x803a)boot.SwitchMapAuthoritative(Id(packet.Bytes),BitConverter.ToInt32(packet.Bytes,6),BitConverter.ToInt32(packet.Bytes,10));
                else if(Spawn(op))
                {
                    // Production remote/monster receipt is gated by Switching. NPC/ground
                    // receipt is not; late source records therefore require a real removal.
                    // The captured initial batch belongs to source map 1. Existing
                    // remote/monster map guards discard that source lease after ready;
                    // NPC/ground have no corresponding map guard on their native path.
                    bool admitted=op==0x8013||op==0x8046||boot.Receiving&&boot.Built&&GameBootstrap.CurMapId==(packet.Batch==0?1:trace.Map);
                    if(packet.Batch>0)Require(admitted,trace.Name+": first target snapshot lost during switching");
                    byte[] body=packet.Bytes.Skip(2).ToArray();
                    if(op==0x8013)
                    {
                        Require(PrivateServerGroundItemCodec.TryDecodeSpawn(body,out WorldGroundItemState state),contextForPacket(trace,op));
                        groundContext.SpawnGroundItem(state);
                        if(packet.Batch==0)sourceObjects.Add(GroundItems.ObjectForTest(state.GroundId));
                    }
                    else if(op==0x8046)
                    {
                        PrivateServerWorldStateDecoder.DecodeNpcState(body,out WorldNpcState state);npcContext.ReceiveNetworkNpcSpawn(state);
                        if(packet.Batch==0&&GameBootstrap.NetNpcObjectsForIndependentRuntime.TryGetValue(state.EntityId,out GameObject npc))sourceObjects.Add(npc);
                    }
                    else if(admitted)visible.Add(Key(op,Id(packet.Bytes)));
                    if(packet.Batch>0)spawnCount++;
                }
                else if(op==0x8014)groundContext.RemoveGroundItem(Id(packet.Bytes));
                else if(op==0x8047)npcContext.ReceiveNetworkNpcRemove(Id(packet.Bytes));
                else if(op==0x8010){visible.Remove(Key(0x800f,Id(packet.Bytes)));visible.Remove(Key(0x8023,Id(packet.Bytes)));}
            }
            boot.StepScheduledForTest(scheduled);
            if(readyCount==trace.Builds&&boot.Active==0&&up.Count==0&&down.Count==0)break;
        }
        string context=trace.Name+" delay="+initialDelay+" uplink="+uplinkDelay+" gap="+packetGap+" profile="+profile;
        Require(frame<1000,context+": did not settle");
        Require(boot.Builds.Count==trace.Builds&&boot.Ready.Count==trace.Builds,context+": extra/missing builds or ready");
        Require(boot.Built&&boot.Receiving&&GameBootstrap.CurMapId==trace.FinalMap,context+": wrong destination or receive gate");
        var expected=new HashSet<string>(trace.Batches.Last().Where(p=>Spawn(Op(p))).Select(p=>Key(Op(p),Id(p))));
        visible.UnionWith(GroundItems.KeysForTest());
        visible.UnionWith(GameBootstrap.NetNpcRowsForIndependentRuntime.Keys.Select(id=>Key(0x8046,id)));
        Require(expected.Count==4,context+": fixture must contain all four target entity kinds");
        Require(visible.SetEquals(expected),context+": source entity leaked or target entity missing; actual="+string.Join(",",visible)+" expected="+string.Join(",",expected));
        Require(sourceObjects.All(obj=>obj==null||obj.Destroyed),context+": stale source object survived its native removal/clear");
        Require(GroundItems.AllObjectsAliveForTest()&&GameBootstrap.NetNpcObjectsForIndependentRuntime.Values.All(obj=>obj!=null&&!obj.Destroyed),context+": source cleanup destroyed a target object");
        Require(boot.TestLocalNpc!=null&&!boot.TestLocalNpc.Destroyed,context+": source network removal destroyed the target map-local NPC");
        Require(spawnCount==4*trace.Builds,context+": first snapshots were repeated/lost");
        cases++;
    }
    static string contextForPacket(Trace trace,ushort op)=>trace.Name+": actual decoder rejected "+op.ToString("x4");
    static void Main()
    {
        List<Trace> traces=Read();
        foreach(Trace trace in traces)
        {
            int start=cases;
            foreach(int delay in new[]{0,4,8,14})
            foreach(int up in new[]{0,2,9})
            foreach(int gap in new[]{0,1,3})
            for(int profile=0;profile<profiles.Length;profile++)Run(trace,delay,up,gap,profile);
            Console.WriteLine("PASS production-wire "+trace.Name+": "+(cases-start)+" FIFO/frame schedules");
        }
        Console.WriteLine($"PASS server-recorded map trace: {traces.Count} real Session/Router sequences, {cases} client FIFO schedules, {checks} checks; real iterator/ground/NPC receivers, substitute Unity/combat registry");
    }
}
