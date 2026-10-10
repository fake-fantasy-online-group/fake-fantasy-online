using System.Text;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class RichCode
{
    private static readonly StringBuilder _sb = new StringBuilder(256);

    public static string Strip(string s)
    {
        if (string.IsNullOrEmpty(s) || s.IndexOf('/') < 0) return s;
        _sb.Clear();
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '/' && i + 1 < s.Length)
            {
                if (s[i + 1] == '[')
                {
                    int end = s.IndexOf(']', i);
                    if (end >= 0) { i = end; continue; }
                }
                else if (s[i + 1] == 'C' && i + 3 < s.Length)
                { i += 3; continue; }
            }
            _sb.Append(s[i]);
        }
        return _sb.ToString();
    }
}

}
