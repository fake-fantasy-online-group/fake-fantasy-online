using System.Text;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    // 0x060009c7. The original builds this diagnostic text without publishing it.
    private void DiagHere()
    {
        int sortingOrder = sr ? sr.sortingOrder : 0;
        StringBuilder text = new StringBuilder();
        text.Append(string.Format("[诊断/玩家] world=({0:F0},{1:F0}) sortingOrder={2}\n",
            worldX, worldY, sortingOrder));
        if (maskBytes != null && maskW > 0)
        {
            int cx = unchecked((int)(worldX / maskCW));
            int cy = unchecked((int)(worldY / maskCH));
            text.Append(string.Format("  掩码格({0},{1}) 脚下5×5 (0=可走, 80/81=阻挡):\n", cx, cy));
            for (int y = unchecked(cy - 2); y <= unchecked(cy + 2); y = unchecked(y + 1))
            {
                text.Append("    ");
                for (int x = unchecked(cx - 2); x <= unchecked(cx + 2); x = unchecked(x + 1))
                {
                    if (x < 0 || y < 0 || x >= maskW || y >= maskH)
                    {
                        text.Append(" --");
                    }
                    else
                    {
                        int value = maskBytes[unchecked(maskOff + y * maskW + x)];
                        text.Append(x == cx && y == cy
                            ? string.Format("[{0:X2}]", value)
                            : string.Format(" {0:X2}", value));
                    }
                }
                text.Append("\n");
            }
        }
    }
}
}
