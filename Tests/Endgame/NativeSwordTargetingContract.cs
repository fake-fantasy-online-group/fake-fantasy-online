using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.Presentation.ReferenceAssets;

internal static class NativeSwordTargetingContract
{
    private static int checks;
    private static readonly HashSet<int> Areas = new HashSet<int> {10121,10125,10127,10128};
    private static readonly HashSet<int> Self = new HashSet<int> {10107,10109,10114,10115,10124,10130};
    private static readonly HashSet<int> Passive = new HashSet<int> {10101,10106,10108,10110,10112,10118,10120};
    private static void Check(bool value,string message)
    { checks++; if(!value) throw new Exception(message); }
    private static void Reset()
    {
        foreach(var name in new[]{"skInfo","skLayout","skLoaded"})
        {
            var f=typeof(Win05).GetField(name,BindingFlags.NonPublic|BindingFlags.Static);
            f.SetValue(null,f.FieldType==typeof(bool)?(object)false:null);
        }
    }
    private static Dictionary<string,object> MutatedRow(int id,int shape)
    {
        return new Dictionary<string,object>{["id"]=id,["icon"]=id,["castkind"]=2,["tgt"]=2,
            ["shape"]=shape,["dist"]=999,["radius"]=999,["rwidth"]=180,["rectl"]=999,["rectw"]=999,
            ["canSelf"]=1,["canFriendly"]=1,["canEnemy"]=1,["desc"]="放置一个陷阱，对周围所有敌人造成伤害。"};
    }
    private static void CheckSword(int id)
    {
        var info=Win05.SkillInfo(id);
        bool area=Areas.Contains(id),self=Self.Contains(id),passive=Passive.Contains(id);
        int kind=passive?18:area?2:id==10105?17:1;
        Check(info.tgt==kind,$"{id} native target kind");
        Check(info.castkind==(passive?0:area||self?2:1),$"{id} original cast mode");
        Check(info.shape==(area?2:0),$"{id} native shape");
        Check(info.canFriendly==0 && info.canSelf==(self?1:0) && info.canEnemy==(!self&&!passive?1:0),$"{id} permissions");
        Check(area || info.rwidth==0 && info.rectl==0 && info.rectw==0,$"{id} no invented cone/rectangle");
        if(area) Check(info.radius==999 && info.dist==999 && info.castkind==2 && info.rwidth==180 && info.rectl==999 && info.rectw==999,$"{id} genuine AoE parameters preserved");
        var aim=SkillTargeting.Resolve(id,out var range,out var radius,out var angle,out var len,out var width,out var trap);
        Check(aim==(area||self||passive?SkillTargeting.Aim.Instant:SkillTargeting.Aim.Single),$"{id} actual selector");
        if(!area&&!self&&!passive) Check(range==(id==10105?400:id==10117?300:75) && info.dist==range,$"{id} exact server single-target range");
        Check(!trap && angle==0 && len==0 && width==0,$"{id} no counterfeit trap/ground selector");
        Check(SkillTargeting.SelfCenteredCircle(info)==area,$"{id} genuine self-centered area only");
        Check(area||radius==0,$"{id} single cannot acquire splash radius");
    }
    public static void Main()
    {
        foreach(int malformedShape in new[]{0,1,2,3})
        {
            var rows=new List<Dictionary<string,object>>();
            for(int id=10101;id<=10130;id++) rows.Add(MutatedRow(id,malformedShape));
            // The independent effect catalog validates the original total row count.
            for(int i=0;i<192;i++) rows.Add(MutatedRow(20000+i,malformedShape));
            GameTables.Texts["skills"]=BoundedJson.Serialize(new Dictionary<string,object>{{"skills",rows}});
            GameTables.Texts["skillshow"]=BoundedJson.Serialize(new Dictionary<string,object>{{"shows",Enumerable.Range(1,675).Select(id=>(object)new Dictionary<string,object>{{"id",id},{"phy",id<=230?1:0}}).ToArray()}});
            foreach(bool iconsFirst in new[]{false,true})
            {
                Reset();
                if(iconsFirst) Check(Win05.SkillIconId(10102)==10102,"skills-only loader icon");
                for(int id=10101;id<=10130;id++) CheckSword(id);
                var other=Win05.SkillInfo(20000);
                Check(other.shape==malformedShape && other.radius==999 && other.desc.StartsWith("放置"),"non-sword data unchanged");
            }
            Check(ReferenceSkillEffectCatalog.TryCreate(new ReferenceResourceLoader(),out var catalog),"independent effect catalog loads");
            using(catalog)
            {
                for(int id=10101;id<=10130;id++)
                {
                    Check(catalog.TryGetAimRuleForAudit(id,out var cast,out var kind,out var shape,out var distance,out var enemy),"cast rule exists");
                    Check(shape==(Areas.Contains(id)?2:0),$"{id} independent cache shape");
                    Check(kind==(Passive.Contains(id)?18:Areas.Contains(id)?2:id==10105?17:1),$"{id} independent cache target");
                    Check(catalog.ShouldAimAtResolvedTarget(id)==(!Areas.Contains(id)&&!Self.Contains(id)&&!Passive.Contains(id)), $"{id} independent target aim");
                    if(enemy!=0&&!Areas.Contains(id)) Check(distance==(id==10105?400:id==10117?300:75),$"{id} independent cache exact single range");
                }
            }
        }
        // Exact read-only observed resource deviations, resource MD5
        // cbe007d8955b60a992dd4ef3da682c37: single-target tgt=1, but AoE shape.
        foreach(var row in new[]{
            new Win05.SkInfo{id=10102,tgt=1,castkind=1,shape=3,radius=120,dist=100,rwidth=120,canEnemy=1},
            new Win05.SkInfo{id=10122,tgt=1,castkind=1,shape=2,radius=150,dist=75,canEnemy=1}})
        {
            NativeSwordSkillTargeting.Apply(row);
            Check(row.tgt==1 && row.castkind==1 && row.shape==0 && row.radius==0 && row.rwidth==0 && row.dist==75,$"{row.id} observed area conversion restored with authoritative single-target range");
        }
        foreach(var row in new[]{
            new Win05.SkInfo{id=10121,tgt=2,castkind=1,shape=2,radius=150,dist=0,canEnemy=1},
            new Win05.SkInfo{id=10125,tgt=2,castkind=1,shape=2,radius=150,dist=100,canEnemy=1},
            new Win05.SkInfo{id=10127,tgt=2,castkind=2,shape=2,radius=100,dist=0,canEnemy=1},
            new Win05.SkInfo{id=10128,tgt=2,castkind=2,shape=2,radius=150,dist=0,canEnemy=1}})
        {
            int distance=row.dist,radius=row.radius,cast=row.castkind;
            NativeSwordSkillTargeting.Apply(row);
            Check(row.tgt==2 && row.shape==2 && row.dist==distance && row.radius==radius && row.castkind==cast,$"{row.id} observed genuine area unchanged");
        }
        foreach(var id in Self)
        {
            int distance=id==10115?0:id==10124?100:1;
            var row=new Win05.SkInfo{id=id,tgt=1,castkind=2,shape=0,dist=distance,canSelf=1};
            NativeSwordSkillTargeting.Apply(row);
            Check(row.tgt==1 && row.castkind==2 && row.shape==0 && row.radius==0 && row.dist==distance && row.canSelf==1 && row.canEnemy==0 && row.canFriendly==0,$"{id} observed self buff cast mode and parameters unchanged");
        }
        // Production auto-approach uses 0.9 * range - 2 pixels. At 85px,
        // the altered 100px resource would stop while the 75px server rejects.
        CombatWorld.SetAuditTargetDistance(85f);
        CombatWorld.RequestSkill(10102,100f);
        Check(!CombatWorld.WantsApproach() && CombatWorld.AuditTargetInPendingRange(),"reproduce altered 100px premature stop/cast");
        SkillTargeting.Resolve(10102,out var originalRange,out _,out _,out _,out _,out _);
        CombatWorld.RequestSkill(10102,originalRange);
        Check(originalRange==75f && CombatWorld.WantsApproach() && !CombatWorld.AuditTargetInPendingRange(),"85px continues approach and cannot cast outside original 75px");
        CombatWorld.SetAuditTargetDistance(74f);
        Check(CombatWorld.AuditTargetInPendingRange(),"74px enters original cast range");
        CombatWorld.SetAuditTargetDistance(65f);
        Check(!CombatWorld.WantsApproach() && CombatWorld.AuditTargetInPendingRange(),"original inset stops safely inside server range");
        Console.WriteLine($"PASS {checks} original sword targeting checks across both native loaders and the independent effect cache.");
    }
}
