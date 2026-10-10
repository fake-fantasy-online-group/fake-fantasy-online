using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : ICharacterRosterReceiptSource
{
    public void BindCharacterRosterReceipt(Action<IReadOnlyList<ServerCharacterRecord>> receive) =>
        _netClient.OnCharList = receive;

    private bool TryReceiveCharacterListPacket(ushort opcode, byte[] payload)
    {
        if (opcode != ProtocolOpcodes.CharacterList) return false;

        IReadOnlyList<ServerCharacterRecord> roster;
        try
        {
            // Original 0x8004 decodes the whole array before loading OnCharList, even when
            // the slot is null. Reuse the existing exact reader, including its wire failures.
            roster = ReadRoster(new ProtocolPacket(opcode, payload));
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            // Keep the existing Task observer, then preserve native dispatch propagation.
            // No partial roster, successful reply or next inbox dispatch follows this failure.
            DispatchPacket(new ProtocolPacket(opcode, payload,
                characterRosterFailure: ExceptionDispatchInfo.Capture(exception)));
            throw;
        }

        // ARM 0x2510ac / x86_64 0x25f9c0: one live slot load after every completed row. The callback
        // is outside the decode catch: an owner failure propagates and queues no success.
        Action<IReadOnlyList<ServerCharacterRecord>> receive = _netClient.OnCharList;
        receive?.Invoke(roster);

        // Existing login/mutation/return waiters still require this actual packet. The
        // envelope is an observation of this receipt, never another roster publication.
        DispatchPacket(new ProtocolPacket(opcode, payload, receivedCharacterRoster: roster));
        return true;
    }

    private static IReadOnlyList<ServerCharacterRecord> ReadReceivedCharacterRoster(ProtocolPacket packet)
    {
        packet.CharacterRosterFailure?.Throw();
        return packet.ReceivedCharacterRoster ?? throw new InvalidDataException(
            "The character-list receipt was not decoded at dispatch.");
    }
}
}
