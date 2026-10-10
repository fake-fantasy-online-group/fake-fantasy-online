using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    internal partial class NetClient
    {
        private Action openRackWriter;
        private Action<int, byte, int> buyRackWriter;
        private Action<string, int> depositWriter;
        private Action refundListWriter;
        private Action<int, int, int, int, int> refundWriter;
        private Action helpRequestWriter;

        internal void BindHelpRequestForIndependentRuntime(Action writer) => helpRequestWriter = writer;

        public void HelpReqForIndependentRuntime() => helpRequestWriter?.Invoke();

        internal void BindRackCommandsForIndependentRuntime(Action openWriter, Action<int, byte, int> buyWriter,
            Action<string, int> deposit, Action refundList, Action<int, int, int, int, int> refund)
        {
            openRackWriter = openWriter;
            buyRackWriter = buyWriter;
            depositWriter = deposit; refundListWriter = refundList; refundWriter = refund;
        }

        public void RequestDepositForIndependentRuntime(string chain, int amount) => depositWriter?.Invoke(chain, amount);
        public void RackRefundListForIndependentRuntime() => refundListWriter?.Invoke();
        public void RackRefundForIndependentRuntime(int itemId, int unitPrice, int bundle, int day, int shares) =>
            refundWriter?.Invoke(itemId, unitPrice, bundle, day, shares);

        // Official 0x060006eb; wire serialization stays in Protocol.
        public void OpenRackForIndependentRuntime() => openRackWriter?.Invoke();

        // Official 0x060006f1; preserve the source and quantity supplied by the native owner.
        public void BuyRackForIndependentRuntime(int itemId, byte source = 0, int count = 1) => buyRackWriter?.Invoke(itemId, source, count);
    }
}
