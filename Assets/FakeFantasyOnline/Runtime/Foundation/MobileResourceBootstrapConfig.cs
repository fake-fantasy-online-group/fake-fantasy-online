using System;
using System.IO;
using System.Net;

namespace FakeFantasyOnline.Foundation
{
/// <summary>Small public bootstrap configuration, independent of game assets and login protocol.</summary>
[Serializable]
public sealed class MobileResourceBootstrapConfig
{
    public const string FileName = "resource-bootstrap.json";
    public const int MaxConfigBytes = 16384;
    public int schemaVersion;
    public string mode, appVersion, resourceManifestUrl, dispatchUrl;
    public bool developmentHttp;

    public void Validate(bool isDevelopmentBuild)
    {
        if (schemaVersion != 1 || mode != "resource-free" || string.IsNullOrWhiteSpace(appVersion) ||
            appVersion.Length > 64 || appVersion.IndexOfAny(new[] { '\t', '\r', '\n', '/', '\\' }) >= 0)
            throw new InvalidDataException("Unsupported resource bootstrap configuration.");
        if (developmentHttp && !isDevelopmentBuild)
            throw new InvalidDataException("Development HTTP requires an explicit development build.");
        if (!string.IsNullOrWhiteSpace(resourceManifestUrl)) ValidateEndpoint(resourceManifestUrl, developmentHttp && isDevelopmentBuild);
        if (!string.IsNullOrWhiteSpace(dispatchUrl)) ValidateEndpoint(dispatchUrl, developmentHttp && isDevelopmentBuild);
    }

    public static Uri ValidateEndpoint(string value, bool allowDevelopmentLanHttp)
    {
        Uri uri;
        if (value != null) foreach (char c in value) if (char.IsControl(c)) throw new InvalidDataException("Control characters are not allowed in URLs.");
        if (string.IsNullOrWhiteSpace(value) || value != value.Trim() || value.Length > 4096 ||
            !Uri.TryCreate(value, UriKind.Absolute, out uri) || string.IsNullOrEmpty(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) ||
            uri.IsLoopback || uri.Host == "0.0.0.0" || (uri.Host == "::" || uri.Host == "[::]") || value.IndexOf('\\') >= 0 ||
            (uri.Scheme != Uri.UriSchemeHttps && !(uri.Scheme == Uri.UriSchemeHttp && allowDevelopmentLanHttp && IsPrivateLan(uri.Host))))
            throw new InvalidDataException("Use a credential-free HTTPS URL. HTTP is allowed only for explicitly enabled development LAN IPs.");
        return uri;
    }

    private static bool IsPrivateLan(string host)
    {
        IPAddress address;
        if (!IPAddress.TryParse(host, out address)) return false;
        byte[] b = address.GetAddressBytes();
        return b.Length == 4 && (b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168));
    }
}
}
