using System;
using System.IO;
using System.Linq;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

internal static class DualWeaponPresentationContract
{
    static int assertions, poses;
    static void Check(bool condition,string message)
    {
        assertions++;if(!condition)throw new Exception(message);
    }
    static CharacterAppearanceRecord Appearance(int right,int left=0)=>new CharacterAppearanceRecord(0,0,0,right,left,0,3,8,75);
    static ReferenceAvatarAssets Plan(int right,int left,int gender)
    {
        CharacterAppearanceRecord source=Appearance(right,left);
        var row=new CharacterSelectionItem("test","test",3,gender,1,1,60,"",source,null,0);
        Check(ReferenceAvatarAssetPlan.TryCreate(row,out var result),"valid asset plan");
        Check(source.RightWeapon==right&&source.LeftWeapon==left&&source.AttackVariant==3&&source.WeaponCategory==8&&source.AttackDistance==75,"source appearance/combat fields unchanged");
        return result;
    }
    static void Selection()
    {
        foreach(int gender in new[]{0,1})foreach(int model in new[]{51,60,1282})
        {
            var plan=Plan(model,0,gender);
            Check(plan.Parts.Count(x=>x.Tag==6&&x.Folder=="weaponr"&&x.Model==model)==1,"right-hand source");
            Check(plan.Parts.Count(x=>x.Tag==5&&x.Folder=="weaponl"&&x.Model==model)==1,"paired left-hand source");
            var left=plan.Parts.Single(x=>x.Tag==5);
            foreach(int action in new[]{1,2,3,5,6,7,8,9,10,11,12,13})
            {
                string prefix="avatar/weaponl/weaponl"+(model*100+action+(gender==1?0:50)).ToString("D4");
                Check(left.CandidatePathsForAction(action)[0]==prefix+".gsn","gender/action-specific left artwork");
                Check(left.GlowCandidatePathsForAction(action)[0]==prefix+"e.gsn","gender/action-specific left glow");
            }
        }
        // No race/category/slot inference: all models outside the source whitelist remain single.
        foreach(int model in new[]{0,1,6,30,70,101,402,999,65535})
            Check(!Plan(model,0,1).Parts.Any(x=>x.Tag==5),"non-dual weapon must not gain left art: "+model);
        Check(Plan(60,402,1).Parts.Single(x=>x.Tag==5).Model==402,"explicit shield remains authoritative");
        Check(Plan(0,402,1).Parts.Single(x=>x.Tag==5).Model==402,"standalone left appearance remains");
        Check(ReferenceAvatarWeaponPresentation.LeftModel(60,-1)==-1,"preserve nonzero left sentinel");
        for(int direction=0;direction<8;direction++)
            Check(AvatarDepth.DirFold[direction]>=0&&AvatarDepth.DirFold[direction]<5,"all eight screen directions use covered source directions");
        foreach(int model in new[]{60,1,51,30,0,60,0})
            Check(Plan(model,0,1).Parts.Any(x=>x.Tag==5)==(model==60||model==51),"switch/unequip preview has no retained left art");
        Console.WriteLine("PASS visual-only paired model selection, preview, explicit left, switches and action paths");
    }
    static PlayerAvatar Build(int gender,int model,int left=0)
    {
        var input=new PlayerAvatar.Appear{WeaponR=model,WeaponL=left};
        PlayerAvatar result=PlayerAvatar.Build(gender,1,1,input,1);
        Check(input.WeaponR==model&&input.WeaponL==left,"build must not mutate caller appearance");
        Check(result.ok,"real source build: "+gender+"/"+model);
        return result;
    }
    static void Paired(PlayerAvatar.Pose pose,string context)
    {
        Check(pose!=null,"missing pose: "+context);
        var left=pose.layers.Where(x=>x.tag==5&&!x.additive).ToArray();
        var right=pose.layers.Where(x=>x.tag==6&&!x.additive).ToArray();
        Check(left.Length==1&&right.Length==1,"two independent hands: "+context);
        Check(left[0].spr!=null&&right[0].spr!=null,"both sprites decoded: "+context);
        Check(left[0].spr.name.StartsWith("Pweaponl")&&right[0].spr.name.StartsWith("Pweaponr"),"dedicated art rather than duplicated/mirrored sprite: "+context);
        Check(!ReferenceEquals(left[0].spr,right[0].spr),"distinct hand sprites: "+context);
        poses++;
    }
    static void Matched(PlayerAvatar.Pose actual,PlayerAvatar.Pose explicitPair,string context)
    {
        Paired(actual,context);
        Check(actual.layers.Length==explicitPair.layers.Length,"same layer count as explicit pair: "+context);
        for(int i=0;i<actual.layers.Length;i++)
        {
            var a=actual.layers[i];var b=explicitPair.layers[i];
            Check(a.tag==b.tag&&a.additive==b.additive&&a.offX==b.offX&&a.offY==b.offY&&a.spr==b.spr,"original binding/depth/action parity: "+context);
        }
    }
    static void Raster(string path,Sprite sprite)
    {
        var t=sprite.texture;var px=t.GetPixels32();using(var file=new BinaryWriter(File.Create(path)))
        {
            file.Write(t.width);file.Write(t.height);
            for(int y=t.height-1;y>=0;y--)for(int x=0;x<t.width;x++)
            {var p=px[y*t.width+x];file.Write(p.r);file.Write(p.g);file.Write(p.b);file.Write(p.a);}
        }
    }
    static void Assets(string directory)
    {
        ResLoader.I=new ResLoader{Root=Path.Combine(directory,"resources")};
        string previews=Path.Combine(directory,"preview");Directory.CreateDirectory(previews);
        foreach(int gender in new[]{0,1})foreach(int model in new[]{51,60,1282})
        {
            var automatic=Build(gender,model);var explicitPair=Build(gender,model,model);
            // Nonzero empty-art sentinel reproduces the old right-only composition for review.
            var before=model==60?Build(gender,model,-1):null;
            for(int d=0;d<5;d++)
            {
                Matched(automatic.stand[d],explicitPair.stand[d],$"stand {gender}/{model}/{d}");
                Matched(automatic.sit[d],explicitPair.sit[d],$"sit {gender}/{model}/{d}");
                for(int f=0;f<8;f++)Matched(automatic.walk[d,f],explicitPair.walk[d,f],$"walk {gender}/{model}/{d}/{f}");
                Raster(Path.Combine(previews,$"g{gender}-m{model}-d{d}.rgba"),automatic.Still(d));
                if(before!=null)Raster(Path.Combine(previews,$"before-g{gender}-m{model}-d{d}.rgba"),before.Still(d));
            }
            foreach(int variant in new[]{0,1,2})
            {
                var actual=automatic.AttackPose(variant,out int count);var expected=explicitPair.AttackPose(variant,out int expectedCount);
                Check(actual!=null&&count==expectedCount,"attack action available");
                for(int d=0;d<5;d++)for(int f=0;f<count;f++)Matched(actual[d,f],expected[d,f],$"attack {gender}/{model}/{variant}/{d}/{f}");
            }
            var magic=automatic.MagicPose(out int magicCount);var explicitMagic=explicitPair.MagicPose(out int explicitMagicCount);
            Check(magic!=null&&magicCount==explicitMagicCount,"magic action available");
            for(int d=0;d<5;d++)for(int f=0;f<magicCount;f++)Matched(magic[d,f],explicitMagic[d,f],$"magic {gender}/{model}/{d}/{f}");
            foreach(int action in new[]{3,8,9,10,11,12})
            {
                var actual=automatic.PoseAt(action);var expected=explicitPair.PoseAt(action);
                for(int d=0;d<5;d++)Matched(actual[d],expected[d],$"pose {gender}/{model}/{action}/{d}");
            }
            Check(automatic.GlowModelOf(5)==model,"left glow follows visual pair");
            if(model==1282)Check(automatic.stand.Any(x=>x.layers.Any(layer=>layer.tag==5&&layer.additive)),"dual animated weapon retains its separate left glow");
            before?.Dispose();
            automatic.Dispose();explicitPair.Dispose();
        }
        foreach(int model in new[]{60,1,51,30,0,60,0})
        {
            var avatar=Build(1,model);
            for(int d=0;d<5;d++)Check(avatar.stand[d].layers.Any(x=>x.tag==5)==(model==60||model==51),"world build switching leaves no stale left layer");
            avatar.Dispose();
        }
        var shield=Build(1,60,402);Check(shield.GlowModelOf(5)==402,"explicit left model in runtime");
        for(int d=0;d<5;d++)Check(shield.stand[d].layers.Single(x=>x.tag==5).spr.name.StartsWith("Pweaponl402"),"explicit left resource remains");
        Console.WriteLine("PASS "+poses+" real-asset poses across both genders, five art directions, stand/run/sit/attacks/magic/other poses; original binding and layer-order parity");
    }
    static int Main()
    {
        try
        {
            string directory=Environment.GetEnvironmentVariable("FFO_AVATAR_EVIDENCE_DIR");GameTables.Root=directory;
            Selection();
            if(!string.IsNullOrEmpty(directory))Assets(directory);
            else Console.WriteLine("SKIP real-asset poses (set FFO_AVATAR_EVIDENCE_DIR)");
            Console.WriteLine("PASS "+assertions+" dual-weapon assertions");return 0;
        }
        catch(Exception error){Console.Error.WriteLine(error);return 1;}
    }
}
