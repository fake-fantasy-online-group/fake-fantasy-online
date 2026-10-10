using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerAvatar
{
    private static string CapFile(string path)
    {
        int slash = path.LastIndexOf('/');
        if (slash < 0)
            return char.ToUpper(path[0]).ToString() + path.Substring(1);

        return path.Substring(0, slash + 1) +
            char.ToUpper(path[slash + 1]).ToString() + path.Substring(slash + 2);
    }

    private static void LoadWeaponDepth()
    {
        if (_depth != null) return;
        _depth = new int[7][][][];
        for (int part = 0; part < 7; part++)
        {
            _depth[part] = new int[3][][];
            for (int action = 0; action < 3; action++)
            {
                _depth[part][action] = new int[5][];
                _depth[part][action][0] = new int[8];
                _depth[part][action][1] = new int[8];
                _depth[part][action][2] = new int[8];
                _depth[part][action][3] = new int[8];
                _depth[part][action][4] = new int[8];
            }

            string path = _partIni[part];
            byte[] raw = ReadPkg("avatar/" + path + ".ini");
            if (raw == null)
            {
                raw = ReadPkg("avatar/" + CapFile(path) + ".ini");
                if (raw == null) continue;
            }

            StringBuilder builder = new StringBuilder(raw.Length);
            for (int i = 0; i < raw.Length; i++) builder.Append((char)raw[i]);
            int pose = -1;
            int dir = -1;
            foreach (string source in builder.ToString().Split('\n'))
            {
                string line = source.Trim();
                if (line.StartsWith("["))
                {
                    pose = line == "[stand]" ? 0 : line == "[run]" ? 1 : line == "[sit]" ? 2 : -1;
                    dir = -1;
                }
                else if (line.StartsWith("(dir"))
                {
                    int parsed;
                    int.TryParse(line.Substring(4, line.IndexOf(')') - 4), out parsed);
                    dir = unchecked(parsed - 1);
                }
                else if (pose >= 0 && (uint)dir < 5 && line.Length > 0 &&
                    (char.IsDigit(line[0]) || line[0] == '-'))
                {
                    string[] tokens = line.Split(',');
                    for (int frame = 0; frame < 8 && frame + 1 < tokens.Length; frame++)
                        int.TryParse(tokens[frame + 1].Trim(), out _depth[part][pose][dir][frame]);
                    dir = -1;
                }
            }
        }
    }

    public Pose StandAt(int dir, float timeSec)
    {
        if (stand == null) return null;
        dir = Mathf.Clamp(dir, 0, 4);
        if (standAnim != null && dir < standAnim.Length)
        {
            Pose[] frames = standAnim[dir];
            if (frames != null && frames.Length > 1)
            {
                float time = Mathf.Max(0f, timeSec);
                int wingCount = Mathf.Max(1, standWingFpd);
                int bodyFrame = standBodyFpd > 1 ? (int)(time / StandFrameSec) % standBodyFpd : 0;
                int wingFrame = standWingFpd > 1 ? (int)(time / WingStandFrameSec) % wingCount : 0;
                int frame = unchecked(bodyFrame * wingCount + wingFrame);
                if (frame >= frames.Length) frame %= frames.Length;
                Pose result = frames[frame];
                if (result != null) return result;
            }
        }
        return stand[dir];
    }
}

}
