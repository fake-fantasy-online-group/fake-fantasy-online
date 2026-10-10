using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ChatUI
{
    // Original instance slots +0x78 / +0x80; initialized with the other constructor fields.
    internal string InputTextForIndependentRuntime { get => inputText; set => inputText = value; }
    internal string ChannelForIndependentRuntime { get => channel; set => channel = value; }

    // 060000f7, complete live-field body. Frozen ARM 0x23760c / x86_64 0x2455e0.
    // The existing TakeOutgoing owner is the FlushShares delegation; neither the callback nor
    // its outgoing-root reset is reproduced here. No captured intent or typed send decision.
    public void SendNow()
    {
        string text = inputText.Trim();
        if (text.Length == 0)
        {
            inputText = string.Empty;
            return;
        }

        text = EmoteCodes.Expand(text);
        if (text == "/pos" || text == "/坐标" || text == "/where")
        {
            if (PlayerController.I != null)
            {
                // Re-read after Unity's alive check, then FacingOct before either coordinate.
                PlayerController local = PlayerController.I;
                int facingOct = local.FacingOct();
                Add("系统", string.Format(
                    "当前坐标  x={0}  y={1}   朝向 dir={2}({3})   [摆NPC: 后台填 x/y/dir 即可]",
                    (int)local.worldX, (int)local.worldY, facingOct,
                    PlayerController.OctToName(facingOct)),
                    new Color(120f / 255f, 230f / 255f, 120f / 255f, 1f));
                inputText = string.Empty;
                return;
            }
        }

        if (text == "/队伍" || text == "/team" || text == "/teamdiag")
        {
            Add("系统", DescribeLocalParty(),
                new Color(120f / 255f, 230f / 255f, 120f / 255f, 1f));
            Add("系统", "提示: 只有你自己一个人的队伍, 左边本来就是空的(面板只画队友, 不画自己)。",
                new Color(180f / 255f, 180f / 255f, 180f / 255f, 1f));
            inputText = string.Empty;
            return;
        }

        // The original one-string StartsWith overload and Unity singleton gate. GmCmd has
        // no OnSend invocation or share flush. Exceptions precede the execution row/clear.
        if (text.StartsWith("/") && NetClient.I != null)
        {
            NetClient.I.GmCmdForIndependentRuntime(text.Substring(1));
            Add("系统", "执行: " + text,
                new Color(180f / 255f, 180f / 255f, 180f / 255f, 1f));
            inputText = string.Empty;
            return;
        }

        if (!string.IsNullOrEmpty(whisperTarget))
        {
            TakeOutgoing(text, pendingRefs);
            OnSend?.Invoke("密语:" + whisperTarget, text);
            // Both target reads belong after the callback, not to its routing snapshot.
            Add("密语", "对 " + whisperTarget + " 说: " + text,
                new Color(1f, 150f / 255f, 220f / 255f, 1f), whisperTarget);
        }
        else
        {
            TakeOutgoing(text, pendingRefs);
            OnSend?.Invoke(channel, text);
            // The callback may change channel. Each comparison and the row label read the
            // current field; suppression is unrelated to whether the mapper sent a request.
            if (channel != "附近" && channel != "队伍" && channel != "家族" && channel != "世界")
            {
                Add(channel, (string.IsNullOrEmpty(CharData.name) ? "我" : CharData.name) + ": " + text,
                    Color.white);
            }
        }

        inputText = string.Empty;
    }

    // Original f7's local party arm. Keep list re-reads, self predicate, append order and the
    // six ordered fragment evaluations; PartyName is read only after the first four fragments.
    private static string DescribeLocalParty()
    {
        int count = SocialClient.Party.Count;
        StringBuilder roster = new StringBuilder();
        int selfCount = 0;
        for (int index = 0; index < count; index++)
        {
            bool self = SocialClient.IsSelf(index);
            if (roster.Length > 0)
            {
                roster.Append(',');
            }

            roster.Append(SocialClient.Party[index].Item2);
            selfCount += self ? 1 : 0;
            roster.Append(self ? "(我)" : string.Empty);
        }

        return string.Concat(new string[]
        {
            string.Format("队伍诊断: 名单={0} 可见队友={1} 被判为我={2}",
                count, count - selfCount, selfCount),
            string.Format(" | 我的charId={0} 我的实体id={1}", CharData.charId, NetClient.SelfEntityId),
            string.Format(" | charId段={0}个 队长id={1}", SocialClient.PartyCharIds.Count,
                SocialClient.PartyLeaderId),
            " | 队名=",
            string.IsNullOrEmpty(SocialClient.PartyName) ? "(无)" : SocialClient.PartyName,
            string.Format(" | 成员=[{0}]   ★把这一行发给GM", roster)
        });
    }
}
}
