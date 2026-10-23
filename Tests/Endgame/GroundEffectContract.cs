using System;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Protocol;
using FakeFantasyOnline.Presentation.ReferenceAssets;
class GroundEffectContract
{
 static void Check(bool value,string message) { if(!value) throw new Exception(message); }
 static byte[] Packet(byte mode=1,int duration=1500,string effect="FX") {
  var w=new WireWriter(); w.WriteInt32(7);w.WriteInt32(2);w.WriteInt32(120);w.WriteInt32(240);w.WriteString("zone");w.WriteString(effect);w.WriteInt32(duration);w.WriteByte(mode);return w.ToArray();
 }
 static void Main() {
  WorldSkillHitEffect e;
  Check(PrivateServerWorldStateDecoder.TryDecodeGroundEffect(Packet(),out e),"decode");
  Check(e.IsGround&&e.EntityId==7&&e.MapId==2&&e.X==120&&e.Y==240&&e.DurationMS==1500&&e.Mode==1,"fields");
  var rev=e.WithRevision(13);Check(rev.IsGround&&rev.Key=="zone"&&rev.X==120&&rev.Revision==13,"queue revision preserves fixed coordinates");
  Check(PrivateServerWorldStateDecoder.TryDecodeGroundEffect(Packet(2,0,""),out e),"stop");
  Check(!PrivateServerWorldStateDecoder.TryDecodeGroundEffect(Packet(3),out e),"unknown mode");
  Check(!PrivateServerWorldStateDecoder.TryDecodeGroundEffect(Packet(1,-1),out e),"negative duration");
  Check(!PrivateServerWorldStateDecoder.TryDecodeGroundEffect(Packet(0,0,""),out e),"empty play");
  byte[] valid=Packet();for(int n=0;n<valid.Length;n++){var shortPacket=new byte[n];Array.Copy(valid,shortPacket,n);Check(!PrivateServerWorldStateDecoder.TryDecodeGroundEffect(shortPacket,out e),"truncation");}
  var extra=new byte[valid.Length+1];Array.Copy(valid,extra,valid.Length);Check(!PrivateServerWorldStateDecoder.TryDecodeGroundEffect(extra,out e),"trailing");
  var legacy=new WorldSkillHitEffect(7,"FX").WithRevision(2);Check(!legacy.IsGround&&legacy.EntityId==7,"legacy");
  var world=new CombatWorld();
  world.PlayGroundEffect(new WorldSkillHitEffect(7,2,120,240,"zone","FX",1500,1));
  var fixedFx=EffectPlayer.Created[0];
  Check(fixedFx.Sustained && fixedFx.Follow==null && fixedFx.Duration==1.5f && Math.Abs(fixedFx.Position.x+3.8f)<0.001 && Math.Abs(fixedFx.Position.y-1.6f)<0.001,"fixed world pixel conversion and no actor follow");
  world.PlayGroundEffect(new WorldSkillHitEffect(7,2,500,400,"zone","FX",0,1));
  Check(fixedFx.Stopped && EffectPlayer.Created[1].Duration==0,"replacement and indefinite");
  world.PlayGroundEffect(new WorldSkillHitEffect(7,2,0,0,"zone","",0,2));
  Check(EffectPlayer.Created[1].Stopped,"explicit stop");
  world.PlayGroundEffect(new WorldSkillHitEffect(8,2,0,0,"one","FX",0,0));
  world.ClearGroundEffects();Check(EffectPlayer.Created[2].Stopped,"map teardown clears one-shot too");
  var state=new EndgameClientState();state.Begin(new EndgameRequest("hello"));
  state.Apply(EndgameResponse.Parse("{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"],\"result\":{\"riftPages\":\"1\"}}"));
  Check(!state.GroundFXV1,"hello cannot opt into ground");
  var cap=new EndgameRequest("capabilities");Check(cap.Json.Contains("endgame.ground-fx.v1")&&!new EndgameRequest("hello").Json.Contains("endgame.ground-fx.v1"),"secondary only");
  Check(state.Begin(cap),"capability request accepted");
  state.Apply(EndgameResponse.Parse("{\"kind\":\"capabilities\",\"requestId\":\""+cap.RequestID+"\",\"ok\":true,\"capabilities\":[\"endgame.v1\",\"endgame.rift-pages.v1\",\"endgame.ground-fx.v1\"]}"));
  Check(state.GroundFXV1,"explicit acknowledgement");state.Reset();Check(!state.GroundFXV1,"connection reset");
  Console.WriteLine("PASS ground wire validation, fixed queue metadata, stop, malformed packets, legacy contract; all managed core source compiled");
 }
}
