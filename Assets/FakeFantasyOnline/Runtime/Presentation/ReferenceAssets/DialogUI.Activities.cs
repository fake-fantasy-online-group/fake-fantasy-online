using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class DialogUI
{
    // Official DialogUI.BuildMenu: these rows follow appearance services, before trials.
    private void AddActivityOptions()
    {
        if (NpcName.Contains("小丫助手"))
            AddActivityCommand("获取邀请码", "invite");
        if (NpcName.Contains("王小丫") && !NpcName.Contains("小丫助手"))
        {
            AddActivityCommand("进入/退出 PK战场", "pk", true);
            AddActivityCommand("吊村大乱斗 3人场(40~60级)", "arena3", true);
            AddActivityCommand("吊村大乱斗 5人场(60级以上)", "arena5", true);
        }
        if (NpcName.Contains("空投宝箱"))
        {
            Greeting = "一个从天而降的箱子，看起来能打开。";
            AddActivityCommand("打开宝箱", "openbox");
        }
    }

    // 0600023c-0600023e use Unity null semantics, re-read I and send before hiding.
    // Other command rows hide first and use the native CLR null-conditional receiver.
    private void AddActivityCommand(string label, string command, bool sendBeforeHide = false)
    {
        Add(new ReferenceNpcDialogOption(label, () =>
        {
            if (sendBeforeHide)
            {
                if (NetClient.I != null) NetClient.I.GmCmdForIndependentRuntime(command);
                Visible = false;
            }
            else
            {
                Visible = false;
                NetClient.I?.GmCmdForIndependentRuntime(command);
            }
        }));
    }

    // Official DialogUI.BuildMenu: these rows follow hard-dungeon entries, before task offers.
    private void AddTianshenOptions()
    {
        if (NpcName.Contains("天神考验使者"))
        {
            Greeting = "天神设下考验：守住天神守护灵，击退一波波天魔。你可要接下这份差事？";
            Add(new ReferenceNpcDialogOption("挑战天神考验（普通·50关）", () =>
            {
                Visible = false;
                TianshenEnter(1);
            }));
            Add(new ReferenceNpcDialogOption("挑战天神考验（困难·100关）", () =>
            {
                Visible = false;
                TianshenEnter(2);
            }));
            Add(new ReferenceNpcDialogOption("了解规则", ShowTianshenRules));
        }
        if (NpcName.Contains("天神考验将军"))
        {
            Greeting = "护卫听令！要召多少兵，全看你手上的召唤令。";
            AddActivityCommand("召唤守城护卫（肉盾型）", "tssummon tank");
            AddActivityCommand("召唤守城护法（强攻型）", "tssummon mage");
            AddActivityCommand("召唤守城仙子（恢复型）", "tssummon healer");
            AddActivityCommand("训练守城护卫（升下一档）", "tsupgrade tank");
            AddActivityCommand("训练守城护法（升下一档）", "tsupgrade mage");
            AddActivityCommand("训练守城仙子（升下一档）", "tsupgrade healer");
            AddActivityCommand("擂鼓催阵（提前召唤下一批怪物）", "tscall");
            AddActivityCommand("查看战况（关数/召唤令/守护灵）", "tsinfo");
            Add(new ReferenceNpcDialogOption("退出战场（判挑战失败）", () =>
            {
                Visible = false;
                SysMsgBox.Show("确定退出天神考验战场?\n退出即【判挑战失败】, 按已通过的关数结算奖励, 本次不能再回来。",
                    () => NetClient.I?.GmCmdForIndependentRuntime("tsleave"), null);
            }));
        }
    }

    private static void TianshenEnter(int difficulty)
    {
        if (SocialClient.Party.Count == 0)
        {
            ChatUI.Add("系统", "挑战天神考验需要先创建队伍(一个人也要建队, 进入后不能再邀人)。",
                new Color(1f, 0.47058824f, 0.47058824f, 1f));
            return;
        }
        NetClient.I?.GmCmdForIndependentRuntime("tianshen " + difficulty.ToString());
    }

    private void ShowTianshenRules()
    {
        Visible = false;
        Color color = new Color(0.70588237f, 0.8627451f, 1f, 1f);
        ChatUI.Add("系统", "【天神大考验】每天可挑战1次(用天神考验卷轴可加3次)，须先创建队伍(单人队)。", color);
        ChatUI.Add("系统", "进场获得15个护卫召唤令，在场内【天神考验将军】处召唤/训练护卫(最多8个)，每通过一关再得召唤令。", color);
        ChatUI.Add("系统", "守护灵被破坏、超时、场上怪物过多、或退出战场(退队/掉线)都算挑战失败，按已通过关数结算奖励。", color);
        ChatUI.Add("系统", "副本内死亡不掉经验金钱、替身娃娃无效，可自动复活2次，第3次阵亡送回龙城。", color);
    }
}
}
