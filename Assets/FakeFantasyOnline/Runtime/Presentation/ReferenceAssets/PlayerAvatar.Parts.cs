using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerAvatar
{
    public int GlowModelOf(int tag)
    {
        if (tag == 5) return _ap.WeaponL;
        if (tag == 6) return _ap.WeaponR;
        return 0;
    }

    private List<(int, string, byte[], byte[], byte[], byte[])> PartsFor(int weaponAction = 0)
    {
        List<(int, string, byte[], byte[], byte[], byte[])> parts =
            new List<(int, string, byte[], byte[], byte[], byte[])>();

        void Add(int bind, string folder, int model)
        {
            if (model < 1) return;
            string gender = _female ? "f" : "m";
            string key = string.Format("P{0}{1}a{2}{3}", folder, model, weaponAction, gender);
            if ((bind == 5 || bind == 6) && weaponAction > 0)
            {
                byte[] action = LoadPart(folder, model, weaponAction, _female);
                if (action != null)
                {
                    parts.Add((bind, key, action, action, action, action));
                    return;
                }
            }
            byte[] standPart = LoadPart(folder, model, 1, _female);
            if (standPart == null) return;
            key = string.Format("P{0}{1}a0{2}", folder, model, gender);
            byte[] walkPart = LoadPart(folder, model, 2, _female) ?? standPart;
            byte[] sitPart = LoadPart(folder, model, 3, _female) ?? standPart;
            byte[] ridePart = LoadPart(folder, model, 13, _female) ?? standPart;
            parts.Add((bind, key, standPart, walkPart, sitPart, ridePart));
        }

        Add(6, "weaponr", _ap.WeaponR);
        Add(5, "weaponl", _ap.WeaponL);
        Add(4, "backpack", _ap.Backpack);
        Add(1, "cap", _ap.Cap);
        Add(2, "face", _ap.Face);
        return parts;
    }

    private List<(int, string, byte[], byte[], byte[], byte[])> GlowPartsFor(int weaponAction = 0)
    {
        List<(int, string, byte[], byte[], byte[], byte[])> parts =
            new List<(int, string, byte[], byte[], byte[], byte[])>();

        void AddG(int bind, string folder, int model)
        {
            if (model < 1) return;
            string gender = _female ? "f" : "m";
            string key = string.Format("G{0}{1}a{2}{3}", folder, model, weaponAction, gender);
            if (weaponAction > 0)
            {
                byte[] action = LoadPart(folder, model, weaponAction, _female, "e");
                if (action == null) return;
                parts.Add((bind, key, action, action, action, action));
                return;
            }
            byte[] standPart = LoadPart(folder, model, 1, _female, "e");
            if (standPart == null) return;
            key = string.Format("G{0}{1}a0{2}", folder, model, gender);
            byte[] walkPart = LoadPart(folder, model, 2, _female, "e") ?? standPart;
            byte[] sitPart = LoadPart(folder, model, 3, _female, "e") ?? standPart;
            byte[] ridePart = LoadPart(folder, model, 13, _female, "e") ?? standPart;
            parts.Add((bind, key, standPart, walkPart, sitPart, ridePart));
        }

        AddG(6, "weaponr", _ap.WeaponR);
        AddG(5, "weaponl", _ap.WeaponL);
        return parts;
    }

    private static byte[] LoadPart(string folder, int model, int action, bool female, string suffix = "")
    {
        int partBase = model * 100;
        int[] ids = female
            ? new[] { partBase + action + 50, partBase + 51, partBase + action, partBase + 1 }
            : new[] { partBase + action, partBase + 1 };
        foreach (int id in ids)
        {
            byte[] bytes = ReadPkg(string.Format("avatar/{0}/{1}{2:D4}{3}.gsn", folder, folder, id, suffix));
            if (bytes != null) return bytes;
        }
        return null;
    }

    private static int Key(int d, int f) => d * 128 + f;

    private static void MergeGlow(Dictionary<int, List<Layer>> main, Dictionary<int, List<Layer>> glow)
    {
        foreach (KeyValuePair<int, List<Layer>> pair in glow)
        {
            List<Layer> layers;
            if (!main.TryGetValue(pair.Key, out layers))
            {
                main[pair.Key] = pair.Value;
                continue;
            }
            foreach (Layer glowLayer in pair.Value)
            {
                int at = layers.Count;
                for (int i = layers.Count - 1; i >= 0; i--)
                {
                    if (layers[i].tag == glowLayer.tag && !layers[i].additive)
                    {
                        at = i + 1;
                        break;
                    }
                }
                layers.Insert(at, glowLayer);
            }
        }
    }
}

}
