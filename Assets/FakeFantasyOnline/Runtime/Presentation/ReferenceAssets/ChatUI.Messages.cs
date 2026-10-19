using Share = FakeFantasyOnline.Application.WorldChatSharedEntryState;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ChatUI
{
    internal struct Line
    {
        public string ch, text;
        public Color col;
        public string who;
        public List<Share> shares;
    }
    internal static IReadOnlyList<Line> SystemLinesForIndependentRuntime => _sysTmp;
    internal static IReadOnlyList<Line> OrdinaryLinesForIndependentRuntime => _chatTmp;
    private static bool partitionCacheValid;
    private static LineCacheKey partitionCacheKey;
    internal static void PartitionLinesForIndependentRuntime()
    {
        LineCacheKey key = CurrentLineCacheKey;
        if (partitionCacheValid && partitionCacheKey.Equals(key)) return;
        _sysTmp.Clear();
        _chatTmp.Clear();
        foreach (Line line in chatLines)
        {
            if (!ChanShown(line.ch)) continue;
            if (line.ch == "系统") _sysTmp.Add(line);
            else _chatTmp.Add(line);
        }
        partitionCacheKey = key;
        partitionCacheValid = true;
    }

    public static int Ver;
    private static int sysSeq;

    // The whisper system-line colour 0x060000e5 passes as the third ChatUI.Add argument. Both
    // frozen slices load it as four float constants - arm64 fmov s0/s1/s2/s3 = 0x3f800000 /
    // 0x3f34b4b5 / 0x3f5cdcdd / 0x3f800000 at 0x231f14..0x231f28 and x86_64 movsd xmm0 / xmm1
    // from the same eight bytes at 0x23faff / 0x23fb07 - i.e. exactly 1f, 180f / 255f,
    // 220f / 255f, 1f and not the rounded 0.706f / 0.863f decimals.
    internal static readonly Color WhisperSystemLineColor =
        new Color(1f, 180f / 255f, 220f / 255f, 1f);

    // 0x060000e5 ChatUI.StartWhisper(System.String) - arm64 rva=0x231d60 extent 484,
    // x86_64 rva=0x23f9a0 extent 400. Both slices are the same four steps with no other branch
    // than the injected cctor latch and the class-init checks:
    //   1. UnityEngine.Object.op_Equality(I, null) -> return when true, i.e. an unmounted or
    //      destroyed owner makes the whole member a silent no-op (arm64 0x231dbc / x86
    //      0x23fa49, reached again as the implicit null guard at 0x231e88 / 0x23fa7b).
    //   2. I.whisperTarget = name ?? "" - arm64 csel x1, usage_10335, x19 / str x1,[x0,#0x88],
    //      x86 test rbx / mov rsi,[usage] / mov [rdi+0x88],rsi. No trim and no other rewrite.
    //   3. I.panelsVisible = true (arm64 strb w10,[x9,#0x69] / x86 mov byte [rcx+0x69],1) and
    //      the static _pendingFocus = true (arm64 strb w10,[x8,#0x58] / x86 mov byte [rax+0x58],1).
    //   4. tail call Add("系统", "已对 " + name + " 开启密语, 输入内容回车发送",
    //      WhisperSystemLineColor, "", null) - usage 17895 / 17363 / 10660 / 10335, shares null
    //      (x3=0 / rcx=0). The concat takes the raw argument, never the coalesced one.
    // Neither inputText (+0x78) nor the pending share lists are touched, so text already typed
    // and shares already inserted survive a target switch, and there is no self-target gate.
    public static void StartWhisper(string name)
    {
        if (I == null)
        {
            return;
        }

        I.whisperTarget = name ?? string.Empty;
        I.panelsVisible = true;
        _pendingFocus = true;
        Add(
            "系统",
            "已对 " + name + " 开启密语, 输入内容回车发送",
            WhisperSystemLineColor,
            string.Empty);
    }

    public static void Add(string ch, string text, Color col, string who = "", List<Share> shares = null)
    {
        chatLines.Add(new Line { ch = ch, text = text, col = col, who = who, shares = shares });
        lineContentRevision = unchecked(lineContentRevision + 1);
        Ver = unchecked(Ver + 1);
        if (chatLines.Count > 800) chatLines.RemoveAt(0);
        if (ch == "系统") sysSeq = unchecked(sysSeq + 1);
    }

    public static List<(string, string, Color, string, List<Share>)> Snapshot()
    {
        var result = new List<(string, string, Color, string, List<Share>)>(chatLines.Count);
        foreach (Line line in chatLines)
            result.Add((line.ch, line.text, line.col, line.who, line.shares));
        return result;
    }

    public static string RecentText(int n)
    {
        if (chatLines.Count == 0) return "";
        var result = new StringBuilder();
        for (int i = Math.Max(0, unchecked(chatLines.Count - n)); i < chatLines.Count; i++)
        {
            Line line = chatLines[i];
            if (!string.IsNullOrEmpty(line.ch)) result.Append('[').Append(line.ch).Append("] ");
            result.Append(line.text).Append('\n');
        }
        return result.ToString().TrimEnd('\n');
    }

    internal static int LineCountForIndependentRuntime => chatLines.Count;
    internal static int SystemSequenceForIndependentRuntime => sysSeq;
    internal static void ClearLinesForIndependentRuntime()
    {
        chatLines.Clear();
        lineContentRevision = unchecked(lineContentRevision + 1);
    }
}
}
