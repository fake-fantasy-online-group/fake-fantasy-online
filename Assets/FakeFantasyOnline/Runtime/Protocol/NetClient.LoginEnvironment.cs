using System;

namespace FakeFantasyOnline.Protocol
{

/// <summary>
/// Host capabilities for the original Login/Register environment reads. These are callbacks,
/// not a sampled environment: the recovered writers decide when each one is invoked.
/// </summary>
public sealed class NetClientLoginEnvironment
{
    public delegate bool Detector(out string reason);

    public NetClientLoginEnvironment(
        Func<string> deviceUniqueIdentifier,
        Detector detectVm,
        Detector detectDebug)
    {
        DeviceUniqueIdentifier = deviceUniqueIdentifier
            ?? throw new ArgumentNullException(nameof(deviceUniqueIdentifier));
        DetectVm = detectVm ?? throw new ArgumentNullException(nameof(detectVm));
        DetectDebug = detectDebug ?? throw new ArgumentNullException(nameof(detectDebug));
    }

    public Func<string> DeviceUniqueIdentifier { get; }

    public Detector DetectVm { get; }

    public Detector DetectDebug { get; }

    // Compatibility for non-Unity/editor diagnostics which construct NetClient/Gateway
    // without host probes. The real title path always supplies the Bootstrap capabilities.
    // Even this fallback reads its options only when the writer reaches the corresponding
    // callback; construction never invokes a getter or detector.
    internal static NetClientLoginEnvironment FromDiagnosticOptions(PrivateServerOptions options)
    {
        return new NetClientLoginEnvironment(
            () => options.DeviceIdentifier,
            (out string reason) =>
            {
                reason = null;
                return options.VmDetected;
            },
            (out string reason) =>
            {
                reason = null;
                return options.DebugDetected;
            });
    }
}

}
