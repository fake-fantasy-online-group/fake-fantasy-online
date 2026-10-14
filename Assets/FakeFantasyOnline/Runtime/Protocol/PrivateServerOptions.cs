using System;

namespace FakeFantasyOnline.Protocol
{

public sealed class PrivateServerOptions
{
    private const string ModulusBase64 =
        "vf85v+zaAwbAFQOnjQqx5OvOiW93YHy2ArgrXLQpjCg/dtTHCaK2Fqh5KBkjdXtnNYRflYxraE6dljJParWM6QVqhvNqh/fwXIzYrnSAtSzcIKaQ8pMrUoIuj8eJlX+YIiGVAYLZnbPlMVW+RM4IwdaJRxaRq9DNquvyfAnyIwFrguc2sbBiVgXcwSwo6WxsAQk//IFZWAjQGNDTlHs6LTB7kc+l/iO+7nWbrj9dRTZ/W/dZDOXmFtKxUxEYzodMyOBkN/KtACu3EWG33VP9YT1VQKmY/O8C+WCTYrD2XdsYL7fbpflGXcC2RCJUBVZOG2Z6q0iCYjPgqm9skskE0Q==";
    private const string ExponentBase64 = "AQAB";

    public PrivateServerOptions(
        Uri dispatchUri,
        string deviceIdentifier,
        TimeSpan? connectTimeout = null,
        TimeSpan? responseTimeout = null,
        bool vmDetected = false,
        bool debugDetected = false,
        Uri noticeUri = null,
        Uri versionManifestUriMac = null,
        Uri versionManifestUriWin = null)
    {
        DispatchUri = dispatchUri ?? throw new ArgumentNullException(nameof(dispatchUri));
        NoticeUri = noticeUri ?? new Uri(DispatchUri.GetLeftPart(UriPartial.Authority) + "/changelog.json");
        VersionManifestUriMac = ValidateVersionManifestUri(versionManifestUriMac, nameof(versionManifestUriMac));
        VersionManifestUriWin = ValidateVersionManifestUri(versionManifestUriWin, nameof(versionManifestUriWin));
        if (string.IsNullOrWhiteSpace(deviceIdentifier))
        {
            throw new ArgumentException(
                "Device identifier cannot be empty.",
                nameof(deviceIdentifier));
        }

        DeviceIdentifier = deviceIdentifier;
        ConnectTimeout = connectTimeout ?? TimeSpan.FromSeconds(10);
        ResponseTimeout = responseTimeout ?? TimeSpan.FromSeconds(15);
        VmDetected = vmDetected;
        DebugDetected = debugDetected;

        if (ConnectTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(connectTimeout));
        }

        if (ResponseTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(responseTimeout));
        }
    }

    public Uri DispatchUri { get; }

    public Uri NoticeUri { get; }

    // Optional private endpoints only. Neither platform inherits an official or
    // dispatch-derived URL when its version manifest is unconfigured.
    public Uri VersionManifestUriMac { get; }

    public Uri VersionManifestUriWin { get; }

    public string DeviceIdentifier { get; }

    public TimeSpan ConnectTimeout { get; }

    public TimeSpan ResponseTimeout { get; }

    public bool VmDetected { get; }

    public bool DebugDetected { get; }

    internal byte[] GetRsaModulus()
    {
        return Convert.FromBase64String(ModulusBase64);
    }

    internal byte[] GetRsaExponent()
    {
        return Convert.FromBase64String(ExponentBase64);
    }

    public static PrivateServerOptions LocalDefault()
    {
        return new PrivateServerOptions(
            new Uri("http://127.0.0.1:8088/dispatch.json"),
            Guid.NewGuid().ToString("D").ToUpperInvariant(),
            versionManifestUriMac: ReadVersionManifestUri("FFO_VERSION_MANIFEST_URL_MAC"),
            versionManifestUriWin: ReadVersionManifestUri("FFO_VERSION_MANIFEST_URL_WIN"));
    }

    public static PrivateServerOptions MobileConfigured(string dispatchUrl)
    {
        Uri uri;
        if (!Uri.TryCreate(dispatchUrl, UriKind.Absolute, out uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            uri.IsLoopback || uri.Host == "0.0.0.0" || uri.Host == "::" ||
            !string.IsNullOrEmpty(uri.UserInfo))
            throw new ArgumentException("Mobile dispatch requires an explicit reachable HTTP(S) server URL; loopback and embedded credentials are not allowed.", nameof(dispatchUrl));
        return new PrivateServerOptions(uri, Guid.NewGuid().ToString("D").ToUpperInvariant());
    }

    private static Uri ReadVersionManifestUri(string variable)
    {
        string value = Environment.GetEnvironmentVariable(variable);
        return string.IsNullOrWhiteSpace(value) ? null : new Uri(value, UriKind.Absolute);
    }

    private static Uri ValidateVersionManifestUri(Uri uri, string parameter)
    {
        if (uri != null && (!uri.IsAbsoluteUri ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
            throw new ArgumentException("A private version manifest must use an absolute HTTP or HTTPS URL.", parameter);
        return uri;
    }
}
}
