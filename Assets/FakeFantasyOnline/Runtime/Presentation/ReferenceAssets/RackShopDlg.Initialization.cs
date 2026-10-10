using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public sealed partial class RackShopDlg
    {
        // Official 0x06000ad5: one ordered declaration block preserves allocation/publication
        // order across partial files. Keep implicit initialization and native BeforeFieldInit.
        private static readonly string[] CzChains = { "TRON", "BSC", "ETH", "XLAYER" };
        private static readonly int[] CzQuick = { 10, 50, 100, 1000 };
        private static int czRate = 100;
        private static List<(byte, string)> cats = new List<(byte, string)>();
        private static List<Good> goods = new List<Good>();
        private static bool refundTabOn = true;
        private static List<RefundRow> refundRows = new List<RefundRow>();
        private static int refundPct = 80;
        private static int refundDays = 30;
        private static string refundCats = "";
        private static bool refundUsedOk = true;
        private static readonly string[] FitSlotCat = { "cap", "face", "body", "backpack", "weaponr" };
        private static readonly string[] FitSlotName = { "帽", "面", "衣", "背", "武" };
        private static readonly string[] SoulPartOrder = { "戒指", "项链", "手镯", "鞋", "帽", "衣服", "面饰", "背饰", "武器", "法阵" };
        private static readonly string[] AvatarPartOrder = { "头盔", "面饰", "服装", "背包", "武器" };
        private static List<FitItem> fitCatalog = new List<FitItem>();
        private static readonly Dictionary<int, FitItem> fitById = new Dictionary<int, FitItem>();
        private static readonly List<FitItem> _fitAcc = new List<FitItem>();
        private static readonly HashSet<int> _fitAccIds = new HashSet<int>();
        private static readonly Color GOLD = new Color(1f, 0.92f, 0.66f, 1f);
        private static readonly Color GRAY = new Color(0.62f, 0.60f, 0.52f, 1f);
        private static readonly Color YEL = new Color(1f, 1f, 0.35f, 1f);
        private static string _fitSearchLast = string.Empty;
        private static float _fitSearchChangedAt = -99f;
        private static float _fitSentAt = -99f;
        private static readonly Dictionary<int, (string name, int price, string desc)> _fitDesc =
            new Dictionary<int, (string name, int price, string desc)>();
        private static readonly List<int> _fitDescWant = new List<int>();
        private static float _fitDescFlushAt = -99f;
    }
}
