using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{

/// <summary>
/// Strict source translation of the official skill-book replacement and one-I32 writers.
/// </summary>
public static class PrivateServerSkillBookCodec
{

    public static bool TryDecodeSkillBook(byte[] payload, out WorldSkillBookState state)
    {
        return TryDecodeSkillBook(payload, out state, out _);
    }

    // Compatibility decoder. Live dispatch retains reached facts and propagates the Buf failure.
    public static bool TryDecodeSkillBook(byte[] payload, out WorldSkillBookState state,
        out WorldSkillBookReceipt receipt)
    {
        state = null;
        receipt = null;
        if (payload == null) return false;
        try
        {
            state = DecodeSkillBook(payload, out receipt);
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException ||
            exception is EndOfStreamException ||
            exception is InvalidDataException ||
            exception is OverflowException ||
            exception is IndexOutOfRangeException)
        {
            state = null;
            return false;
        }
    }

    internal static WorldSkillBookState DecodeSkillBook(byte[] payload,
        out WorldSkillBookReceipt receipt, bool projectSnapshot = true)
    {
        receipt = null;
        bool pointsRead = false;
        int points = 0;
        List<WorldLearnedSkillState> rows = null;
        try
        {
            var reader = new NetClient.Buf(payload);
            points = reader.I32();
            pointsRead = true;
            int count = reader.U16();
            rows = new List<WorldLearnedSkillState>(count);
            for (int index = 0; index < count; index++)
            {
                int id = reader.I32();
                int level = reader.I32();
                rows.Add(new WorldLearnedSkillState(id, level));
            }

            var bonuses = new Dictionary<int, int>();
            if (reader.Remaining >= 2)
            {
                int bonusCount = reader.U16();
                for (int i = 0; i < bonusCount && reader.Remaining >= 8; i++)
                {
                    int id = reader.I32();
                    bonuses[id] = reader.I32();
                }
            }
            receipt = new WorldSkillBookReceipt(points, rows, bonuses);
            // Bound owners consume the ordered receipt and capture their actual roots.
            // An unused immutable projection must not fail before those native stores.
            return projectSnapshot ? receipt.ApplyToSnapshot(WorldSkillBookState.Empty) : null;
        }
        catch (Exception exception) when (
            exception is IndexOutOfRangeException || exception is ArgumentException)
        {
            // Preserve the already-proved reached roots, then rethrow the same failure.
            // Owner calls occur in the gateway's finally, outside this decode-only catch.
            if (receipt == null && pointsRead)
                receipt = new WorldSkillBookReceipt(points, rows, null);
            throw;
        }
    }

    public static bool TryDecodeLifeSkills(
        byte[] payload,
        out WorldLifeSkillBookState state)
    {
        state = null;
        try
        {
            WireReader reader = new WireReader(payload);
            int count = reader.ReadUInt16();
            var learned = new Dictionary<int, WorldLifeSkillState>();
            var canLearn = new HashSet<int>();
            for (int index = 0; index < count; index++)
            {
                int id = reader.ReadInt32();
                int level = reader.ReadInt32();
                string name = reader.ReadString();
                byte eligible = reader.ReadByte();
                if (level >= 0) learned[id] = new WorldLifeSkillState(id, level, name, false);
                else if (eligible != 0) canLearn.Add(id);
            }
            state = new WorldLifeSkillBookState(new List<WorldLifeSkillState>(learned.Values), canLearn);
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException ||
            exception is EndOfStreamException ||
            exception is InvalidDataException ||
            exception is OverflowException ||
            exception is DecoderFallbackException)
        {
            state = null;
            return false;
        }
    }

    public static bool TryEncodeUpgrade(int skillId, out byte[] payload)
    {
        return TryEncodeOneInt32(skillId, false, out payload);
    }

    public static bool TryEncodeLearnLife(int skillId, out byte[] payload)
    {
        return TryEncodeOneInt32(skillId, true, out payload);
    }

    private static bool TryEncodeOneInt32(
        int skillId,
        bool allowZero,
        out byte[] payload)
    {
        payload = null;
        if (skillId < 0 || (!allowZero && skillId == 0))
        {
            return false;
        }

        WireWriter writer = new WireWriter();
        writer.WriteInt32(skillId);
        payload = writer.ToArray();
        return true;
    }
}

}
