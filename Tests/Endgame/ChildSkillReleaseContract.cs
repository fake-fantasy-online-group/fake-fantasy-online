using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.Protocol;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;
class ChildHost:IHostExecutor {public bool IsHostThread=>true;public Task RunAsync(Action a,CancellationToken t){a();return Task.CompletedTask;} }
class ChildSkillReleaseContract {
 static int checks;static void Check(bool v,string m){checks++;if(!v)throw new Exception(m);}
 static byte[] Packet(int caster=7,int skill=123,int target=9,int map=2,byte mode=0){var w=new WireWriter();foreach(int x in new[]{caster,skill,target,map,120,240,700,600})w.WriteInt32(x);w.WriteByte(mode);return w.ToArray();}
 static void Receive(PrivateServerGateway g,ushort op,byte[] bytes){typeof(PrivateServerGateway).GetMethod("ProcessWorldPacket",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(g,new object[]{new ProtocolPacket(op,bytes),CancellationToken.None});}
 static void TestOriginalRows(){
  string fixture=Environment.GetEnvironmentVariable("FFO_NATIVE_CHILD_FIXTURE");
  if(string.IsNullOrEmpty(fixture))return;
  GameTables.NativeFixture=fixture;
  typeof(SkillEffectDB).GetField("map",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,null);
  foreach(int skill in new[]{10319,10409}){
   int effect=skill==10319?47:40,delay=skill==10319?550:370;
   var entry=SkillEffectDB.Get(skill);
   Check(entry!=null&&entry.atk.Count==0&&entry.fly.Count==1&&entry.hit.Count==1,"original row parsed into fly+hit only");
   Check(entry.fly[0].effect==effect&&entry.fly[0].index==20&&entry.fly[0].delay==delay,"native projectile ID/phase/delay");
   Check(entry.hit[0].effect==effect&&entry.hit[0].index==30&&entry.hit[0].delay==0,"native impact ID/phase/delay");
   var g=new PrivateServerGateway(PrivateServerOptions.LocalDefault(),new ChildHost(),()=>0f);
   var view=new ReferenceFirstWorldWindowOwner{session=g};
   EffectPlayer.Created.Clear();MonoBehaviour.Yields.Clear();int sounds=SoundManager.Played.Count;
   Receive(g,ProtocolOpcodes.ChildSkillRelease,Packet(skill:skill));view.Sync();
   Check(EffectPlayer.Created.Count==1,"original mode0 creates projectile, no synthetic hit");
   Check(EffectPlayer.Created[0].Name==string.Format("TFX{0:D3}_20",effect)&&EffectPlayer.Created[0].MoveDuration==delay,"native release EFF name and cosmetic travel");
   Check(Math.Abs(EffectPlayer.Created[0].Position.x+3.8f)<.001&&EffectPlayer.Created[0].Destination.Value.x==2,"original release frozen origin/center");
   Receive(g,ProtocolOpcodes.ChildSkillRelease,Packet(skill:skill,mode:1));view.Sync();
   Check(EffectPlayer.Created.Count==2&&EffectPlayer.Created[1].Name==string.Format("TFX{0:D3}_30",effect),"original mode1 creates only native landed hit");
   Check(EffectPlayer.Created[1].Position.x==2&&EffectPlayer.Created[1].Position.y==-2,"original impact landed center");
   Check(MonoBehaviour.Yields.Count==0,"original landed hit does not inherit projectile delay");
   view.Sync(true);Check(EffectPlayer.Created.Count==2,"original native release/impact once each");
   // No invented audio fixture: original skillsound is not available in this run.
   Check(SoundManager.Played.Count==sounds,"absent sound row does not invent audio");
  }
  Console.WriteLine("PASS 22 original BIN-derived native table assertions: 10319 TFX047_20/30 and 10409 TFX040_20/30; actual parser and mode0/mode1 pipeline. Not published compact JSON, sound assets or Unity rendering.");
 }
 static void Main(){
  WorldSkillHitEffect e;var bytes=Packet();Check(ProtocolOpcodes.ChildSkillRelease==0x80f2,"opcode");Check(PrivateServerWorldStateDecoder.TryDecodeChildSkillRelease(bytes,out e),"decode");
  Check(e.IsChildRelease&&!e.IsGround&&e.EntityId==7&&e.SkillId==123&&e.TargetEntityId==9&&e.MapId==2&&e.OriginX==120&&e.OriginY==240&&e.X==700&&e.Y==600,"all wire fields");
  e=e.WithRevision(4);Check(e.Revision==4&&e.IsChildRelease&&e.OriginX==120&&e.TargetEntityId==9,"queue preserves origin/target");
  for(int n=0;n<33;n++){var b=new byte[n];Array.Copy(bytes,b,n);Check(!PrivateServerWorldStateDecoder.TryDecodeChildSkillRelease(b,out e),"truncated");}
  Check(!PrivateServerWorldStateDecoder.TryDecodeChildSkillRelease(new byte[34],out e),"trailing");Check(!PrivateServerWorldStateDecoder.TryDecodeChildSkillRelease(null,out e),"null");
  Check(!PrivateServerWorldStateDecoder.TryDecodeChildSkillRelease(Packet(caster:0),out e),"caster required");Check(!PrivateServerWorldStateDecoder.TryDecodeChildSkillRelease(Packet(skill:0),out e),"skill required");Check(!PrivateServerWorldStateDecoder.TryDecodeChildSkillRelease(Packet(map:0),out e),"map required");
  var g=new PrivateServerGateway(PrivateServerOptions.LocalDefault(),new ChildHost(),()=>0f);var view=new ReferenceFirstWorldWindowOwner{session=g};
  CombatNumber.PlayerTransform.position=new Vector3(999,888,0);CombatNumber.PlayerTransform.components[typeof(PlayerController)]=new PlayerController();CombatWorld.Monsters[9]=new Transform{position=new Vector3(6,5,0)};
  long casts=g.SkillCastStateVersion;
  Receive(g,ProtocolOpcodes.ChildSkillRelease,bytes);Check(g.SkillHitEffects.Count==1&&g.SkillHitEffects[0].Revision>0,"gateway queued one release");view.Sync();
  Check(EffectPlayer.Created.Count==2,"only native fly and atk, no two hit phases or script");
  var runner=MonoBehaviour.Created[0];
  Check(ReferenceEquals(typeof(SkillCaster).GetField("caster",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(runner),CombatNumber.PlayerTransform),"source transform resolves to owner");
  Check(ReferenceEquals(typeof(SkillCaster).GetField("target",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(runner),CombatWorld.Monsters[9]),"target transform identity preserved");
  var fly=EffectPlayer.Created[0];Check(fly.Name=="TFX043_20"&&Math.Abs(fly.Position.x+3.8f)<.001&&Math.Abs(fly.Position.y-1.6f)<.001,"native projectile frozen origin, not live caster");
  Check(fly.Destination.HasValue&&fly.Destination.Value.x==2&&fly.Destination.Value.y==-2,"authoritative target center");Check(fly.Direction==2&&fly.Flip,"caster orientation read only");
  Check(EffectPlayer.Created[1].Name=="TFX042_10"&&EffectPlayer.Created[1].Position.x==fly.Position.x,"native attack at source");
  Check(SoundManager.Played.Count==1&&SoundManager.Played[0]==101,"fire only, no preparation sound");Check(CombatWorld.Hits==0,"release cannot synthesize landed hit");Check(g.SkillCastStateVersion==casts,"no cast state or cooldown publication");
  view.Sync();view.Sync(true);Check(EffectPlayer.Created.Count==2&&SoundManager.Played.Count==1,"cursor renders once even forced repaint");
  var hit=new WireWriter();hit.WriteInt32(9);hit.WriteString("TFX044_30");Receive(g,ProtocolOpcodes.SkillHitEffect,hit.ToArray());view.Sync();Check(CombatWorld.Hits==1&&EffectPlayer.Created.Count==2,"actual server hit independent exactly once");view.Sync(true);Check(CombatWorld.Hits==1,"no duplicate hit repaint");
  int soundCount=SoundManager.Played.Count;int waits=MonoBehaviour.Yields.Count;
  Receive(g,ProtocolOpcodes.ChildSkillRelease,Packet(mode:1));view.Sync();
  Check(EffectPlayer.Created.Count==4&&EffectPlayer.Created[2].Name=="TFX044_30"&&EffectPlayer.Created[3].Name=="TFX045_50","real landed native impact only, all hit phases");
  Check(EffectPlayer.Created[2].Position.x==2&&EffectPlayer.Created[2].Position.y==-2&&SoundManager.Played.Count==soundCount,"native impact at landed center without fire sound");
  view.Sync(true);Check(EffectPlayer.Created.Count==4,"impact once");
  Check(MonoBehaviour.Yields.Count==waits,"landed impact does not wait for a second projectile");
  EffectPlayer.Created.RemoveRange(2,2);
  Check(!PrivateServerWorldStateDecoder.TryDecodeChildSkillRelease(Packet(mode:2),out e),"unknown mode rejected");
  Receive(g,ProtocolOpcodes.ChildSkillRelease,Packet(map:3));view.Sync();Check(EffectPlayer.Created.Count==2,"wrong map does not leak art");
  CombatWorld.Players[8]=new RemotePlayer();Receive(g,ProtocolOpcodes.ChildSkillRelease,Packet(caster:8,skill:124,target:0));view.Sync();Check(EffectPlayer.Created.Count==3&&EffectPlayer.Created[2].Name=="TFX046_10"&&EffectPlayer.Created[2].Position.x==2&&EffectPlayer.Created[2].Position.y==-2,"remote ground-only native attack at center");
  Receive(g,ProtocolOpcodes.ChildSkillRelease,Packet(caster:12345));view.Sync();Check(EffectPlayer.Created.Count==5,"missing actor still uses authoritative fixed origin");
  var legacy=new WorldSkillHitEffect(9,"FX").WithRevision(42);Check(!legacy.IsChildRelease&&!legacy.IsGround&&legacy.Effect=="FX","legacy event unchanged");
  EffectPlayer.Created.Clear();SkillCaster.Cast(123,CombatNumber.PlayerTransform,CombatWorld.Monsters[9],100);Check(EffectPlayer.Created.Count==1&&EffectPlayer.Created[0].Name=="script:999","ordinary cast script untouched");
  EffectPlayer.Created.Clear();
  typeof(SkillCaster).GetMethod("CastData",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{123,CombatNumber.PlayerTransform,CombatWorld.Monsters[9],100f,null,false});
  Check(EffectPlayer.Created.Count==4,"ordinary native data cast retains release and automatic hit phases");
  TestOriginalRows();
  Console.WriteLine("PASS child skill release "+checks+" assertions: real gateway/codec/queue/cursor, native phases+sound, frozen origin+center, local/remote/missing actors, malformed, map isolation, once, no cast/CD publication, no automatic hits, legacy cast preserved. Unity rendering not exercised.");
 }
}
