using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;


namespace FakeFantasyOnline.Foundation
{

/// <summary>Strict bounded JSON codec. Does not reflect over objects or expose CLR type metadata.</summary>
public static class BoundedJson
{
    public const int MaxJsonLength = 1048576;
    public const int MaxDepth = JsonData.MaxDepth;
    public const int MaxNodes = JsonData.MaxNodes;

    public static object Parse(string json)
    {
        if (json == null) throw new ArgumentNullException(nameof(json));
        if (json.Length > MaxJsonLength) throw new FormatException("JSON exceeds the character limit.");
        return new Reader(json).ReadDocument();
    }

    public static string Serialize(object value)
    {
        // Validation also snapshots mutable caller collections before serializing.
        var safe = JsonData.CopyValue(value);
        var writer = new Writer();
        writer.Write(safe);
        return writer.ToString();
    }

    private sealed class Writer
    {
        private readonly StringBuilder buffer = new StringBuilder();
        public override string ToString() { return buffer.ToString(); }
        private void Add(string value)
        {
            if (value.Length > MaxJsonLength - buffer.Length) throw new ArgumentException("Serialized JSON exceeds the character limit.");
            buffer.Append(value);
        }
        private void Add(char value)
        {
            if (buffer.Length == MaxJsonLength) throw new ArgumentException("Serialized JSON exceeds the character limit.");
            buffer.Append(value);
        }
        private void String(string value)
        {
            Add('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': Add("\\\""); break;
                    case '\\': Add("\\\\"); break;
                    case '\b': Add("\\b"); break;
                    case '\f': Add("\\f"); break;
                    case '\n': Add("\\n"); break;
                    case '\r': Add("\\r"); break;
                    case '\t': Add("\\t"); break;
                    default:
                        if (c < 32) Add("\\u" + ((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else Add(c);
                        break;
                }
            }
            Add('"');
        }
        public void Write(object value)
        {
            if (value == null) { Add("null"); return; }
            if (value is string text) { String(text); return; }
            if (value is bool boolean) { Add(boolean ? "true" : "false"); return; }
            if (value is IReadOnlyDictionary<string, object> dictionary)
            {
                Add('{');
                bool first = true;
                foreach (var pair in dictionary)
                {
                    if (!first) Add(',');
                    first = false;
                    String(pair.Key); Add(':'); Write(pair.Value);
                }
                Add('}'); return;
            }
            if (value is IReadOnlyList<object> list)
            {
                Add('[');
                for (int i = 0; i < list.Count; i++) { if (i != 0) Add(','); Write(list[i]); }
                Add(']'); return;
            }
            if (value is double d) { Add(d.ToString("R", CultureInfo.InvariantCulture)); return; }
            if (value is float f) { Add(f.ToString("R", CultureInfo.InvariantCulture)); return; }
            if (value is decimal m) { Add(m.ToString("G29", CultureInfo.InvariantCulture)); return; }
            Add(((IFormattable)value).ToString(null, CultureInfo.InvariantCulture));
        }
    }

    private sealed class Reader
    {
        private readonly string json;
        private int index;
        private int nodes;
        private int characters;
        public Reader(string json) { this.json = json; }
        private FormatException Error(string message) { return new FormatException(message + " At character " + index.ToString(CultureInfo.InvariantCulture) + "."); }
        private void Space()
        {
            while (index < json.Length && (json[index] == ' ' || json[index] == '\t' || json[index] == '\n' || json[index] == '\r')) index++;
        }
        public object ReadDocument()
        {
            Space();
            var result = Value(0);
            Space();
            if (index != json.Length) throw Error("Unexpected trailing content");
            return result;
        }
        private bool Take(char c)
        {
            if (index < json.Length && json[index] == c) { index++; return true; }
            return false;
        }
        private object Value(int depth)
        {
            if (depth > MaxDepth) throw Error("JSON exceeds the nesting limit");
            if (++nodes > MaxNodes) throw Error("JSON exceeds the item limit");
            Space();
            if (index == json.Length) throw Error("Expected a value");
            char c = json[index];
            if (c == '"') return String();
            if (c == '{')
            {
                index++; Space();
                var result = new Dictionary<string, object>(StringComparer.Ordinal);
                if (Take('}')) return result;
                while (true)
                {
                    if (index == json.Length || json[index] != '"') throw Error("Expected an object key");
                    string key = String(); Space();
                    if (!Take(':')) throw Error("Expected ':'");
                    if (result.ContainsKey(key)) throw Error("Duplicate object key");
                    result.Add(key, Value(depth + 1)); Space();
                    if (Take('}')) return result;
                    if (!Take(',')) throw Error("Expected ',' or '}'");
                    Space();
                }
            }
            if (c == '[')
            {
                index++; Space();
                var result = new List<object>();
                if (Take(']')) return result;
                while (true)
                {
                    result.Add(Value(depth + 1)); Space();
                    if (Take(']')) return result;
                    if (!Take(',')) throw Error("Expected ',' or ']'");
                }
            }
            if (c == 't') { Literal("true"); return true; }
            if (c == 'f') { Literal("false"); return false; }
            if (c == 'n') { Literal("null"); return null; }
            if (c == '-' || Digit(c)) return Number();
            throw Error("Expected a JSON value");
        }
        private void Literal(string expected)
        {
            if (json.Length - index < expected.Length || string.CompareOrdinal(json, index, expected, 0, expected.Length) != 0)
                throw Error("Invalid JSON literal");
            index += expected.Length;
        }
        private string String()
        {
            index++;
            var result = new StringBuilder();
            while (index < json.Length)
            {
                char c = json[index++];
                if (c == '"')
                {
                    string text = result.ToString();
                    // Surrogate pairs must be valid whether raw, escaped, or mixed.
                    for (int i = 0; i < text.Length; i++)
                    {
                        if (char.IsHighSurrogate(text[i]))
                        {
                            if (++i >= text.Length || !char.IsLowSurrogate(text[i])) throw Error("Unpaired Unicode surrogate");
                        }
                        else if (char.IsLowSurrogate(text[i])) throw Error("Unpaired Unicode surrogate");
                    }
                    characters += text.Length;
                    if (characters > JsonData.MaxAggregateStringLength) throw Error("JSON exceeds the text limit");
                    return text;
                }
                if (c < 32) throw Error("Unescaped control character in string");
                if (c == '\\')
                {
                    if (index == json.Length) throw Error("Incomplete string escape");
                    switch (json[index++])
                    {
                        case '"': c = '"'; break;
                        case '\\': c = '\\'; break;
                        case '/': c = '/'; break;
                        case 'b': c = '\b'; break;
                        case 'f': c = '\f'; break;
                        case 'n': c = '\n'; break;
                        case 'r': c = '\r'; break;
                        case 't': c = '\t'; break;
                        case 'u':
                            if (json.Length - index < 4) throw Error("Incomplete Unicode escape");
                            int code = 0;
                            for (int i = 0; i < 4; i++)
                            {
                                char h = json[index++];
                                int digit = h >= '0' && h <= '9' ? h - '0' : h >= 'a' && h <= 'f' ? h - 'a' + 10 : h >= 'A' && h <= 'F' ? h - 'A' + 10 : -1;
                                if (digit < 0) throw Error("Invalid Unicode escape");
                                code = code * 16 + digit;
                            }
                            c = (char)code; break;
                        default: throw Error("Invalid string escape");
                    }
                }
                if (result.Length == JsonData.MaxStringLength) throw Error("String exceeds the character limit");
                result.Append(c);
            }
            throw Error("Unterminated string");
        }
        private static bool Digit(char c) { return c >= '0' && c <= '9'; }
        private object Number()
        {
            int start = index;
            Take('-');
            if (index == json.Length) throw Error("Incomplete number");
            if (Take('0')) { if (index < json.Length && Digit(json[index])) throw Error("Leading zeros are not allowed"); }
            else
            {
                if (json[index] < '1' || json[index] > '9') throw Error("Expected number digits");
                while (index < json.Length && Digit(json[index])) index++;
            }
            bool fractional = false;
            if (Take('.'))
            {
                fractional = true;
                int digits = index;
                while (index < json.Length && Digit(json[index])) index++;
                if (digits == index) throw Error("Expected fractional digits");
            }
            if (index < json.Length && (json[index] == 'e' || json[index] == 'E'))
            {
                fractional = true; index++;
                if (index < json.Length && (json[index] == '+' || json[index] == '-')) index++;
                int digits = index;
                while (index < json.Length && Digit(json[index])) index++;
                if (digits == index) throw Error("Expected exponent digits");
            }
            // Bound both parser work and numeric conversion work for hostile inputs.
            if (index - start > 256) throw Error("Number exceeds the digit limit");
            string token = json.Substring(start, index - start);
            if (!fractional)
            {
                if (long.TryParse(token, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long signed)) return signed;
                if (ulong.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out ulong unsigned)) return unsigned;
            }
            if (decimal.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal exact) &&
                SameNumber(token, exact.ToString("G29", CultureInfo.InvariantCulture))) return exact;
            if (!fractional) throw Error("Integer exceeds the exact supported numeric range");
            if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double approximate) ||
                double.IsNaN(approximate) || double.IsInfinity(approximate)) throw Error("Number is outside the finite numeric range");
            if (approximate == 0 && HasNonzeroMantissa(token)) throw Error("Number underflows the supported numeric range");
            return approximate;
        }
        private static bool HasNonzeroMantissa(string value)
        {
            foreach (char c in value)
            {
                if (c == 'e' || c == 'E') break;
                if (c >= '1' && c <= '9') return true;
            }
            return false;
        }
        private static bool SameNumber(string left, string right) { return Normalize(left) == Normalize(right); }
        private static string Normalize(string text)
        {
            bool negative = text[0] == '-';
            int position = negative ? 1 : 0;
            var digits = new StringBuilder();
            int fractionalDigits = 0;
            bool fraction = false;
            while (position < text.Length && text[position] != 'e' && text[position] != 'E')
            {
                char c = text[position++];
                if (c == '.') fraction = true;
                else { digits.Append(c); if (fraction) fractionalDigits++; }
            }
            int exponent = 0;
            if (position < text.Length)
            {
                position++;
                bool exponentNegative = position < text.Length && text[position] == '-';
                if (position < text.Length && (text[position] == '-' || text[position] == '+')) position++;
                for (; position < text.Length; position++) exponent = Math.Min(1000000, exponent * 10 + text[position] - '0');
                if (exponentNegative) exponent = -exponent;
            }
            string allDigits = digits.ToString();
            int first = 0;
            while (first < allDigits.Length && allDigits[first] == '0') first++;
            if (first == allDigits.Length) return "0";
            int last = allDigits.Length - 1;
            while (allDigits[last] == '0') last--;
            int power = exponent - fractionalDigits + allDigits.Length - 1 - last;
            return (negative ? "-" : "") + allDigits.Substring(first, last - first + 1) + "e" + power.ToString(CultureInfo.InvariantCulture);
        }
    }
}
}
