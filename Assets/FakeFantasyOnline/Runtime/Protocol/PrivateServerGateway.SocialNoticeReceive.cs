using System.Collections.Generic;
using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    // Both Pump forms enter here before stage-dependent retention. A callback failure leaves
    // no queued envelope, so partial native effects are never retried by a later window.
    private void ReceiveSocialNotice(ushort opcode, byte[] payload,
        ISocialNoticeReceiveOwner context = null)
    {
        ISocialNoticeReceiveOwner owner = _worldReceiveOwner as ISocialNoticeReceiveOwner ?? context;
        DecodedSocialNotice notice;
        if (opcode == ProtocolOpcodes.PrivateChat)
        {
            PrivateServerWorldStateDecoder.DecodePrivateChat(payload,
                out string sender, out string text, out IReadOnlyList<WorldChatSharedEntryState> shares);
            owner?.ReceivePrivateChat(sender, text, shares);
            // Retention is observational; its allocation must not prevent native Receive.
            notice = new DecodedSocialNotice(sender, text, shares);
        }
        else if (owner != null)
        {
            // Original Dispatch 0x8035 supplies this exact tuple list to SetList. No DTO,
            // read-only wrapper or second row list may precede the native call.
            List<(int, byte, int, string, string)> requests =
                NetClient.ReadSystemRequestRows(new NetClient.Buf(payload));
            owner.ReceiveSystemRequests(requests);
            notice = new DecodedSocialNotice(requests);
        }
        else
        {
            notice = DecodeSocialNotice(opcode, payload, false);
        }
        DispatchPacket(new ProtocolPacket(opcode, payload, socialNotice: notice,
            socialNoticeOwnerApplied: owner != null));
    }

    // The retained original list is only an observation after SetList returns. Materialize
    // immutable rows when explicitly read, never while delivering a later native packet.
    private List<(int, byte, int, string, string)> _systemRequestRowsObservation;

    private IReadOnlyList<WorldSystemRequestState> ReadSystemRequests()
    {
        lock (_worldStateSync)
        {
            if (_systemRequestRowsObservation != null)
            {
                var rows = new List<WorldSystemRequestState>(_systemRequestRowsObservation.Count);
                foreach (var row in _systemRequestRowsObservation)
                    rows.Add(new WorldSystemRequestState(row.Item1, row.Item2, row.Item3,
                        row.Item4, row.Item5));
                Volatile.Write(ref _systemRequests, rows.AsReadOnly());
                _systemRequestRowsObservation = null;
            }
            return Volatile.Read(ref _systemRequests);
        }
    }

    private void PublishSystemRequests(ProtocolPacket packet, CancellationToken cancellationToken)
    {
        DecodedSocialNotice notice = packet.SocialNotice ??
            DecodeSocialNotice(packet.Opcode, packet.Payload, false);
        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _systemRequestRowsObservation = notice.RequestRows;
            if (notice.RequestRows == null)
                Volatile.Write(ref _systemRequests, notice.Requests);
            if (!packet.SocialNoticeOwnerApplied)
                _socialUiUpdates.Enqueue(new WorldSocialUiUpdate(notice.Requests));
            Interlocked.Increment(ref _systemRequestsVersion);
        }
    }

    private void ResetSystemRequestsObservation()
    {
        // Called under _worldStateSync. Do not construct a snapshot just to retire it.
        bool hadRequests = _systemRequestRowsObservation != null
            ? _systemRequestRowsObservation.Count != 0
            : Volatile.Read(ref _systemRequests).Count != 0;
        _systemRequestRowsObservation = null;
        Volatile.Write(ref _systemRequests, System.Array.Empty<WorldSystemRequestState>());
        if (hadRequests) Interlocked.Increment(ref _systemRequestsVersion);
    }

    private static DecodedSocialNotice DecodeSocialNotice(ushort opcode, byte[] payload, bool startup)
    {
        if (opcode == ProtocolOpcodes.PrivateChat)
        {
            PrivateServerWorldStateDecoder.DecodePrivateChat(payload,
                out string sender, out string text, out IReadOnlyList<WorldChatSharedEntryState> shares);
            return new DecodedSocialNotice(sender, text, shares);
        }
        PrivateServerWorldStateDecoder.DecodeSystemRequests(payload,
            out IReadOnlyList<WorldSystemRequestState> requests);
        return new DecodedSocialNotice(requests);
    }

    // Manual/context-free collectors may have a raw packet. Decode that compatibility input
    // once but never claim it called a native owner or promote it to a delayed gameplay event.
    private static ProtocolPacket RetainSocialNotice(ProtocolPacket packet)
    {
        if (packet.SocialNotice != null) return packet;
        return new ProtocolPacket(packet.Opcode, packet.Payload,
            socialNotice: DecodeSocialNotice(packet.Opcode, packet.Payload, true));
    }

    internal sealed class DecodedSocialNotice
    {
        internal DecodedSocialNotice(string sender, string text,
            IReadOnlyList<WorldChatSharedEntryState> shares)
        { SenderName = sender; Text = text; Shares = shares; }
        internal DecodedSocialNotice(IReadOnlyList<WorldSystemRequestState> requests)
        { Requests = requests; }
        internal DecodedSocialNotice(List<(int, byte, int, string, string)> requests)
        { RequestRows = requests; }
        internal string SenderName { get; }
        internal string Text { get; }
        // Preserve the reader's nullable list and exact row references through the envelope.
        internal IReadOnlyList<WorldChatSharedEntryState> Shares { get; }
        internal IReadOnlyList<WorldSystemRequestState> Requests { get; }
        internal List<(int, byte, int, string, string)> RequestRows { get; }
    }
}
}
