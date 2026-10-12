using System;
using System.IO;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.Presentation.ReferenceAssets;

class SkillBehaviorPresentationAcceptance
{
    static int checks,writes;static string tip;static NetClient client;
    static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
    static EndgameClientState State(bool behavior=true, bool variants=false)
    {
        var s=new EndgameClientState();s.Begin(new EndgameRequest("hello"));
        s.Apply(EndgameResponse.Parse("{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\",\"endgame.growth.v5\""+(behavior?",\"endgame.skill-behavior.v1\"":"")+(variants?",\"endgame.skill-variants.v1\"":"")+"]}"));
        EndgamePanel.GrowthState=s;return s;
    }
    static Dictionary<string,object> Obj(object value)=>(Dictionary<string,object>)value;
    static object[] Array(object value){var input=(IReadOnlyList<object>)value;var result=new object[input.Count];for(int i=0;i<input.Count;i++)result[i]=input[i];return result;}
    static Dictionary<string,object> Only(string source,int id)
    {
        var root=Obj(BoundedJson.Parse(source));foreach(var row in Array(root["skillRanks"]))if(Convert.ToInt32(Obj(row)["id"])==id){root["skillRanks"]=new object[]{row};return root;}throw new Exception("fixture lacks"+id);
    }
    static EndgameSkillRankView Apply(EndgameClientState s,Dictionary<string,object> root)
    {Check(s.Apply(EndgameResponse.Parse(BoundedJson.Serialize(root))),"published fixture");return s.SkillRanks[0];}
    static void MP(int id,int available,bool allowed)
    {writes=0;tip=null;CharData.mp=available;client.UseSkill(id,2);Check(writes==(allowed?1:0),"actual NetClient MP boundary"+id+" mp"+available+" tip"+tip);}
    // Upper-bound equipped packets are not the baseline 149-row fixtures: they
    // may contain a cross-profession grant and have equipment-resolved MP/ranks.
    static void VariantPackets(string[] args)
    {
        Check(args.Length==6,"five full variant packets required after --variant-packets");
        var professions=new HashSet<int>(); var variants=new HashSet<string>();
        for(int index=1;index<args.Length;index++)
        {
            string text=File.ReadAllText(args[index]); int bytes=System.Text.Encoding.UTF8.GetByteCount(text);
            Check(bytes<61440,"full variant packet under transport budget");
            var packet=EndgameResponse.Parse(text);
            var legacy=State();Check(!legacy.Apply(packet) && !legacy.HasSkillRanks,"full variant packet cannot activate without variant cap");
            var state=State(variants:true);Check(state.SkillVariantsV1 && state.Apply(packet),"full variant packet applies with complete capabilities");
            Check(state.SkillRanks.Count>=29 && state.SkillRanks.Count<=31,"full profession plus bounded treasure grant");
            var ids=new HashSet<int>(); int formCount=0;
            foreach(var row in state.SkillRanks)
            {
                Check(ids.Add(row.ID),"unique ID within full packet");
                if(row.ID==10212){Check(row.Contract==null,"unresolved theft remains held");continue;}
                var contract=row.Contract;Check(contract!=null,"full packet contract present");
                var behavior=contract.Current;
                // Changing race only chooses the local presentation owner; the exact
                // row itself remains the authoritative server equipment projection.
                CharData.race=contract.Profession-1;
                var info=Win05.SkillInfo(row.ID);
                Check(info!=null && info.name==contract.Name && info.icon==contract.Icon,"full packet original identity");
                Check(AuthoritativeProfessionSkill.Current(row.ID)==behavior,"full packet exact current plan");
                AuthoritativeProfessionSkill.TryResolve(row.ID,out var aim,out var range,out var radius,out var angle,out var length,out var width,out var trap);
                if(behavior==null || !behavior.Usable){Check(aim==SkillTargeting.Aim.Unavailable,"full packet uncastable row remains unavailable");continue;}
                Check(range==behavior.CastDistance && radius==behavior.Area.Radius && angle==behavior.Area.Angle && length==behavior.Area.Length && width==behavior.Area.Width,"full packet exact targeting geometry");
                var wanted=behavior.InputMode=="entity"?SkillTargeting.Aim.Single:behavior.InputMode=="point"?SkillTargeting.Aim.Circle:behavior.InputMode=="direction"?SkillTargeting.Aim.Direction:behavior.InputMode=="trapEntity"?SkillTargeting.Aim.Trap:SkillTargeting.Aim.Instant;
                Check(aim==wanted,"full packet input mode");
                var variant=behavior.Variant;if(variant==null)continue;
                formCount++;variants.Add(variant.ID);professions.Add(contract.Profession);
                Check(state.CurrentSkillVariant(row.ID)==variant && variant.MaxTargets==(row.ID==10017 || row.ID==10102?4:1),"full packet finite variant target cap");
                Check(row.CurrentEffectLines.Count>0 && row.CurrentEffectLines[0].Contains(variant.Name),"full packet variant text matches plan");
                if(row.ID==10401)Check(behavior.PrepareMilliseconds==0 && behavior.CooldownMilliseconds>=1200 && behavior.MPCost>0,"full packet flash fire preserves paid cooldown");
            }
            Check(formCount==1,"upper-bound packet contains its one declared form");
            Console.WriteLine("Full variant upper-bound packet: "+Path.GetFileName(args[index])+" "+bytes+" bytes / "+state.SkillRanks.Count+" rows");
        }
        Check(professions.Count==5 && variants.Count==5,"five professions and five distinct equipment forms");
        Console.WriteLine("Skill variant full-packet Presentation acceptance: "+checks+" checks passed (conservative server metadata upper bound; no Unity/Player).");
    }
    static void Main(string[] args)
    {
        Check(args.Length>=5,"five server response fixtures required");
        // Deliberately polluted/missing synthetic resources; no external table is imported.
        GameTables.Texts["skills"]="{\"skills\":[{\"id\":10401,\"name\":\"旁观火弹\",\"icon\":5046,\"castkind\":1,\"tgt\":1,\"canEnemy\":1,\"dist\":450},{\"id\":10306,\"name\":\"污染净世\",\"icon\":1,\"levels\":[{\"lv\":1,\"mp\":100}]},{\"id\":10307,\"name\":\"污染祝祷\",\"icon\":1,\"levels\":[{\"lv\":1,\"mp\":120}]},{\"id\":10212,\"name\":\"偷窃术\",\"icon\":5031,\"desc\":\"legacy theft placeholder\",\"castkind\":1,\"tgt\":1,\"canEnemy\":1,\"levels\":[{\"lv\":1,\"mp\":69}]}]}";
        CharData.skills[10306]=CharData.skills[10307]=CharData.skills[10212]=1;
        client=new NetClient();NetClient.I=client;client.BindSkillCommandsForIndependentRuntime((s,f)=>tip=s,(id,target)=>writes++,(id,x,y,target)=>writes++,on=>{},()=>{});
        if(args[0]=="--variant-packets"){VariantPackets(args);return;}
        var seen=new HashSet<int>();string healer=null,assassin=null;
        foreach(string path in new[]{args[0],args[1],args[2],args[3],args[4]})
        {
            string text=File.ReadAllText(path);var response=EndgameResponse.Parse(text);CharData.race=response.SkillRanks[0].Contract.Profession-1;var s=State();Check(s.Apply(response),"real server rows apply");
            foreach(var row in s.SkillRanks)
            {
                Check(seen.Add(row.ID),"unique profession ID"+row.ID);
                if(row.ID==10212){assassin=text;Check(row.Contract==null,"theft explicit server hold");Check(!AuthoritativeProfessionSkill.Uses(row.ID),"theft keeps approved legacy path");Check(Win05.SkillInfo(row.ID)?.name=="偷窃术","theft never becomes SQL damage identity");continue;}
                Check(AuthoritativeProfessionSkill.Uses(row.ID),"typed provider selected"+row.ID);
                var info=Win05.SkillInfo(row.ID);var contract=row.Contract;var value=contract.Current;
                Check(info!=null && info.name==contract.Name && info.icon==contract.Icon,"real loader authoritative identity"+row.ID);
                Check(Win05.SkillIconId(row.ID)==contract.Icon,"hotbar icon"+row.ID);
                Check(AuthoritativeProfessionSkill.LearnedActive(row.ID)==value.Usable,"active availability"+row.ID);
                Check(AuthoritativeProfessionSkill.TryResolve(row.ID,out var aim,out var range,out var radius,out var angle,out var length,out var width,out var trap),"typed targeting owns"+row.ID);
                Check(range==(value.Usable?value.CastDistance:0),"literal range retained"+row.ID);
                if(value.Usable){Check(radius==value.Area.Radius && angle==value.Area.Angle && length==value.Area.Length && width==value.Area.Width,"exact geometry"+row.ID);var wanted=value.InputMode=="entity"?SkillTargeting.Aim.Single:value.InputMode=="point"?SkillTargeting.Aim.Circle:value.InputMode=="direction"?SkillTargeting.Aim.Direction:value.InputMode=="trapEntity"?SkillTargeting.Aim.Trap:SkillTargeting.Aim.Instant;Check(aim==wanted,"structured input"+row.ID);}else Check(aim==SkillTargeting.Aim.Unavailable,"passive never castable"+row.ID);
                if(row.ID==10306){healer=text;Check(value.MPCost==48 && info.name=="净化术","verified cost/name48");MP(10306,47,false);MP(10306,48,true);MP(10306,60,true);}
                if(row.ID==10307){Check(value.MPCost==35 && info.name=="祝福术","verified cost/name35");MP(10307,34,false);MP(10307,35,true);MP(10307,50,true);}
                if(row.ID==10316 || row.ID==10318 || row.ID==10321)Check(info.icon>0 && info.canFriendly==1 && info.canSelf==1 && info.desc==string.Empty,"missing raw healer row completely provided"+row.ID);
            }
        }
        Check(seen.Count==149 && healer!=null && assassin!=null,"all149 across five professions");
        CharData.race=3;var state=State();Check(state.Apply(EndgameResponse.Parse(healer)),"healer state");
        Check(!AuthoritativeProfessionSkill.Uses(10401) && Win05.SkillInfo(10401)?.name=="旁观火弹","other profession receipt keeps legacy metadata");
        Check(!AuthoritativeProfessionSkill.TryResolve(10401,out _,out _,out _,out _,out _,out _,out _),"other profession targeting fallback remains owned by legacy consumer");
        var ally=new RemotePlayer{id=2};var outsider=new RemotePlayer{id=3};SocialClient.Party.Add(2);
        Check(AuthoritativeProfessionSkill.ValidateTarget(10306,ally)==null,"same party accepted");
        Check(AuthoritativeProfessionSkill.ValidateTarget(10306,outsider)!=null,"arbitrary friendly rejected");
        ally.IsDead=true;Check(AuthoritativeProfessionSkill.ValidateTarget(10306,ally)!=null,"living cleanse rejects dead");
        Check(AuthoritativeProfessionSkill.ValidateTarget(10305,ally)==null,"resurrection accepts dead party");
        ally.IsDead=false;Check(AuthoritativeProfessionSkill.ValidateTarget(10305,ally)!=null,"pure resurrect rejects living");
        Check(AuthoritativeProfessionSkill.ValidateTarget(10306,new MonsterEntity())!=null,"cleanse cannot target enemy");
        var selectedMonster=new MonsterEntity();TargetSelect.Current=selectedMonster;TargetSelect.CurrentSourceForIndependentRuntime=selectedMonster;
        Check(AuthoritativeProfessionSkill.TryFriendlyOrSelf(10306,out var selfTarget,out var selfError) && selfTarget==0 && selfError==null,"support hotkey keeps declared self fallback while monster selected");
        Check(!AuthoritativeProfessionSkill.TryFriendlyOrSelf(10305,out _,out _),"resurrect cannot silently self fallback");
        TargetSelect.Current=outsider;TargetSelect.CurrentSourceForIndependentRuntime=outsider;
        Check(AuthoritativeProfessionSkill.TryFriendlyOrSelf(10306,out _,out var outsiderError) && outsiderError!=null,"selected nonparty player never becomes self fallback");
        TargetSelect.Current=null;TargetSelect.CurrentSourceForIndependentRuntime=null;
        CharData.race=2;var theftState=State();Check(theftState.Apply(EndgameResponse.Parse(assassin)),"theft snapshot accepted");MP(10212,69,true);
        CharData.race=3;state=State();Check(state.Apply(EndgameResponse.Parse(healer)),"restore healer snapshot");
        var high=Only(healer,10306);var highRow=Obj(Array(high["skillRanks"])[0]);highRow["hardRank"]=20L;highRow["softRank"]="7";highRow["effectiveRank"]="27";highRow["nextEffectiveRank"]="28";highRow["canInvest"]=false;Obj(Obj(highRow["contract"])["current"])["mpCost"]=901L;
        Apply(state,high);Check(state.SkillRank(10306).EffectiveRank=="27","equipment ranks above20 retained");MP(10306,900,false);MP(10306,901,true);
        var highBehavior=Obj(Obj(highRow["contract"])["current"]);
        // Even high-rank efficiency retains a positive integer MP admission gate.
        highBehavior["mpCost"]=1L;Apply(state,high);
        MP(10306,0,false);MP(10306,1,true);
        // A newer inventory revision invalidates old typed cost before replacement arrives.
        state.Apply(EndgameResponse.Parse("{\"kind\":\"snapshot\",\"ok\":true,\"revision\":\"2\",\"snapshotId\":\"new-gear\",\"chunk\":0,\"chunks\":1}"));
        Check(AuthoritativeProfessionSkill.Current(10306)==null,"stale cost unavailable");
        AuthoritativeProfessionSkill.TryResolve(10306,out var staleAim,out _,out _,out _,out _,out _,out _);Check(staleAim==SkillTargeting.Aim.Unavailable,"stale target never guessed");
        // Missing current must never borrow expensive next for admission or claim readiness.
        var unlearned=Only(healer,10306);var row0=Obj(Array(unlearned["skillRanks"])[0]);row0["hardRank"]=0L;row0["effectiveRank"]="0";row0["nextEffectiveRank"]="1";row0["canCast"]=false;Obj(row0["contract"])["current"]=null;
        var fresh=State();Apply(fresh,unlearned);Check(AuthoritativeProfessionSkill.Current(10306)==null && !AuthoritativeProfessionSkill.LearnedActive(10306),"unlearned no next-cost borrow");
        fresh.Reset();Check(!AuthoritativeProfessionSkill.Uses(10306) && AuthoritativeProfessionSkill.Current(10306)==null,"reconnect resets provider and cached identity");
        var legacyGrowth=State(false);Check(legacyGrowth.Apply(EndgameResponse.Parse(healer)),"old capability compatible packet parse");MP(10306,60,true);MP(10307,50,true);
        EndgamePanel.GrowthState=null;MP(10306,60,false);MP(10307,50,false);
        for(int i=5;i<args.Length;i++) {
            string text=File.ReadAllText(args[i]);Check(System.Text.Encoding.UTF8.GetByteCount(text)<61440,"worst packet byte limit");
            var packet=EndgameResponse.Parse(text);var worst=State();Check(worst.Apply(packet),"worst real packet parsed and atomically applied");
            Check(worst.SkillRanks.Count<=31,"production one-treasure-grant bound");
            Console.WriteLine("Worst real packet: "+args[i]+" "+System.Text.Encoding.UTF8.GetByteCount(text)+" bytes / "+worst.SkillRanks.Count+" rows");
        }
        Console.WriteLine("Skill behavior actual Presentation acceptance: "+checks+" checks passed");
    }
}
