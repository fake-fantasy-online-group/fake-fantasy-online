using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Exact action ids emitted by the locked 1.5.8 PlayerActionMenu.Fire switch.
/// </summary>
public enum ReferencePlayerActionMenuAction
{
    None = -1,
    Trade = 0,
    FamilyInvite = 2,
    Whisper = 3,
    Gossip = 4,
    PartyInvite = 10,
    PartyKick = 12,
    PartyLeader = 13,
    FollowLeader = 14,
    Apprentice = 20,
    Mentor = 21,
    ApprenticeDissolve = 31,
    FriendAdd = 22,
    FriendDelete = 23,
    PartyCreate = 24,
    FollowPlayer = 25,
    PartyJoin = 26,
    BrowseStall = 30,
    Inspect = 40,
    RideTogether = 41,
    DismountTogether = 42,
    RideInvite = 43,
    DuelArena = 44,
    BlockRemove = 27,
    BlockChat = 28,
    BlockParty = 29,
    CopyLine = 50,
    CopyBlock = 51,
    SelectCopy = 52
}

public sealed class ReferencePlayerActionMenuItem
{
    public ReferencePlayerActionMenuItem(
        string label,
        ReferencePlayerActionMenuAction action)
    {
        Label = label ?? string.Empty;
        Action = action;
    }

    public string Label { get; }

    public ReferencePlayerActionMenuAction Action { get; }
}

/// <summary>
/// Immutable projection of the global owners read by PlayerActionMenu.Build.
/// </summary>
/// <remarks>
/// Keeping the global reads outside the renderer makes the recovered menu deterministic and
/// prevents it from becoming a service locator. Missing social owners keep their original
/// zero-initialized state; action execution remains outside this visible-surface boundary.
/// </remarks>
public sealed class ReferencePlayerActionMenuSnapshot
{
    public ReferencePlayerActionMenuSnapshot(
        int targetEntityId,
        string targetName,
        int localEntityId,
        bool targetIsStallOwner,
        bool targetIsPartyMember,
        int partyMemberCount,
        int partyLeaderEntityId,
        bool targetIsFriend,
        int rideTogetherTargetEntityId,
        bool localCanRideTogether,
        bool targetCanCarryPassenger,
        bool canInviteFamily,
        string copyLine,
        string copyBlock,
        int localRideSeats = 0,
        bool targetIsBlocked = false)
    {
        TargetEntityId = targetEntityId;
        TargetName = targetName ?? string.Empty;
        LocalEntityId = localEntityId;
        TargetIsStallOwner = targetIsStallOwner;
        TargetIsPartyMember = targetIsPartyMember;
        PartyMemberCount = Math.Max(0, partyMemberCount);
        PartyLeaderEntityId = partyLeaderEntityId;
        TargetIsFriend = targetIsFriend;
        RideTogetherTargetEntityId = rideTogetherTargetEntityId;
        LocalCanRideTogether = localCanRideTogether;
        TargetCanCarryPassenger = targetCanCarryPassenger;
        CanInviteFamily = canInviteFamily;
        CopyLine = ReferenceFirstWorldPlayerActionMenu.Plain(copyLine);
        CopyBlock = ReferenceFirstWorldPlayerActionMenu.Plain(copyBlock);
        LocalRideSeats = Math.Max(0, localRideSeats);
        TargetIsBlocked = targetIsBlocked;
    }

    public int TargetEntityId { get; }
    public string TargetName { get; }
    public int LocalEntityId { get; }
    public bool TargetIsStallOwner { get; }
    public bool TargetIsPartyMember { get; }
    public int PartyMemberCount { get; }
    public int PartyLeaderEntityId { get; }
    public bool TargetIsFriend { get; }
    public int RideTogetherTargetEntityId { get; }
    public bool LocalCanRideTogether { get; }
    public bool TargetCanCarryPassenger { get; }
    public bool CanInviteFamily { get; }
    public string CopyLine { get; }
    public string CopyBlock { get; }
    public int LocalRideSeats { get; }
    public bool TargetIsBlocked { get; }
    public bool LocalCanInvitePassenger => LocalRideSeats > 1;
}

/// <summary>
/// Source translation of the official PlayerActionMenu owner.
/// </summary>
/// <remarks>
/// This class owns only rendering, pointer capture and destination intents. It never calls the
/// network directly: each action must later be adapted through Application and server authority.
/// </remarks>
public sealed class ReferenceFirstWorldPlayerActionMenu : IDisposable
{
    public const string RowBackgroundPath = "res/uires/gameui/menubtn.gsa";
    public const string DownLeftPath = "res/uires/patch/patchdownleft.gsa";
    public const string DownMiddlePath = "res/uires/patch/patchdownmiddle.gsa";
    public const string DownRightPath = "res/uires/patch/patchdownright.gsa";
    public const string LeftMiddlePath = "res/uires/patch/patchleftmiddle.gsa";
    public const string RightMiddlePath = "res/uires/patch/patchrightmiddle.gsa";
    public const string UpLeftPath = "res/uires/patch/patchupleft.gsa";
    public const string UpMiddlePath = "res/uires/patch/patchupmiddle.gsa";
    public const string UpRightPath = "res/uires/patch/patchupright.gsa";

    public const float Width = 98f;
    public const float RowHeight = 22f;
    public const float ButtonWidth = 90f;
    public const float Corner = 11f;
    public const float Edge = 3f;
    public const float TopPadding = 3f;
    public const float BottomPadding = 2f;

    private bool disposed;
    private readonly PlayerActionMenu nativeMenu;

    // Projection of the last snapshot handed to Open plus the anchor it carried. These exist so the
    // Editor audit can still pin the official placement maths and the row table; nothing here is
    // rendered and nothing here decides whether the menu is on screen - the singleton does that.
    private readonly List<ReferencePlayerActionMenuItem> items =
        new List<ReferencePlayerActionMenuItem>();
    private Vector2 anchor;
    private int targetEntityId;
    private string targetName = string.Empty;

    // The official PlayerActionMenu singleton is the only menu in the product now. This type keeps
    // the recovered surface the audits still use - the action enum, the snapshot, BuildItems and
    // Plain - and delegates anything that used to render or hold menu state to the singleton, so
    // there is no second renderer, no second visible flag and no second Fire dispatch path.
    private ReferenceFirstWorldPlayerActionMenu(PlayerActionMenu nativeMenu)
    {
        this.nativeMenu = nativeMenu;
    }

    public bool Ready => !disposed && PlayerActionMenu.I != null;

    public bool Visible => !disposed && PlayerActionMenu.Visible;

    public IReadOnlyList<ReferencePlayerActionMenuItem> Items => items;

    public int TargetEntityId => targetEntityId;

    public string TargetName => targetName;

    internal static void FireBrowseStall(int targetId)
    {
        NetClient client = NetClient.I;
        if (client == null) return;
        client.StallBrowseForIndependentRuntime(targetId);
    }

    public ReferencePlayerActionMenuAction LastAction { get; private set; } =
        ReferencePlayerActionMenuAction.None;

    public int LastActionTargetEntityId { get; private set; }

    public string LastActionTargetName { get; private set; } = string.Empty;

    public string LastActionCopyLine { get; private set; } = string.Empty;

    public string LastActionCopyBlock { get; private set; } = string.Empty;

    /// <summary>
    /// Kept so the owner's existing readiness gate keeps working. It no longer loads the nine GSA
    /// files: the singleton owns them, and loading them twice would be a second resource path.
    /// </summary>
    public static bool TryCreate(
        ReferenceResourceLoader resources,
        out ReferenceFirstWorldPlayerActionMenu owner)
    {
        owner = null;
        if (resources == null) return false;
        PlayerActionMenu.Ensure();
        if (PlayerActionMenu.I == null) return false;
        owner = new ReferenceFirstWorldPlayerActionMenu(PlayerActionMenu.I);
        return true;
    }

    /// <summary>
    /// Forwards to the singleton, which anchors on the pointer and decides for itself whether the
    /// menu is worth showing (an empty item list keeps it hidden).
    /// </summary>
    public bool Open(ReferencePlayerActionMenuSnapshot snapshot, Vector2 topOriginAnchor)
    {
        ThrowIfDisposed();
        if (snapshot == null) return false;
        ResetLastAction();
        targetEntityId = snapshot.TargetEntityId;
        targetName = snapshot.TargetName ?? string.Empty;
        anchor = topOriginAnchor;
        items.Clear();
        BuildItems(snapshot, items);
        PlayerActionMenu.Open(
            snapshot.TargetEntityId, snapshot.TargetName, snapshot.CopyLine, snapshot.CopyBlock);
        // Same contract as the implementation this replaced: Open itself succeeded. An empty row
        // table only means the singleton keeps itself hidden - the official decides that, and the
        // caller must not treat "nothing to show" as a failed frame.
        return true;
    }

    public void Hide()
    {
        PlayerActionMenu.Hide();
    }

    /// <summary>
    /// Observes the native owner. Unity alone invokes its GUI body.
    /// </summary>
    public bool Draw(float scale)
    {
        ThrowIfDisposed();
        ResetLastAction();
        return Ready;
    }

    public Rect Box(int viewportWidth, int viewportHeight, float scale)
    {
        if (viewportWidth < 1 || viewportHeight < 1 || !(scale > 0f) ||
            !IsFinite(scale))
        {
            return new Rect();
        }

        float width = Width * scale;
        float height = (items.Count * RowHeight + TopPadding + BottomPadding) * scale;
        float left = anchor.x;
        float top = anchor.y;
        if (left + width > viewportWidth)
        {
            left = viewportWidth - width;
        }

        if (top + height > viewportHeight)
        {
            top = viewportHeight - height;
        }

        return new Rect(Mathf.Max(0f, left), Mathf.Max(0f, top), width, height);
    }


    public bool MouseOver(Vector2 topOriginPointer, int viewportWidth, int viewportHeight, float scale)
    {
        // FamilyDetailDlg also opens the singleton directly, without updating this adapter's
        // diagnostic snapshot. Hit-test the very box drawn by the singleton in every entry path.
        return !disposed && PlayerActionMenu.ContainsPointer(topOriginPointer);
    }

    private static bool ContainsInclusive(Rect rect, Vector2 point)
    {
        return point.x >= rect.xMin && point.x <= rect.xMax &&
               point.y >= rect.yMin && point.y <= rect.yMax;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private void ResetLastAction()
    {
        LastAction = ReferencePlayerActionMenuAction.None;
        LastActionTargetEntityId = 0;
        LastActionTargetName = string.Empty;
        LastActionCopyLine = string.Empty;
        LastActionCopyBlock = string.Empty;
    }


    public static IReadOnlyList<ReferencePlayerActionMenuItem> BuildItems(
        ReferencePlayerActionMenuSnapshot snapshot)
    {
        List<ReferencePlayerActionMenuItem> result =
            new List<ReferencePlayerActionMenuItem>();
        BuildItems(snapshot, result);
        return result;
    }

    public static string Plain(string value)
    {
        if (string.IsNullOrEmpty(value) || value.IndexOf('\\') < 0)
        {
            return value ?? string.Empty;
        }

        char[] buffer = new char[value.Length];
        int written = 0;
        for (int offset = 0; offset < value.Length; offset++)
        {
            if (offset + 3 < value.Length && value[offset] == '\\' &&
                value[offset + 1] == 'e' &&
                IsHex(value[offset + 2]) && IsHex(value[offset + 3]))
            {
                offset += 3;
                continue;
            }

            buffer[written++] = value[offset];
        }

        return new string(buffer, 0, written);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        ResetLastAction();
        items.Clear();
        targetEntityId = 0;
        targetName = string.Empty;
    }

    private static void BuildItems(
        ReferencePlayerActionMenuSnapshot snapshot,
        ICollection<ReferencePlayerActionMenuItem> target)
    {
        target.Clear();
        if (snapshot == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(snapshot.CopyLine))
        {
            Add(target, "复制这一行", ReferencePlayerActionMenuAction.CopyLine);
        }

        if (!string.IsNullOrEmpty(snapshot.CopyBlock))
        {
            Add(target, "复制聊天记录", ReferencePlayerActionMenuAction.CopyBlock);
            Add(target, "选取复制…", ReferencePlayerActionMenuAction.SelectCopy);
        }

        // Same two official 06000974 entry gates as PlayerActionMenu.Build: the name test, then
        // String.op_Equality(targetName, CharData.name). Official has no targetId == 0 gate and no
        // id-based self test, so a name whose id is 0 (a sender who is not in this world) still
        // gets the full identity row table.
        if (string.IsNullOrEmpty(snapshot.TargetName) ||
            snapshot.TargetName == CharData.name)
        {
            return;
        }

        if (snapshot.TargetIsStallOwner)
        {
            Add(target, "看 摊", ReferencePlayerActionMenuAction.BrowseStall);
        }

        Add(target, "密 语", ReferencePlayerActionMenuAction.Whisper);
        Add(target, "交谈/语音", ReferencePlayerActionMenuAction.Gossip);
        Add(target, "邀请参加交易", ReferencePlayerActionMenuAction.Trade);

        if (snapshot.TargetIsPartyMember)
        {
            if (snapshot.PartyLeaderEntityId != snapshot.LocalEntityId)
            {
                Add(target, "跟随队长", ReferencePlayerActionMenuAction.FollowLeader);
            }
            else
            {
                Add(target, "踢出队伍", ReferencePlayerActionMenuAction.PartyKick);
                Add(target, "转让队长", ReferencePlayerActionMenuAction.PartyLeader);
            }
        }
        else if (snapshot.PartyMemberCount < 1)
        {
            Add(target, "创建队伍", ReferencePlayerActionMenuAction.PartyCreate);
            Add(target, "申请加入", ReferencePlayerActionMenuAction.PartyJoin);
        }
        else
        {
            Add(target, "邀请入队", ReferencePlayerActionMenuAction.PartyInvite);
        }

        Add(target, "跟随玩家", ReferencePlayerActionMenuAction.FollowPlayer);
        Add(
            target,
            snapshot.TargetIsFriend ? "删除好友" : "加为好友",
            snapshot.TargetIsFriend
                ? ReferencePlayerActionMenuAction.FriendDelete
                : ReferencePlayerActionMenuAction.FriendAdd);
        if (snapshot.TargetIsBlocked)
            Add(target, "解除屏蔽", ReferencePlayerActionMenuAction.BlockRemove);
        else
        {
            // Same two official 1.5.8 rows as PlayerActionMenu.Build: 17347 = 屏蔽此人 (act 28) and
            // 17345 = 屏蔽他所有角色 (act 29). The labels used here were the 1.3.4 captions
            // 屏蔽私聊 / 屏蔽组队, which no longer match the displayed menu; the enum names
            // BlockChat/BlockParty stay because they only carry act ids 28/29. Nothing renders this
            // table, so the sync changes audit-visible rows only and has no player effect.
            Add(target, "屏蔽此人", ReferencePlayerActionMenuAction.BlockChat);
            Add(target, "屏蔽他所有角色", ReferencePlayerActionMenuAction.BlockParty);
        }
        Add(target, "拜  师", ReferencePlayerActionMenuAction.Apprentice);
        Add(target, "收  徒", ReferencePlayerActionMenuAction.Mentor);
        Add(target, "查看玩家装备", ReferencePlayerActionMenuAction.Inspect);
        Add(target, "决斗竞技场", ReferencePlayerActionMenuAction.DuelArena);

        // Official row 42 is inside the `rideAnchor != 0` arm; 0 == 0 must not emit 下  车.
        if (snapshot.RideTogetherTargetEntityId != 0 &&
            snapshot.RideTogetherTargetEntityId == snapshot.TargetEntityId)
        {
            Add(target, "下  车", ReferencePlayerActionMenuAction.DismountTogether);
        }
        else if (snapshot.RideTogetherTargetEntityId == 0 &&
            snapshot.LocalCanRideTogether && snapshot.TargetCanCarryPassenger)
        {
            Add(target, "搭乘坐骑", ReferencePlayerActionMenuAction.RideTogether);
        }
        else if (snapshot.RideTogetherTargetEntityId == 0 && !snapshot.LocalCanRideTogether &&
            snapshot.LocalCanInvitePassenger &&
            snapshot.TargetEntityId != 0)
        {
            Add(target, "邀请上车", ReferencePlayerActionMenuAction.RideInvite);
        }

        if (snapshot.CanInviteFamily)
        {
            Add(target, "邀请加入家族", ReferencePlayerActionMenuAction.FamilyInvite);
        }
    }

    private static void Add(
        ICollection<ReferencePlayerActionMenuItem> target,
        string label,
        ReferencePlayerActionMenuAction action)
    {
        target.Add(new ReferencePlayerActionMenuItem(label, action));
    }




    private static bool IsHex(char value)
    {
        return value >= '0' && value <= '9' ||
               value >= 'a' && value <= 'f' ||
               value >= 'A' && value <= 'F';
    }


    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(ReferenceFirstWorldPlayerActionMenu));
        }
    }
}

}
