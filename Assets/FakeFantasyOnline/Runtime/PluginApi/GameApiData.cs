using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace FakeFantasyOnline.PluginApi
{

/// <summary>Validates and freezes JSON-shaped data without retaining caller-owned mutable collections.</summary>
public static class GameApiData
{
    public const int MaxDepth = 32;
    public const int MaxNodes = 65536;
    public const int MaxStringLength = 262144;
    public const int MaxAggregateStringLength = 1048576;

    public static IReadOnlyDictionary<string, object> CopyObject(IReadOnlyDictionary<string, object> value)
    {
        if (value == null) return new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(StringComparer.Ordinal));
        return (IReadOnlyDictionary<string, object>)new Copier().Copy(value, 0);
    }

    public static object CopyValue(object value) { return new Copier().Copy(value, 0); }

    internal static string RequireName(string value, string parameter)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A non-empty name is required.", parameter);
        return CheckText(value, parameter);
    }

    internal static string CheckText(string value, string parameter)
    {
        if (value == null) return null;
        if (value.Length > MaxStringLength) throw new ArgumentException("Text exceeds the character limit.", parameter);
        for (int i = 0; i < value.Length; i++)
        {
            if (char.IsHighSurrogate(value[i]))
            {
                if (++i >= value.Length || !char.IsLowSurrogate(value[i]))
                    throw new ArgumentException("Text contains an unpaired Unicode surrogate.", parameter);
            }
            else if (char.IsLowSurrogate(value[i])) throw new ArgumentException("Text contains an unpaired Unicode surrogate.", parameter);
        }
        return value;
    }

    private sealed class ReferenceComparer : IEqualityComparer<object>
    {
        public new bool Equals(object x, object y) { return ReferenceEquals(x, y); }
        public int GetHashCode(object value) { return RuntimeHelpers.GetHashCode(value); }
    }

    private sealed class Copier
    {
        private readonly HashSet<object> active = new HashSet<object>(new ReferenceComparer());
        private int nodes;
        private int characters;

        private string Text(string value)
        {
            CheckText(value, nameof(value));
            characters += value.Length;
            if (characters > MaxAggregateStringLength) throw new ArgumentException("JSON data exceeds the text limit.");
            return value;
        }

        public object Copy(object value, int depth)
        {
            if (depth > MaxDepth) throw new ArgumentException("JSON data exceeds the nesting limit.");
            if (++nodes > MaxNodes) throw new ArgumentException("JSON data exceeds the item limit.");
            if (value == null || value is bool) return value;
            if (value is string text) return Text(text);
            if (value is byte || value is sbyte || value is short || value is ushort || value is int || value is uint ||
                value is long || value is ulong || value is decimal) return value;
            if (value is double d)
            {
                if (double.IsNaN(d) || double.IsInfinity(d)) throw new ArgumentException("JSON numbers must be finite.");
                return d;
            }
            if (value is float f)
            {
                if (float.IsNaN(f) || float.IsInfinity(f)) throw new ArgumentException("JSON numbers must be finite.");
                return f;
            }
            if (!active.Add(value)) throw new ArgumentException("JSON data must not contain cycles.");
            try
            {
                if (value is IReadOnlyDictionary<string, object> readOnly)
                {
                    var copy = new Dictionary<string, object>(StringComparer.Ordinal);
                    foreach (var pair in readOnly)
                    {
                        if (pair.Key == null) throw new ArgumentException("JSON object keys must be strings.");
                        copy.Add(Text(pair.Key), Copy(pair.Value, depth + 1));
                    }
                    return new ReadOnlyDictionary<string, object>(copy);
                }
                if (value is IDictionary dictionary)
                {
                    var copy = new Dictionary<string, object>(StringComparer.Ordinal);
                    foreach (DictionaryEntry pair in dictionary)
                    {
                        if (!(pair.Key is string key)) throw new ArgumentException("JSON object keys must be strings.");
                        copy.Add(Text(key), Copy(pair.Value, depth + 1));
                    }
                    return new ReadOnlyDictionary<string, object>(copy);
                }
                if (value is IList list)
                {
                    var copy = new List<object>();
                    foreach (var item in list) copy.Add(Copy(item, depth + 1));
                    return new ReadOnlyCollection<object>(copy);
                }
                if (value is IReadOnlyList<object> readOnlyList)
                {
                    var copy = new List<object>();
                    foreach (var item in readOnlyList) copy.Add(Copy(item, depth + 1));
                    return new ReadOnlyCollection<object>(copy);
                }
                throw new ArgumentException("Unsupported JSON value type: " + value.GetType().FullName);
            }
            finally { active.Remove(value); }
        }
    }
}
}
