using System.Text;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class LoadTrace
{
    // Official 060001fb. Keep Single arithmetic and the default formatting culture.
    private static string Mb(long b)
    {
        return string.Format("{0:F1}MB", b / 1048576f);
    }

    // Official 060001fe. This returns the path without creating a directory.
    private static string OutDir()
    {
        return System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "traces");
    }

    // Official 060001fd captures the mutable tag once, after allocating the builder.
    private static string SafeTag()
    {
        StringBuilder sb = new StringBuilder();
        string tag = _tag;

        for (int i = 0; i < tag.Length; i++)
        {
            char c = tag[i];
            sb.Append(char.IsLetterOrDigit(c) ? c : '_');
        }

        string text = sb.ToString();
        if (text.Length > 40)
        {
            return text.Substring(0, 40);
        }

        return text.Length == 0 ? "trace" : text;
    }
}

}
