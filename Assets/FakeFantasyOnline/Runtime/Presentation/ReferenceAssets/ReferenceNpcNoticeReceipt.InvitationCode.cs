using System;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal static partial class ReferenceNpcNoticeReceipt
{
    // Dispatch 0600077f: arm64 2529d8/25a6bc/25aab4..25ac88; x86_64
    // 261b40/26901b/26941f..2695ce. Failure tails to DialogUI.ShowSay only when visible.
    internal static void ReceiveInvitationCodeResult(WorldInvitationCodeResult result,
        Func<DialogUI> currentDialog)
    {
        if (result.Result != 0 && result.Code.Length > 0)
        {
            string lifetime;
            if (result.LifetimeSeconds <= 0)
                lifetime = "长期有效";
            else if (result.LifetimeSeconds < 60)
                lifetime = string.Format("{0} 秒后失效", result.LifetimeSeconds);
            else
                lifetime = string.Format("{0} 分 {1} 秒后失效",
                    result.LifetimeSeconds / 60, result.LifetimeSeconds % 60);

            string body = string.Concat(new[]
            {
                result.Code,
                "\n\n有效期: ",
                lifetime,
                result.RemainingAccounts > 0
                    ? string.Format("    还能注册 {0} 个账号", result.RemainingAccounts)
                    : string.Empty,
                "\n把上面这串字符发给朋友, 让他在【注册】页面的「邀请码」一栏填进去。\n选中后按 Ctrl+C(Mac 用 ⌘+C)即可复制。过期即作废。"
            });
            if (!string.IsNullOrEmpty(result.Message))
                body = string.Concat(body, "\n", result.Message);
            ChatCopyOverlay.OpenAtReceiptForIndependentRuntime(body, "你的邀请码");
            ChatUI.Add("系统", string.Concat("邀请码: ", result.Code),
                new Color(120f / 255f, 1f, 120f / 255f, 1f), string.Empty, null);
        }
        else if (!string.IsNullOrEmpty(result.Message))
        {
            DialogUI dialog = currentDialog();
            if (dialog != null && dialog.Visible)
                dialog.ShowSay(result.Message);
        }
    }
}

}
