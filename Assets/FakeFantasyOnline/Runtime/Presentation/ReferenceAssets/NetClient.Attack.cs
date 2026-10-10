using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    // Official 0600074d (arm64 0x26f894 / 192 bytes, x86_64 rva 0x27ee00): the normal
    // attack request. The official body allocates one request object, stores the target id into
    // its single int field at +0x10, resolves the writer and calls NetClient.Send(this, 0x100c,
    // writer). Allocation failure is the IL2CPP non-returning null helper (arm64 FUN_00177a74 /
    // x86_64 FUN_0016c1b0), i.e. an exception, not a silent skip. No other field, opcode or
    // ordering exists in either body.
    // The transport stays in Protocol: this facet only owns the member identity and forwards the
    // target id, exactly like the other command bindings.
    private Action<int> writeAttack;

    internal void BindAttackWriterForIndependentRuntime(Action<int> writer) => writeAttack = writer;

    public void AttackForIndependentRuntime(int targetId) => writeAttack(targetId);
}
}
