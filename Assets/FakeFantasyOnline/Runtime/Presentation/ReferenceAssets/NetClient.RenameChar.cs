using System;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    // Official 0600075d forwards the caller's name into the rename request on opcode
    // 0x1079; the wire writer stays a bound delegate so the recovery keeps the UI contract without
    // re-implementing the socket encoder.
    private Action<string> writeRenameChar;
    internal void BindRenameCharForIndependentRuntime(Action<string> writer) => writeRenameChar = writer;
    public void SendRenameCharForIndependentRuntime(string name) => writeRenameChar(name);

    // Official Dispatch 0x8057 decision order; wire reading stays in Protocol.
    internal static void ApplyRenameResultForIndependentRuntime(int code, string message, string name)
    {
        if (code == 0)
        {
            CharData.name = name;
            CombatWorld.RenamePlayer(SelfEntityId, name);
            ChatUI.Add("系统", "改名成功，你的新名字是：" + name,
                new Color(120f / 255f, 1f, 120f / 255f, 1f), "", null);
            return;
        }
        if (code == 10)
        {
            RenameDlg.Open();
            return;
        }
        ChatUI.Add("系统", string.IsNullOrEmpty(message) ? "改名失败" : message,
            new Color(1f, 120f / 255f, 120f / 255f, 1f), "", null);
    }
}
}
