using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ChatUI
{
    // 060000db. Each pending-list access reads the original static root independently;
    // all instance/input/notice ordering remains in the already-recovered body.
    public static bool InsertShare(byte kind, byte tab, int slot, string name)
    {
        if (I == null) return false;

        if (pendingRefs.Count >= 4)
        {
            Add("系统", "一句话最多分享4件", new Color(1f, 180f / 255f, 120f / 255f, 1f));
            return false;
        }

        ChatUI inputOwner = I;
        if (unchecked(inputOwner.inputText.Length + name.Length + 2) >= 121)
        {
            Add("系统", "输入框里字太多了, 先发出去再分享",
                new Color(1f, 180f / 255f, 120f / 255f, 1f));
            return false;
        }

        inputOwner.inputText = string.Concat(inputOwner.inputText, "【", name, "】");
        pendingRefs.Add((kind, tab, slot, name));
        I.panelsVisible = true;
        _pendingFocus = true;
        Add("系统", "已插入【" + name + "】—— 发出后对方鼠标悬停可看详情",
            new Color(150f / 255f, 215f / 255f, 1f, 1f));
        return true;
    }
}
}
