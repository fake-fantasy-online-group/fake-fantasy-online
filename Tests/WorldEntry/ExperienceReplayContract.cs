using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

class ExperienceReplayContract
{
    static int checks;
    static void Check(bool value, string why) { if (!value) throw new Exception(why); checks++; }
    static WorldCombatNumberState N(long revision, int value = 12, byte type = 4, byte target = 0) => new WorldCombatNumberState(target,value,type,revision);
    static readonly WorldCombatResultState[] Empty = new WorldCombatResultState[0];
    static bool Poll(ReferenceFirstWorldInitialFrame f, ClientSessionCoordinator s, IReadOnlyList<WorldCombatNumberState> ns, long version) => f.ApplyAuthoritativeCombatFeedbackRevision(Empty,0,ns,version,s);
    static void Main()
    {
        var gateway = new StreamGateway(); var session = new ClientSessionCoordinator(gateway);
        var first = new ReferenceFirstWorldInitialFrame(); first.Activate();
        var history = new[] { N(1) };
        Check(Poll(first,session,history,1) && CombatNumber.Rendered == 1,"first XP is rendered once");
        Check(Poll(first,session,history,1) && CombatNumber.Rendered == 1,"same frame does not replay");
        var next = new ReferenceFirstWorldInitialFrame(); next.Activate();
        Check(Poll(next,session,history,1) && CombatNumber.Rendered == 1,"new map must not replay consumed XP");
        for (int i=0;i<3;i++) { next = new ReferenceFirstWorldInitialFrame(); next.Activate(); Check(Poll(next,session,history,1) && CombatNumber.Rendered == 1,"repeated same-map frame rebuild does not replay"); }
        history = new[] { N(1), N(2), N(3) };
        Check(Poll(next,session,history,3) && CombatNumber.Rendered == 3,"two equal-valued new rewards remain distinct");
        var owner = next.NpcSpawnOwnerForIndependentRuntime; owner.Switching = true;
        history = new[] { N(1),N(2),N(3),N(4) };
        Check(Poll(next,session,history,4) && CombatNumber.Rendered == 3 && session.CombatNumberPresentation.PendingCount==1,"switching queues XP and does not reject world updates");
        next = new ReferenceFirstWorldInitialFrame(); next.Activate(); Time.unscaledTime += .3f;
        Check(Poll(next,session,history,4) && CombatNumber.Rendered == 4,"new ready frame renders gap reward exactly once");
        history = new[] { N(1),N(2),N(3),N(4),N(5) }; CombatNumber.ArtReady=false;
        Check(Poll(next,session,history,5) && session.CombatNumberPresentation.PendingCount==1,"missing glyph/font is retryable and nonfatal");
        history = new[] { N(5),N(6,type:0),N(7,type:4) };
        CombatNumber.FailValue=12; CombatNumber.ArtReady=true; history[2]=N(7,34);
        Check(Poll(next,session,history,7) && CombatNumber.Rendered==5,"failed XP does not block later new XP");
        Check(session.CombatNumberPresentation.PendingCount==1,"ordinary missing-art number is one-shot, only XP retries");
        CombatNumber.FailValue=-1; Time.unscaledTime+=.3f;
        Check(Poll(next,session,history,7) && CombatNumber.Rendered==6,"unchanged journal retries only failed reward");
        Check(session.CombatNumberPresentation.ConsumedRevision==7,"success closes consumed prefix hole");
        next = new ReferenceFirstWorldInitialFrame(); next.Activate();
        Check(Poll(next,session,history,7) && CombatNumber.Rendered==6,"successful records around previous hole never replay");
        gateway.CombatNumberStreamGeneration++;
        Check(Poll(new ReferenceFirstWorldInitialFrame().Activate(),session,new[]{N(1)},8) && CombatNumber.Rendered==7,"reconnect resets revision before next frame observes journal");
        session.Connected=false;
        Check(Poll(next,session,new[]{N(2)},9) && CombatNumber.Rendered==7 && session.CombatNumberPresentation.PendingCount==0,"disconnect prompt cannot replay on frozen player");
        session.Connected=true; gateway.CombatNumberStreamGeneration++;
        var mismatch = new ReferenceFirstWorldInitialFrame().Activate(); CombatNumber.BindPlayerTransformForIndependentRuntime(new Transform());
        Check(Poll(mismatch,session,new[]{N(1)},10) && CombatNumber.Rendered==7,"stale registered transform cannot acknowledge XP");
        mismatch.Activate(); Time.unscaledTime+=.3f;
        Check(Poll(mismatch,session,new[]{N(1)},10) && CombatNumber.Rendered==8,"correct local transform permits pending XP");
        var state = new WorldCombatNumberPresentation(); var seen = new List<long>();
        var many = new List<WorldCombatNumberState>(); for(int i=1;i<=256;i++) many.Add(N(i));
        state.TryPresent(many,0,n=>false);
        Check(state.PendingCount==256 && state.DroppedPresentationCount==0 && !state.TakeBacklogWarning(),"256 pending preserves every popup");
        many = new List<WorldCombatNumberState>(many); many.Add(N(257)); state.TryPresent(many,.1f,n=>false);
        Check(state.PendingCount==256 && state.DroppedPresentationCount==1 && state.TakeBacklogWarning() && !state.TakeBacklogWarning(),"257th drops oldest visual only and warns once");
        int attempts=0; state.TryPresent(many,.2f,n=>{attempts++;return false;}); Check(attempts==0,"retries are throttled");
        state.TryPresent(many,.3f,n=>{seen.Add(n.Revision);return true;});
        Check(seen.Count==16 && seen[0]==2 && seen[15]==17,"bounded retry budget keeps later rewards and drops oldest");
        for(int i=0;i<16;i++) state.TryPresent(many,1+i,n=>{seen.Add(n.Revision);return true;});
        Check(state.PendingCount==0 && seen.Count==256 && seen[255]==257,"all retained rewards eventually render without duplication");
        many.Add(N(258)); state.TryPresent(new List<WorldCombatNumberState>(many),20,n=>{seen.Add(n.Revision);return true;});
        Check(seen.Count==257 && seen[256]==258,"fresh reward after saturation still renders");
        state.Clear(); Check(state.PendingCount==0 && state.ConsumedRevision==0 && state.DroppedPresentationCount==0,"connection reset clears all presentation state");
        CombatNumber.ArtReady=false; Time.time=100; var anchor = new Transform(); CombatNumber.RegisterPlayer(anchor,1,100);
        for(int i=0;i<50;i++) Check(!CombatNumber.TrySpawnOnForIndependentRuntime(0,99,4),"failed art reports no presentation");
        CombatNumber.ArtReady=true;
        Check(CombatNumber.TrySpawnOnForIndependentRuntime(0,99,4) && Math.Abs(CombatNumber.LastPosition.y-1)<.001f,"failed attempts do not stack popup offsets");
        CombatNumber.BindPlayerTransformForIndependentRuntime(null);
        Check(!CombatNumber.TrySpawnOnForIndependentRuntime(0,99,4),"missing anchor is retryable");
        Check(CombatNumber.TrySpawnForIndependentRuntime(Vector3.zero,0,4,100),"native zero-value no-op is terminal");
        // Exercise the production gateway's journal retirement, rather than only assigning
        // a test epoch: same authenticated socket, different character, revision starts at 1.
        var realGateway = new FakeFantasyOnline.Protocol.PrivateServerGateway();
        var characterSession = new ClientSessionCoordinator(realGateway);
        var characterA = new ReferenceFirstWorldInitialFrame().Activate();
        realGateway.PublishForTest(10); int beforeCharacter = CombatNumber.Rendered;
        Check(Poll(characterA,characterSession,realGateway.Numbers,realGateway.Version) && CombatNumber.Rendered==beforeCharacter+1,"character A renders first edge");
        CombatNumber.ArtReady=false; realGateway.PublishForTest(20);
        Check(Poll(characterA,characterSession,realGateway.Numbers,realGateway.Version) && characterSession.CombatNumberPresentation.PendingCount==1,"character A retains failed edge");
        long generation = realGateway.CombatNumberStreamGeneration;
        realGateway.RetireCharacterForTest();
        Check(realGateway.Numbers.Count==0 && realGateway.CombatNumberStreamGeneration==generation+1,"world-to-title atomically clears journal and increments epoch");
        Check(characterSession.CombatNumberPresentation.PendingCount==0,"same-socket character boundary clears pending A edge");
        CombatNumber.ArtReady=true; realGateway.PublishForTest(30);
        Check(realGateway.Numbers[0].Revision==1,"character B journal restarts revision");
        var characterB = new ReferenceFirstWorldInitialFrame().Activate(); Time.unscaledTime+=.3f;
        Check(Poll(characterB,characterSession,realGateway.Numbers,realGateway.Version) && CombatNumber.Rendered==beforeCharacter+2,"B new edge renders without replaying A consumed or pending edges");
        Check(Poll(new ReferenceFirstWorldInitialFrame().Activate(),characterSession,realGateway.Numbers,realGateway.Version) && CombatNumber.Rendered==beforeCharacter+2,"B map rebuild also retains consumption");
        SceneOwnershipCases();
        Console.WriteLine("PASS experience replay/queue/frame/spawn contract ("+checks+" assertions)");
    }
    static void SceneOwnershipCases()
    {
        CombatNumber.ArtReady=true; CombatNumber.FailValue=-1;
        var gateway = new StreamGateway(); var session = new ClientSessionCoordinator(gateway);
        var old = new ReferenceFirstWorldInitialFrame().Activate();
        int shown = CombatNumber.Rendered;
        var journal = new[]{N(1,101)};
        Check(Poll(old,session,journal,1) && CombatNumber.Rendered==shown+1,"scene-owned already-shown reward acknowledged");
        CombatNumber.ArtReady=false; journal=new[]{N(1,101),N(2,202)};
        Check(Poll(old,session,journal,2) && session.CombatNumberPresentation.PendingCount==1,"known old live frame owns missing-art reward");
        old.NpcSpawnOwnerForIndependentRuntime.Switching=true;
        journal=new[]{N(1,101),N(2,202),N(3,303)};
        Check(Poll(old,session,journal,3) && session.CombatNumberPresentation.PendingCount==2,"first receipt during switching stays unknown rather than assumed old");
        session.RetireCombatNumberSceneForIndependentRuntime();
        Check(session.CombatNumberPresentation.PendingCount==1 && session.CombatNumberPresentation.RetiredScenePresentationCount==1,"retirement removes only proven old-scene pending");
        Check(journal[0].Value==101 && journal[1].Value==202 && journal[2].Value==303,"retirement never mutates authoritative receipt values");
        CombatNumber.ArtReady=true; var next=new ReferenceFirstWorldInitialFrame().Activate(); Time.unscaledTime+=1;
        Check(Poll(next,session,journal,3) && CombatNumber.Rendered==shown+2,"new scene preserves unknown transition reward, no old shown/pending replay");
        Check(session.CombatNumberPresentation.PendingCount==0,"unknown transition receipt consumed once");
        Check(Poll(next,session,new[]{N(1,101),N(2,202)},4) && CombatNumber.Rendered==shown+2,"late old immutable journal snapshot cannot resurrect retired or shown revisions");
        journal=new[]{N(1,101),N(2,202),N(3,303),N(4,404)};
        Check(Poll(next,session,journal,5) && CombatNumber.Rendered==shown+3,"genuine new-map reward is not swallowed");
        // The wire has no scene identity for an arbitrary notification still in flight.
        // It is a new receipt, not proof of replay, even when its value matches old XP.
        journal=new[]{N(5,202)};
        Check(Poll(next,session,journal,6) && CombatNumber.Rendered==shown+4,"unknown late first-time receipt is preserved even with old equal amount");
        session.RetireCombatNumberSceneForIndependentRuntime();
        Check(Poll(new ReferenceFirstWorldInitialFrame().Activate(),session,journal,7) && CombatNumber.Rendered==shown+4,"duplicate/rebuilt frame cannot replay newly consumed reward");
        CombatNumber.ArtReady=false; var third=new ReferenceFirstWorldInitialFrame().Activate();
        Check(Poll(third,session,new[]{N(6,606)},8) && session.CombatNumberPresentation.PendingCount==1,"new scene owns its own missing-art receipt");
        session.RetireCombatNumberSceneForIndependentRuntime();
        Check(session.CombatNumberPresentation.PendingCount==0 && session.CombatNumberPresentation.RetiredScenePresentationCount==2,"later retirement clears that scene only");
        var mismatch=new ReferenceFirstWorldInitialFrame().Activate();
        mismatch.NpcSpawnOwnerForIndependentRuntime.Window.Frame=new ReferenceFirstWorldInitialFrame();
        Check(Poll(mismatch,session,new[]{N(7,707)},9) && session.CombatNumberPresentation.PendingCount==1,"superseded frame cannot claim an unseen receipt");
        session.RetireCombatNumberSceneForIndependentRuntime();
        Check(session.CombatNumberPresentation.PendingCount==1,"unknown superseded-frame receipt survives retirement");
        CombatNumber.ArtReady=true; Time.unscaledTime+=1;
        Check(Poll(new ReferenceFirstWorldInitialFrame().Activate(),session,new[]{N(7,707)},10) && CombatNumber.Rendered==shown+5,"live new owner displays preserved first-time receipt");
        session.Connected=false;
        Check(Poll(third,session,new[]{N(8,808)},11) && session.CombatNumberPresentation.PendingCount==0,"disconnect clears scene-owned and unknown visual state");
        session.Connected=true; gateway.CombatNumberStreamGeneration++;
        Check(Poll(new ReferenceFirstWorldInitialFrame().Activate(),session,new[]{N(1,909)},12) && CombatNumber.Rendered==shown+6,"new connection accepts fresh revision 1 without old ownership leakage");
        var acquire = new WorldCombatNumberPresentation(); var unknown = new[]{N(1,1001)};
        acquire.TryPresent(unknown,0,n=>false,false); acquire.RetireScene();
        Check(acquire.PendingCount==1 && acquire.RetiredScenePresentationCount==0,"unknown loading receipt survives prior retirement");
        acquire.TryPresent(unknown,1,n=>false,true);
        Check(acquire.PendingCount==1,"failed live-scene retry keeps reward pending while acquiring ownership");
        acquire.RetireScene();
        Check(acquire.PendingCount==0 && acquire.RetiredScenePresentationCount==1 && acquire.ConsumedRevision==1,"next retirement drops only acquired visual and retains consumed revision fence");
    }
}
namespace FakeFantasyOnline.Application
{
    class StreamGateway : IWorldCombatNumberStreamSource { public long CombatNumberStreamGeneration {get;set;} }
    public sealed partial class ClientSessionCoordinator
    {
        readonly object _gateway;
        public bool Connected=true;
        public ClientSessionCoordinator(object gateway) { _gateway=gateway; }
    }
    public class WorldCombatResultState { public long Revision; public bool NativeCombatHandled,Healing,Missed,Critical; public int TargetEntityId,AttackerEntityId,TargetIncarnation,AttackerIncarnation,ReceiveSequence,Flags; }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public class GameBootstrap { public bool Switching; public TestWindow Window; }
    public class TestWindow { public ReferenceFirstWorldInitialFrame Frame; }
    public class TestCharacter { public int EntityId; public TestAppearance Appearance=new TestAppearance(); }
    public class TestAppearance { public int WeaponCategory; }
    public class ReferenceWorldMonsterActor { public bool TryApplyCombatResult(WorldCombatResultState r)=>true; public bool StartAttackAnimation()=>true; }
    public class TestPlayer { public Transform Root=new Transform(); public bool StartHurtAnimation()=>true; }
    public class ReferenceWorldCombatSound { public static bool ShouldPlayHit(int f)=>false; public bool TryPlayHit(int c,bool b)=>true; }
    internal partial class ReferenceWorldCombatFeedback
    {
        void ThrowIfDisposed() {}
        public bool TrySpawnResult(ReferenceWorldMonsterActor a,WorldCombatResultState r)=>true;
        public bool TrySpawnLocalResult(TestPlayer a,WorldCombatResultState r)=>true;
    }
    public partial class ReferenceFirstWorldInitialFrame
    {
        bool built=true,nativeCombatReceiveEnabled,entityReceiveEnabled,localPlayerDead;
        ReferenceWorldCombatFeedback combatFeedback=new ReferenceWorldCombatFeedback();
        Dictionary<int,ReferenceWorldMonsterActor> monsterActors=new Dictionary<int,ReferenceWorldMonsterActor>();
        TestCharacter character=new TestCharacter(); TestPlayer player=new TestPlayer(); ReferenceWorldCombatSound combatSound;
        long lastCombatResultsVersion=-1,lastCombatNumbersVersion=-1,lastCombatResultRevision,lastCombatNumberRevision;
        readonly WorldCombatNumberPresentation standaloneCombatNumbers=new WorldCombatNumberPresentation();
        public GameBootstrap NpcSpawnOwnerForIndependentRuntime {get;set;}
        public ReferenceFirstWorldInitialFrame Activate() { NpcSpawnOwnerForIndependentRuntime=new GameBootstrap {Window=new TestWindow {Frame=this}}; CombatNumber.RegisterPlayer(player.Root,1,100); return this; }
        bool MatchesEntityReceiveIncarnation(int e,int i,int s)=>true;
        void ThrowIfDisposed() {}
    }
    public static class CharData { public static bool riding; }
    public static class BitmapFont { public static bool Ready; public static Texture2D BakeNameTag(string a,string b,Color c,Color d,int e)=>null; }
    public partial class CombatNumber
    {
        public static bool ArtReady=true; public static int FailValue=-1,Rendered; public static Vector3 LastPosition;
        static Material overlayMat; static int glyphScale=1;
        int type; float ppu,life,hSign,startYWorld; Transform anchor; Vector3 basePos; Texture2D _ownedTex; Sprite _ownedSp;
        static List<Sprite> BuildGlyphs(int value,int type)=>ArtReady && value!=FailValue ? new List<Sprite>{new Sprite()} : null;
        static CombatNumber Rent()=>new CombatNumber();
        SpriteRenderer NextChild() { Rendered++; LastPosition=transform.position; return new SpriteRenderer(); }
        void ReturnToPool() { ForgetExperienceScene(); }
    }
}
namespace UnityEngine
{
    public class Object { public static implicit operator bool(Object o)=>o!=null; }
    public class MonoBehaviour:Object { public Transform transform=new Transform(); }
    public class Transform:Object { public Vector3 position,localPosition,localScale; }
    public class Shader:Object { public static Shader Find(string s)=>new Shader(); }
    public class Material:Object { public Material(Shader s){} }
    public class Texture2D:Object { public int width,height; }
    public struct Rect { public float width,height; public Rect(float x,float y,float w,float h){width=w;height=h;} }
    public class Sprite:Object { public Rect rect=new Rect(0,0,10,10); public static Sprite Create(Texture2D t,Rect r,Vector2 p,float ppu)=>new Sprite(); }
    public class SpriteRenderer:Object { public Transform transform=new Transform(); public Sprite sprite; public Color color; }
    public struct Vector2 { public Vector2(float x,float y){} }
    public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;} public static Vector3 zero=>new Vector3(); public static Vector3 one=>new Vector3(1,1,1); public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z); }
    public struct Color { public float a; public Color(float r,float g,float b,float alpha=1){a=alpha;} public static Color white=>new Color(); }
    public static class Time { public static float time,unscaledTime; public static int frameCount; }
    public static class Mathf { public static float Max(float a,float b)=>Math.Max(a,b); public static int Max(int a,int b)=>Math.Max(a,b); }
    public static class Random { public static float value=>.2f; }
    public static class Debug { public static void LogWarning(string s){} }
}

namespace FakeFantasyOnline.Protocol
{
    public sealed partial class PrivateServerGateway
    {
        readonly object _worldStateSync=new object();
        IReadOnlyList<WorldCombatNumberState> _combatNumbers=new WorldCombatNumberState[0];
        long _combatNumbersVersion,_combatNumberRevision;
        public IReadOnlyList<WorldCombatNumberState> Numbers=>_combatNumbers;
        public long Version=>_combatNumbersVersion;
        public void PublishForTest(int value) { var next=new List<WorldCombatNumberState>(_combatNumbers); next.Add(new WorldCombatNumberState(0,value,4,++_combatNumberRevision)); _combatNumbers=next; _combatNumbersVersion++; }
        public void RetireCharacterForTest()=>ClearCombatNumberPresentationJournal();
    }
}
