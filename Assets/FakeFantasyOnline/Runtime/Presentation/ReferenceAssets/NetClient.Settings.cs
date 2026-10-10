using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<int> writePkMode;
    private Action<int> writeGlowMode;
    private Action<bool> writeSmartCast;
    private Action<byte> writePickQuality;

    internal void BindSettingsWritersForIndependentRuntime(Action<int> pkMode, Action<int> glowMode,
        Action<bool> smartCast, Action<byte> pickQuality)
    {
        writePkMode = pkMode;
        writeGlowMode = glowMode;
        writeSmartCast = smartCast;
        writePickQuality = pickQuality;
    }

    public void SetPkModeForIndependentRuntime(int mode) => writePkMode(mode);
    public void SetGlowModeForIndependentRuntime(int mode) => writeGlowMode(mode);
    public void SetSmartCastForIndependentRuntime(bool enabled) => writeSmartCast(enabled);
    public void PetPickQualityForIndependentRuntime(byte quality) => writePickQuality(quality);
}
}
