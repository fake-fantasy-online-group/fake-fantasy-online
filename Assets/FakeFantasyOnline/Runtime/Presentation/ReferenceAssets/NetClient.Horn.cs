using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    // Official 060006dc: the shout request. The official body (arm64 0x267454/256B, x86_64
    // 0x2768e0/240B) builds a capture of (text, shares, hornItemId, skin) and hands it to
    // NetClient.Send(0x1058, ...) where the writer (060008ae, arm64 0x278120/164B, x86_64
    // 0x2871a0/160B) emits U8(5), Str(text), the capped share triples (060006dd), I32(hornItemId)
    // and U8(skin). shares is optional in the official signature and defaults to null; this
    // facade only forwards the four values and takes no serialization decision of its own.
    private Action<string, int, byte, List<(byte, byte, int)>> writeHorn;

    internal void BindHornWriterForIndependentRuntime(
        Action<string, int, byte, List<(byte, byte, int)>> writer) => writeHorn = writer;

    public void SendHornForIndependentRuntime(string text, int hornItemId, byte skin,
        List<(byte, byte, int)> shares = null)
        => writeHorn?.Invoke(text, hornItemId, skin, shares);
}
}
