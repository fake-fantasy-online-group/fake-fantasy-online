using System.Collections.Generic;
using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
    public sealed partial class PrivateServerGateway : IMailStateSource
    {
        private WorldMailState _mailState = WorldMailState.Empty;
        private long _mailStateVersion;
        private WorldMailUpdate _mailReceipt;
        private long _mailReceiptVersion;

        public WorldMailState MailState => Volatile.Read(ref _mailState);
        public long MailStateVersion => Interlocked.Read(ref _mailStateVersion);
        public WorldMailUpdate MailReceipt => Volatile.Read(ref _mailReceipt);
        public long MailReceiptVersion => Interlocked.Read(ref _mailReceiptVersion);

        // Both Pump routes consume these cases before startup/world retention. One reader,
        // one current-owner callback, then diagnostic publication; no frame or build replay.
        private bool TryReceiveMailPacket(ushort opcode, byte[] payload, IMailReceiveOwner owner)
        {
            WorldMailUpdate receipt;
            if (opcode == ProtocolOpcodes.MailList)
            {
                WorldMailState state = ReadMailState(payload);
                bool applied = owner != null && owner.TryReceiveMailList(state);
                receipt = new WorldMailUpdate(state, applied);
            }
            else if (opcode == ProtocolOpcodes.MailNew)
            {
                bool hasNew = new NetClient.Buf(payload).U8() != 0;
                bool applied = owner != null && owner.TryReceiveMailNew(hasNew);
                receipt = new WorldMailUpdate(hasNew, applied);
            }
            else return false;

            lock (_worldStateSync)
            {
                if (receipt.List != null)
                {
                    Volatile.Write(ref _mailState, receipt.List);
                    Interlocked.Increment(ref _mailStateVersion);
                }
                Volatile.Write(ref _mailReceipt, receipt);
                Interlocked.Increment(ref _mailReceiptVersion);
            }
            return true;
        }

        // Official 1.5.8 Dispatch 0x801e -> MailDlg.SetMailList.
        private static WorldMailState ReadMailState(byte[] payload)
        {
            var reader = new NetClient.Buf(payload);
            int total = reader.I32();
            int capacity = reader.I32();
            int newCount = reader.I32();
            int count = reader.U16();
            var mails = new List<WorldMailItemState>(count);
            for (int i = 0; i < count; i++)
            {
                long id = reader.I64();
                string from = reader.Str();
                string title = reader.Str();
                string body = reader.Str();
                long money = reader.I64();
                long caiyu = reader.I64();
                byte type = (byte)reader.U8();
                bool read = reader.U8() != 0;
                bool canClaim = reader.U8() != 0;
                bool canReturn = reader.U8() != 0;
                long time = reader.I64();
                int attachmentCount = reader.U8();
                var attachments = new List<WorldMailAttachment>(attachmentCount);
                for (int j = 0; j < attachmentCount; j++)
                    attachments.Add(new WorldMailAttachment(reader.I32(), reader.I32(), reader.Str(), reader.Str()));
                mails.Add(new WorldMailItemState(id, from, title, body, money, caiyu, type,
                    read, canClaim, canReturn, time, attachments));
            }
            return new WorldMailState(total, capacity, newCount, mails);
        }

        private void ResetMailStateLocked()
        {
            Volatile.Write(ref _mailState, WorldMailState.Empty);
            Interlocked.Increment(ref _mailStateVersion);
            Volatile.Write(ref _mailReceipt, null);
            Interlocked.Increment(ref _mailReceiptVersion);
            // Disconnect bookkeeping is not an 801e/801f packet. The session owner releases
            // native UI state at the actual final world/title boundary, never through replay.
        }
    }
}
