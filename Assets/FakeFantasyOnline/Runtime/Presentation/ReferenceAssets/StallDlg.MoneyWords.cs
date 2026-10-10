using System.Text;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class StallDlg
{
    public static string MoneyWords(long m)
    {
        if (m <= 0) return "0铜";
        var text = new StringBuilder();
        long gold = m / 1000000;
        long silver = (m % 1000000) / 1000;
        long copper = m % 1000;
        if (gold > 0) text.Append(gold).Append('金');
        if (silver > 0) text.Append(silver).Append('银');
        if (copper > 0 || text.Length == 0) text.Append(copper).Append('铜');
        return text.ToString();
    }
}
}
