using System;
using System.Net.NetworkInformation;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Official 1.5.8 virtual-machine detector: graphics device name, device model and the MAC
// address prefix of every network adapter. Both tables come from the official static
// initializer, which fills them in this exact order.
public static class VmDetect
{
    private static readonly string[] SuspiciousNames =
    {
        "vmware", "virtualbox", "vbox", "virtual machine", "parallels",
        "microsoft basic render", "qemu", "virtio", "llvmpipe", "swiftshader", "hyper-v"
    };

    private static readonly (byte, byte, byte)[] VmMacOuis =
    {
        (0x00, 0x05, 0x69),
        (0x00, 0x0C, 0x29),
        (0x00, 0x1C, 0x14),
        (0x00, 0x50, 0x56),
        (0x08, 0x00, 0x27),
        (0x00, 0x15, 0x5D),
        (0x00, 0x16, 0x3E),
        (0x00, 0x1C, 0x42),
        (0x52, 0x54, 0x00)
    };

    private static bool? _cached;
    private static string _cachedReason;

    // 060008d3: once a result exists it is reused, so a positive hit is sticky.
    public static bool Detect(out string reason)
    {
        if (_cached.HasValue)
        {
            reason = _cachedReason;
            return _cached.Value;
        }
        bool found = DetectCore(out reason);
        _cached = found;
        _cachedReason = reason;
        return found;
    }

    // 060008d4: the graphics device name, then the device model, then the MAC prefixes.
    private static bool DetectCore(out string reason)
    {
        string gpu = (SystemInfo.graphicsDeviceName ?? string.Empty).ToLowerInvariant();
        foreach (string suspicious in SuspiciousNames)
        {
            if (gpu.Contains(suspicious))
            {
                reason = "gpu:" + suspicious;
                return true;
            }
        }

        string model = (SystemInfo.deviceModel ?? string.Empty).ToLowerInvariant();
        foreach (string suspicious in SuspiciousNames)
        {
            if (model.Contains(suspicious))
            {
                reason = "model:" + suspicious;
                return true;
            }
        }

        foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
        {
            PhysicalAddress address = adapter.GetPhysicalAddress();
            byte[] mac = address != null ? address.GetAddressBytes() : null;
            if (mac == null || mac.Length <= 2)
                continue;
            foreach ((byte first, byte second, byte third) in VmMacOuis)
            {
                if (mac[0] == first && mac[1] == second && mac[2] == third)
                {
                    reason = string.Format("mac:{0:X2}-{1:X2}-{2:X2}", mac[0], mac[1], mac[2]);
                    return true;
                }
            }
        }

        reason = null;
        return false;
    }
}

}
