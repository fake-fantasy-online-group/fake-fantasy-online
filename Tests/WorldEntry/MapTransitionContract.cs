using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FakeFantasyOnline.Presentation.ReferenceAssets;

// Unity scheduling, resources and rendering are deterministic substitutes. The runner
// compiles the real BuildWorldCore, SwitchMap, transition/fade iterators and sky methods.
namespace UnityEngine
{
    public class Object { public static T FindAnyObjectByType<T>() where T : class => null; }
    public class Texture2D : Object { }
    public class Transform { public GameObject gameObject; public void SetParent(Transform parent, bool world) { } }
    public class GameObject : Object
    {
        public readonly Transform transform;
        public GameObject(string name) { transform = new Transform { gameObject = this }; }
    }
    public struct Color
    {
        public Color(float r, float g, float b, float a = 1) { }
        public static Color black => default(Color);
    }
    public struct Vector2 { public Vector2(float x, float y) { } }
    public class Camera : Object { public static Camera main = new Camera(); public Color backgroundColor; }
    public static class Time { public static float unscaledDeltaTime = .05f; }
    public static class Mathf
    {
        public static float Min(float a, float b) => Math.Min(a, b);
        public static float Clamp01(float x) => Math.Max(0, Math.Min(1, x));
    }
    public static class Debug
    {
        public static readonly List<string> Errors = new List<string>();
        public static readonly List<string> Logs = new List<string>();
        public static void Log(object value) { Logs.Add(value.ToString()); }
        public static void LogError(object value) { Errors.Add(value.ToString()); }
    }
    public sealed class AsyncOperation { public bool Done; }
    public static class Resources
    {
        public static bool StallUnload;
        public static readonly HashSet<string> FallbackMaps = new HashSet<string>();
        public static T Load<T>(string name) where T : class => FallbackMaps.Contains(name) ? new Texture2D() as T : null;
        public static AsyncOperation UnloadUnusedAssets() => new AsyncOperation { Done = !StallUnload };
    }
}

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public sealed class CoroutineRun
    {
        internal readonly Stack<IEnumerator> Stack = new Stack<IEnumerator>();
        internal object Yielded;
        internal bool Done;
        internal CoroutineRun(IEnumerator iterator) { Stack.Push(iterator); }
        internal void Stop(bool dispose = true)
        {
            Done = true;
            while (Stack.Count > 0)
            {
                IEnumerator iterator = Stack.Pop();
                if (dispose) (iterator as IDisposable)?.Dispose();
            }
        }
        internal void Advance()
        {
            if (Done || Yielded is AsyncOperation operation && !operation.Done) return;
            try
            {
                while (Stack.Count > 0)
                {
                    IEnumerator top = Stack.Peek();
                    if (!top.MoveNext()) { (Stack.Pop() as IDisposable)?.Dispose(); continue; }
                    if (top.Current is IEnumerator nested) { Stack.Push(nested); continue; }
                    Yielded = top.Current;
                    return;
                }
                Done = true;
            }
            catch { Stop(); throw; }
        }
    }
    public sealed partial class PlayerController
    {
        public float worldX, worldY;
        static int followId;
        float followLostT;
        bool followLastOk;
        void LoadMask() { }
    }
    public static class MapLink
    {
        public sealed class MapProc { public object poly; public int mapTo; public float toX, toY; public string cmd; }
        public static bool PointInPoly(Vector2 point, object polygon) => (bool)polygon;
    }
    public static class SocialClient
    {
        public static List<(int, string, int, int, int, int, int, int, bool)> Party =
            new List<(int, string, int, int, int, int, int, int, bool)>();
    }
    public static class ChatUI { public static void Add(string channel, string text, Color color, string who) { } }
    public static class DifficultyBox { public static void Show(string title, string normal, string hard, Action n, Action h) { } }
    internal enum RiftPortalContinuation { Waiting, Finished, Sent, Public }
    internal sealed class RiftHazardInvalidationStub
    {
        internal void NoteRiftSceneChange() { }
        internal void NoteRiftSceneChangeRejected(object root, int map) { }
    }
    internal static class EndgamePanel
    {
        internal static RiftHazardInvalidationStub WorldState => null;
        internal static bool OwnsRift;
        internal static bool TryTransferRiftPortalFromWorld() => OwnsRift;
        internal static bool TryTransferRiftPortalFromWorld(out Func<RiftPortalContinuation> retry) { retry = null; return OwnsRift; }
        internal static bool TryGetRiftPortalFromWorld(out string run, out int floor, out long generation, out float x, out float y)
        { run = null; floor = 0; generation = 0; x = y = 0; return false; }
    }
    public class NetClient
    {
        public static NetClient I;
        public void GmCmdForIndependentRuntime(string command) { }
    }
    public sealed class Doll { public PlayerController SourceControllerForIndependentRuntime; public Transform Root = new GameObject("player").transform; public float WorldX => SourceControllerForIndependentRuntime.worldX; public float WorldY => SourceControllerForIndependentRuntime.worldY; }
    public sealed class WorldEntryRecord
    {
        public int MapId; public float X, Y;
        public WorldEntryRecord WithMapPosition(int map, float x, float y) => new WorldEntryRecord { MapId = map, X = x, Y = y };
    }
    public enum ReferenceFirstWorldWindowFailure { None, UnexpectedConstructionFailure, MissingSourceAsset }
    public static class MusicManager { public static Music I; }
    public sealed class Music { public void PlayMap(string name) { } }
    public sealed class Frame
    {
        public float MapWidthForIndependentRuntime = 1000, MapHeightForIndependentRuntime = 1000;
        public void ObserveNativeNpcsForIndependentRuntime() { }
    }
    public sealed class TestResources { public ResLoader Core; }
    public sealed class TestMapDescriptor { public string file; }
    public sealed partial class ReferenceFirstWorldWindowOwner
    {
        readonly GameBootstrap owner;
        readonly WorldEntryRecord projected;
        public Transform WorldRoot;
        public bool Built;
        public object GossipDialog, DealDialog;
        public Frame Frame = new Frame();
        public ReferenceFirstWorldWindowFailure LastFailure;
        internal GameBootstrap.BuildWorldCheckpoints BuildCheckpointsForIndependentRuntime;
        internal GameBootstrap.PreparedMapSource PreparedMapSourceForIndependentRuntime { get; set; }
        public ReferenceFirstWorldWindowOwner(object session, Transform parent, WorldEntryRecord entry,
            object music, object sound, object names, GameBootstrap boot, object gossip, object deal)
        { owner = boot; projected = entry; }
        public void AdoptConstructionRootForIndependentRuntime(Transform root, bool transition) { WorldRoot = root; }
        public bool BuildForViewport(int width, int height, float scale)
        {
            owner.BuildMapBytes.Add(ReadConstructionMapForTest(ResLoader.I, owner.mapName));
            return owner.ConstructForTest(this, projected);
        }
        public void SetBuildGenerationForIndependentRuntime(long generation) { }
        public void Dispose() { WorldRoot = null; }
    }
    public sealed class ResLoader
    {
        public static ResLoader I = new ResLoader();
        public object Pkg;
        public readonly HashSet<string> Missing = new HashSet<string>();
        public readonly List<string> Reads = new List<string>();
        public byte[] LastBytes;
        public Action ReadHook;
        public bool ThrowRead;
        public byte[] ReadMap(string name)
        {
            Reads.Add(name);
            Action hook = ReadHook; ReadHook = null; hook?.Invoke();
            if (ThrowRead) { ThrowRead = false; throw new InvalidOperationException("resource read failed"); }
            return LastBytes = Missing.Contains(name) ? null : new byte[] { (byte)Reads.Count };
        }
    }
    public static class MapDef
    {
        public static bool ThrowLoad;
        public static void EnsureLoaded(object pkg)
        { if (ThrowLoad) { ThrowLoad = false; throw new InvalidOperationException("map definitions failed"); } }
    }
    public static class LoadTrace
    {
        public static bool On;
        public static bool AutoOnMapSwitch => false;
        public static readonly List<string> Phases = new List<string>();
        public static void Begin(string value) { }
        public static void Line(string value) { }
        public static void Phase(string value, float time) { Phases.Add(value); }
    }
    public sealed class TestSession { public bool Connected = true; public static int Retires; public void RetireCombatNumberSceneForIndependentRuntime() { Retires++; } }
    public static class CombatNumber { public static int Releases; public static void ReleaseExperienceForSceneTransition() { Releases++; } }
    public static class GroundItems { public static void ClearAll() { } }
    public static class CombatWorld
    {
        public static int Clears;
        public static readonly HashSet<int> Entities = new HashSet<int>();
        public static void ClearAll() { Clears++; Entities.Clear(); }
    }
    public static class StallDlg { public static readonly List<object> Owners = new List<object>(); }
    public static class ArtCaches
    {
        public static bool ThrowEvict;
        public static void EvictAtSafePoint()
        { if (ThrowEvict) { ThrowEvict = false; throw new InvalidOperationException("eviction failed"); } }
    }

    public partial class GameBootstrap
    {
        const float MaxFadeStep = .05f;
        bool switching;
        (int, float, float)? _queuedSwitch;
        Transform worldRoot = new GameObject("old-world").transform;
        Doll pcRef = new Doll { SourceControllerForIndependentRuntime = new PlayerController { worldX = 10, worldY = 20 } };
        string _prevMapName;
        int _pendingMapId;
        public static int CurMapId;
        public string mapName = "qw0001";
        public float spawnX = 10, spawnY = 20, fadeInSec = .15f, fadeOutSec = .2f;
        float fadeAlpha;
        static Color _pendingSky;
        public static Action<int, float, float> DoTeleport;
        TestSession session = new TestSession();
        object sharedMusic, sharedCombatSound, processNpcNames, persistentGossip, persistentDeal;
        Transform worldParent = new GameObject("parent").transform;
        WorldEntryRecord entry = new WorldEntryRecord { MapId = 1, X = 10, Y = 20 };
        bool nativeStartupWorld, configured = true;
        int viewportWidth = 800, viewportHeight = 600;
        float dpiScale = 1, pixelsPerUnit = 100;
        long windowBuildGeneration;
        ReferenceFirstWorldWindowOwner window, constructingWindow;
        public ReferenceFirstWorldWindowFailure LastBuildFailure { get; private set; }
        readonly List<object> npcSRs = new List<object>(), fadeRegs = new List<object>(), occluders = new List<object>();
        readonly Dictionary<object, object> _scanOpaqueCache = new Dictionary<object, object>();
        int ObjLoadMs, ObjSliceMs, ObjSpawnMs, ObjFadeMs, ObjN, ObjSliceN, ObjAnimN, ObjPlainN;
        readonly List<CoroutineRun> coroutines = new List<CoroutineRun>();
        public readonly List<WorldEntryRecord> Builds = new List<WorldEntryRecord>();
        public readonly List<byte[]> BuildMapBytes = new List<byte[]>();
        public readonly List<WorldEntryRecord> Ready = new List<WorldEntryRecord>();
        public readonly Queue<string> BuildOutcomes = new Queue<string>();
        public Action<GameBootstrap> OnReady;
        public int Started, MaxConcurrent;
        public bool DisposeOnStop = true;
        public bool Receiving => !switching;
        public bool Busy => Active != 0;
        public bool BlackHeld => _holdSkyBlack;
        public float Alpha => fadeAlpha;
        public int Active => coroutines.Count(c => !c.Done);
        public bool Built => worldRoot != null && window != null && window.Built;
        public float X => pcRef?.SourceControllerForIndependentRuntime.worldX ?? -1;
        public float Y => pcRef?.SourceControllerForIndependentRuntime.worldY ?? -1;
        public PlayerController Player => pcRef?.SourceControllerForIndependentRuntime;
        public void HitPortalForTest(int map, float x, float y)
        {
            Player.transferProcs = new List<MapLink.MapProc> {
                new MapLink.MapProc { mapTo = map, toX = x, toY = y, poly = true }
            };
            Player.AdvanceTransferForIndependentRuntime();
        }
        public GameBootstrap()
        {
            CurMapId = 1;
            Player.onTransfer = SwitchMap;
            window = new ReferenceFirstWorldWindowOwner(null, worldParent, entry, null, null, null, this, null, null)
                { WorldRoot = worldRoot, Built = true };
        }
        public static string MapSourceName(int id) => "qw" + id.ToString("D4");
        public CoroutineRun StartCoroutine(IEnumerator iterator)
        {
            var run = new CoroutineRun(iterator); coroutines.Add(run); Started++;
            MaxConcurrent = Math.Max(MaxConcurrent, Active);
            run.Advance();
            return run;
        }
        public void StopAllCoroutines()
        { foreach (var run in coroutines.ToArray()) run.Stop(DisposeOnStop); coroutines.Clear(); }
        public void Step()
        { foreach (var run in coroutines.Where(c => !c.Done).ToArray()) run.Advance(); }
        public CoroutineRun[] ScheduledBeforeHostPumpForTest() => coroutines.Where(c => !c.Done).ToArray();
        public void StepScheduledForTest(CoroutineRun[] scheduled)
        { foreach (var run in scheduled) if (!run.Done) run.Advance(); }
        public void Until(Func<bool> condition)
        {
            for (int i = 0; i < 500 && !condition(); i++) Step();
            if (!condition()) throw new Exception("scheduler did not reach requested checkpoint");
        }
        public void Drain() => Until(() => Active == 0);
        public bool ConstructForTest(ReferenceFirstWorldWindowOwner candidate, WorldEntryRecord projected)
        {
            Builds.Add(projected);
            // Publish a native player before a possible later UI/asset construction failure.
            pcRef = new Doll { SourceControllerForIndependentRuntime = new PlayerController { worldX = projected.X, worldY = projected.Y } };
            Player.onTransfer = SwitchMap;
            string outcome = BuildOutcomes.Count > 0 ? BuildOutcomes.Dequeue() : "ok";
            if (outcome == "throw") throw new InvalidOperationException("injected construction failure");
            if (outcome == "missing") { candidate.LastFailure = ReferenceFirstWorldWindowFailure.MissingSourceAsset; return false; }
            if (outcome == "reject") { candidate.LastFailure = ReferenceFirstWorldWindowFailure.UnexpectedConstructionFailure; return false; }
            candidate.Built = outcome != "partial";
            return true;
        }
        void SendPositionForIndependentRuntime(int map, int x, int y, bool entered)
        {
            if (!entered) return;
            Ready.Add(new WorldEntryRecord { MapId = map, X = x, Y = y });
            OnReady?.Invoke(this);
        }
        void ReleaseBuiltWorld(bool sceneRegistriesReleased = false, Transform releasedRoot = null, bool preservePlayerTransform = false)
        { worldRoot = null; window = null; constructingWindow = null; if (!preservePlayerTransform) pcRef = null; }
        void Destroy(GameObject value) { }
        int EvictMapObjectsAtSafePoint() => 0;
        string MapObjBudgetStat() => "test";
        void ClearNetNpcs() { }
        void EnsureDialogUIForIndependentRuntime() { }
        void PublishNetNpcSceneForIndependentRuntime(float width, float height, float ppu) { }
        void ApplyNetGatherZones() { }
        void ComputeOccFloors() { }
        WorldEntryRecord CaptureNativeWorldEntryForIndependentRuntime() => entry;
    }
}

static class MapTransitionContract
{
    static int checks, scenarios;
    static void Check(bool condition, string name) { checks++; if (!condition) throw new Exception(name); }
    static GameBootstrap Fresh()
    {
        ResLoader.I = new ResLoader(); MapDef.ThrowLoad = false; ArtCaches.ThrowEvict = false;
        Resources.StallUnload = false; Resources.FallbackMaps.Clear(); Debug.Errors.Clear(); Debug.Logs.Clear();
        LoadTrace.On = false; LoadTrace.Phases.Clear();
        CombatWorld.Clears = 0; CombatWorld.Entities.Clear();
        Time.unscaledDeltaTime = .05f; EndgamePanel.OwnsRift = false; TestSession.Retires = 0; CombatNumber.Releases = 0;
        return new GameBootstrap();
    }
    static void Scenario(string name, Action body)
    { body(); scenarios++; Console.WriteLine("PASS " + name); }
    static void Settled(GameBootstrap boot, int map, float x, float y)
    {
        boot.Drain();
        Check(!boot.Busy && boot.Receiving && boot.Active == 0, "transition ownership released");
        Check(!boot.BlackHeld && boot.Alpha == 0, "black overlay and sky released");
        Check(boot.Built && GameBootstrap.CurMapId == map && boot.X == x && boot.Y == y, "authoritative destination is complete");
        // A finished coroutine can synchronously start the queued one before returning,
        // but its own fading/building body must already be finished at that point.
    }
    static string State(GameBootstrap b) => $"{b.Builds.Count}:{b.Ready.Count}:{GameBootstrap.CurMapId}:{b.Receiving}:{b.Alpha:F2}:{b.BlackHeld}";
    static void Flow(string name, GameBootstrap b)
    {
        var trace = new List<string> { State(b) };
        for (int i=0; i<500 && b.Active!=0; i++) { b.Step(); trace.Add(State(b)); }
        Check(b.Active==0, "original iterator completes");
        Console.WriteLine("FLOW " + name + " " + string.Join("|",trace));
    }
    static void Main()
    {
        Scenario("first client-prebuilt map publishes one ready after a complete world", () => {
            var b=Fresh(); b.OnReady=owner=>{
                Check(owner.Built && owner.Receiving, "complete player/world and open receiver precede first ready");
                CombatWorld.Entities.Add(2001);
            };
            b.SwitchMap(2,200,210); Check(b.Builds.Count==0 && b.Ready.Count==0 && !b.Receiving, "preflight/fade precede build and ready");
            Flow("first-client",b); Settled(b,2,200,210);
            Check(b.Builds.Count==1 && b.Ready.Count==1 && CombatWorld.Entities.Contains(2001), "first snapshot survives remaining fade with no second build or ready");
        });
        Scenario("first server-driven map uses the same initial lifecycle", () => {
            var b=Fresh(); b.SwitchMapAuthoritative(2,200,210); Flow("first-server",b); Settled(b,2,200,210);
            Check(b.Builds.Count==1&&b.Ready.Count==1, "one authoritative transfer has one ready");
        });
        Scenario("consecutive completed transfers retain one ready each", () => {
            var b=Fresh(); b.SwitchMap(2,200,210); b.Drain(); b.SwitchMap(3,300,310);
            Flow("consecutive",b); Settled(b,3,300,310);
            Check(b.Builds.Select(x=>x.MapId).SequenceEqual(new[]{2,3}) && b.Ready.Select(x=>x.MapId).SequenceEqual(new[]{2,3}), "each real destination built and acknowledged once");
        });
        Scenario("real same-map transfer is not swallowed by coordinate deduplication", () => {
            var b=Fresh(); b.SwitchMapAuthoritative(1,55,65); Flow("same-map",b); Settled(b,1,55,65);
            Check(b.Builds.Count==1&&b.Ready.Count==1, "same-map world is really rebuilt and acknowledged");
        });
        Scenario("queued same landing before build does not emit compensatory second ready", () => {
            var b=Fresh(); b.SwitchMap(2,200,210); b.SwitchMapAuthoritative(2,200,210);
            Flow("queued-same",b); Settled(b,2,200,210);
            Check(b.Builds.Count==1&&b.Ready.Count==1, "original same landing suppression has no extra publication/ready");
        });
        Scenario("queued distinct authority is executed by original post-fade replay", () => {
            var b=Fresh(); b.SwitchMap(2,200,210); b.SwitchMapAuthoritative(3,300,310);
            Flow("queued-distinct",b); Settled(b,3,300,310);
            Check(b.Builds.Count==2&&b.Ready.Count==2, "both real loads own their original ready");
        });
        Scenario("a genuine later teleport retains original native behavior", () => {
            var b=Fresh(); b.SwitchMap(2,200,210); b.Until(()=>b.Ready.Count==1);
            b.SwitchMapAuthoritative(2,200,210); Flow("late-real-same",b); Settled(b,2,200,210);
            Check(b.Builds.Count==2&&b.Ready.Count==2, "later native teleport is not globally discarded");
        });
        Scenario("throwing construction rolls back through the original map path", () => {
            var b=Fresh(); b.BuildOutcomes.Enqueue("throw"); b.SwitchMap(2,200,210);
            Flow("throw-rollback",b); Settled(b,1,10,20);
            Check(b.Ready.Count==1&&b.Ready[0].MapId==1, "only successful rollback reports ready");
        });
        Scenario("missing map retains original scene without ready", () => {
            var b=Fresh(); ResLoader.I.Missing.Add("map/qw0002.gsm"); b.SwitchMap(2,200,210);
            Flow("missing",b); Settled(b,1,10,20);
            Check(b.Builds.Count==0&&b.Ready.Count==0, "missing target does not retire original world");
        });
        Scenario("spawn inside an ordinary portal requires a leave and re-enter edge",()=>{
            var b=Fresh();b.Player.transferProcs=new List<MapLink.MapProc>{new MapLink.MapProc{poly=true,mapTo=2,toX=200,toY=210}};
            b.Player.StartForIndependentRuntime();b.Player.AdvanceTransferForIndependentRuntime();
            Check(b.Started==0&&b.Player.WasInsideTransferForIndependentRuntime,"original Start suppresses a portal under the spawn point");
            b.Player.transferProcs[0].poly=false;b.Player.AdvanceTransferForIndependentRuntime();
            b.Player.transferProcs[0].poly=true;b.Player.AdvanceTransferForIndependentRuntime();Settled(b,2,200,210);
            Check(b.Builds.Count==1&&b.Ready.Count==1,"ordinary walking enters the shared local prebuild path after leaving spawn gate");
        });
        Scenario("command portals do not imply a local map prebuild",()=>{
            var b=Fresh();b.Player.transferProcs=new List<MapLink.MapProc>{new MapLink.MapProc{poly=true,mapTo=2,toX=200,toY=210,cmd="taoyu"}};
            SocialClient.Party.Clear();b.Player.AdvanceTransferForIndependentRuntime();
            Check(b.Started==0&&b.Builds.Count==0&&b.Ready.Count==0,"party/script portal branch cannot be classified as empty-command local prediction");
        });
        Scenario("private rift portal never starts a speculative public-map build", () => {
            var b=Fresh(); EndgamePanel.OwnsRift=true; b.HitPortalForTest(3,300,310);
            Check(b.Started==0&&b.Builds.Count==0&&b.Ready.Count==0,"rift route remains server-owned instead of world-map bounce");
        });
#if !ORIGINAL_BASELINE
        Scenario("retained synchronous byte handoff does not add a yield or a second read", () => {
            var b=Fresh(); b.SwitchMap(2,200,210); byte[] first=ResLoader.I.LastBytes;
            Check(ResLoader.I.Reads.Count==1&&b.Builds.Count==0,"same original first-frame synchronous preflight");
            Settled(b,2,200,210);
            Check(ResLoader.I.Reads.Count==1&&ReferenceEquals(first,b.BuildMapBytes.Single()),"constructor consumes exact prepared bytes once");
            var prepared=new GameBootstrap.PreparedMapSource(ResLoader.I,"map/qw0004.gsm"); byte[] saved=ResLoader.I.LastBytes;
            Check(ReferenceEquals(saved,prepared.ReadOnce(ResLoader.I,"map/qw0004.gsm"))&&!prepared.HasBytes,"handoff is single-use, not retained map cache");
        });
        foreach(string failure in new[]{"missing","reject","partial"})
            Scenario("incomplete "+failure+" target cannot report ready",()=>{
                var b=Fresh();b.BuildOutcomes.Enqueue(failure);b.SwitchMap(2,200,210);Settled(b,1,10,20);
                Check(b.Builds.Count==2&&b.Ready.Count==1&&b.Ready[0].MapId==1,"half-built root does not falsely acknowledge target");
            });
        Scenario("incomplete rollback cannot report ready either",()=>{
            var b=Fresh();b.BuildOutcomes.Enqueue("partial");b.BuildOutcomes.Enqueue("missing");b.SwitchMap(2,200,210);b.Drain();
            Check(b.Ready.Count==0,"no successfully built world means no ready");
        });
        foreach(bool afterBuild in new[]{false,true})
        foreach(bool disposal in new[]{false,true})
            Scenario("final exit cancels original iterator afterBuild="+afterBuild+" dispose="+disposal,()=>{
                var b=Fresh();b.DisposeOnStop=disposal;b.SwitchMap(2,200,210);
                if(afterBuild)b.Until(()=>b.Builds.Count==1);
                int builds=b.Builds.Count, ready=b.Ready.Count;b.ExitForTest();b.Step();b.Step();
                Check(b.Active==0&&!b.Built&&!b.BlackHeld&&b.Alpha==0,"exit releases coroutine and black overlay independently of disposal");
                Check(b.Builds.Count==builds&&b.Ready.Count==ready,"retired iterator cannot resurrect a map or send late ready");
            });
        Scenario("exit also retires a blocked resource-unload wait",()=>{
            var b=Fresh();Resources.StallUnload=true;b.SwitchMap(2,200,210);b.Step();b.Step();b.ExitForTest();b.Step();
            Check(b.Active==0&&b.Builds.Count==0&&b.Ready.Count==0,"pending resource wait cannot outlive logout");
        });
        Scenario("resource rejection rearms only after the original portal is left",()=>{
            var b=Fresh();ResLoader.I.Missing.Add("map/qw0002.gsm");b.HitPortalForTest(2,200,210);
            Check(!b.Player.TransferredForIndependentRuntime&&b.Player.WasInsideTransferForIndependentRuntime,"rejected live portal remains suppressed inside polygon but no longer permanently transferred");
            ResLoader.I.Missing.Clear();b.Player.AdvanceTransferForIndependentRuntime();
            Check(b.Builds.Count==0&&b.Ready.Count==0,"no repeated load on every frame still standing inside");
            b.Player.transferProcs[0].poly=false;b.Player.AdvanceTransferForIndependentRuntime();
            b.Player.transferProcs[0].poly=true;b.Player.AdvanceTransferForIndependentRuntime();Settled(b,2,200,210);
            Check(b.Builds.Count==1&&b.Ready.Count==1,"fresh leave/re-enter loads and acknowledges exactly once");
        });
        Scenario("synchronous resource exception restores original metadata and permits retry",()=>{
            var b=Fresh();ResLoader.I.ThrowRead=true;b.SwitchMap(2,200,210);
            Check(b.Receiving&&!b.Busy&&b.mapName=="qw0001"&&b.spawnX==10&&b.spawnY==20,"read exception clears loading and restores source fields");
            Check(b.Ready.Count==0&&b.Builds.Count==0,"rejected preflight never creates or acknowledges a world");
            b.SwitchMap(3,300,310);Settled(b,3,300,310);
            Check(b.Builds.Count==1&&b.Ready.Count==1,"later valid request retries cleanly");
        });
        Scenario("XP hooks retire only when original transition commits to retiring the world",()=>{
            var b=Fresh();ResLoader.I.Missing.Add("map/qw0002.gsm");b.SwitchMap(2,200,210);
            Check(TestSession.Retires==0&&CombatNumber.Releases==0,"rejected target never retires old scene visuals");
            b.SwitchMap(3,300,310);b.Drain();Check(TestSession.Retires==1&&CombatNumber.Releases==1,"one actual transition performs one visual-only retirement");
        });
#endif
        Console.WriteLine($"PASS {checks} assertions across {scenarios} original-lifecycle source scenarios; Unity rendering not exercised");
    }
}
