using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using FakeFantasyOnline.Presentation.ReferenceAssets;

internal static class NativeEffectReuseContract
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags Statics = BindingFlags.Static | BindingFlags.NonPublic;
    private static readonly Type Player = typeof(EffectPlayer);
    private static readonly IDictionary Cache = (IDictionary)Player.GetField("tplCache", Statics).GetValue(null);
    private static readonly IDictionary Aliases = (IDictionary)Player.GetField("LegendaryEffectAliases", Statics).GetValue(null);
    private static int assertions;
    private static int groups;

    private static void Check(bool condition, string label)
    {
        assertions++;
        if (!condition) throw new Exception(label);
    }
    private static void Equal<T>(T expected, T actual, string label)
    { Check(EqualityComparer<T>.Default.Equals(expected, actual), label + ": expected " + expected + ", got " + actual); }
    private static void Same(object expected, object actual, string label)
    { Check(ReferenceEquals(expected, actual), label); }
    private static object Field(object instance, string name)
    { return instance.GetType().GetField(name, Fields).GetValue(instance); }
    private static T Field<T>(object instance, string name) { return (T)Field(instance, name); }
    private static IList Layers(object template) { return Field<IList>(template, "layers"); }
    private static object Invoke(string method, params object[] args)
    { return Player.GetMethod(method, Statics).Invoke(null, args); }
    private static object Load(string name, bool quiet = false) { return Invoke("LoadTemplate", name, quiet); }
    private static object Parse(string text) { return Invoke("Parse", text); }
    private static byte[] Read(string path) { return (byte[])Invoke("ReadRes", new object[] { path }); }
    private static byte[] Bytes(string text) { return Encoding.GetEncoding("ISO-8859-1").GetBytes(text); }
    private static string PathFor(string effect) { return "effect/" + effect + ".eff"; }
    private static void Reset() { Cache.Clear(); ResLoader.Reset(); }
    private static void Put(string effect, string text) { ResLoader.I.Overlay[PathFor(effect)] = Bytes(text); }
    private static void Calls(params string[] expected)
    { Equal(string.Join("|", expected), string.Join("|", ResLoader.Calls), "resource call order"); }
    private static void Pass(string name) { groups++; Console.WriteLine("PASS " + name); }
    private static string Number(float value) { return value.ToString("R", CultureInfo.InvariantCulture); }

    // Synthetic EFF configuration, not extracted/copied game fixtures.
    private static string Layer(string sprite, int marker, params float[] times)
    {
        var frames = new List<string>();
        for (int i = 0; i < times.Length; i++)
            frames.Add("[" + Number(times[i]) + ",[[" + (marker + i + 1) + "," + (i * i - 2) + "," + (i + 3) + "],[1.25,0.75],37,[30,60,90],0.4]]");
        return "[[0,['" + sprite + "',2,175,-3],[4],0,0,[12.5,-7.25]],[" + string.Join(",", frames) + "]]";
    }
    private static string Effect(params string[] layers)
    { return "[0,[" + string.Join(",", layers) + "],[7]]"; }

    private static string Snapshot(object value)
    {
        if (value == null) return "null";
        if (value is string) return JsonSerializer.Serialize((string)value);
        Type type = value.GetType();
        if (type.IsPrimitive || type.IsEnum || value is decimal)
            return value is IFormattable ? ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture) : value.ToString();
        if (value is IEnumerable)
            return "[" + string.Join(",", ((IEnumerable)value).Cast<object>().Select(Snapshot)) + "]";
        return "{" + string.Join(",", type.GetFields(Fields).OrderBy(f => f.Name, StringComparer.Ordinal)
            .Select(f => f.Name + ":" + Snapshot(f.GetValue(value)))) + "}";
    }

    private static void MappingContract()
    {
        Equal(186, Aliases.Count, "complete historical alias count");
        int whole = 0, filtered = 0;
        foreach (DictionaryEntry entry in Aliases)
        {
            string name = (string)entry.Key;
            string original = Field<string>(entry.Value, "OriginalEffect");
            int selector = Field<int>(entry.Value, "SelectedMagicId");
            Check(name.StartsWith("LP_", StringComparison.Ordinal), "historical LP alias name");
            Check(!string.IsNullOrEmpty(original) && !Aliases.Contains(original), "alias must point directly to original " + name);
            Check(selector >= 0, "nonnegative selector " + name);
            Check(entry.Value.GetType().GetField("OriginalEffect", Fields).IsInitOnly, "original field is readonly");
            Check(entry.Value.GetType().GetField("SelectedMagicId", Fields).IsInitOnly, "selector field is readonly");
            if (selector == 0) whole++; else filtered++;
        }
        Equal(53, whole, "whole-original count");
        Equal(133, filtered, "filtered-original count");
        Mapping("LP_ZS_A02_01", "WF0021", 0);
        Mapping("LP_ZS_A02_02", "TFX081_30", 1068);
        Mapping("LP_ZS_A12_01", "TFX1032_40", 322);
        Mapping("LP_ZS_A13_01", "TFX1248_40", 1046);
        Mapping("LP_ZS_U04_01", "TFX1248_40", 330);
        Pass("186 mapping constants: 53 whole, 133 filtered; fixed representative mappings");
    }
    private static void Mapping(string name, string original, int selector)
    {
        Check(Aliases.Contains(name), "mapped alias " + name);
        Equal(original, Field<string>(Aliases[name], "OriginalEffect"), "original for " + name);
        Equal(selector, Field<int>(Aliases[name], "SelectedMagicId"), "selector for " + name);
    }

    private static void WholeOriginalContract()
    {
        foreach (bool originalFirst in new[] { false, true })
        {
            Reset();
            string source = Effect(Layer("magic3007", 0, 0, 150, 300), Layer("magic9000", 20, 0, 1400));
            Put("WF0021", source);
            Put("LP_ZS_A02_01", Effect(Layer("legacy-overlay-must-not-load", 1, 0, 9000)));
            object original = originalFirst ? Load("WF0021") : null;
            object alias = Load("LP_ZS_A02_01", true);
            if (original == null) original = Load("WF0021");
            Same(original, alias, "whole alias directly shares original template");
            Same(Layers(original), Layers(alias), "whole alias directly shares layer list");
            Same(alias, Load("LP_ZS_A02_01"), "alias cache hit");
            Equal(Snapshot(Parse(source)), Snapshot(alias), "whole template parser parity");
            Equal(1500f, Field<float>(alias, "lifeMs"), "whole source lifetime");
            Equal(7, Field<int>(alias, "loopFromTail"), "whole loop setting");
            Same(original, Load("lp_zs_a02_01"), "lowercase known alias resolves native original");
            Same(original, Load("Lp_Zs_A02_01"), "mixed-case known alias resolves native original");
            Calls("overlay:effect/WF0021.eff");
        }
        Pass("whole reuse, source-first/alias-first cache identity, case compatibility, ignored stale LP overlay");
    }

    private static void FilteredContract(string aliasName, string originalName, int selector)
    {
        foreach (bool originalFirst in new[] { false, true })
        {
            Reset();
            string unpadded = "magic" + selector;
            string padded = "magic" + selector.ToString("D5", CultureInfo.InvariantCulture);
            string first = Layer(unpadded, 10, 0, 250, 700);
            string second = Layer(padded, 20, 0, 1000, 400); // maximum is intentionally not the final frame.
            string third = Layer("magic000" + selector, 30, 0, 550);
            string source = Effect(Layer("magic999999", 0, 0, 5000), first,
                Layer(unpadded + "0", 0, 0, 3000), second,
                Layer(unpadded + "x", 0, 0, 3000), Layer("magic+" + selector, 0, 0, 3000),
                Layer("prefix" + unpadded, 0, 0, 3000), third);
            Put(originalName, source);
            Put(aliasName, Effect(Layer("legacy-must-not-load", 0, 0, 9900)));
            object original = originalFirst ? Load(originalName) : null;
            object alias = Load(aliasName);
            if (original == null) original = Load(originalName);
            Check(alias != null && !ReferenceEquals(original, alias), "filtered alias has independent template");
            Check(!ReferenceEquals(Layers(original), Layers(alias)), "filtered alias has independent list");
            Equal(8, Layers(original).Count, "source list not trimmed");
            Equal(3, Layers(alias).Count, "all repeated matching layers retained, no prefix/suffix false positives");
            Same(Layers(original)[1], Layers(alias)[0], "first matching layer reference/order");
            Same(Layers(original)[3], Layers(alias)[1], "padded matching layer reference/order");
            Same(Layers(original)[7], Layers(alias)[2], "third matching layer reference/order");
            Equal(1100f, Field<float>(alias, "lifeMs"), "max retained keyframe plus 100ms tail");
            Equal(5100f, Field<float>(original, "lifeMs"), "source lifetime not shortened");
            Equal(7, Field<int>(alias, "loopFromTail"), "filtered loop retained");
            Equal(Snapshot(Parse(Effect(first, second, third))), Snapshot(alias), "cropped parser parity including private spline arrays");
            Equal(Snapshot(Parse(source)), Snapshot(original), "original unchanged after filtering");
            object layer = Layers(alias)[0];
            Equal(2, Field<int>(layer, "blend"), "blend");
            Equal(4, Field<int>(layer, "blendMode"), "blendMode");
            Equal(175f, Field<float>(layer, "speed"), "speed");
            Equal(12.5f, Field<float>(layer, "offX"), "offX");
            Equal(-7.25f, Field<float>(layer, "offY"), "offY");
            Equal(-3f, Field<float>(layer, "cField"), "cField");
            Same(Field(Layers(original)[1], "kfs"), Field(layer, "kfs"), "keyframe list preserved by reference");
            foreach (string spline in new[] { "sx", "sy", "sz" })
            {
                Check(Field(layer, spline) != null, spline + " exists for three-keyframe layer");
                Same(Field(Layers(original)[1], spline), Field(layer, spline), spline + " preserved by reference");
            }
            Same(alias, Load(aliasName, true), "filtered alias cache hit including quiet=true");
            Equal(Snapshot(alias), Snapshot(Load(aliasName.ToLowerInvariant())), "case-insensitive filtered alias");
            Calls("overlay:" + PathFor(originalName));
        }
    }

    private static void SiblingCacheContract()
    {
        string[] names = { "TFX1248_40", "LP_ZS_A13_01", "LP_ZS_U04_01", "LP_ZS_A22_01" };
        int[][] orders = { new[] { 0, 1, 2, 3 }, new[] { 1, 2, 0, 3 }, new[] { 2, 3, 1, 0 } };
        foreach (int[] order in orders)
        {
            Reset();
            Put(names[0], Effect(Layer("magic1046", 1, 0, 500), Layer("magic0330", 2, 0, 900),
                Layer("magic1046", 3, 0, 700), Layer("magic9999", 4, 0, 3000)));
            object[] loaded = new object[4];
            foreach (int i in order) loaded[i] = Load(names[i]);
            Equal(4, Layers(loaded[0]).Count, "sibling source layers remain complete");
            Equal(2, Layers(loaded[1]).Count, "1046 sibling retains repeated layers");
            Equal(1, Layers(loaded[2]).Count, "330 sibling excludes 1046");
            Equal(2, Layers(loaded[3]).Count, "second 1046 alias retains repeated layers");
            Same(Layers(loaded[0])[0], Layers(loaded[1])[0], "1046 shares source layer");
            Same(Layers(loaded[0])[1], Layers(loaded[2])[0], "330 shares source layer");
            Same(Layers(loaded[1])[0], Layers(loaded[3])[0], "equal selectors share source layer");
            Check(!ReferenceEquals(loaded[1], loaded[3]), "each filtered alias has its own template");
            Check(!ReferenceEquals(Layers(loaded[1]), Layers(loaded[3])), "each filtered alias has its own list");
            Equal(800f, Field<float>(loaded[1], "lifeMs"), "1046 sibling lifetime");
            Equal(1000f, Field<float>(loaded[2], "lifeMs"), "330 sibling lifetime");
            Equal(3100f, Field<float>(loaded[0], "lifeMs"), "native sibling lifetime remains complete");
            Calls("overlay:" + PathFor(names[0]));
            // Deliberately mutate a test-only alias list to prove list isolation, then discard caches.
            Layers(loaded[1]).Clear();
            Equal(4, Layers(loaded[0]).Count, "test-only list edit cannot trim native cache");
            Equal(2, Layers(loaded[3]).Count, "test-only list edit cannot trim sibling cache");
        }
        Pass("different selectors and repeated aliases share one source read without cache/list contamination");
    }

    private static void ExhaustiveSyntheticContract()
    {
        foreach (DictionaryEntry entry in Aliases)
        {
            Reset();
            string name = (string)entry.Key;
            string sourceName = Field<string>(entry.Value, "OriginalEffect");
            int selector = Field<int>(entry.Value, "SelectedMagicId");
            int magic = selector == 0 ? 42 : selector;
            Put(sourceName, Effect(Layer("magic" + magic, 1, 0, 200), Layer("magic999999", 2, 0, 900)));
            Put(name, Effect(Layer("legacy-only", 0, 0, 9000)));
            object alias = Load(name);
            object source = Load(sourceName);
            Check(alias != null, "every mapping resolves " + name);
            Equal(selector == 0 ? 2 : 1, Layers(alias).Count, "every mapping selector " + name);
            Equal(selector == 0 ? 1000f : 300f, Field<float>(alias, "lifeMs"), "every mapping lifetime " + name);
            if (selector == 0) Same(source, alias, "every whole mapping shares source " + name);
            else Same(Layers(source)[0], Layers(alias)[0], "every filtered mapping shares layer " + name);
            Calls("overlay:" + PathFor(sourceName));
        }
        Pass("all 186 alias names execute production loader/selector against synthetic originals");
    }

    private static void MissingContract()
    {
        foreach (string alias in new[] { "LP_ZS_A02_01", "LP_ZS_A02_02" })
        {
            Reset();
            string source = Field<string>(Aliases[alias], "OriginalEffect");
            Put(alias, Effect(Layer("stale-lp-only", 0, 0, 600)));
            Equal<object>(null, Load(alias), "missing original returns null instead of legacy LP " + alias);
            Check(Cache.Contains(alias) && Cache.Contains(source), "both misses are cached");
            int reads = ResLoader.Calls.Count;
            Put(source, Effect(Layer("magic1068", 0, 0, 100)));
            Equal<object>(null, Load(alias, true), "cached alias miss remains stable");
            Equal<object>(null, Load(source), "cached native miss remains stable");
            Equal(reads, ResLoader.Calls.Count, "negative cache avoids repeat reads");
            Check(!ResLoader.Calls.Any(c => c.EndsWith(PathFor(alias), StringComparison.Ordinal)), "never probes mapped LP file on source miss");
        }
        Reset();
        Put("TFX081_30", Effect(Layer("magic10680", 0, 0, 400), Layer("magic1068x", 1, 0, 800)));
        Put("LP_ZS_A02_02", Effect(Layer("magic1068", 0, 0, 800)));
        Equal<object>(null, Load("LP_ZS_A02_02"), "missing selected id never falls back to unrelated layers or LP");
        object original = Load("TFX081_30");
        Equal(2, Layers(original).Count, "nonmatching original remains usable");
        Equal(900f, Field<float>(original, "lifeMs"), "nonmatching original timing unchanged");
        Equal<object>(null, Load("LP_ZS_A02_02"), "selected-id miss cached");
        Calls("overlay:effect/TFX081_30.eff");
        Reset();
        Put("TFX081_30", "[0,[]]");
        Equal<object>(null, Load("LP_ZS_A02_02"), "empty original has no selected layers");
        Reset();
        Put("TFX081_30", "[0]");
        Equal<object>(null, Load("LP_ZS_A02_02"), "unparseable original returns null");
        Pass("missing original, missing selector, malformed/empty source and negative cache never fall back");
    }

    private static void NativeAndResourceContract()
    {
        const string name = "ordinary-native", path = "effect/ordinary-native.eff";
        byte[] patch = Bytes(Effect(Layer("overlay", 1, 0, 100)));
        byte[] q21 = Bytes(Effect(Layer("q21", 2, 0, 200)));
        byte[] current = Bytes(Effect(Layer("05", 3, 0, 300)));
        Reset();
        ResLoader.I = null;
        Equal<byte[]>(null, Read(path), "no resource loader");
        Equal<byte[]>(null, Read(null), "no loader returns before path dereference");
        Reset();
        ResLoader.I.Overlay[path] = patch;
        ResLoader.I.Pkg24.Data[path] = q21;
        ResLoader.I.Pkg.Data[path] = current;
        Same(patch, Read(path), "overlay beats both packages");
        Calls("overlay:" + path);
        Reset();
        ResLoader.I.Pkg24.Data[path] = q21;
        ResLoader.I.Pkg.Data[path] = current;
        Same(q21, Read(path), "default q21 beats different 05 bytes");
        Calls("overlay:" + path, "overlay:" + path, "ff21:" + path, "q21:" + path);
        Reset();
        ResLoader.I.PkgFf21.Data[path] = q21;
        ResLoader.I.Pkg24.Data[path] = current;
        Same(q21, Read(path), "ff21 precedes q21");
        Calls("overlay:" + path, "overlay:" + path, "ff21:" + path);
        Reset();
        ResLoader.I.Pkg.Data[path] = current;
        Same(current, Read(path), "default falls through missing q21 to 05");
        Calls("overlay:" + path, "overlay:" + path, "ff21:" + path, "q21:" + path, "q21:" + path, "05:" + path);
        Reset();
        SkillEffectDB.Use05Fx = true;
        ResLoader.I.Pkg.Data[path] = current;
        ResLoader.I.Pkg24.Data[path] = q21;
        Same(current, Read(path), "05 branch prefers 05");
        Calls("overlay:" + path, "05:" + path);
        Reset();
        SkillEffectDB.Use05Fx = true;
        ResLoader.I.Pkg24.Data[path] = q21;
        Same(q21, Read(path), "05 branch falls back to q21");
        Calls("overlay:" + path, "05:" + path, "q21:" + path);
        Reset();
        SkillEffectDB.Use05Fx = true;
        ResLoader.I.Pkg = null;
        ResLoader.I.Pkg24 = null;
        Equal<byte[]>(null, Read(path), "missing package handles return null");
        Calls("overlay:" + path);
        Reset();
        const string focused = "effect/聚气-test.eff";
        ResLoader.I.Pkg.Data[focused] = current;
        ResLoader.I.Pkg24.Data[focused] = q21;
        Same(current, Read(focused), "focused effect retains current-package priority");
        Calls("overlay:" + focused, "05:" + focused);
        Reset();
        ResLoader.I.Pkg24.Data[focused] = q21;
        Same(q21, Read(focused), "focused miss uses normal fallback");
        Calls("overlay:" + focused, "05:" + focused, "overlay:" + focused, "ff21:" + focused, "q21:" + focused);
        Reset();
        bool nullFailed = false;
        try { Read(null); }
        catch (TargetInvocationException e) { nullFailed = e.InnerException is NullReferenceException; }
        Check(nullFailed, "existing null-path Contains failure remains unchanged after overlay miss");
        Calls("overlay:");
        Reset();
        ResLoader.I.Pkg24.Data[path] = q21;
        object native = Load(name);
        Equal("q21", Field<string>(Layers(native)[0], "sprite"), "ordinary native loads q21 bytes");
        Same(native, Load(name), "ordinary native cached");
        Calls("overlay:" + path, "overlay:" + path, "ff21:" + path, "q21:" + path);
        Reset();
        Put("LP_UNLISTED_01", Effect(Layer("unknown-lp-fallback", 0, 0, 450)));
        Equal("unknown-lp-fallback", Field<string>(Layers(Load("LP_UNLISTED_01"))[0], "sprite"), "unknown LP still loads its own EFF");
        Calls("overlay:effect/LP_UNLISTED_01.eff");
        Reset();
        Put("补血", Effect(Layer("blood-file", 0, 0, 100)));
        Put("补魔法", Effect(Layer("magic-file", 0, 0, 200)));
        Equal("magic-file", Field<string>(Layers(Load("补血"))[0], "sprite"), "blood name swap retained");
        Equal("blood-file", Field<string>(Layers(Load("补魔法"))[0], "sprite"), "magic name swap retained");
        Calls("overlay:effect/补魔法.eff", "overlay:effect/补血.eff");
        Pass("actual ReadRes and production resolver precedence, distinct q21/05 bytes, native/unknown LP/name-swap fallbacks");
    }

    private static void AliasPackagePriorityContract()
    {
        const string alias = "LP_SS_A02_01", original = "TFX036_40";
        Mapping(alias, original, 289);
        foreach (bool use05 in new[] { false, true })
        {
            Reset();
            SkillEffectDB.Use05Fx = use05;
            string path = PathFor(original);
            ResLoader.I.Pkg24.Data[path] = Bytes(Effect(Layer("magic0289", 21, 0, 2100)));
            ResLoader.I.Pkg.Data[path] = Bytes(Effect(Layer("magic289", 5, 0, 500)));
            ResLoader.I.Overlay[PathFor(alias)] = Bytes(Effect(Layer("legacy-overlay", 0, 0, 9900)));
            object loaded = Load(alias);
            Equal(use05 ? 600f : 2200f, Field<float>(loaded, "lifeMs"), "mapped alias obeys native package preference");
            Same(Layers(Load(original))[0], Layers(loaded)[0], "mapped alias uses selected package's source layer");
            if (use05) Calls("overlay:" + path, "05:" + path);
            else Calls("overlay:" + path, "overlay:" + path, "ff21:" + path, "q21:" + path);
        }
        Reset();
        string originalPath = PathFor(original);
        ResLoader.I.Pkg24.Data[originalPath] = Bytes(Effect(Layer("magic0289", 21, 0, 2100)));
        Put(original, Effect(Layer("magic289", 99, 0, 900)));
        Equal(1000f, Field<float>(Load(alias), "lifeMs"), "original-name overlay still wins for mapped alias");
        Calls("overlay:" + originalPath);
        Pass("mapped alias resolves original overlay/q21/05 instead of hardcoding a package or consulting stale LP");
    }

    private static void PrivateCorpusContract(string file)
    {
        using (JsonDocument document = JsonDocument.Parse(File.ReadAllText(file)))
        {
            Equal(186, document.RootElement.GetArrayLength(), "private corpus size");
            int shortened = 0, whole = 0, filtered = 0;
            foreach (JsonElement row in document.RootElement.EnumerateArray())
            {
                string name = row.GetProperty("alias").GetString();
                string sourceName = row.GetProperty("original").GetString();
                int selector = row.GetProperty("selectedMagicId").GetInt32();
                Mapping(name, sourceName, selector);
                object expected = Parse(row.GetProperty("expectedText").GetString());
                foreach (bool sourceFirst in new[] { false, true })
                {
                    Reset();
                    ResLoader.I.Pkg24.Data[PathFor(sourceName)] = Bytes(row.GetProperty("sourceText").GetString());
                    Put(name, Effect(Layer("legacy-bait-must-not-be-read", 0, 0, 9000)));
                    object original = sourceFirst ? Load(sourceName) : null;
                    object alias = Load(name);
                    if (original == null) original = Load(sourceName);
                    Check(alias != null, "private corpus alias exists " + name);
                    Equal(Snapshot(expected), Snapshot(alias), "actual approved wrapper parser parity " + name);
                    if (selector == 0) Same(original, alias, "private exact template identity " + name);
                    else
                    {
                        Check(!ReferenceEquals(original, alias) && !ReferenceEquals(Layers(original), Layers(alias)), "private filter isolation " + name);
                        foreach (object layer in Layers(alias))
                            Check(Layers(original).Cast<object>().Any(candidate => ReferenceEquals(candidate, layer)), "private layer reference preserved " + name);
                    }
                    Same(alias, Load(name), "private alias cached " + name);
                    Calls("overlay:" + PathFor(sourceName), "overlay:" + PathFor(sourceName), "ff21:" + PathFor(sourceName), "q21:" + PathFor(sourceName));
                    if (!sourceFirst && Field<float>(alias, "lifeMs") != Field<float>(original, "lifeMs")) shortened++;
                }
                if (selector == 0) whole++; else filtered++;
            }
            Equal(53, whole, "private exact count");
            Equal(133, filtered, "private filtered count");
            Console.WriteLine("PASS private corpus: 186 aliases x both cache orders deeply equal approved wrappers (all fields, keyframes, spline arrays); " + shortened + " shortened lifetimes");
            groups++;
        }
    }

    public static int Main(string[] args)
    {
        try
        {
            MappingContract();
            WholeOriginalContract();
            FilteredContract("LP_ZS_A02_02", "TFX081_30", 1068);
            FilteredContract("LP_ZS_A12_01", "TFX1032_40", 322);
            Pass("padded/unpadded numeric matching, repeated layer order, metadata/keyframes/splines, loop and recomputed lifetime");
            SiblingCacheContract();
            ExhaustiveSyntheticContract();
            MissingContract();
            NativeAndResourceContract();
            AliasPackagePriorityContract();
            if (args.Length != 0) PrivateCorpusContract(args[0]);
            Console.WriteLine("PASS " + groups + " groups, " + assertions + " assertions against production EffectPlayer.cs + LegendaryAliases.cs");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }
}
