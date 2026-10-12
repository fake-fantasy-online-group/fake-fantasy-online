using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// One source-recovered row in the official shared UITip layout.
/// </summary>
public sealed class ReferenceFirstWorldUiTipRow
{
    public ReferenceFirstWorldUiTipRow(string text, Color color, bool centered)
    {
        Text = text ?? string.Empty;
        Color = color;
        Centered = centered;
    }

    public string Text { get; }
    public Color Color { get; }
    public bool Centered { get; }
}

/// <summary>
/// Immutable result of the official UITip.Layout boundary.
/// </summary>
public sealed class ReferenceFirstWorldUiTipLayout
{
    public ReferenceFirstWorldUiTipLayout(
        IReadOnlyList<ReferenceFirstWorldUiTipRow> rows,
        float width,
        float height)
    {
        Rows = rows ?? throw new ArgumentNullException(nameof(rows));
        Width = width;
        Height = height;
    }

    public IReadOnlyList<ReferenceFirstWorldUiTipRow> Rows { get; }
    public float Width { get; }
    public float Height { get; }
}

/// <summary>
/// Hosted request/diagnostic borrower of the original 1.5.8 UITip owner.
/// </summary>
/// <remarks>
/// Hosted requests retain their existing validation and call order, then publish to native UITip.
/// Its sole automatic renderer owns depth, native Repaint cleanup, lifetime and font readiness.
/// The old explicit layout/placement APIs remain diagnostic helpers, not another drawing path.
/// </remarks>
public sealed class ReferenceFirstWorldUiTip : IDisposable
{
    public const string DownLeftPath = "ui/equipdlg05/pdl.png";
    public const string DownMiddlePath = "ui/equipdlg05/pdm.png";
    public const string DownRightPath = "ui/equipdlg05/pdr.png";
    public const string LeftMiddlePath = "ui/equipdlg05/plm.png";
    public const string RightMiddlePath = "ui/equipdlg05/prm.png";
    public const string UpLeftPath = "ui/equipdlg05/pul.png";
    public const string UpMiddlePath = "ui/equipdlg05/pum.png";
    public const string UpRightPath = "ui/equipdlg05/pur.png";

    public const float MaximumTextWidthPixels = 150f;
    public const float PointerOffsetPixels = 16f;
    public const float PointerFallbackGapPixels = 4f;
    public const float ComparisonGapPixels = 2f;
    public const int GuiDepth = 1;

    private readonly ReferenceWorldBitmapFont font;
    private bool disposed;

    private ReferenceFirstWorldUiTip(ReferenceWorldBitmapFont font)
    {
        this.font = font;
        Ready = true;
    }

    public bool Ready { get; private set; }
    public bool HasRequest => !disposed && !string.IsNullOrEmpty(UITip.RequestedTextForIndependentRuntime);
    public string RequestedText => disposed ? string.Empty : UITip.RequestedTextForIndependentRuntime ?? string.Empty;
    public string RequestedComparison => disposed ? string.Empty : UITip.RequestedComparisonForIndependentRuntime ?? string.Empty;
    public float RequestedScale => disposed ? 0f : UITip.RequestedScaleForIndependentRuntime;
    public Vector2 RequestedMouse => disposed ? Vector2.zero : UITip.RequestedMouseForIndependentRuntime;
    public Rect LastPrimaryRect { get; private set; }
    public Rect LastComparisonRect { get; private set; }

    public static bool TryCreate(
        ReferenceResourceLoader resources,
        out ReferenceFirstWorldUiTip owner)
    {
        owner = null;
        if (resources == null)
        {
            return false;
        }

        ReferenceWorldBitmapFont font;
        if (!ReferenceWorldBitmapFont.TryCreate(resources, out font) || font == null)
        {
            return false;
        }

        // This map borrower only supplies requests and diagnostic layout. The original
        // UITip renderer loads its own borders lazily from Render, after BitmapFont.Ready.
        // Missing borders therefore cannot reject or dispose a constructing world.
        owner = new ReferenceFirstWorldUiTip(font);
        return true;
    }

    /// <summary>
    /// Translates UITip.Show, including the official current-event mouse fallback.
    /// </summary>
    public bool Request(string text, float scale)
    {
        Event current = Event.current;
        Vector2 mouse;
        if (current != null)
        {
            mouse = current.mousePosition;
        }
        else
        {
            Vector3 input = Input.mousePosition;
            mouse = new Vector2(input.x, Screen.height - input.y);
        }

        return RequestAt(text, scale, mouse);
    }

    /// <summary>
    /// Deterministic form of UITip.Show used by the background source audit.
    /// </summary>
    public bool RequestAt(string text, float scale, Vector2 mousePosition)
    {
        if (!Ready || disposed || string.IsNullOrEmpty(text) ||
            !IsPositiveFinite(scale) || !IsFinite(mousePosition.x) ||
            !IsFinite(mousePosition.y))
        {
            return false;
        }

        UITip.ShowAtForIndependentRuntime(text, scale, mousePosition);
        return true;
    }

    public bool RequestComparison(
        string text,
        string comparisonText,
        float scale)
    {
        Event current = Event.current;
        Vector2 mouse;
        if (current != null)
        {
            mouse = current.mousePosition;
        }
        else
        {
            Vector3 input = Input.mousePosition;
            mouse = new Vector2(input.x, Screen.height - input.y);
        }

        return RequestComparisonAt(text, comparisonText, scale, mouse);
    }

    /// <summary>
    /// Deterministic form of UITip.ShowCompare used by the background source audit.
    /// </summary>
    public bool RequestComparisonAt(
        string text,
        string comparisonText,
        float scale,
        Vector2 mousePosition)
    {
        if (!Ready || disposed || string.IsNullOrEmpty(text) ||
            !IsPositiveFinite(scale) || !IsFinite(mousePosition.x) ||
            !IsFinite(mousePosition.y))
        {
            return false;
        }
        UITip.ShowCompareAtForIndependentRuntime(text, comparisonText, scale, mousePosition);
        return true;
    }

    // Compatibility/liveness only. Unity invokes the one original UITipRenderer.OnGUI;
    // no second render or Layout-time clearing is performed by a map borrower.
    public bool DrawRequested() => Ready && !disposed && Screen.width >= 1 && Screen.height >= 1;

    public ReferenceFirstWorldUiTipLayout BuildLayout(string text, float scale)
    {
        if (!Ready || disposed || text == null || !IsPositiveFinite(scale))
        {
            return null;
        }

        List<ReferenceFirstWorldUiTipRow> rows =
            new List<ReferenceFirstWorldUiTipRow>();
        string normalized = text.Replace("\\n", "\n");
        string[] sourceLines = normalized.Split('\n');
        for (int sourceIndex = 0; sourceIndex < sourceLines.Length; sourceIndex++)
        {
            string line = sourceLines[sourceIndex];
            bool centered = line.Length > 0 && line[0] == '\u0007';
            if (centered)
            {
                line = line.Substring(1);
            }

            Color color;
            if (line.Length >= 2 && line[0] == '\u0003' &&
                line[1] >= '0' && line[1] <= '5')
            {
                color = QualityColor(line[1] - '0');
                line = line.Substring(2);
            }
            else if (line.Length > 0 && IsDirectColorPrefix(line[0]))
            {
                char prefix = line[0];
                line = line.Substring(1);
                color = PrefixColor(prefix);
            }
            else
            {
                color = LineColor(line, sourceIndex);
            }

            IReadOnlyList<string> wrapped = WrapByWidth(
                line,
                MaximumTextWidthPixels * scale,
                scale);
            for (int wrappedIndex = 0; wrappedIndex < wrapped.Count; wrappedIndex++)
            {
                rows.Add(new ReferenceFirstWorldUiTipRow(
                    wrapped[wrappedIndex],
                    color,
                    centered));
            }
        }

        float maximum = 0f;
        for (int index = 0; index < rows.Count; index++)
        {
            maximum = Mathf.Max(maximum, font.Measure(rows[index].Text, scale));
        }

        return new ReferenceFirstWorldUiTipLayout(
            rows.ToArray(),
            12f * scale + maximum,
            8f * scale + 16f * scale * rows.Count);
    }

    public IReadOnlyList<string> WrapByWidth(
        string line,
        float maximumWidth,
        float scale)
    {
        List<string> lines = new List<string>();
        if (string.IsNullOrEmpty(line))
        {
            lines.Add(string.Empty);
            return lines;
        }

        if (!(maximumWidth > 0f) || font.Measure(line, scale) <= maximumWidth)
        {
            lines.Add(line);
            return lines;
        }

        StringBuilder current = new StringBuilder();
        for (int index = 0; index < line.Length; index++)
        {
            char value = line[index];
            if (current.Length > 0 &&
                font.Measure(current.ToString() + value, scale) > maximumWidth)
            {
                lines.Add(current.ToString());
                current.Length = 0;
            }

            current.Append(value);
        }

        if (current.Length > 0)
        {
            lines.Add(current.ToString());
        }

        return lines;
    }

    public float MeasureText(string text, float scale)
    {
        return font.Measure(text, scale);
    }

    public static Color32 QualityColor(int quality)
    {
        switch (quality)
        {
            case 1:
                return new Color32(64, 224, 96, 255);
            case 2:
                return new Color32(82, 150, 252, 255);
            case 3:
                return new Color32(210, 90, 255, 255);
            case 4:
                return new Color32(255, 180, 40, 255);
            case 5:
                return new Color32(183, 155, 98, 255);
            default:
                return new Color32(247, 245, 245, 255);
        }
    }

    public static Color LineColor(string line, int sourceLineIndex)
    {
        if (sourceLineIndex == 0)
        {
            return new Color32(255, 220, 120, 255);
        }

        if (line != null && line.StartsWith("套装", StringComparison.Ordinal))
        {
            return new Color32(96, 160, 255, 255);
        }

        if (line != null &&
            (line.Contains(" +") || line.Contains(" -")))
        {
            return new Color32(96, 230, 230, 255);
        }

        return Color.white;
    }

    public static Rect PlacePrimary(
        Vector2 mouse,
        float width,
        float height,
        float scale,
        int screenWidth,
        int screenHeight)
    {
        float normalX = mouse.x + PointerOffsetPixels * scale;
        float normalY = mouse.y + PointerOffsetPixels * scale;
        float x = normalX + width <= screenWidth
            ? normalX
            : mouse.x - width - PointerFallbackGapPixels * scale;
        float y = normalY + height <= screenHeight
            ? normalY
            : mouse.y - height - PointerFallbackGapPixels * scale;
        return new Rect(Mathf.Max(0f, x), Mathf.Max(0f, y), width, height);
    }

    public static Rect PlaceComparison(
        Rect primary,
        float width,
        float height,
        float scale,
        int screenWidth,
        int screenHeight)
    {
        float left = primary.x - width - ComparisonGapPixels * scale;
        float x = left >= 0f
            ? left
            : primary.x + primary.width + ComparisonGapPixels * scale;
        if (x + width > screenWidth)
        {
            x = Mathf.Max(0f, screenWidth - width);
        }

        float y = primary.y;
        if (y + height > screenHeight)
        {
            y = Mathf.Max(0f, screenHeight - height);
        }

        return new Rect(x, y, width, height);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        // Native request and renderer lifetime belong to the independent scene owner.
        LastPrimaryRect = default(Rect);
        LastComparisonRect = default(Rect);
        Ready = false;
        disposed = true;
    }

    private static Color PrefixColor(char prefix)
    {
        switch (prefix)
        {
            case '\u0001':
                return new Color32(120, 220, 120, 255);
            case '\u0002':
                return new Color32(128, 128, 128, 255);
            case '\u0004':
                return new Color32(255, 80, 80, 255);
            case '\u0005':
                return new Color32(86, 196, 255, 255);
            case '\u0006':
                return new Color32(255, 136, 48, 255);
            default:
                return Color.white;
        }
    }

    private static bool IsDirectColorPrefix(char prefix)
    {
        return prefix == '\u0001' || prefix == '\u0002' ||
               prefix == '\u0004' || prefix == '\u0005' ||
               prefix == '\u0006';
    }

    private static bool IsPositiveFinite(float value)
    {
        return value > 0f && IsFinite(value);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

}
