using System.Collections.Generic;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.PluginApi
{
/// <summary>Compatibility facade over the shared immutable JSON data boundary.</summary>
public static class GameApiData
{
    public const int MaxDepth = JsonData.MaxDepth;
    public const int MaxNodes = JsonData.MaxNodes;
    public const int MaxStringLength = JsonData.MaxStringLength;
    public const int MaxAggregateStringLength = JsonData.MaxAggregateStringLength;
    public static IReadOnlyDictionary<string, object> CopyObject(IReadOnlyDictionary<string, object> value) => JsonData.CopyObject(value);
    public static object CopyValue(object value) => JsonData.CopyValue(value);
    internal static string RequireName(string value, string parameter) => JsonData.RequireName(value, parameter);
    internal static string CheckText(string value, string parameter) => JsonData.CheckText(value, parameter);
}
}
