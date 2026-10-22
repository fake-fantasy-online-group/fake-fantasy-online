using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace UnityEngine
{
    public class TextAsset { public byte[] bytes; }
    public static class Resources { public static T Load<T>(string name) where T : class => null; }
    public static class Debug { public static void LogWarning(string value) { } }
}

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    internal static class LoadTrace
    {
        public static long Now => 0;
        public static bool On => false;
        public static float Ms(long time) => 0;
        public static void Pkg(string name, string source, int packed, int original, float read, float inflate) { }
    }

    // Only Unity/component/cache ownership is substituted. Both complete production
    // ObjectPaths and PkgArchive sources (including mounts and binary index parsing) run.
    public partial class ResLoader
    {
        public PkgArchive Pkg { get; set; }
        private readonly Dictionary<int, string> objPathCache = new Dictionary<int, string>();
        private readonly Dictionary<int, string> set2city = new Dictionary<int, string>();
        internal Dictionary<int, string> Cities => set2city;
        internal static bool ObjectName(int index, out string name) => MapObjNames.TryGetValue(index, out name);
    }
}

internal static class ObjectCityIndexProbe
{
    internal static long IndexKeys, LegacyKeys;
}

internal static class ObjectCityIndexContract
{
    private static int checks;
    private static readonly FieldInfo SnapshotField = typeof(PkgArchive).GetField(
        "objectCandidateIndex", BindingFlags.NonPublic | BindingFlags.Instance);

    private static void Check(bool passed, string label)
    {
        checks++;
        if (!passed) throw new Exception(label);
    }

    private static object Snapshot(PkgArchive archive) => SnapshotField.GetValue(archive);
    private static int Image(int city, int kind, int number) => unchecked((city << 24) | (kind << 16) | number);

    private static string Package(string root, string name, IEnumerable<string> paths, byte payload = 1)
    {
        string file = Path.Combine(root, name);
        Directory.CreateDirectory(Path.GetDirectoryName(file));
        byte[][] names = paths.Select(Encoding.ASCII.GetBytes).ToArray();
        int indexSize = names.Sum(value => value.Length + 18);
        using (var stream = File.Create(file))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(0u);
            writer.Write((uint)names.Length);
            writer.Write(16u);
            writer.Write((uint)indexSize);
            for (int i = 0; i < names.Length; i++)
            {
                writer.Write((ushort)names[i].Length);
                writer.Write(names[i]);
                writer.Write(0u);
                writer.Write((uint)(16 + indexSize + i));
                writer.Write(1u);
                writer.Write(1u);
            }
            foreach (byte[] unused in names) writer.Write(payload);
        }
        return file;
    }

    private static void Compare(PkgArchive archive, string kind, params int[] numbers)
    {
        foreach (int number in numbers)
        {
            var expected = archive.FindAll("object/" + kind + "/", string.Format("_{0:D4}.gso", number));
            var actual = archive.FindObjectCandidates(kind, number);
            Check(expected.SequenceEqual(actual), "Same candidate bytes, casing and order: " + kind + "/" + number);
        }
    }

    // Oracle is the old BuildCityTable algorithm: full FindAll query per distinct
    // kind/number, then a vote for every occurrence in the input, not every distinct ID.
    private static Dictionary<int, string> LegacyCities(PkgArchive archive, IEnumerable<int> imageIds)
    {
        var candidatesByObject = new Dictionary<long, List<string>>();
        var countsByCity = new Dictionary<int, Dictionary<string, int>>();
        foreach (int imageId in imageIds)
        {
            int kind = (imageId >> 16) & 0xff;
            string name;
            if (!ResLoader.ObjectName(kind, out name)) continue;
            long key = ((long)kind << 32) | (uint)(imageId & 0xffff);
            List<string> cities;
            if (!candidatesByObject.TryGetValue(key, out cities))
            {
                cities = new List<string>();
                foreach (string path in archive.FindAll("object/" + name + "/", string.Format("_{0:D4}.gso", imageId & 0xffff)))
                {
                    string[] parts = path.Substring(path.LastIndexOf('/') + 1).Split('_');
                    if (parts.Length > 2) cities.Add(parts[1]);
                }
                candidatesByObject[key] = cities;
            }
            int city = (int)((uint)imageId >> 24);
            Dictionary<string, int> counts;
            if (!countsByCity.TryGetValue(city, out counts)) countsByCity[city] = counts = new Dictionary<string, int>();
            foreach (string candidate in cities)
            {
                int count;
                counts.TryGetValue(candidate, out count);
                counts[candidate] = count + 1;
            }
        }
        var result = new Dictionary<int, string>();
        foreach (var city in countsByCity)
        {
            string best = null;
            int bestCount = 0;
            foreach (var candidate in city.Value)
                if (candidate.Value > bestCount) { best = candidate.Key; bestCount = candidate.Value; }
            if (best != null) result[city.Key] = best;
        }
        return result;
    }

    private static void CompareCities(ResLoader loader, int[] ids, string label)
    {
        var expected = LegacyCities(loader.Pkg, ids);
        loader.BuildCityTable(ids);
        Check(expected.Count == loader.Cities.Count && expected.All(pair =>
            loader.Cities.TryGetValue(pair.Key, out string city) && city == pair.Value), label);
    }

    private static void SemanticsAndMutation(string root)
    {
        var archive = new PkgArchive();
        string[] paths = {
            "object/CC/CC_A_0001.gso", "OBJECT/cc/deep/CC_B_0001.GSO",
            "object/CC/deep/deeper/X_C_0001.gso", "object/CC/CC_A_00001.gso",
            "object/CC/CC_B_0002.gso", "object/CC/CC_a_0003.gso",
            "object/CC/CC_A_0004.gso", "object/CC/CC_T_0000.gso",
            "object/CC/CC_T_9999.gso", "object/CC/CC_T_10000.gso",
            "object/CC/CC_T_65535.gso", "object/CC/CC_T_065535.gso",
            "object/CC/CC_T_003.gso", "object/CCP/CCP_T_0001.gso",
            "object/CC/_0005.gso", "object/CC/odd_name_0006.gso",
            "object/CC/CC__0007.gso", "object/CC/dir_0001.gso/file.gso",
            "object/CC/CC_T_0001.gso.bak", "object/CC/CC_T_0001.gsm",
            "notobject/CC/CC_T_0001.gso", "object/CCextra/CC_T_0001.gso",
            "object\\CC\\deep\\CC_D_0001.gso", "", "object/CC", "object/CC/no_underscores.gso"
        };
        string first = Package(root, "first.pkg", paths);
        Check(archive.MountPkgs(new[] { first }), "Production package mount");
        Compare(archive, "CC", 0, 1, 2, 3, 4, 5, 6, 7, 8, 9999, 10000, 65535);
        Compare(archive, "cc", 1, 2, 65535);
        Compare(archive, "CCP", 1, 2);
        Compare(archive, "missing", 1);
        object initial = Snapshot(archive);
        Check(initial != null, "First lookup builds one snapshot");
        Compare(archive, "CC", 1, 8);
        Check(ReferenceEquals(initial, Snapshot(archive)), "Hit and miss reuse the mounted snapshot");
        var readOnly = archive.FindObjectCandidates("CC", 1) as IList<string>;
        Check(readOnly != null && readOnly.IsReadOnly, "Candidate list cannot be changed by consumers");
        Check(archive.FindUnique("object/CC/", "_0001.gso") == null, "Ambiguous FindUnique is unchanged");
        Check(archive.FindUnique("object/CC/", "_0002.gso") == "object/CC/CC_B_0002.gso", "Unique FindUnique is unchanged");

        var loader = new ResLoader { Pkg = archive };
        CompareCities(loader, new[] { Image(90, 1, 1) }, "Tie winner and nested candidate ordering match");
        Check(loader.Cities[90] == "A", "First original candidate wins equal city counts");
        CompareCities(loader, new[] { Image(90, 1, 3), Image(90, 1, 4), Image(90, 1, 4) }, "Case-sensitive city vote buckets match");
        Check(loader.Cities[90] == "A", "City codes differing by case are not merged");
        CompareCities(loader, new[] { Image(90, 1, 1), Image(90, 1, 2), Image(90, 1, 2) }, "Repeated input vote weight matches");
        Check(loader.Cities[90] == "B", "Duplicate placements still add votes");
        CompareCities(loader, new[] { Image(90, 1, 5), Image(91, 1, 7), Image(92, 255, 1), Image(93, 1, 65000) }, "Malformed filenames, empty city and missing kind/path match");
        CompareCities(loader, Array.Empty<int>(), "Each map clears old city votes");
        var random = new Random(104729);
        for (int pass = 0; pass < 80; pass++)
        {
            int[] ids = Enumerable.Range(0, 60).Select(_ => Image(90 + random.Next(5), 1 + random.Next(3), random.Next(12))).ToArray();
            CompareCities(loader, ids, "Random ordered voting differential " + pass);
        }

        string newPackage = Package(root, "new.pkg", new[] { "object/CC/CC_New_0001.gso", "object/CC/CC_New_0008.gso" }, 2);
        Check(archive.MountPkgs(new[] { newPackage }), "Additional package mounts");
        Check(Snapshot(archive) == null, "Index mutation invalidates cached hit and miss together");
        Compare(archive, "CC", 1, 8);
        Check(!ReferenceEquals(initial, Snapshot(archive)), "New revision replaces the snapshot");
        Check(archive.FindObjectCandidates("CC", 8).Count == 1, "Former miss resolves after mutation");
        object revised = Snapshot(archive);
        Check(archive.MountPkgs(new[] { first }), "Repeated first-wins mount succeeds");
        Check(ReferenceEquals(revised, Snapshot(archive)), "No-op first-wins mount needs no rebuild");

        Package(root, "legacy/objects-new.pkg", new[] { "OBJECT/cc/CC_A_0001.GSO" }, 9);
        Check(archive.Mount(new[] { Path.Combine(root, "legacy") }), "Legacy last-wins mount succeeds");
        Check(Snapshot(archive) == null, "Overwriting an existing entry invalidates snapshot");
        Check(archive.Read("object/CC/CC_A_0001.gso")[0] == 9, "Legacy last-wins byte source unchanged");
        Compare(archive, "CC", 1);
        string loose = Path.Combine(root, "loose");
        Directory.CreateDirectory(Path.Combine(loose, "object/CC"));
        File.WriteAllText(Path.Combine(loose, "object/CC/CC_Loose_0009.gso"), "loose");
        archive.FindObjectCandidates("CC", 9);
        Check(archive.MountLooseDirs(new[] { loose }), "Loose directory mounts through production code");
        Check(Snapshot(archive) == null, "Loose successful insertion invalidates snapshot");
        Compare(archive, "CC", 1, 8, 9);

        var separate = new PkgArchive();
        separate.MountPkgs(new[] { newPackage });
        Compare(separate, "CC", 1, 2, 8);
        Check(!ReferenceEquals(Snapshot(archive), Snapshot(separate)), "New archive never reuses old mount state");
    }

    private static void BoundedWork(string root)
    {
        var paths = new List<string>();
        for (int i = 0; i < 30000; i++) paths.Add("res/unrelated/asset" + i + ".gsa");
        for (int i = 0; i < 2000; i++) paths.Add(string.Format("object/CC/CC_{0}_{1:D4}.gso", (i & 1) == 0 ? "A" : "B", i));
        var archive = new PkgArchive();
        archive.MountPkgs(new[] { Package(root, "large.pkg", paths) });
        var loader = new ResLoader { Pkg = archive };
        ObjectCityIndexProbe.IndexKeys = ObjectCityIndexProbe.LegacyKeys = 0;
        loader.BuildCityTable(Enumerable.Range(0, 1000).Select(i => Image(90, 1, i)));
#if INDEX_PROBES
        Check(ObjectCityIndexProbe.IndexKeys == archive.FileCount, "First map builds candidates in exactly one full index pass");
        Check(ObjectCityIndexProbe.LegacyKeys == 0, "First map avoids the original per-object full scans");
#endif
        object first = Snapshot(archive);
        for (int map = 0; map < 30; map++)
        {
            // Other half of the object catalog is cold on the second map.
            loader.BuildCityTable(Enumerable.Range((map & 1) * 1000, 1000).Select(i => Image(90, 1, i)));
            Check(ReferenceEquals(first, Snapshot(archive)), "Repeated maps retain one derived snapshot " + map);
        }
#if INDEX_PROBES
        Check(ObjectCityIndexProbe.IndexKeys == archive.FileCount, "Exactly one full index pass, not one per object or map");
        Check(ObjectCityIndexProbe.LegacyKeys == 0, "BuildCityTable never enters legacy FindAll full-index loop");
        archive.MountPkgs(new[] { Package(root, "large-added.pkg", new[] { "object/CC/CC_New_65535.gso" }) });
        loader.BuildCityTable(new[] { Image(90, 1, 65535) });
        Check(ObjectCityIndexProbe.IndexKeys == 32000 + 32001, "One additional full pass after an actual mount mutation");
        Check(ObjectCityIndexProbe.LegacyKeys == 0, "Mutation rebuild does not restore per-object scans");
        Console.WriteLine("WORK: 32,000 keys; 31 maps x 1,000 objects; one 32,000-key pass, zero legacy scans; one rebuild after mutation.");
#endif
    }

    private static int Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "ffo-object-city-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            SemanticsAndMutation(root);
            BoundedWork(root);
            Console.WriteLine("PASS object city index: " + checks + " checks. Actual package/mount/path/voting sources; Unity ownership stubbed. No Android/FPS claim.");
            return 0;
        }
        finally { Directory.Delete(root, true); }
    }
}
