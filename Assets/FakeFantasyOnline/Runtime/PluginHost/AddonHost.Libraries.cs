using System;
using System.Text;
using MoonSharp.Interpreter;

namespace FakeFantasyOnline.PluginHost
{
public sealed partial class AddonHost
{
    // Deliberately small, bounded native helpers. Pattern matching, callback sort, format,
    // unpack, and arbitrary repetition are not imported from the standard library.
    private void AddBoundedLibraries(Script script, Instance addon)
    {
        var strings = new Table(script);
        Bind(strings, "len", addon, null, args => DynValue.NewNumber(LuaValue.String(args[0], "text").Length));
        Bind(strings, "lower", addon, null, args => DynValue.NewString(LuaValue.String(args[0], "text").ToLowerInvariant()));
        Bind(strings, "upper", addon, null, args => DynValue.NewString(LuaValue.String(args[0], "text").ToUpperInvariant()));
        Bind(strings, "sub", addon, null, args =>
        {
            string text = LuaValue.String(args[0], "text");
            double rawStart = LuaValue.Number(args[1], 1), rawEnd = LuaValue.Number(args[2], -1);
            if (Math.Abs(rawStart) > int.MaxValue || Math.Abs(rawEnd) > int.MaxValue) throw new ScriptRuntimeException("String index is out of range.");
            int start = (int)rawStart, end = (int)rawEnd;
            start = start < 0 ? text.Length + start + 1 : start; end = end < 0 ? text.Length + end + 1 : end;
            start = Math.Max(1, start); end = Math.Min(text.Length, end);
            return DynValue.NewString(end < start ? "" : text.Substring(start - 1, end - start + 1));
        });
        Bind(strings, "rep", addon, null, args =>
        {
            string text = LuaValue.String(args[0], "text"), separator = args[2].IsNil() ? "" : LuaValue.String(args[2], "separator");
            double count = LuaValue.Number(args[1]);
            if (count < 0 || count > LuaValue.MaxString || count != Math.Floor(count) ||
                text.Length * count + separator.Length * Math.Max(0, count - 1) > LuaValue.MaxString)
                throw new ScriptRuntimeException("string.rep output exceeds 32768 characters.");
            var output = new StringBuilder();
            for (int i = 0; i < (int)count; i++) { if (i > 0) output.Append(separator); output.Append(text); }
            return DynValue.NewString(output.ToString());
        });
        script.Globals.Set("string", DynValue.NewTable(strings));
        var tables = new Table(script);
        Bind(tables, "insert", addon, null, args =>
        {
            var table = RequireTable(args[0]); int length = BoundedLength(table);
            if (length >= 4096) throw new ScriptRuntimeException("table.insert supports at most 4096 array entries.");
            int position = args.Count < 3 ? length + 1 : ArrayIndex(args[1], length + 1);
            if (position < 1 || position > length + 1) throw new ScriptRuntimeException("Insert index is out of range.");
            DynValue value = args.Count < 3 ? args[1] : args[2];
            for (int i = length; i >= position; i--) table.Set(i + 1, table.Get(i));
            table.Set(position, value); return DynValue.Nil;
        });
        Bind(tables, "remove", addon, null, args =>
        {
            var table = RequireTable(args[0]); int length = BoundedLength(table);
            if (length == 0) return DynValue.Nil;
            int position = args[1].IsNil() ? length : ArrayIndex(args[1], length);
            if (position < 1 || position > length) throw new ScriptRuntimeException("Remove index is out of range.");
            var value = table.Get(position);
            for (int i = position; i < length; i++) table.Set(i, table.Get(i + 1));
            table.Set(length, DynValue.Nil); return value;
        });
        Bind(tables, "concat", addon, null, args =>
        {
            var table = RequireTable(args[0]); int length = BoundedLength(table);
            string separator = args[1].IsNil() ? "" : LuaValue.String(args[1], "separator");
            int start = args[2].IsNil() ? 1 : ArrayIndex(args[2], 4096);
            int end = args[3].IsNil() ? length : ArrayIndex(args[3], 4096);
            var output = new StringBuilder();
            for (int i = start; i <= end; i++)
            {
                var item = table.Get(i);
                if (item.Type != DataType.String && item.Type != DataType.Number) throw new ScriptRuntimeException("Concat entries must be strings or numbers.");
                string text = item.ToPrintString();
                if (output.Length + text.Length + (i > start ? separator.Length : 0) > LuaValue.MaxString)
                    throw new ScriptRuntimeException("table.concat output exceeds 32768 characters.");
                if (i > start) output.Append(separator); output.Append(text);
            }
            return DynValue.NewString(output.ToString());
        });
        script.Globals.Set("table", DynValue.NewTable(tables));
    }
    private static int BoundedLength(Table table)
    { int length = table.Length; if (length > 4096) throw new ScriptRuntimeException("Native array operation exceeds 4096 entries."); return length; }
    private static int ArrayIndex(DynValue value, int maximum)
    {
        double number = LuaValue.Number(value);
        if (number < 1 || number > maximum || number != Math.Floor(number)) throw new ScriptRuntimeException("Array index is out of range.");
        return (int)number;
    }
}
}
