using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.PluginHost
{
/// <summary>Compatibility facade; one strict codec serves plugins and protocol.</summary>
public static class GameJson
{
    public const int MaxJsonLength = BoundedJson.MaxJsonLength;
    public const int MaxDepth = BoundedJson.MaxDepth;
    public const int MaxNodes = BoundedJson.MaxNodes;
    public static object Parse(string json) => BoundedJson.Parse(json);
    public static string Serialize(object value) => BoundedJson.Serialize(value);
}
}
