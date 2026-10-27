// Only Unity/presentation capabilities are substituted. NativeNetClientHost and all gateway,
// session, state, framing and crypto code are the actual production sources/assemblies.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.Protocol;
namespace UnityEngine
{
 public class GameObject
 {
  readonly Dictionary<Type,object> components = new Dictionary<Type,object>();
  public T GetComponent<T>() where T:class { object v; return components.TryGetValue(typeof(T),out v)?(T)v:null; }
  public T AddComponent<T>() where T:class,new() { var v=new T(); components[typeof(T)]=v; return v; }
 }
 public class MonoBehaviour
 {
  public readonly GameObject gameObject = new GameObject();
  public T GetComponent<T>() where T:class => gameObject.GetComponent<T>();
  public Coroutine StartCoroutine(IEnumerator value) => new Coroutine();
  public void StopCoroutine(Coroutine value) { }
 }
 public class Coroutine { }
 public class WaitForSeconds { public WaitForSeconds(float value) { } }
 public static class Debug { public static void LogWarning(object value) { } }
 public static class Time { public static float realtimeSinceStartup; }
 public static class SystemInfo { public static string deviceUniqueIdentifier => "synthetic-host"; }
 public static class JsonUtility { public static T FromJson<T>(string value) => default(T); }
 public enum RuntimePlatform { OSXPlayer, OSXEditor, LinuxPlayer }
 public static class Application { public static RuntimePlatform platform = RuntimePlatform.LinuxPlayer; }
}
namespace FakeFantasyOnline.Presentation
{
 public class ClientRootView : UnityEngine.MonoBehaviour, IWorldReceiveContext, IMailReceiveOwner, ISkillCastReceiveOwner
 {
  public bool Switching => false; public int CurrentMapId => 0;
  public ReferenceAssets.GameBootstrap WorldBootstrapForIndependentRuntime => null;
  public ReferenceAssets.DialogUI NpcDialogAtReceiptForIndependentRuntime() => null;
  public void BindStartupAddressResultForIndependentRuntime(Func<bool> value) { }
  internal void BindNetworkInstanceForIndependentRuntime(INetClientInstanceHost host, Action project, Func<bool> startup) { }
  public void NoteDisconnectedForIndependentRuntime() { }
  public void BeginEnterWorld(string name, Action<string> action) { action(name); }
  public bool TryReceiveMailList(WorldMailState state) => false;
  public bool TryReceiveMailNew(bool value) => false;
  public bool TryReceiveSkillCast(WorldSkillCastState state) => false;
  public void ApplyPublishedWorldEvents() { }
 }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
 public class EndgamePanel
 {
  public IEndgameClient Bound; public int Binds, Unbinds;
  public void Bind(IEndgameClient client) { Bound=client; Binds++; }
  public void Unbind(IEndgameClient client) { if(ReferenceEquals(Bound,client)) { Bound=null; Unbinds++; } }
 }
 public class DialogUI { }
 public class GameBootstrap { public static int CurMapId; }
 public static class CustomMaps { public static bool IsCustom(int id) => false; }
 public static class VmDetect { public static bool Detect(out string reason) { reason=""; return false; } }
 public static class DebugDetect { public static bool Detect(out string reason) { reason=""; return false; } }
 internal class NetClient : UnityEngine.MonoBehaviour
 {
  public static float DispatchMsPerFrame=5;
  public static ClientConfig.Srv[] Servers=new ClientConfig.Srv[0];
  public void BindCloseForIndependentRuntime(Action action) { }
  public void BindInstanceForIndependentRuntime(INetClientInstanceBinding binding, FakeFantasyOnline.Presentation.INetClientInstanceHost host, Action pump) { }
  public void BindTitleLoginForIndependentRuntime(Action<Action<bool>> connect,Action<string,string> login,Action cancel) { }
  public void BindTitleEntryForIndependentRuntime(Action<string> entry) { }
  public void BindTitleEntryPrefixForIndependentRuntime(Action clear,Action caps) { }
  public void BindLastDropReasonReaderForIndependentRuntime(Func<string> read) { }
  public void BindVersionManifestsForIndependentRuntime(string mac,string win) { }
  public IEnumerator CheckVersion() { yield break; }
 }
 public class ReferenceCharacterSkillUsePort : ICharacterSkillUsePort, IDisposable
 {
  internal ReferenceCharacterSkillUsePort(NetClient owner, ClientSessionCoordinator session, IGameServerGateway writer, Func<GameBootstrap> world) { }
  public void ProjectCurrentStateForIndependentRuntime() { }
  public bool RequestSkill(int skill,float range,CancellationToken token) => false;
  public Task<Result> CastAtTargetAsync(int skill,CancellationToken token) => null;
  public Task<Result> UseSkillAsync(int skill,int target,CancellationToken token) => null;
  public Task<Result> UseSkillAtAsync(int skill,int x,int y,int target,CancellationToken token) => null;
  public void Dispose() { }
 }
 public class LoadTraceReceivePort : IWorldReceiveTracePort
 {
  public bool On => false; public long Now => 0; public float Ms(long value) => 0;
  public void Net(int id,int length,float ms) { } public void Backlog(int depth) { }
 }
 public class ReferenceWorldReceiveOwner : IWorldReceiveOwner
 {
  public ReferenceWorldReceiveOwner(Func<DialogUI> dialog) { }
  public Func<IMailReceiveOwner> MailOwnerAtReceiptForIndependentRuntime {get;set;}
  public Func<ISkillCastReceiveOwner> SkillCastOwnerAtReceiptForIndependentRuntime {get;set;}
  public void ReceiveSpawnOnEffect(int target, string effectName) {  }
  public void BeginSelfData() {  }
  public void ReceiveSelfDataIdentity(int entityId) {  }
  public void ReceiveEntrySettings(int? petViewMask, bool? smartCast) {  }
  public void ReceiveLocalFaction(WorldLocalFactionState state) {  }
  public void ClearOwnedTitles() {  }
  public void AddOwnedTitle(string title) {  }
  public void SetCurrentTitle(string title) {  }
  public void ReceiveEntryLastLogin() {  }
  public void ReceiveEntryIdentity(string name, long characterId) {  }
  public void LoadEntryNameMode() {  }
  public void ReceiveNpcChatter(WorldNpcChatterState state) {  }
  public void ReceiveNpcTaskOffers(WorldNpcTaskOffersState state) {  }
  public void ReceiveTaskLog(WorldTaskLogState state) {  }
  public void BeginBuffRows(int count) {  }
  public void AddBuffRow(int skillId, int iconId, int remainingSeconds, string name, string description, bool good, int positionMode, float receiveTime) {  }
  public void CompleteBuffRows(bool invisible, byte immobile) {  }
  public void CompleteBuffTail(bool doubleExperience) {  }
  public void ReceivePlayerAttributes(IReadOnlyList<int> attributes, IReadOnlyList<int> attributeTotals) {  }
  public void ReceivePlayerAttributeFields(IReadOnlyList<int> attributes) {  }
  public void ReceivePlayerRingFields(IReadOnlyList<int> attributes) {  }
  public void ReceiveEnterAttributeFields(IReadOnlyList<int> attributes) {  }
  public WorldEntryRetainedScalars ReadEntryRetainedScalars() { return default(WorldEntryRetainedScalars); }
  public void ReceiveInventoryWeightLimit(int maximumWeight) {  }
  public void ReceiveInventoryHeader(long money, long caiYu, int weight, int maximumWeight, long honor) {  }
  public void ReceiveCounterWindow(int charges, int ttlMilliseconds, int skillId) {  }
  public void ReceiveRaidState(bool active, int wave, int total, int nextSec, int remainSec, int alive) {  }
  public void ReceiveHotbar(WorldHotbarState state) {  }
  public void ReceiveNewbieSkillBookTip(WorldSkillBookState state) {  }
  public void ReceiveNewbiePetSkillsTip(WorldPlayerPetStatusState pet) {  }
  public void ReceiveNewbieTipSeed(List<int> ids) {  }
  public void ReceiveRoundTripLatency(int latencyMs) {  }
  public void ReceiveOnlineCount(int onlineCount) {  }
 }
}
namespace FakeFantasyOnline.Bootstrap
{
 internal class UnityAddressRequest : FakeFantasyOnline.Protocol.NetClient.IAddressRequest
 {
  internal UnityAddressRequest(string url) { }
  public int Timeout {set { }} public object Send() => null; public int Result => 0; public string Error => ""; public string Text => ""; public void Dispose() { }
 }
}
