using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public sealed partial class RackShopDlg
    {
        public static RackShopDlg I;
        private GUIStyle _czEdit;
        private int czOk;
        private int czRAmount;
        private long czRCaiyu;
        private Texture2D czQrTex;

        // Official 0x06000a94: display aliases, without normalization of unknown inputs.
        private static string CzStd(string id)
        {
            if (id == "TRON") return "TRON(TRC20)";
            if (id == "BSC") return "BSC(BEP20)";
            if (id == "ETH") return "ETH(ERC20)";
            return id == "XLAYER" ? "X Layer" : id;
        }

        // Official 0x06000a95. A receipt cannot create or open the window.
        public static void OnDepositInfo(int ok, string chain, string address, int amount,
            long caiyu, string warning, string qrB64)
        {
            if (I == null) return;
            if (amount > 0 && caiyu > 0) czRate = unchecked((int)(caiyu / amount));
            I.czOk = ok;
            I.czRChain = chain;
            I.czAddr = address;
            I.czRAmount = amount;
            I.czRCaiyu = caiyu;
            I.czRWarn = warning;
            I.czLocalWarn = "";
            I.czQrB64 = qrB64 ?? "";
        }

        // Official 0x06000ace: this placeholder exists in the shipping client itself.
        private void DrawRechargePlaceholder()
        {
            Rect rect = L(34f, 65f, 776f, 524f);
            FrameBox(rect);
            LblC(new Rect(rect.x, rect.y + rect.height * 0.5f - 7f * s, rect.width, 14f * s),
                "充值返利(暂未开放)", YEL);
        }

        // Selection is drawn by the blank artwork; the network/amount behavior stays in the caller.
        private bool CzBtn(Rect rect, string label, bool selected, Vector2 mouse)
            => StateBtn(rect, null, label, mouse, selected);
    }
}
