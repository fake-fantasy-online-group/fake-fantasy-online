using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using FakeFantasyOnline.PluginApi;

namespace FakeFantasyOnline.PluginHost
{
public enum AddonState { Discovered, Loading, Enabled, Disabled, Faulted }

public sealed class AddonStatus
{
    internal AddonStatus(string id, string name, string version, AddonState state, string error)
    { Id = id; Name = name; Version = version; State = state; Error = error ?? string.Empty; }
    public string Id { get; }
    public string Name { get; }
    public string Version { get; }
    public AddonState State { get; }
    public string Error { get; }
}

internal sealed class AddonPackage
{
    internal const int MaxSourceBytes = 131072;
    internal string Id, Name, Version, Directory, Entry;
    internal readonly List<string> Dependencies = new List<string>();
    internal readonly HashSet<string> Capabilities = new HashSet<string>(StringComparer.Ordinal);
    internal static readonly HashSet<string> SupportedCapabilities = new HashSet<string>(StringComparer.Ordinal)
    { "game.observe", "game.act", "game.events", "ui.panel", "storage.local" };

    internal static bool ValidId(string value)
    { return value != null && Regex.IsMatch(value, "^[a-z][a-z0-9_-]{0,63}$"); }

    internal static AddonPackage Read(string directory)
    {
        directory = Path.GetFullPath(directory);
        CheckPath(directory);
        var data = GameJson.Parse(ReadText(Path.Combine(directory, "addon.json"), 16384)) as Dictionary<string, object>;
        if (data == null) throw new FormatException("addon.json must be an object.");
        var p = new AddonPackage { Directory = directory, Id = Required(data, "id"), Name = Required(data, "name"),
            Version = Required(data, "version"), Entry = Required(data, "entry") };
        if (!ValidId(p.Id) || !string.Equals(Path.GetFileName(directory), p.Id, StringComparison.Ordinal))
            throw new FormatException("Addon ID must match its directory and use lowercase letters, digits, underscores or hyphens.");
        if (Required(data, "apiVersion") != PluginApiVersion.Current) throw new FormatException("Unsupported addon API version.");
        ReadStrings(data, "dependencies", p.Dependencies);
        var capabilities = new List<string>(); ReadStrings(data, "capabilities", capabilities);
        foreach (var cap in capabilities)
        { if (!SupportedCapabilities.Contains(cap)) throw new FormatException("Unsupported capability: " + cap); p.Capabilities.Add(cap); }
        foreach (var dep in p.Dependencies)
            if (!ValidId(dep) || dep == p.Id) throw new FormatException("Invalid dependency: " + dep);
        if (Path.IsPathRooted(p.Entry) || p.Entry.IndexOf('\\') >= 0 || !p.Entry.EndsWith(".lua", StringComparison.OrdinalIgnoreCase))
            throw new FormatException("Entry must be a relative .lua source path.");
        foreach (var segment in p.Entry.Split('/'))
            if (segment.Length == 0 || segment == "." || segment == ".." || !Regex.IsMatch(segment, "^[A-Za-z0-9_.-]+$"))
                throw new FormatException("Invalid entry path.");
        return p;
    }
    internal string ReadSource()
    {
        var source = ReadText(Path.Combine(Directory, Entry), MaxSourceBytes);
        if (source.StartsWith("MoonSharp_dump_b64::", StringComparison.Ordinal) || source.IndexOf('\0') >= 0)
            throw new FormatException("Only plain Lua source is allowed.");
        return source;
    }
    internal static string ReadText(string file, int maximum)
    {
        CheckPath(file);
        if (new FileInfo(file).Length > maximum) throw new IOException("File exceeds addon size limit.");
        var value = File.ReadAllText(file, new UTF8Encoding(false, true));
        if (Encoding.UTF8.GetByteCount(value) > maximum) throw new IOException("File exceeds addon size limit.");
        return value;
    }
    internal static void CheckPath(string path)
    {
        var current = Path.GetFullPath(path);
        while (!string.IsNullOrEmpty(current))
        {
            if ((File.Exists(current) || System.IO.Directory.Exists(current)) &&
                (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Addon paths cannot contain symbolic links or reparse points.");
            var parent = Path.GetDirectoryName(current);
            if (parent == current) break;
            current = parent;
        }
    }
    private static string Required(Dictionary<string, object> values, string key)
    {
        object v; if (!values.TryGetValue(key, out v) || !(v is string) || string.IsNullOrWhiteSpace((string)v) || ((string)v).Length > 256)
            throw new FormatException("Missing or invalid manifest field: " + key);
        return (string)v;
    }
    private static void ReadStrings(Dictionary<string, object> values, string key, List<string> target)
    {
        object raw; if (!values.TryGetValue(key, out raw)) return;
        var list = raw as List<object>; if (list == null || list.Count > 32) throw new FormatException("Invalid " + key);
        foreach (var item in list)
        {
            if (!(item is string) || ((string)item).Length > 128 || target.Contains((string)item)) throw new FormatException("Invalid " + key);
            target.Add((string)item);
        }
    }
}

internal sealed class AddonStorage
{
    private const int Quota = 65536;
    private readonly string _directory, _path;
    private Dictionary<string, object> _values;
    internal AddonStorage(string root, string id)
    {
        _directory = Path.Combine(root, id); _path = Path.Combine(_directory, "storage.json");
        AddonPackage.CheckPath(_path);
        _values = File.Exists(_path) ? GameJson.Parse(AddonPackage.ReadText(_path, Quota)) as Dictionary<string, object> : new Dictionary<string, object>(StringComparer.Ordinal);
        if (_values == null) throw new FormatException("Addon storage must be a JSON object.");
    }
    internal object Get(string key)
    { ValidateKey(key); object value; return _values.TryGetValue(key, out value) ? value : null; }
    internal void Set(string key, object value)
    {
        ValidateKey(key);
        var next = new Dictionary<string, object>(_values, StringComparer.Ordinal);
        if (value == null) next.Remove(key); else next[key] = value;
        string json = GameJson.Serialize(next);
        if (Encoding.UTF8.GetByteCount(json) > Quota) throw new IOException("Addon storage quota exceeded (64 KiB).");
        AddonPackage.CheckPath(_path); Directory.CreateDirectory(_directory);
        string temporary = _path + ".tmp"; AddonPackage.CheckPath(temporary);
        File.WriteAllText(temporary, json, new UTF8Encoding(false));
        if (File.Exists(_path)) File.Replace(temporary, _path, null); else File.Move(temporary, _path);
        _values = next;
    }
    private static void ValidateKey(string key)
    { if (string.IsNullOrEmpty(key) || key.Length > 128) throw new ArgumentException("Storage key must contain 1 to 128 characters."); }
}
}
