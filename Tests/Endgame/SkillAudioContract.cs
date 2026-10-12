using System;
using System.Linq;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

internal static class SkillAudioContract
{
    private static int assertions;
    private static readonly int[] WeaponCategories={0,1,2,4,6,8,16,32};
    private static void Check(bool condition,string message)
    {
        assertions++;
        if(!condition) throw new Exception(message);
    }
    private static void Reset(int category=1)
    {
        SoundManager.Calls.Clear();
        CombatNumber.Count=MissFx.Count=EffectPlayer.Impacts=EffectPlayer.Projectiles=SkillCaster.Casts=0;
        CharData.weaponCType=category;CharData.atkDist=category==16?400:75;CharData.riding=false;
        NetClient.SelfEntityId=1;NetClient.PendingCastSkill=0;NetClient.PendingCastGround=null;
        CombatWorld.I=new CombatWorld();
        CombatWorld.I.monsters[10]=new MonsterEntity{id=10,Hp=1000};
        CombatWorld.I.monsters[11]=new MonsterEntity{id=11,Hp=1000};
        CombatWorld.I.players[2]=new RemotePlayer{id=2};
        CombatWorld.I.players[3]=new RemotePlayer{id=3};
        PlayerController.I=new PlayerController();
        PlayerController.I.BindAvatarForIndependentRuntime(new PlayerAvatar());
        CombatNumber.PlayerTransform=PlayerController.I.transform;
    }
    private static WorldSkillCastState Cast(int skill,int phase,int caster=1,int target=10)
    {
        var state=new WorldSkillCastState{SkillId=skill,Phase=phase,CasterEntityId=caster,TargetEntityId=target,
            ChantHoldMilliseconds=300,CooldownMilliseconds=1200};
        ReferenceWorldReceiveOwner.ReceiveSkillCastForIndependentRuntime(state);
        return state;
    }
    private static bool WeaponHit => SoundManager.Calls.Any(c=>c.id>=1260&&c.id<=1267);
    private static bool WeaponSwing => SoundManager.Calls.Any(c=>c.id>=1254&&c.id<=1259);
    private static void LocalSkillPhasesAndDamage()
    {
        foreach(int category in WeaponCategories)
        foreach(int skill in new[]{10201,10501})
        {
            Reset(category);
            var prepare=Cast(skill,0);
            Check(SoundManager.Calls.SequenceEqual(new[]{(31010,25)}),"prepare must retain its own cast sound/delay");
            ReferenceWorldReceiveOwner.ReceiveSkillCastForIndependentRuntime(prepare);
            Check(SoundManager.Calls.Count==1,"reobserving same cast receipt must not duplicate audio");
            SoundManager.Calls.Clear();
            var release=Cast(skill,1);
            Check(SoundManager.Calls.SequenceEqual(new[]{(31020,0),(31021,80)}),"release must retain all fire sounds and delays");
            Check(SkillCaster.Casts==1,"one skill effect release");
            Check(PlayerController.I.ActionFramesForIndependentRuntime!=null,"physical and magic skill poses both remain");
            ReferenceWorldReceiveOwner.ReceiveSkillCastForIndependentRuntime(release);
            Check(SoundManager.Calls.Count==2&&SkillCaster.Casts==1,"release receipt is one-shot");
            CombatWorld.OnCombat(1,10,40,8,960);
            CombatWorld.OnCombat(1,11,50,9,950);
            CombatWorld.OnCombat(1,10,60,8,900);
            Check(SoundManager.Calls.Count==2&&!WeaponHit&&!WeaponSwing,"area and multi-hit skills must not add weapon sounds");
            Check(CombatNumber.Count==3&&EffectPlayer.Impacts==3,"skill damage numbers and hit effects retained");
            Check(CombatWorld.I.monsters[10].Hp==900&&CombatWorld.I.monsters[11].Hp==950,"authoritative HP retained");
            // A real attack can occur immediately after or between skill hits.
            PlayerController.I.PlayAttackForIndependentRuntime(new Vector3(1,0,0),1f);
            int swing=CombatWorld.SwingSndForIndependentRuntime(category);
            Check(swing<=0||SoundManager.Calls.Any(c=>c.id==swing),"real weapon swing retained");
            CombatWorld.OnCombat(1,10,20,0,880);
            Check(SoundManager.Calls.Last().id==CombatWorld.HitSoundForIndependentRuntime(category,false),"real basic hit retained without quiet window");
            int count=SoundManager.Calls.Count;
            CombatWorld.OnCombat(1,10,30,8,850);
            Check(SoundManager.Calls.Count==count,"later segment/proc does not add another basic hit");
        }
    }
    private static void BasicAttackFlagsAndPvp()
    {
        foreach(int category in WeaponCategories)
        {
            Reset(category);
            CombatWorld.OnCombat(1,10,40,0,960);
            Check(SoundManager.Calls.Single().id==CombatWorld.HitSoundForIndependentRuntime(category,false),"ordinary hit table");
            SoundManager.Calls.Clear();
            CombatWorld.OnCombat(1,10,80,1,880);
            Check(SoundManager.Calls.Single().id==1267,"critical basic hit retained");
            SoundManager.Calls.Clear();
            CombatWorld.OnCombat(1,10,0,2,880);
            Check(SoundManager.Calls.Count==0&&MissFx.Count==1,"miss has no hit sound");
            CombatWorld.OnCombat(1,2,40,8,960);
            Check(SoundManager.Calls.Count==0&&CombatWorld.I.players[2].Hp==960,"local skill hitting a remote player suppresses only weapon hit");
            CombatWorld.OnCombat(1,2,40,0,920);
            Check(SoundManager.Calls.Single().id==CombatWorld.HitSoundForIndependentRuntime(category,false),"local PvP basic hit retained");
            SoundManager.Calls.Clear();
            CombatWorld.OnCombat(1,10,880,13,0);
            Check(SoundManager.Calls.Count==0&&CombatWorld.I.monsters[10].IsDying,"critical fatal skill retains death without ordinary critical sound");
        }
    }
    private static void RemoteAndMonsterSkillAudio()
    {
        Reset();
        Cast(10501,0,2);
        Check(SoundManager.Calls.Count==0,"remote prepare preserves native phase guard");
        Cast(10501,1,2);
        Check(SoundManager.Calls.SequenceEqual(new[]{(31020,0),(31021,80)})&&CombatWorld.I.players[2].Casts==1,"remote release retains skill sound");
        CombatWorld.I.monsters[11].NoAttack=true; // Native pet/support feedback also reaches HitSnd.
        CombatWorld.OnCombat(2,10,40,8,960);
        CombatWorld.OnCombat(2,11,40,8,960);
        Check(EffectPlayer.Projectiles==0&&!WeaponHit&&!WeaponSwing,"remote skill results must not create basic projectiles or audio");
        CombatWorld.OnCombat(2,10,20,0,940);
        Check(EffectPlayer.Projectiles==1,"genuine remote basic projectile retained");
        SoundManager.Calls.Clear();
        Cast(10501,1,2,1);
        CombatWorld.OnCombat(2,1,40,8,960);
        Check(SoundManager.Calls.SequenceEqual(new[]{(31020,0),(31021,80),(1701,0)}),"incoming skill keeps skill sounds and local hurt voice");
        Check(EffectPlayer.Projectiles==1,"incoming skill does not add a remote weapon projectile");
        CombatWorld.OnCombat(2,1,20,0,940);
        Check(EffectPlayer.Projectiles==2,"incoming remote basic attack still has its projectile");
        Reset();
        Cast(10501,0,10,1);
        Check(CombatWorld.I.monsters[10].Prepares==1&&SoundManager.Calls.Count==0,"monster prepare retained");
        Cast(10501,1,10,1);
        Check(SoundManager.Calls.SequenceEqual(new[]{(31020,0),(31021,80)})&&CombatWorld.I.monsters[10].Attacks==1,"monster skill fire sound and animation retained");
    }
    private static void MissingSkillSoundAndRiding()
    {
        Reset();
        Cast(99999,1);
        CombatWorld.OnCombat(1,10,10,8,990);
        Check(SoundManager.Calls.Count==0&&SkillCaster.Casts==1,"missing skill sound must not fall back to a weapon hit");
        PlayerController.I.PlayAttackForIndependentRuntime(new Vector3(1,0,0),1);
        Check(SoundManager.Calls.Single().id==1256,"unknown skill does not mute later basic swing");
        SoundManager.Calls.Clear();CharData.riding=true;
        PlayerController.I.PlayAttackForIndependentRuntime(new Vector3(1,0,0),1);
        Check(SoundManager.Calls.Count==0,"existing riding guard retained");
    }
    public static void Main()
    {
        GameTables.Text="{\"sounds\":[{\"id\":10201,\"cast\":31010,\"cd\":25,\"fire\":[31020,31021],\"fd\":[0,80]},{\"id\":10501,\"cast\":31010,\"cd\":25,\"fire\":[31020,31021],\"fd\":[0,80]}]}";
        LocalSkillPhasesAndDamage();BasicAttackFlagsAndPvp();RemoteAndMonsterSkillAudio();MissingSkillSoundAndRiding();
        Console.WriteLine("PASS skill audio: "+assertions+" production-owner assertions (managed substitutes; no audible Unity validation)");
    }
}
