using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class TeamDlg
{
    private void DrawParty(Vector2 pointer, float top)
    {
        for (int i = 0; i < SocialClient.Party.Count; i++)
        {
            var member = SocialClient.Party[i];
            float y = top + i * 26;
            Rect row = L(18, y, 142, 25);
            Highlight(row, selParty == i, pointer);
            bool leader = member.Item1 == SocialClient.PartyLeaderId;
            Texture2D head = TeamBar.Head(member.Item6, member.Item7, member.Item8);
            if (head != null) GUI.DrawTexture(L(20, y, 24, 24), head);
            if (BitmapFont.Ready)
            {
                string suffix = string.Format(" Lv{0}", member.Item3) + (leader ? "  [队长]" : string.Empty);
                BitmapFont.DrawOutline(winX + 48 * s, winY + (y + 2) * s, Elide(member.Item2, suffix, 108), leader ? GOLD : new Color(.95f, .95f, .92f, 1), s);
            }
            if (member.Item5 > 0)
            {
                Rect healthBar = L(48, y + 15, 108, 4);
                GUI.color = new Color(0, 0, 0, .5f);
                GUI.DrawTexture(healthBar, Texture2D.whiteTexture);
                GUI.color = new Color(.4f, .8f, .3f, 1);
                float health = Mathf.Clamp01((float)member.Item4 / member.Item5);
                GUI.DrawTexture(new Rect(healthBar.x, healthBar.y, healthBar.width * health, healthBar.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            bool self = SocialClient.IsSelf(i);
            if (Click(row, pointer))
            {
                selParty = i;
                if (self) SocialClient.SelectedMemberId = 0;
                else
                {
                    SocialClient.SelectedMemberId = member.Item1;
                    if (runtime == null || !runtime.SelectPlayer(member.Item1)) TargetSelect.Clear();
                }
            }
            if (!self && RClick(row, pointer)) PlayerActionMenu.Open(member.Item1, member.Item2);
        }
    }
    private int ScrollRows(Rect content, int count, float height, ref int scroll, Vector2 pointer)
    {
        int rows = Mathf.Max(1, (int)((height - 4) / 16));
        int max = Mathf.Max(0, count - rows);
        if (Event.current.type == EventType.ScrollWheel && content.Contains(pointer))
        { scroll = Mathf.Clamp(scroll + (int)Mathf.Sign(Event.current.delta.y), 0, max); Event.current.Use(); }
        scroll = Mathf.Clamp(scroll, 0, max);
        return rows;
    }
    private void DrawFriends(Vector2 pointer, Rect content, float top, float height)
    {
        int count = SocialClient.Friends.Count;
        if (count == 0)
        { Center(new Rect(content.x, content.center.y - 7 * s, content.width, 14 * s), "暂无好友", GRAY); return; }
        int rows = ScrollRows(content, count, height, ref friendScroll, pointer);
        float width = count > rows ? 126 : 142;
        for (int i = 0; i < rows && i + friendScroll < count && top + 2 + (i + 1) * 16 <= top + height; i++)
        {
            int index = i + friendScroll;
            var friend = SocialClient.Friends[index];
            float y = top + 2 + i * 16;
            Rect row = L(18, y, width, 15);
            Highlight(row, index == selFriend, pointer);
            if (BitmapFont.Ready) BitmapFont.DrawOutline(winX + 22 * s, winY + (y + 1) * s, Elide(friend.Item1, friend.Item2 ? string.Empty : " (离线)", width - 8), friend.Item2 ? new Color(.45f, .95f, .4f, 1) : GRAY, s);
            if (Click(row, pointer)) selFriend = index;
            if (RClick(row, pointer))
            { selFriend = index; PlayerActionMenu.Open(CombatWorld.FindPlayerIdByName(friend.Item1), friend.Item1); }
        }
        if (count > rows) NativeScroll.BarRows(this, L(148, top + 2, 14, height - 4), rows, count, ref friendScroll);
    }
    private void DrawBlocks(Vector2 pointer, Rect content, float top, float height)
    {
        int count = SocialClient.Blocks.Count;
        if (count == 0)
        { Center(new Rect(content.x, content.center.y - 7 * s, content.width, 14 * s), "屏蔽名单为空", GRAY); return; }
        int rows = ScrollRows(content, count, height, ref blockScroll, pointer);
        float width = count > rows ? 126 : 142;
        for (int i = 0; i < rows && i + blockScroll < count && top + 2 + (i + 1) * 16 <= top + height; i++)
        {
            int index = i + blockScroll;
            var blocked = SocialClient.Blocks[index];
            float y = top + 2 + i * 16;
            Rect row = L(18, y, width, 15);
            Highlight(row, index == selBlock, pointer);
            if (BitmapFont.Ready) BitmapFont.DrawOutline(winX + 22 * s, winY + (y + 1) * s, Elide(blocked.Item2 + (blocked.Item3 ? "(全部角色)" : string.Empty), string.Empty, width - 8), GRAY, s);
            if (Click(row, pointer)) selBlock = index;
        }
        if (count > rows) NativeScroll.BarRows(this, L(148, top + 2, 14, height - 4), rows, count, ref blockScroll);
    }
    private void DrawMaster(Vector2 pointer)
    {
        Rect master = L(16, 62, 146, 20), prentice = L(16, 246, 146, 20), content = L(16, 82, 146, 164);
        Texture2D masterTitle = TA("res/uires/teamdlg/mastertitle.gsa", 0);
        Texture2D prenticeTitle = TA("res/uires/teamdlg/prenticetitle.gsa", 0);
        if (masterTitle != null)
        { GUI.color = masterSub == 0 ? Color.white : new Color(.6f, .6f, .62f, .9f); GUI.DrawTexture(master, masterTitle); GUI.color = Color.white; }
        if (prenticeTitle != null)
        { GUI.color = masterSub == 1 ? Color.white : new Color(.6f, .6f, .62f, .9f); GUI.DrawTexture(prentice, prenticeTitle); GUI.color = Color.white; }
        Fill(content, new Color(0, 0, 0, .22f));
        var rows = new List<(string text, bool online, string name)>();
        if (masterSub == 0)
        {
            if (!string.IsNullOrEmpty(SocialClient.MasterName))
                rows.Add((string.Format("师父 {0} Lv{1}", SocialClient.MasterName, SocialClient.MasterLevel), SocialClient.MasterOnline, SocialClient.MasterName));
            foreach (var peer in SocialClient.Peers)
                rows.Add((string.Format("同门 {0} Lv{1}", peer.Item1, peer.Item2), peer.Item3, peer.Item1));
        }
        else foreach (var apprentice in SocialClient.Apprentices)
            rows.Add((string.Format("徒弟 {0} Lv{1}", apprentice.Item1, apprentice.Item2), apprentice.Item3, apprentice.Item1));
        if (rows.Count == 0)
            Center(new Rect(content.x, content.center.y - 7 * s, content.width, 14 * s), masterSub == 0 ? "暂无师门" : "暂无本门弟子", GRAY);
        for (int i = 0; i < rows.Count && i < 10; i++)
        {
            var item = rows[i];
            Rect row = L(16, 83 + i * 16, 146, 16);
            if (row.Contains(pointer)) Fill(row, new Color(1, 1, 1, .1f));
            Text(20, 84 + i * 16, item.text, item.online ? new Color(150f / 255, 240f / 255, 150f / 255, 1) : new Color(150f / 255, 150f / 255, 150f / 255, 1));
            if (RClick(row, pointer)) PlayerActionMenu.Open(CombatWorld.FindPlayerIdByName(item.name), item.name);
        }
        if (Click(master, pointer)) masterSub = 0;
        else if (Click(prentice, pointer)) masterSub = 1;
        else if (Click(content, pointer)) MasterInfoDlg.ToggleForIndependentRuntime();
    }
    private void DrawActions(Vector2 pointer)
    {
        (string, float, float)[] actions = TABS[tab].actions;
        if (tab == 1)
        {
            if (SocialClient.Party.Count == 0) actions = new[] { ("createbtn", 2f, 26f) };
            else if (!SocialClient.IsLeaderMe) actions = new[] { ("followbtn", 2f, 2f), ("leavebtn", 60f, 26f) };
            else actions = new[] { ("dismissbtn", 2f, 26f) };
        }
        foreach (var action in actions)
        {
            string name = action.Item1;
            if (!BtnImg(L(48 + action.Item2, 318 + action.Item3, 47, 18), TA("res/uires/common/" + name + ".gsa", 0), name, pointer)) continue;
            if (name == "leavebtn")
            {
                if (NetClient.I != null) SysMsgBox.Show("确定要离开队伍吗？", () => NetClient.I.PartyLeaveForIndependentRuntime());
            }
            else if (name == "dismissbtn")
            {
                if (NetClient.I != null) SysMsgBox.Show("确定要解散队伍吗？", () => NetClient.I.PartyDismissForIndependentRuntime());
            }
            else if (name == "createbtn")
            {
                string localName = runtime?.LocalName();
                runtime?.OpenTeamConfig((string.IsNullOrEmpty(localName) ? "我" : localName) + "的小队", 0);
            }
            else if (name == "followbtn")
            {
                if (SocialClient.PartyLeaderId != 0 && !SocialClient.IsLeaderMe && PlayerController.I != null)
                    PlayerController.I.FollowMate(SocialClient.PartyLeaderId);
            }
            if (tab == 0 && selFriend >= 0 && selFriend < SocialClient.Friends.Count)
            {
                string friend = SocialClient.Friends[selFriend].Item1;
                if (name == "chatbtn") runtime?.OpenGossip(friend);
                if (name == "delbtn" && NetClient.I != null)
                    SysMsgBox.Show("确定删除好友【" + friend + "】吗？", () =>
                    {
                        if (ReferenceEquals(NetClient.I, null)) throw new System.NullReferenceException();
                        _ = runtime.FriendCommands.DeleteAsync(friend, CancellationToken.None);
                        selFriend = -1;
                    });
            }
            if (tab == 4 && selBlock >= 0 && selBlock < SocialClient.Blocks.Count && name == "delbtn" && NetClient.I != null)
            {
                var blocked = SocialClient.Blocks[selBlock];
                SysMsgBox.Show("解除对【" + blocked.Item2 + "】的屏蔽？\n解除后又能收到他的聊天和各种请求。", () =>
                {
                    if (ReferenceEquals(NetClient.I, null)) throw new System.NullReferenceException();
                    _ = runtime.BlockCommands.DeleteAsync(blocked.Item1, CancellationToken.None);
                    selBlock = -1;
                });
            }
        }
    }
}
}
