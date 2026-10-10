using System;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public sealed partial class RackShopDlg
    {
        // Official 0x06000ad0.
        private void DrawRecharge(Vector2 mouse)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.28f);
            GUI.DrawTexture(L(34f, 65f, 776f, 524f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.color = new Color(0.12f, 0.1f, 0.08f, 0.96f);
            GUI.DrawTexture(L(112f, 88f, 620f, 478f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (BitmapFont.Ready)
                BitmapFont.CenterOutline(L(112f, 102f, 620f, 22f), "USDT 充值", GOLD,
                    Mathf.Max(1f, (float)Math.Round(s * 1.7f)));
            LblC(L(112f, 134f, 620f, 14f), "到账后自动把彩玉发到你的邮箱", GRAY);
            Lbl(150f, 166f, "① 选择网络(只支持这四个, 务必和你钱包提币的网络一致):", new Color(0.95f, 0.95f, 0.92f, 1f));
            for (int i = 0; i < CzChains.Length; i++)
            {
                if (CzBtn(L(150f + i * 136f, 188f, 128f, 32f), CzStd(CzChains[i]), czChain == CzChains[i], mouse)
                    && czChain != CzChains[i])
                {
                    czChain = CzChains[i];
                    czOk = 0;
                    czAddr = "";
                    czRWarn = "";
                }
            }
            Lbl(150f, 236f, "② 充值金额(USDT):", new Color(0.95f, 0.95f, 0.92f, 1f));
            for (int i = 0; i < CzQuick.Length; i++)
            {
                if (CzBtn(L(150f + i * 96f, 258f, 88f, 32f), CzQuick[i].ToString(), czAmount == CzQuick[i].ToString(), mouse))
                {
                    czAmount = CzQuick[i].ToString();
                    czLocalWarn = "";
                }
            }
            if (_czEdit == null)
                _czEdit = new GUIStyle(GUI.skin.textField) { font = NpcChatter.CnFont(), alignment = TextAnchor.MiddleCenter };
            _czEdit.fontSize = (int)Math.Round(s * 14f);
            string edit = GUI.TextField(L(150f + CzQuick.Length * 96f, 258f, 110f, 32f), czAmount ?? "", 9, _czEdit);
            if (edit != czAmount)
            {
                var digits = new StringBuilder();
                foreach (char c in edit ?? "") if (char.IsDigit(c)) digits.Append(c);
                czAmount = digits.ToString();
                czLocalWarn = "";
            }
            int amount = int.TryParse(czAmount, out int parsed) ? parsed : 0;
            if (BitmapFont.Ready)
                BitmapFont.DrawOutline(winX + 150f * s, winY + 304f * s, "= " + ((long)amount * czRate) + " 彩玉",
                    new Color(0.62f, 0.82f, 0.95f, 1f), Mathf.Max(1f, (float)Math.Round(s * 1.4f)));
            if (!string.IsNullOrEmpty(czLocalWarn)) Lbl(300f, 306f, czLocalWarn, new Color(1f, 0.45f, 0.4f, 1f));
            if (StateBtn(L(302f, 338f, 240f, 38f), null, "获取充值地址", mouse))
            {
                if (amount < 10) czLocalWarn = "最小充值金额 10 USDT";
                else if (NetClient.I != null)
                {
                    czLocalWarn = "";
                    NetClient.I.RequestDepositForIndependentRuntime(czChain, amount);
                }
            }
            if (czOk != 1 || string.IsNullOrEmpty(czAddr))
            {
                if (czOk == 0 && !string.IsNullOrEmpty(czRWarn)) Lbl(150f, 392f, czRWarn, new Color(1f, 0.45f, 0.4f, 1f));
                return;
            }
            int displayAmount = amount >= 10 ? amount : czRAmount;
            Lbl(150f, 392f, string.Format("请向以下地址转 {0} USDT(仅 {1}):", displayAmount, CzStd(czRChain)), new Color(0.95f, 0.95f, 0.92f, 1f));
            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(L(150f, 414f, 322f, 26f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            Lbl(156f, 418f, czAddr, GOLD);
            if (StateBtn(L(480f, 414f, 56f, 26f), null, "复制", mouse))
            {
                GUIUtility.systemCopyBuffer = czAddr;
                czCopiedAt = Time.unscaledTime;
            }
            if (Time.unscaledTime - czCopiedAt < 1.5f) Lbl(480f, 444f, "已复制", new Color(0.4f, 1f, 0.4f, 1f));
            Lbl(150f, 450f, "⚠ 只能用上面这个网络转入!", new Color(1f, 0.45f, 0.4f, 1f));
            Lbl(150f, 468f, "上面四个之外的网络不会自动到账,得联系客服人工处理", new Color(1f, 0.45f, 0.4f, 1f));
            Lbl(150f, 490f, string.Format("到账后自动发 {0} 彩玉到邮箱(约1分钟)。", (long)displayAmount * czRate), new Color(0.62f, 0.82f, 0.95f, 1f));
            if (czQrB64 != czQrBuilt)
            {
                czQrBuilt = czQrB64;
                if (czQrTex != null)
                {
                    Destroy(czQrTex);
                    czQrTex = null;
                }
                if (!string.IsNullOrEmpty(czQrB64))
                {
                    try
                    {
                        byte[] bytes = Convert.FromBase64String(czQrB64);
                        var texture = new Texture2D(2, 2);
                        texture.filterMode = FilterMode.Point;
                        if (ImageConversion.LoadImage(texture, bytes)) czQrTex = texture;
                    }
                    catch
                    {
                        czQrTex = null;
                    }
                }
            }
            if (czQrTex != null)
            {
                LblC(L(564f, 388f, 130f, 14f), "手机钱包扫码", new Color(0.95f, 0.95f, 0.92f, 1f));
                GUI.DrawTexture(L(564f, 404f, 130f, 130f), czQrTex);
            }
        }
    }
}
