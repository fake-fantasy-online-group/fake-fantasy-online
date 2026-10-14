using System;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public static class MobileClientPresentation { public static bool Enabled; }
    public static class GameHud { public static bool OverUI; }
    public static class CombatWorld
    {
        public static int MonsterAttacks,PvpAttacks;
        public static void SetTarget(MonsterEntity value,bool attack) { MonsterAttacks++; }
        public static void SetPvpTarget(RemotePlayer value) { PvpAttacks++; }
    }
    public static class TargetSelect { public static int Selects; public static void Select(object value) { Selects++; } }
    public partial class MonsterEntity { public bool friendly,ally; public void Click()=>OnMouseDown(); }
    public partial class RemotePlayer { public bool HostileToLocal; public void Click()=>OnMouseDown(); }
    internal partial class GroundItemEntity { public int id=9,gx=2,gy=3; public void Click()=>OnMouseDown(); }
    public class PlayerController
    {
        public static PlayerController I=new PlayerController();
        public int Pickups;
        public void WalkToPickup(int id,int x,int y) { Pickups++; }
    }
    public class NetClient
    {
        public static NetClient I=new NetClient();
        public int Pickups;
        public void PickItemForIndependentRuntime(int id) { Pickups++; }
    }
}
class WorldClickContract
{
    static int checks;
    static void Check(bool value,string name) { if(!value)throw new Exception(name);checks++; }
    static int Main()
    {
        var mob=new FakeFantasyOnline.Presentation.ReferenceAssets.MonsterEntity();
        var remote=new FakeFantasyOnline.Presentation.ReferenceAssets.RemotePlayer();
        var item=new FakeFantasyOnline.Presentation.ReferenceAssets.GroundItemEntity();
        mob.Click();Check(FakeFantasyOnline.Presentation.ReferenceAssets.CombatWorld.MonsterAttacks==1,"desktop monster attack");
        mob.friendly=true;mob.Click();Check(FakeFantasyOnline.Presentation.ReferenceAssets.TargetSelect.Selects==1,"desktop friendly select");
        mob.friendly=false;mob.ally=true;mob.Click();Check(FakeFantasyOnline.Presentation.ReferenceAssets.TargetSelect.Selects==2,"desktop ally select");
        remote.Click();Check(FakeFantasyOnline.Presentation.ReferenceAssets.TargetSelect.Selects==3,"desktop remote select");
        remote.HostileToLocal=true;remote.Click();Check(FakeFantasyOnline.Presentation.ReferenceAssets.CombatWorld.PvpAttacks==1,"desktop hostile attack");
        item.Click();Check(FakeFantasyOnline.Presentation.ReferenceAssets.PlayerController.I.Pickups==1,"desktop pickup navigation");
        FakeFantasyOnline.Presentation.ReferenceAssets.PlayerController.I=null;item.Click();
        Check(FakeFantasyOnline.Presentation.ReferenceAssets.NetClient.I.Pickups==1,"desktop no-player pickup fallback");
        FakeFantasyOnline.Presentation.ReferenceAssets.GameHud.OverUI=true;
        mob.Click();remote.Click();item.Click();
        Check(FakeFantasyOnline.Presentation.ReferenceAssets.CombatWorld.PvpAttacks==1&&FakeFantasyOnline.Presentation.ReferenceAssets.TargetSelect.Selects==3&&FakeFantasyOnline.Presentation.ReferenceAssets.NetClient.I.Pickups==1,"desktop UI blocks callbacks");
        FakeFantasyOnline.Presentation.ReferenceAssets.GameHud.OverUI=false;
        FakeFantasyOnline.Presentation.ReferenceAssets.MobileClientPresentation.Enabled=true;
        mob.ally=false;mob.Click();remote.Click();item.Click();
        Check(FakeFantasyOnline.Presentation.ReferenceAssets.CombatWorld.MonsterAttacks==1&&FakeFantasyOnline.Presentation.ReferenceAssets.CombatWorld.PvpAttacks==1&&FakeFantasyOnline.Presentation.ReferenceAssets.NetClient.I.Pickups==1,"mobile synthetic mouse never bypasses touch ownership");
        Console.WriteLine("PASS desktop/mobile world click adapters: "+checks+" checks");return 0;
    }
}
