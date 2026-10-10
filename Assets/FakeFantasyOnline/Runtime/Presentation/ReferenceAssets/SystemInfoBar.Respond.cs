namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public sealed partial class SystemInfoBar
    {
        // Official 0x06000e3c and closures 0x06000e43/0x06000e44 capture the complete Req value.
        private void OpenConfirm(Req q)
        {
            SysMsgBox.Show(q.text + "  是否接受？", () => Respond(q, true), () => Respond(q, false));
        }

        // Official 0x06000e3d. Server requests remain until the server replaces the list.
        private void Respond(Req q, bool accept)
        {
            if (q.kind == 4)
            {
                if (!accept) return;
                GossipDlg.Open(q.fromName);
                if (q.local)
                {
                    gossip.RemoveAll(row => row.fromName == q.fromName);
                    return;
                }
                NetClient.I?.SysMsgRespondForIndependentRuntime(q.reqId, 1);
                return;
            }
            NetClient.I?.SysMsgRespondForIndependentRuntime(q.reqId, accept ? (byte)1 : (byte)0);
        }
    }
}
