using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace FakeFantasyOnline.Application
{
// Non-reflective readers shared by the exact v1 DTOs. No caller-owned arrays escape.
internal static class EndgameData
{
    public static IReadOnlyDictionary<string, object> Object(object value)
    {
        return value as IReadOnlyDictionary<string, object> ?? throw new FormatException("Expected endgame object.");
    }
    public static void Keys(IReadOnlyDictionary<string, object> value, params string[] allowed)
    {
        foreach (string key in value.Keys)
            if (Array.IndexOf(allowed, key) < 0) throw new FormatException("Unknown endgame field: " + key);
    }
    public static string Text(object value, string key, int maximum = 8192)
    {
        if (!(value is string text) || text.Length > maximum) throw new FormatException("Invalid text: " + key);
        return text;
    }
    public static string String(IReadOnlyDictionary<string, object> value, string key, int maximum = 8192)
    {
        return value.TryGetValue(key, out object item) ? Text(item, key, maximum) : string.Empty;
    }
    public static string Identity(object value, string key)
    {
        string text = Text(value, key, 19);
        if (text.Length == 0) return text;
        if (text.Length > 1 && text[0] == '0') throw new FormatException("Noncanonical identity: " + key);
        foreach (char c in text) if (c < '0' || c > '9') throw new FormatException("Nondecimal identity: " + key);
        if (!long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out _)) throw new FormatException("Identity overflow: " + key);
        return text;
    }
    public static string ID(IReadOnlyDictionary<string, object> value, string key)
    {
        return Identity(String(value, key), key);
    }
    public static int Integer(object value, string key)
    {
        if (!(value is long n) || n < int.MinValue || n > int.MaxValue) throw new FormatException("Invalid int32: " + key);
        return (int)n;
    }
    public static int Int(IReadOnlyDictionary<string, object> value, string key)
    {
        return value.TryGetValue(key, out object item) ? Integer(item, key) : 0;
    }
    public static long Long(IReadOnlyDictionary<string, object> value, string key)
    {
        if (!value.TryGetValue(key, out object item)) return 0;
        if (!(item is long n)) throw new FormatException("Invalid int64: " + key);
        return n;
    }
    public static uint UInt(IReadOnlyDictionary<string, object> value, string key)
    {
        if (!value.TryGetValue(key, out object item)) return 0;
        if (!(item is long n) || n < 0 || n > uint.MaxValue) throw new FormatException("Invalid uint32: " + key);
        return (uint)n;
    }
    public static bool Bool(IReadOnlyDictionary<string, object> value, string key)
    {
        if (!value.TryGetValue(key, out object item)) return false;
        if (!(item is bool b)) throw new FormatException("Invalid bool: " + key);
        return b;
    }
    public static T Optional<T>(IReadOnlyDictionary<string, object> data, string key, Func<object, T> parse) where T : class
    {
        return data.TryGetValue(key, out object item) && item != null ? parse(item) : null;
    }
    public static IReadOnlyList<T> List<T>(IReadOnlyDictionary<string, object> data, string key, int maximum, Func<object, T> parse)
    {
        if (!data.TryGetValue(key, out object item) || item == null) return Array.Empty<T>();
        if (!(item is IReadOnlyList<object> values) || values.Count > maximum) throw new FormatException("Invalid list: " + key);
        var rows = new List<T>(values.Count);
        foreach (object value in values) rows.Add(parse(value));
        return new ReadOnlyCollection<T>(rows);
    }
    public static IReadOnlyDictionary<string, string> StringMap(IReadOnlyDictionary<string, object> data, string key)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (data.TryGetValue(key, out object value) && value != null)
        {
            var map = Object(value);
            if (map.Count > 128) throw new FormatException("Too many result fields.");
            foreach (var pair in map) result.Add(Text(pair.Key, key, 64), Text(pair.Value, pair.Key, 2048));
        }
        return new ReadOnlyDictionary<string, string>(result);
    }
}
}
