using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Protocol;
using FakeFantasyOnline.Presentation.ReferenceAssets;
class IdentityHost : IHostExecutor { public bool IsHostThread=>true; public Task RunAsync(Action a,CancellationToken t){a();return Task.CompletedTask;} }
class NativeAttackIdentityContract {
 static int checks;
 static void Check(bool v,string s){checks++;if(!v)throw new Exception(s);}
 static void Appearance(WireWriter w,int c,int reach){for(int i=0;i<6;i++)w.WriteUInt16(0);w.WriteByte(0);w.WriteByte((byte)c);w.WriteUInt16((ushort)reach);}
 static byte[] AppearancePacket(int c,int reach,int? native){var w=new WireWriter();w.WriteInt32(7);Appearance(w,c,reach);w.WriteString("");w.WriteString("");w.WriteString("");w.WriteByte(1);w.WriteByte(0);if(native.HasValue)w.WriteUInt16((ushort)native);return w.ToArray();}
 static byte[] SpawnPacket(int c,int reach,int? native){var w=new WireWriter();w.WriteInt32(7);w.WriteString("remote");w.WriteInt32(0);w.WriteInt32(0);w.WriteInt32(1);for(int i=0;i<4;i++)w.WriteByte(0);w.WriteString("");Appearance(w,c,reach);w.WriteByte(0);w.WriteString("");w.WriteString("");w.WriteByte(0);w.WriteInt32(100);w.WriteInt32(100);w.WriteString("");w.WriteByte(0);w.WriteInt32(0);w.WriteByte(0);w.WriteInt32(1);w.WriteByte(1);w.WriteByte(0);w.WriteInt32(0);w.WriteByte(0);w.WriteByte(0);if(native.HasValue)w.WriteUInt16((ushort)native);return w.ToArray();}
 static byte[] Truncate(byte[] p){Array.Resize(ref p,p.Length-1);return p;}
 static byte[] SelfPacket(int? native){var w=new WireWriter();w.WriteInt32(7);w.WriteByte(0);w.WriteString("self");w.WriteInt32(1);w.WriteInt32(0);w.WriteInt32(0);for(int i=0;i<4;i++)w.WriteByte(0);w.WriteUInt16(0);w.WriteString("");Appearance(w,1,180);w.WriteInt32(0);w.WriteString("");w.WriteInt32(0);w.WriteInt64(1);w.WriteByte(1);w.WriteByte(0);w.WriteByte(0);w.WriteInt32(0);if(native.HasValue)w.WriteUInt16((ushort)native);return w.ToArray();}
 static void TestSelf(){
  foreach(int? native in new int?[]{120,null,200,null}){
   var g=new PrivateServerGateway(PrivateServerOptions.LocalDefault(),new IdentityHost(),()=>0f,worldReceiveOwner:new ReceiveOwner());
   typeof(PrivateServerGateway).GetField("_connectionLifetime",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(g,new CancellationTokenSource());
   WorldEntryRecord received=null;g.BindWorldEntryReceipt(e=>{received=e;g.CaptureWorldEntryCompletion(e)();});
   var receive=typeof(PrivateServerGateway).GetMethod("TryReceiveWorldEntryWithoutWaiter",BindingFlags.Instance|BindingFlags.NonPublic);
   Check((bool)receive.Invoke(g,new object[]{(ushort)0x8003,SelfPacket(native)}),"self handled");
   Check(received.Appearance.NativeAttackDistance==native&&received.Appearance.AttackIdentityDistance==(native??180),"actual self receipt native or legacy");
   if(native.HasValue){bool failed=false;try{receive.Invoke(g,new object[]{(ushort)0x8003,Truncate(SelfPacket(native))});}catch(TargetInvocationException){failed=true;}Check(failed,"short self tail rejected");}
  }
 }
 static void Main(){
  TestSelf();
  foreach(int c in new[]{0,1,2,4,8,16,32,64,128,255})foreach(int native in new[]{0,50,120,150,151,200,600}){
   int effective=native*3/2;
   WorldPlayerAppearanceState a;Check(PrivateServerWorldStateDecoder.TryDecodePlayerAppearance(AppearancePacket(c,effective,native),out a),"appearance");
   Check(a.Appearance.AttackDistance==effective&&a.Appearance.AttackIdentityDistance==native,"split range");
   var live=(WorldPlayerAppearanceState)typeof(PrivateServerGateway).GetMethod("DecodePlayerAppearanceAtReceipt",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{AppearancePacket(c,effective,native)});
   Check(live.Appearance.NativeAttackDistance==native,"live receipt decoder");
   WorldRemotePlayerState remote;int map;Check(PrivateServerWorldStateDecoder.TryDecodeRemotePlayerState(SpawnPacket(c,effective,native),out remote,out map),"spawn");
   Check(remote.Appearance.AttackIdentityDistance==native&&remote.Appearance.AttackDistance==effective,"remote split");
   CharData.atkDist=effective;CharData.nativeAtkDist=native;CharData.weaponCType=c;CharData.attrs=null;CharData.attrsTotal=null;
   bool fast=(native>150&&c!=16)||c==8;
   Check(CombatWorld.TestInterval==(fast?.5f:1f),"native cadence c="+c+" native="+native);
   Check(CombatWorld.BasicAtkUsesMagicPose(c,CharData.AttackIdentityDistance)==(c==16&&native>150),"pose");
   var projectile=CombatWorld.TestProjectile(c,CharData.AttackIdentityDistance);
   Check(projectile.HasValue==(native>150&&(c==16||c==32)),"projectile");
   var rp=new RemotePlayer();rp.BindActionAppearanceForIndependentRuntime(remote.Appearance);rp.PlayAttack();
   Check(rp.avatar.MagicCalls==(c==16&&native>150?1:0)&&rp.avatar.AttackCalls==(c==16&&native>150?0:1),"actual remote pose");
   Check(rp.AtkDistPx==effective&&rp.AttackIdentityDistance==native,"remote range");
   Check(ReferenceWorldBasicAttackController.IsInRange(0,0,effective,0,effective),"effective target reach");
   Check(!ReferenceWorldBasicAttackController.IsInRange(0,0,effective+1,0,effective)||effective==0,"outside target reach");
   rp.SetWeapon(c,native);Check(rp.AttackIdentityDistance==native,"unequip clears extension");
  }
  foreach(int c in new[]{0,8,16,32}){
   WorldPlayerAppearanceState a;Check(PrivateServerWorldStateDecoder.TryDecodePlayerAppearance(AppearancePacket(c,180,null),out a)&&a.Appearance.NativeAttackDistance==null&&a.Appearance.AttackIdentityDistance==180,"legacy fallback");
   Check(!PrivateServerWorldStateDecoder.TryDecodePlayerAppearance(Truncate(AppearancePacket(c,180,120)),out a),"short appearance native tail");
   WorldRemotePlayerState r;int map;Check(!PrivateServerWorldStateDecoder.TryDecodeRemotePlayerState(Truncate(SpawnPacket(c,180,120)),out r,out map),"short spawn native tail");
   Check(PrivateServerWorldStateDecoder.TryDecodeRemotePlayerState(SpawnPacket(c,180,null),out r,out map)&&r.Appearance.AttackIdentityDistance==180,"legacy spawn");
  }
  CharData.atkDist=180;CharData.nativeAtkDist=120;CharData.weaponCType=1;CharData.attrsTotal=new int[34];CharData.attrsTotal[33]=200;Check(CombatWorld.TestInterval==.5f,"speed attribute retained");
  Check(ReferenceWorldBasicAttackController.AttackIntervalSeconds(180,1,null,null,120)==1f,"audit helper native range");
  Console.WriteLine("PASS native attack identity "+checks+" assertions: all ctypes,120→180, true ranged, live appearance+remote wire, actual remote pose, projectile/cadence/range, legacy and truncated tails. Unity rendering not exercised.");
 }
}
