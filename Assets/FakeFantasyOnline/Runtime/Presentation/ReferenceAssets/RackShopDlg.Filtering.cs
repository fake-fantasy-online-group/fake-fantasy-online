using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public sealed partial class RackShopDlg
    {
        public struct FitItem
        {
            public int id;
            public string name;
            public byte kind;
            public string part;
            public int price;
            public int model;
            public string desc;
        }

        private const int Cols = 2;
        private const int Rows = 6;
        private const int PerPage = 12;
        private const int CartRows = 9;
        private const int CartQtyMax = 999;
        private int groupFilter;
        private int page;

        // Official 0x06000aa0. Keep wire order and the native five-part invalidation key.
        private void EnsureView()
        {
            if (groupFilter == viewGroup && goods.Count == viewCount && search == viewSearch &&
                shopPartTab == viewPart && fitCatalog.Count == viewCatalog) return;
            viewGroup = groupFilter;
            viewCount = goods.Count;
            viewSearch = search;
            view.Clear();
            viewPart = shopPartTab;
            viewCatalog = fitCatalog.Count;
            string query = string.IsNullOrEmpty(search) ? null : search;
            string[] parts = ShopPartArray();
            foreach (Good good in goods)
            {
                if (good.group == 255 || !(groupFilter == 0 || good.group == groupFilter - 1)) continue;
                if (query != null && (good.name == null || !good.name.Contains(query))) continue;
                if (parts != null && !string.Equals(ShopPartOf(good.id), parts[shopPartTab])) continue;
                view.Add(good);
            }
            page = 0;
        }

        // Official 0x06000aa1.
        private int ShopPartKind()
        {
            string name = CurCatName();
            if (string.IsNullOrEmpty(name)) return -1;
            if (name.Contains("部位灵")) return 1;
            return name.Contains("变装") ? 0 : -1;
        }

        // Official 0x06000aa2 and its byte-id comparison closure 0x06000ad8.
        private string CurCatName()
        {
            if (groupFilter < 1) return null;
            int index = groupFilter - 1;
            if (cats != null && cats.Count > 0)
            {
                var ordered = new List<(byte, string)>(cats);
                ordered.Sort((a, b) => a.Item1 - b.Item1);
                return index >= 0 && index < ordered.Count ? ordered[index].Item2 : null;
            }
            string[] fallback = { "圣变装", "法宝", "部位灵", "宠物材料", "功能道具", "坐骑" };
            return index < fallback.Length ? fallback[index] : null;
        }

        // Official 0x06000aa3: return the whole applicable array only for a valid selection.
        private string[] ShopPartArray()
        {
            int kind = ShopPartKind();
            if (kind < 0 || shopPartTab < 0) return null;
            string[] parts = kind == 0 ? AvatarPartOrder : SoulPartOrder;
            return shopPartTab < parts.Length ? parts : null;
        }

        // Official 0x06000aa4.
        private string ShopPartOf(int id)
        {
            string part = FindGood(id).part;
            return string.IsNullOrEmpty(part) ? null : part;
        }
    }
}
