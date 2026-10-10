using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using MoonSharp.Interpreter;

namespace FakeFantasyOnline.PluginHost
{
internal static class LuaValue
{
    internal const int MaxString = 32768;
    internal static DynValue ToLua(Script script, object value) { int count = 0; return ToLua(script, value, 0, ref count); }
    private static DynValue ToLua(Script script, object value, int depth, ref int count)
    {
        if (++count > 65536 || depth > 32) throw new ScriptRuntimeException("Game data exceeds Lua boundary limit.");
        if (value == null) return DynValue.Nil;
        if (value is string) return DynValue.NewString((string)value);
        if (value is bool) return DynValue.NewBoolean((bool)value);
        var dictionary = value as IReadOnlyDictionary<string, object>;
        if (dictionary != null)
        {
            var table = new Table(script);
            foreach (var pair in dictionary) table.Set(pair.Key, ToLua(script, pair.Value, depth + 1, ref count));
            return DynValue.NewTable(table);
        }
        var enumerable = value as IEnumerable;
        if (enumerable != null)
        {
            var table = new Table(script); int index = 0;
            foreach (var item in enumerable) table.Set(++index, ToLua(script, item, depth + 1, ref count));
            return DynValue.NewTable(table);
        }
        switch (Type.GetTypeCode(value.GetType()))
        {
            case TypeCode.Int64:
                long signed = (long)value;
                return signed > 9007199254740991L || signed < -9007199254740991L
                    ? DynValue.NewString(signed.ToString(CultureInfo.InvariantCulture)) : DynValue.NewNumber(signed);
            case TypeCode.UInt64:
                ulong unsigned = (ulong)value;
                return unsigned > 9007199254740991UL
                    ? DynValue.NewString(unsigned.ToString(CultureInfo.InvariantCulture)) : DynValue.NewNumber(unsigned);
            case TypeCode.Decimal:
                decimal precise = (decimal)value;
                if (precise > 9007199254740991m || precise < -9007199254740991m)
                    return DynValue.NewString(precise.ToString(CultureInfo.InvariantCulture));
                double converted = (double)precise;
                return (decimal)converted == precise ? DynValue.NewNumber(converted) : DynValue.NewString(precise.ToString(CultureInfo.InvariantCulture));
            case TypeCode.Byte: case TypeCode.SByte: case TypeCode.Int16: case TypeCode.UInt16:
            case TypeCode.Int32: case TypeCode.UInt32: case TypeCode.Single: case TypeCode.Double:
                double number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if (double.IsNaN(number) || double.IsInfinity(number)) throw new ScriptRuntimeException("Nonfinite numbers cannot enter Lua.");
                return DynValue.NewNumber(number);
            default: throw new ScriptRuntimeException("Only JSON data can enter Lua.");
        }
    }
    internal static object FromLua(DynValue value)
    { int count = 0; return FromLua(value, new HashSet<Table>(), 0, ref count); }
    private static object FromLua(DynValue value, HashSet<Table> visited, int depth, ref int count)
    {
        if (++count > 8192 || depth > 16) throw new ScriptRuntimeException("Lua data exceeds boundary depth or size limit.");
        if (value.IsNil()) return null;
        if (value.Type == DataType.String)
        { if (value.String.Length > MaxString) throw new ScriptRuntimeException("String exceeds Lua boundary limit."); return value.String; }
        if (value.Type == DataType.Boolean) return value.Boolean;
        if (value.Type == DataType.Number)
        { if (double.IsNaN(value.Number) || double.IsInfinity(value.Number)) throw new ScriptRuntimeException("Numbers must be finite."); return value.Number; }
        if (value.Type != DataType.Table) throw new ScriptRuntimeException("Only JSON values can leave Lua.");
        var table = value.Table;
        if (!visited.Add(table)) throw new ScriptRuntimeException("Cyclic Lua table is not JSON.");
        var dictionary = new Dictionary<string, object>(StringComparer.Ordinal);
        var array = new SortedDictionary<int, object>();
        foreach (var pair in table.Pairs)
        {
            object child = FromLua(pair.Value, visited, depth + 1, ref count);
            if (pair.Key.Type == DataType.String)
            {
                if (pair.Key.String.Length > 256) throw new ScriptRuntimeException("JSON key is too long.");
                dictionary.Add(pair.Key.String, child);
            }
            else if (pair.Key.Type == DataType.Number && pair.Key.Number >= 1 && pair.Key.Number <= 8192 && Math.Floor(pair.Key.Number) == pair.Key.Number)
                array.Add((int)pair.Key.Number, child);
            else throw new ScriptRuntimeException("JSON tables need string keys or consecutive 1-based indices.");
        }
        visited.Remove(table);
        if (array.Count == 0) return dictionary;
        if (dictionary.Count != 0 || !array.ContainsKey(array.Count)) throw new ScriptRuntimeException("Mixed or sparse Lua tables are not JSON.");
        return new List<object>(array.Values);
    }
    internal static IReadOnlyDictionary<string, object> Object(DynValue value)
    {
        if (value.IsNil()) return null;
        var result = FromLua(value) as IReadOnlyDictionary<string, object>;
        if (result == null) throw new ScriptRuntimeException("Expected a table with string keys.");
        return result;
    }
    internal static string String(DynValue value, string name, int limit = MaxString)
    {
        if (value.Type != DataType.String || value.String.Length > limit) throw new ScriptRuntimeException(name + " must be a bounded string.");
        return value.String;
    }
    internal static double Number(DynValue value, double fallback = 0)
    {
        if (value.IsNil()) return fallback;
        if (value.Type != DataType.Number || double.IsNaN(value.Number) || double.IsInfinity(value.Number))
            throw new ScriptRuntimeException("Expected finite number.");
        return value.Number;
    }
}
}
