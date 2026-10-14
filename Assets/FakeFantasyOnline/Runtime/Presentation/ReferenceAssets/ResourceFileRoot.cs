namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
/// <summary>Platform translation only; recovered loaders retain their archive/cache policies.</summary>
internal static class ResourceFileRoot
{
    internal static string Path => FakeFantasyOnline.Foundation.ClientResourcePaths.Resolve(
        UnityEngine.Application.streamingAssetsPath, UnityEngine.Application.isMobilePlatform);
}
}
