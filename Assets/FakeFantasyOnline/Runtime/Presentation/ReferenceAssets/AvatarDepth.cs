using System;
using System.Text;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static class AvatarDepth
{
    public static readonly string[] PARTS;
    public static readonly string[] POSES;
    public static readonly int[] DirFold;
    private const int NDIR = 5;
    private static int[][][][][] _t;
    private static readonly int[][] Q21Base;

    public static bool Loaded => _t != null;

    public static int BaseRank(int tag, int screenDir)
    {
        int[] order = Q21Base[screenDir & 7];
        for (int i = 0; i < order.Length; i++) if (order[i] == tag) return i;
        return 99;
    }

    public static int PoseIdx(string name)
    {
        for (int i = 0; i < POSES.Length; i++) if (POSES[i] == name) return i;
        return -1;
    }

    private static int PoseIndex(string name) => PoseIdx(name);

    public static void EnsureLoaded()
    {
        if (_t != null) return;
        ResLoader loader = ResLoader.I;
        if (loader == null) return;
        int[][][][][] table = new int[PARTS.Length][][][][];
        for (int part = 0; part < PARTS.Length; part++)
        {
            table[part] = new int[2][][][];
            for (int channel = 0; channel < 2; channel++)
            {
                table[part][channel] = new int[POSES.Length][][];
                for (int pose = 0; pose < POSES.Length; pose++)
                    table[part][channel][pose] = new int[NDIR][];
            }
            byte[] raw = loader.ReadRes24First("avatar/" + PARTS[part] + "/" + PARTS[part] + ".ini");
            if (raw != null) ParseIni(raw, table[part]);
        }
        _t = table;
    }

    private static void ParseIni(byte[] raw, int[][][][] outPart)
    {
        StringBuilder builder = new StringBuilder(raw.Length);
        for (int i = 0; i < raw.Length; i++) builder.Append((char)raw[i]);
        int pose = -1;
        int dir = -1;
        int channel = -1;
        foreach (string source in builder.ToString().Split('\n'))
        {
            string line = source.Trim();
            if (line.Length == 0) continue;
            if (line[0] == '[')
            {
                int close = line.IndexOf(']');
                if (close <= 0)
                {
                    pose = -1;
                    continue;
                }
                string name = line.Substring(1, close - 1);
                pose = PoseIndex(name);
                channel = 0;
                if (pose < 0 && name.Length >= 2 && name[name.Length - 1] == 'g')
                {
                    pose = PoseIndex(name.Substring(0, name.Length - 1));
                    channel = 1;
                }
                dir = -1;
                continue;
            }
            if (line[0] == '(')
            {
                dir = -1;
                if (line.Length >= 5 && line[1] == 'd' && line[2] == 'i' && line[3] == 'r')
                {
                    int parsed;
                    if (int.TryParse(line.Substring(4, line.IndexOf(')') - 4), out parsed) &&
                        parsed >= 1 && parsed <= 5) dir = parsed - 1;
                }
                continue;
            }
            if (pose < 0 || dir < 0) continue;
            if (!char.IsDigit(line[0]) && line[0] != '-') continue;
            string[] tokens = line.Split(',');
            if (tokens.Length < 2)
            {
                dir = -1;
                continue;
            }
            int[] values = new int[tokens.Length];
            for (int i = 0; i < tokens.Length; i++) int.TryParse(tokens[i].Trim(), out values[i]);
            outPart[channel][pose][dir] = values;
            dir = -1;
        }
    }

    public static int DepthArt(int part, int poseIdx, int artDir, int frame, int channel)
    {
        if (part < 0 || _t == null || part >= PARTS.Length || poseIdx < 0 ||
            poseIdx >= POSES.Length || artDir < 0 || artDir >= NDIR) return 0;
        int[] values = _t[part][channel == 0 ? 0 : 1][poseIdx][artDir];
        if (values == null || values.Length == 0) return 0;
        return values[frame < 0 ? 0 : frame >= values.Length ? values.Length - 1 : frame];
    }

    public static int Depth(int part, int poseIdx, int screenDir, int frame, int channel) =>
        DepthArt(part, poseIdx, DirFold[screenDir & 7], frame, channel);

    public static void SortByDepth(int[] order, int pose, int screenDir, int frame, int channel)
    {
        EnsureLoaded();
        int[] depths = new int[order.Length];
        for (int i = 0; i < order.Length; i++) depths[i] = Depth(order[i], pose, screenDir, frame, channel);
        for (int end = order.Length - 1; end > 0; end--)
        {
            bool swapped = false;
            for (int i = 0; i < end; i++)
            {
                if (depths[i] <= depths[i + 1]) continue;
                int depth = depths[i]; depths[i] = depths[i + 1]; depths[i + 1] = depth;
                int part = order[i]; order[i] = order[i + 1]; order[i + 1] = part;
                swapped = true;
            }
            if (!swapped) break;
        }
    }

    static AvatarDepth()
    {
        PARTS = new[] { "npc", "cap", "face", "head", "backpack", "weaponl", "weaponr", "pet", "pet_top" };
        POSES = new[] { "stand", "run", "sit", "ride", "attack1", "attack2", "attack3", "prepare1", "prepare2", "prepare3", "magic", "hurt", "die" };
        DirFold = new[] { 0, 1, 2, 3, 2, 1, 0, 4 };
        Q21Base = new[]
        {
            new[] { 6, 7, 9, 4, 0, 3, 2, 1, 5, 8, 10, 11, 12, 13, 14 },
            new[] { 6, 7, 0, 4, 3, 2, 1, 5, 9, 8, 10, 11, 12, 13, 14 },
            new[] { 6, 7, 0, 2, 3, 1, 4, 5, 9, 8, 10, 11, 12, 13, 14 },
            new[] { 7, 6, 5, 0, 2, 3, 1, 4, 9, 8, 10, 11, 12, 13, 14 },
            new[] { 6, 7, 0, 2, 3, 1, 4, 5, 9, 8, 10, 11, 12, 13, 14 },
            new[] { 6, 7, 0, 4, 3, 2, 1, 5, 9, 8, 10, 11, 12, 13, 14 },
            new[] { 6, 7, 9, 4, 0, 3, 2, 1, 5, 8, 10, 11, 12, 13, 14 },
            new[] { 7, 9, 4, 0, 3, 2, 1, 6, 5, 8, 10, 11, 12, 13, 14 },
        };
    }
}

}
