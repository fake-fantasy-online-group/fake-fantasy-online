using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class DialogUI
{
    private bool BuildTravelMenu()
    {
        if (NpcName != "传送仙子") return false;
        Greeting = "欢迎来到幻想世界";
        Add(new ReferenceNpcDialogOption("随机传送", () =>
        {
            var village = NoviceVillages[Random.Range(0, NoviceVillages.Length)];
            Visible = false;
            GameBootstrap.DoTeleport?.Invoke(village.map, village.x, village.y);
        }));
        Add(new ReferenceNpcDialogOption("桃源", () => ShowVillageMenu("桃源", 0, 1)));
        Add(new ReferenceNpcDialogOption("长乐", () => ShowVillageMenu("长乐", 2, 3)));
        return true;
    }

    private void ShowVillageMenu(string region, int eastIdx, int westIdx)
    {
        InSubView = true;
        Greeting = "要去" + region + "的哪个村子？";
        optLabels.Clear();
        optActs.Clear();
        var east = NoviceVillages[eastIdx];
        var west = NoviceVillages[westIdx];
        Add(new ReferenceNpcDialogOption(east.name, () =>
        {
            Visible = false;
            GameBootstrap.DoTeleport?.Invoke(east.map, east.x, east.y);
        }));
        Add(new ReferenceNpcDialogOption(west.name, () =>
        {
            Visible = false;
            GameBootstrap.DoTeleport?.Invoke(west.map, west.x, west.y);
        }));
        Add(new ReferenceNpcDialogOption("返回", BackToMenu));
    }

    private void BackToMenu()
    {
        Greeting = NpcChatter.GetLine(NpcChatter.RoleOf(null));
        if (string.IsNullOrEmpty(Greeting)) Greeting = "还有什么事吗？";
        BuildMenu();
    }

    private void AddDungeonEntryOptions()
    {
        foreach (DungeonEntrances.Dungeon dungeon in DungeonEntrances.For(NpcName))
        {
            Add(new ReferenceNpcDialogOption("进入" + dungeon.name, () =>
            {
                // Native Close (0600021f) only hides, retaining the NPC/subview state.
                Visible = false;
                if (SocialClient.Party.Count == 0)
                {
                    ChatUI.Add("系统", dungeon.name + "需要组队才能进入。",
                        new Color(1f, 0.47058824f, 0.47058824f, 1f));
                    return;
                }
                GameBootstrap.DoTeleport?.Invoke(dungeon.map, dungeon.x, dungeon.y);
            }));
        }
    }

    private void AddDungeonExitAndHardOptions()
    {
        if (NpcName.Contains("结界出口"))
            Add(new ReferenceNpcDialogOption("离开副本(回到入口)", () =>
            {
                Visible = false;
                SysMsgBox.Show("确定离开这个副本、回到入口?",
                    () => NetClient.I?.GmCmdForIndependentRuntime("trialleave"), null);
            }));
        foreach (var dungeon in HardDungeons)
        {
            if (NpcName.Contains(dungeon.npc))
            {
                // The native matching branch owns the name/map capture frame.
                int map = dungeon.map;
                string name = dungeon.name;
                System.Action confirm = null;
                Add(new ReferenceNpcDialogOption("挑战 " + name + "（困难）", () =>
                {
                    Visible = false;
                    string prompt = "进入【" + name + "·困难】？\n怪物更强、经验更多，需要组队。";
                    if (confirm == null)
                        confirm = () => NetClient.I?.GmCmdForIndependentRuntime(string.Format("dungeonhard {0}", map));
                    SysMsgBox.Show(prompt, confirm, null);
                }));
            }
        }
    }
}
}
