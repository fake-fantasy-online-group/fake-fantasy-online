using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    public TextAsset maskAsset;
    public byte[] maskData;

    private void LoadMask()
    {
        byte[] bytes = maskData;
        if (bytes == null)
        {
            if (maskAsset != null) bytes = maskAsset.bytes;
            else return;
            if (bytes == null) return;
        }
        if (bytes.Length < 32) return;
        maskW = BitConverter.ToInt32(bytes, 16);
        maskH = BitConverter.ToInt32(bytes, 20);
        maskCW = BitConverter.ToInt32(bytes, 24);
        maskCH = BitConverter.ToInt32(bytes, 28);
        if (maskCW <= 0) maskCW = 10;
        if (maskCH <= 0) maskCH = 10;
        maskOff = unchecked(bytes.Length - maskH * maskW);
        if (maskW > 0 && maskH > 0 && maskOff >= 32) maskBytes = bytes;
        else maskW = 0;
    }

    public static bool LineOfSight(float fromX, float fromY, float toX, float toY)
    {
        if (I == null) return true;
        float dx = toX - fromX;
        float dy = toY - fromY;
        float distance = Mathf.Sqrt(dx * dx + dy * dy);
        if (distance < 1f) return true;
        float spacing = Mathf.Max(1f, Mathf.Min(I.maskCW, I.maskCH) * 0.5f);
        int count = Mathf.CeilToInt(distance / spacing);
        for (int i = 1; i < count; i++)
        {
            float t = (float)i / count;
            if (!I.IsWalkable(dx * t + fromX, dy * t + fromY)) return false;
        }
        return true;
    }

    // Original Start->LoadMask slot, invoked once after the world assembler supplies sources/dimensions.
    internal void LoadMaskForIndependentRuntime() => LoadMask();
    internal ReferenceWorldCollisionMask CaptureMaskViewForIndependentRuntime() =>
        maskBytes != null && maskW > 0 ? ReferenceWorldCollisionMask.FromDecodedForIndependentRuntime(
            maskBytes, maskOff, maskW, maskH, maskCW, maskCH) : null;
}

}
