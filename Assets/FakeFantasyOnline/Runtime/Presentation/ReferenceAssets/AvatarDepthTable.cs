using System;
using System.Collections.Generic;
using System.Text;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Source translation of the reference AvatarDepth tables and INI parser.
/// </summary>
/// <remarks>
/// The table shape, constants, clamping, channel selection, and stable depth sort are recovered
/// from 06000097 through 060000A0. PlayerAvatar.FramesOf (06000857) adds the recovered Q21 base
/// rank as the tie-breaker used by <see cref="ComparePlayerLayerTags"/>.
/// </remarks>
internal sealed class AvatarDepthTable
{
    private const int ArtDirectionCount = 5;

    private static readonly string[] PartNames =
    {
        "npc",
        "cap",
        "face",
        "head",
        "backpack",
        "weaponl",
        "weaponr",
        "pet",
        "pet_top"
    };

    private static readonly string[] PoseNames =
    {
        "stand",
        "run",
        "sit",
        "ride",
        "attack1",
        "attack2",
        "attack3",
        "prepare1",
        "prepare2",
        "prepare3",
        "magic",
        "hurt",
        "die"
    };

    private static readonly int[] DirectionFold =
    {
        0, 1, 2, 3, 2, 1, 0, 4
    };

    private static readonly int[][] Q21Base =
    {
        new[] { 6, 7, 9, 4, 0, 3, 2, 1, 5, 8, 10, 11, 12, 13, 14 },
        new[] { 6, 7, 0, 4, 3, 2, 1, 5, 9, 8, 10, 11, 12, 13, 14 },
        new[] { 6, 7, 0, 2, 3, 1, 4, 5, 9, 8, 10, 11, 12, 13, 14 },
        new[] { 7, 6, 5, 0, 2, 3, 1, 4, 9, 8, 10, 11, 12, 13, 14 },
        new[] { 6, 7, 0, 2, 3, 1, 4, 5, 9, 8, 10, 11, 12, 13, 14 },
        new[] { 6, 7, 0, 4, 3, 2, 1, 5, 9, 8, 10, 11, 12, 13, 14 },
        new[] { 6, 7, 9, 4, 0, 3, 2, 1, 5, 8, 10, 11, 12, 13, 14 },
        new[] { 7, 9, 4, 0, 3, 2, 1, 6, 5, 8, 10, 11, 12, 13, 14 }
    };

    // The source-owned extraction preserves archive spelling. The original PkgArchive lookup was
    // case-insensitive; these paths therefore name the same inputs as avatar/{part}/{part}.ini.
    private static readonly string[] SourceIniPaths =
    {
        "avatar/npc/Npc.ini",
        "avatar/cap/Cap.ini",
        "avatar/face/Face.ini",
        "avatar/head/Head.ini",
        "avatar/backpack/Backpack.ini",
        "avatar/weaponl/Weaponl.ini",
        "avatar/weaponr/Weaponr.ini",
        "avatar/pet/pet.ini",
        "avatar/pet_top/pet_top.ini"
    };

    private readonly int[][][][][] _table;

    private AvatarDepthTable(
        int[][][][][] table,
        int loadedSourceCount,
        int parsedRowCount,
        int parsedValueCount)
    {
        _table = table;
        LoadedSourceCount = loadedSourceCount;
        ParsedRowCount = parsedRowCount;
        ParsedValueCount = parsedValueCount;
    }

    internal bool Loaded => _table != null;
    internal int LoadedSourceCount { get; }
    internal int ParsedRowCount { get; }
    internal int ParsedValueCount { get; }
    internal int PartCount => PartNames.Length;
    internal int PoseCount => PoseNames.Length;
    internal int DirectionCount => DirectionFold.Length;

    internal static AvatarDepthTable Load(ReferenceResourceLoader resources)
    {
        if (resources == null)
        {
            throw new ArgumentNullException(nameof(resources));
        }

        int[][][][][] table = AllocateTable();
        int sourceCount = 0;
        int rowCount = 0;
        int valueCount = 0;
        for (int part = 0; part < PartNames.Length; part++)
        {
            byte[] raw = resources.ReadRes(SourceIniPaths[part]);
            if (raw == null)
            {
                continue;
            }

            sourceCount++;
            ParseIni(raw, table[part], ref rowCount, ref valueCount);
        }

        return new AvatarDepthTable(table, sourceCount, rowCount, valueCount);
    }

    internal string PartName(int index)
    {
        return index >= 0 && index < PartNames.Length ? PartNames[index] : null;
    }

    internal string PoseName(int index)
    {
        return index >= 0 && index < PoseNames.Length ? PoseNames[index] : null;
    }

    internal int PoseIdx(string name)
    {
        for (int index = 0; index < PoseNames.Length; index++)
        {
            if (string.Equals(PoseNames[index], name, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }

    internal int ArtDirection(int screenDirection)
    {
        return DirectionFold[screenDirection & 7];
    }

    internal int BaseRank(int tag, int screenDirection)
    {
        int[] order = Q21Base[screenDirection & 7];
        for (int index = 0; index < order.Length; index++)
        {
            if (order[index] == tag)
            {
                return index;
            }
        }

        return 99;
    }

    internal int DepthArt(
        int part,
        int poseIndex,
        int artDirection,
        int frame,
        int channel)
    {
        if (_table == null ||
            part < 0 || part >= PartNames.Length ||
            poseIndex < 0 || poseIndex >= PoseNames.Length ||
            artDirection < 0 || artDirection >= ArtDirectionCount)
        {
            return 0;
        }

        int selectedChannel = channel == 0 ? 0 : 1;
        int[] row = _table[part][selectedChannel][poseIndex][artDirection];
        if (row == null || row.Length == 0)
        {
            return 0;
        }

        int selectedFrame = frame;
        if (selectedFrame < 0)
        {
            selectedFrame = 0;
        }
        else if (selectedFrame >= row.Length)
        {
            selectedFrame = row.Length - 1;
        }

        return row[selectedFrame];
    }

    internal int Depth(
        int part,
        int poseIndex,
        int screenDirection,
        int frame,
        int channel)
    {
        return DepthArt(
            part,
            poseIndex,
            DirectionFold[screenDirection & 7],
            frame,
            channel);
    }

    internal void SortByDepth(
        int[] order,
        int poseIndex,
        int screenDirection,
        int frame,
        int channel)
    {
        if (order == null)
        {
            throw new ArgumentNullException(nameof(order));
        }

        int[] depths = new int[order.Length];
        int artDirection = DirectionFold[screenDirection & 7];
        for (int index = 0; index < order.Length; index++)
        {
            depths[index] = DepthArt(
                order[index],
                poseIndex,
                artDirection,
                frame,
                channel);
        }

        for (int remaining = order.Length - 1; remaining > 0; remaining--)
        {
            bool swapped = false;
            for (int index = 0; index < remaining; index++)
            {
                if (depths[index + 1] >= depths[index])
                {
                    continue;
                }

                int depth = depths[index];
                depths[index] = depths[index + 1];
                depths[index + 1] = depth;

                int tag = order[index];
                order[index] = order[index + 1];
                order[index + 1] = tag;
                swapped = true;
            }

            if (!swapped)
            {
                return;
            }
        }
    }

    internal int ComparePlayerLayerTags(
        int leftTag,
        int rightTag,
        int poseIndex,
        int artDirection,
        int frame,
        int screenDirection)
    {
        // User-approved parity exceptions use PlayerAvatar's five stored art directions. South is
        // stored row 4 and keeps wings behind the Doll; the shared upper-diagonal row 2 (northwest
        // and mirrored northeast) keeps wings in front of the body.
        int direction = screenDirection & 7;
        if ((direction == 4 || direction == 2) && leftTag != rightTag &&
            (leftTag == 4 || rightTag == 4))
        {
            bool backpackFirst = direction == 4;
            return leftTag == 4
                ? (backpackFirst ? -1 : 1)
                : (backpackFirst ? 1 : -1);
        }

        int leftDepth = DepthArt(leftTag, poseIndex, artDirection, frame, 1);
        int rightDepth = DepthArt(rightTag, poseIndex, artDirection, frame, 1);
        int depthComparison = leftDepth.CompareTo(rightDepth);
        if (depthComparison != 0)
        {
            return depthComparison;
        }

        return BaseRank(leftTag, screenDirection).CompareTo(
            BaseRank(rightTag, screenDirection));
    }

    internal int RowLength(int part, int channel, int poseIndex, int artDirection)
    {
        if (_table == null ||
            part < 0 || part >= PartNames.Length ||
            channel < 0 || channel > 1 ||
            poseIndex < 0 || poseIndex >= PoseNames.Length ||
            artDirection < 0 || artDirection >= ArtDirectionCount)
        {
            return 0;
        }

        int[] row = _table[part][channel][poseIndex][artDirection];
        return row == null ? 0 : row.Length;
    }

    private static int[][][][][] AllocateTable()
    {
        int[][][][][] table = new int[PartNames.Length][][][][];
        for (int part = 0; part < PartNames.Length; part++)
        {
            table[part] = new int[2][][][];
            for (int channel = 0; channel < 2; channel++)
            {
                table[part][channel] = new int[PoseNames.Length][][];
                for (int pose = 0; pose < PoseNames.Length; pose++)
                {
                    table[part][channel][pose] = new int[ArtDirectionCount][];
                }
            }
        }

        return table;
    }

    private static void ParseIni(
        byte[] raw,
        int[][][][] output,
        ref int parsedRowCount,
        ref int parsedValueCount)
    {
        StringBuilder builder = new StringBuilder(raw.Length);
        for (int index = 0; index < raw.Length; index++)
        {
            builder.Append((char)raw[index]);
        }

        int poseIndex = -1;
        int artDirection = -1;
        int channel = -1;
        string[] lines = builder.ToString().Split('\n');
        foreach (string sourceLine in lines)
        {
            string line = sourceLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (line[0] == '[')
            {
                int closingBracket = line.IndexOf(']');
                if (closingBracket < 1)
                {
                    poseIndex = -1;
                    continue;
                }

                string poseName = line.Substring(1, closingBracket - 1);
                poseIndex = PoseIndex(poseName);
                if (poseIndex < 0 && poseName.Length > 1 &&
                    poseName[poseName.Length - 1] == 'g')
                {
                    poseIndex = PoseIndex(poseName.Substring(0, poseName.Length - 1));
                    channel = 1;
                }
                else
                {
                    channel = 0;
                }

                artDirection = -1;
                continue;
            }

            if (line[0] == '(')
            {
                artDirection = -1;
                if (line.Length >= 5 &&
                    line[1] == 'd' && line[2] == 'i' && line[3] == 'r')
                {
                    int parsedDirection;
                    if (int.TryParse(
                            line.Substring(4, line.IndexOf(')') - 4),
                            out parsedDirection) &&
                        parsedDirection >= 1 && parsedDirection <= ArtDirectionCount)
                    {
                        artDirection = parsedDirection - 1;
                    }
                }

                continue;
            }

            if (poseIndex < 0 || artDirection < 0 ||
                (!char.IsDigit(line[0]) && line[0] != '-'))
            {
                continue;
            }

            string[] columns = line.Split(',');
            if (columns.Length > 1)
            {
                int[] values = new int[columns.Length];
                for (int index = 0; index < columns.Length; index++)
                {
                    int.TryParse(columns[index].Trim(), out values[index]);
                }

                output[channel][poseIndex][artDirection] = values;
                parsedRowCount++;
                parsedValueCount += values.Length;
            }

            artDirection = -1;
        }
    }

    private static int PoseIndex(string name)
    {
        for (int index = 0; index < PoseNames.Length; index++)
        {
            if (string.Equals(PoseNames[index], name, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }
}

}
