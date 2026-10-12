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
        public static void Log(object value) { }
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
    public class NetClient
    {
        public static NetClient I;
        public void GmCmdForIndependentRuntime(string command) { }
    }
    public sealed class Doll { public PlayerController SourceControllerForIndependentRuntime; }
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
    public sealed class ReferenceFirstWorldWindowOwner
    {
        readonly GameBootstrap owner;
        readonly WorldEntryRecord projected;
        public Transform WorldRoot;
        public bool Built;
        public object GossipDialog, DealDialog;
        public Frame Frame = new Frame();
        public ReferenceFirstWorldWindowFailure LastFailure;
        internal GameBootstrap.BuildWorldCheckpoints BuildCheckpointsForIndependentRuntime;
        public ReferenceFirstWorldWindowOwner(object session, Transform parent, WorldEntryRecord entry,
            object music, object sound, object names, GameBootstrap boot, object gossip, object deal)
        { owner = boot; projected = entry; }
        public void AdoptConstructionRootForIndependentRuntime(Transform root, bool transition) { WorldRoot = root; }
        public bool BuildForViewport(int width, int height, float scale) => owner.ConstructForTest(this, projected);
        public void SetBuildGenerationForIndependentRuntime(long generation) { }
        public void Dispose() { WorldRoot = null; }
    }
    public sealed class ResLoader
    {
        public static ResLoader I = new ResLoader();
        public object Pkg;
        public readonly HashSet<string> Missing = new HashSet<string>();
        public Action ReadHook;
        public bool ThrowRead;
        public byte[] ReadMap(string name)
        {
            Action hook = ReadHook; ReadHook = null; hook?.Invoke();
            if (ThrowRead) { ThrowRead = false; throw new InvalidOperationException("resource read failed"); }
            return Missing.Contains(name) ? null : new byte[1];
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
        public static bool On => false;
        public static bool AutoOnMapSwitch => false;
        public static void Begin(string value) { }
        public static void Line(string value) { }
        public static void Phase(string value, float time) { }
    }
    public static class GroundItems { public static void ClearAll() { } }
    public static class CombatWorld { public static void ClearAll() { } }
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
        bool switching, mapTransitionInProgress;
        (int, float, float)? _queuedSwitch;
        (int, float, float, PlayerController)? queuedPortalSwitch;
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
        object session = new object(), sharedMusic, sharedCombatSound, processNpcNames, persistentGossip, persistentDeal;
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
        public readonly List<WorldEntryRecord> Ready = new List<WorldEntryRecord>();
        public readonly Queue<string> BuildOutcomes = new Queue<string>();
        public Action<GameBootstrap> OnReady;
        public int Started, MaxConcurrent;
        public bool DisposeOnStop = true;
        public bool Receiving => !switching;
        public bool Busy => mapTransitionInProgress;
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
        void ReportPos(bool entered = false)
        {
            if (!entered) return;
            Ready.Add(new WorldEntryRecord { MapId = CurMapId, X = X, Y = Y });
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
        Resources.StallUnload = false; Resources.FallbackMaps.Clear(); Debug.Errors.Clear();
        Time.unscaledDeltaTime = .05f;
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
    static void Main()
    {
        Scenario("map-ready reply cannot start a second build/fade", () => {
            var b = Fresh();
            b.OnReady = owner => { if (owner.Ready.Count == 1) owner.SwitchMapAuthoritative(2, 200, 210); };
            b.SwitchMap(2, 200, 210); b.Until(() => b.Builds.Count == 1);
            Check(b.Started == 1, "synchronous map-ready response must not start a second loader");
            Check(b.Receiving, "entity packet gate opens when the world is built");
            Check(b.Busy, "coroutine remains owned after BuildWorld opens packet gate");
            Settled(b, 2, 200, 210);
            Check(b.Builds.Count == 1 && b.Started == 1, "one transfer has exactly one load");
            Check(b.Ready.Count == 2, "queued confirmation receives map-ready acknowledgement");
        });
        Scenario("old-world portal callbacks remain suppressed during fade-in", () => {
            var b = Fresh(); b.SwitchMap(2, 200, 210); b.HitPortalForTest(99, 990, 991);
            Settled(b, 2, 200, 210);
            Check(b.Builds.Count == 1, "retired-world gate cannot overwrite active destination");
        });
        Scenario("new destination portal latch queues a real next transfer during fade-out", () => {
            var b = Fresh(); b.SwitchMap(2, 200, 210); b.Until(() => b.Builds.Count == 1);
            var player = b.Player; b.HitPortalForTest(3, 300, 310);
            Check(player.TransferredForIndependentRuntime && b.Started == 1, "native portal latched but no parallel loader started");
            Settled(b, 3, 300, 310);
            Check(b.Builds.Select(x => x.MapId).SequenceEqual(new[] { 2, 3 }), "latched portal survives fade-out");
        });
        Scenario("same landing authority acknowledgement does not lose a queued portal", () => {
            var b = Fresh(); b.SwitchMap(2, 200, 210); b.Until(() => b.Builds.Count == 1);
            b.HitPortalForTest(3, 300, 310); b.SwitchMapAuthoritative(2, 200, 210);
            Settled(b, 3, 300, 310);
            Check(b.Builds.Count == 2 && b.Ready.Count == 3, "confirm first landing then honor next portal");
        });
        Scenario("new authoritative destination supersedes the old player's queued portal", () => {
            var b = Fresh(); b.SwitchMap(2, 200, 210); b.Until(() => b.Builds.Count == 1);
            b.HitPortalForTest(3, 300, 310); b.SwitchMapAuthoritative(4, 400, 410);
            Settled(b, 4, 400, 410);
            Check(b.Builds.Select(x => x.MapId).SequenceEqual(new[] { 2, 4 }), "server authority wins over portal on superseded map");
        });
        Scenario("late reply during visible fade-out does not reload", () => {
            var b = Fresh(); b.SwitchMap(2, 200, 210);
            b.Until(() => b.Builds.Count == 1 && b.Alpha > 0 && b.Alpha < 1);
            for (int i = 0; i < 4; i++) b.SwitchMapAuthoritative(2, 200, 210);
            Settled(b, 2, 200, 210);
            Check(b.Builds.Count == 1 && b.Started == 1, "fade-out duplicate replies coalesce");
        });
        Scenario("pre-build authoritative landing correction uses the first build", () => {
            var b = Fresh(); b.SwitchMap(2, 200, 210);
            b.SwitchMapAuthoritative(2, 250.5f, 260.25f);
            Settled(b, 2, 250.5f, 260.25f);
            Check(b.Builds.Count == 1 && b.Ready.Count == 1, "corrected landing built/reported once");
        });
        Scenario("pre-build authoritative correction survives a transient build failure", () => {
            var b = Fresh(); b.BuildOutcomes.Enqueue("throw"); b.SwitchMap(2, 200, 210);
            b.SwitchMapAuthoritative(2, 250, 260);
            Settled(b, 2, 250, 260);
            Check(b.Builds.Select(x => x.MapId).SequenceEqual(new[] { 2, 1, 2 }), "queued target retries after rollback");
            Check(b.Ready.Select(x => x.MapId).SequenceEqual(new[] { 1, 2 }), "only built rollback and corrected target report ready");
        });
        Scenario("a real same-map teleport to different coordinates is preserved", () => {
            var b = Fresh(); b.SwitchMap(2, 200, 210); b.Until(() => b.Builds.Count == 1);
            b.SwitchMapAuthoritative(2, 333, 444);
            Settled(b, 2, 333, 444);
            Check(b.Builds.Count == 2 && b.Started == 2, "different same-map landing is not swallowed");
        });
        Scenario("rapid newer destinations retain only the latest authority", () => {
            var b = Fresh(); b.SwitchMap(2, 200, 210);
            b.SwitchMapAuthoritative(3, 300, 310); b.SwitchMapAuthoritative(4, 400, 410);
            b.Until(() => b.Builds.Count == 1); b.SwitchMapAuthoritative(5, 500, 510);
            Settled(b, 5, 500, 510);
            Check(b.Builds.Select(x => x.MapId).SequenceEqual(new[] { 2, 5 }), "only latest queued map executes");
        });
        Scenario("idle server same-map reload remains a valid lifecycle request", () => {
            var b = Fresh(); b.SwitchMapAuthoritative(1, 10, 20);
            Settled(b, 1, 10, 20); Check(b.Builds.Count == 1, "no blanket same-map deduplication");
        });
        Scenario("missing map preserves old world and permits retry", () => {
            var b = Fresh(); ResLoader.I.Missing.Add("map/qw0002.gsm"); b.SwitchMap(2, 200, 210);
            Settled(b, 1, 10, 20); Check(b.Builds.Count == 0 && b.Ready.Count == 0, "missing map never retires old world or reports ready");
            ResLoader.I.Missing.Clear(); b.SwitchMap(2, 200, 210); Settled(b, 2, 200, 210);
        });
        Scenario("a rejected real portal can retry after leaving and re-entering", () => {
            var b = Fresh(); ResLoader.I.Missing.Add("map/qw0002.gsm"); b.HitPortalForTest(2, 200, 210);
            Check(!b.Player.TransferredForIndependentRuntime && b.Player.WasInsideTransferForIndependentRuntime, "rejected portal rearms without a per-frame retry loop");
            b.Player.AdvanceTransferForIndependentRuntime();
            Check(b.Started == 1, "standing inside a missing-resource portal cannot keep retrying");
            b.Player.transferProcs[0].poly = false; b.Player.AdvanceTransferForIndependentRuntime();
            ResLoader.I.Missing.Clear(); b.Player.transferProcs[0].poly = true; b.Player.AdvanceTransferForIndependentRuntime();
            Settled(b, 2, 200, 210);
            Check(b.Builds.Count == 1 && b.Started == 2, "leaving/re-entering the actual portal retries successfully");
        });
        Scenario("resource read failure restores old map metadata before retry", () => {
            var b = Fresh(); ResLoader.I.ThrowRead = true; b.SwitchMap(2, 200, 210);
            Settled(b, 1, 10, 20);
            Check(b.mapName == "qw0001" && b.spawnX == 10 && b.spawnY == 20 && b.Ready.Count == 0, "read exception restores original source and landing");
            b.SwitchMap(2, 200, 210); Settled(b, 2, 200, 210);
        });
        Scenario("missing map still services a queued newer server destination", () => {
            var b = Fresh(); ResLoader.I.Missing.Add("map/qw0002.gsm");
            ResLoader.I.ReadHook = () => b.SwitchMapAuthoritative(3, 300, 310);
            b.SwitchMap(2, 200, 210); Settled(b, 3, 300, 310);
            Check(b.Builds.Count == 1, "missing target did not consume the queued request");
        });
        foreach (string failure in new[] { "missing", "reject", "partial", "throw" })
            Scenario("construction " + failure + " rolls back without a false map-ready", () => {
                var b = Fresh(); b.BuildOutcomes.Enqueue(failure); b.SwitchMap(2, 200, 210);
                Settled(b, 1, 10, 20);
                Check(b.Builds.Count == 2 && b.Ready.All(x => x.MapId == 1), "only complete rollback is acknowledged");
                b.SwitchMap(2, 200, 210); Settled(b, 2, 200, 210);
            });
        Scenario("failed target and failed rollback both release retry ownership", () => {
            var b = Fresh(); b.BuildOutcomes.Enqueue("missing"); b.BuildOutcomes.Enqueue("missing");
            b.SwitchMap(2, 200, 210); b.Drain();
            Check(!b.Busy && !b.BlackHeld && b.Alpha == 0 && b.Ready.Count == 0, "double failure is neither ready nor stuck black");
            b.SwitchMapAuthoritative(3, 300, 310); Settled(b, 3, 300, 310);
        });
        Scenario("an incomplete root cannot deduplicate a queued recovery teleport", () => {
            var b = Fresh(); b.BuildOutcomes.Enqueue("missing"); b.BuildOutcomes.Enqueue("missing");
            b.SwitchMap(2, 200, 210); b.Until(() => b.Builds.Count == 2);
            b.SwitchMapAuthoritative(1, 10, 20); Settled(b, 1, 10, 20);
            Check(b.Builds.Count == 3 && b.Ready.Count == 1, "queued recovery actually constructs a window");
        });
        foreach (bool afterBuild in new[] { false, true })
            Scenario("final exit cancels " + (afterBuild ? "fade-out" : "fade-in") + " and discards queued work", () => {
                var b = Fresh(); b.SwitchMap(2, 200, 210);
                if (afterBuild) { b.Until(() => b.Builds.Count == 1); b.HitPortalForTest(7, 700, 710); }
                b.SwitchMapAuthoritative(3, 300, 310);
                int before = b.Builds.Count;
                b.ExitForTest(); for (int i = 0; i < 30; i++) b.Step();
                Check(b.Active == 0 && !b.Busy && !b.BlackHeld && b.Alpha == 0, "exit cancels all fade ownership");
                Check(b.Builds.Count == before && GameBootstrap.CurMapId == 0, "no coroutine resurrects departed world");
                b.SwitchMap(4, 400, 410); Settled(b, 4, 400, 410);
            });
        Scenario("exit reset does not depend on Unity disposing stopped iterators", () => {
            var b = Fresh(); b.DisposeOnStop = false; b.SwitchMap(2, 200, 210);
            b.Until(() => b.Builds.Count == 1); b.HitPortalForTest(3, 300, 310);
            b.SwitchMapAuthoritative(4, 400, 410); b.ExitForTest();
            Check(!b.Busy && !b.BlackHeld && b.Alpha == 0 && b.Active == 0, "explicit cancellation resets even without iterator finally");
            b.SwitchMap(5, 500, 510); Settled(b, 5, 500, 510);
            Check(b.Builds.Select(x => x.MapId).SequenceEqual(new[] { 2, 5 }), "cancelled queues cannot resume in later world");
        });
        Scenario("unexpected cleanup failure unlocks a later retry", () => {
            var b = Fresh(); ArtCaches.ThrowEvict = true; b.SwitchMap(2, 200, 210);
            bool failed = false; try { b.Drain(); } catch (InvalidOperationException) { failed = true; }
            Check(failed && !b.Busy && !b.BlackHeld && b.Alpha == 0, "iterator finally releases a thrown cleanup");
            b.SwitchMap(3, 300, 310); Settled(b, 3, 300, 310);
        });
        Scenario("map-definition failure before scheduling does not latch loading", () => {
            var b = Fresh(); MapDef.ThrowLoad = true; bool failed = false;
            try { b.SwitchMap(2, 200, 210); } catch (InvalidOperationException) { failed = true; }
            Check(failed && !b.Busy && b.Receiving && b.Active == 0 && b.mapName == "qw0001", "pre-coroutine failure unlocks unchanged world");
            b.SwitchMap(2, 200, 210); Settled(b, 2, 200, 210);
        });
        Scenario("map-definition rejection also rearms the actual portal latch", () => {
            var b = Fresh(); MapDef.ThrowLoad = true; bool failed = false;
            try { b.HitPortalForTest(2, 200, 210); } catch (InvalidOperationException) { failed = true; }
            Check(failed && !b.Player.TransferredForIndependentRuntime && b.Player.WasInsideTransferForIndependentRuntime, "definition error preserves retryable original controller");
            b.Player.AdvanceTransferForIndependentRuntime(); Check(b.Started == 0, "no retry while still inside failed gate");
            b.Player.transferProcs[0].poly = false; b.Player.AdvanceTransferForIndependentRuntime();
            b.Player.transferProcs[0].poly = true; b.Player.AdvanceTransferForIndependentRuntime();
            Settled(b, 2, 200, 210); Check(b.Started == 1, "re-entering recovered gate schedules exactly one load");
        });
        Scenario("stalled asynchronous resource unload remains serialized and can be cancelled", () => {
            var b = Fresh(); Resources.StallUnload = true; b.SwitchMap(2, 200, 210);
            for (int i = 0; i < 100; i++) { b.Step(); b.SwitchMapAuthoritative(3, 300, 310); b.SwitchMap(4, 400, 410); }
            Check(b.Busy && b.Started == 1 && b.Builds.Count == 0, "waiting operation cannot spawn parallel loaders");
            b.ExitForTest(); Resources.StallUnload = false; b.SwitchMap(5, 500, 510); Settled(b, 5, 500, 510);
        });
        Scenario("zero-duration and long-frame fades still complete one lifecycle", () => {
            var b = Fresh(); b.fadeInSec = b.fadeOutSec = 0; Time.unscaledDeltaTime = 4;
            b.SwitchMap(2, 200, 210); Settled(b, 2, 200, 210);
            Check(b.Started == 1 && b.Builds.Count == 1, "zero-duration fade retains construction ownership");
        });
        Console.WriteLine("PASS " + checks + " assertions across " + scenarios + " map transition source scenarios; Unity rendering not exercised");
    }
}
