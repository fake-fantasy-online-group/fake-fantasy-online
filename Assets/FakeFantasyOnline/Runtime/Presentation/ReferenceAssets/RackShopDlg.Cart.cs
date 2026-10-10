using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public sealed partial class RackShopDlg : MonoBehaviour
    {
        public struct Good
        {
            public int id;
            public string name;
            public string desc;
            public int weight;
            public byte group;
            public int price;
            public byte flags;
            public int remain;
            public string part;
            public byte blocked;
            public Texture2D icon;
        }

        private int cartScroll;
        private int prunedCount;
        private float prunedUntil;

        // Official 0x06000a99. Only removed goods are pruned; retained stock is not reclamped.
        private void PruneCart()
        {
            if (cartOrder.Count == 0) return;
            int removed = 0;
            for (int i = cartOrder.Count - 1; i >= 0; i--)
            {
                int id = cartOrder[i];
                bool found = false;
                foreach (Good good in goods)
                    if (good.id == id) { found = true; break; }
                if (found) continue;
                cart.Remove(id);
                qtyEdit.Remove(id);
                cartOrder.RemoveAt(i);
                removed++;
            }
            if (removed > 0)
            {
                prunedCount = removed;
                prunedUntil = Time.realtimeSinceStartup + 20f;
                cartScroll = 0;
            }
        }

        // Official 0x06000aa5.
        private Good FindGood(int id)
        {
            foreach (Good good in goods)
                if (good.id == id) return good;
            return default;
        }

        // Official 0x06000aa6 preserves null/empty and does not add an ellipsis.
        private static string Trunc(string t, int n)
        {
            return string.IsNullOrEmpty(t) || t.Length <= n ? t : t.Substring(0, n);
        }

        // Official 0x06000aa7.
        private static bool IsGold(byte flags) => (flags & 4) != 0;

        // Official 0x06000aa8. The 999 editor limit is not an AddToCart branch.
        private void AddToCart(int id)
        {
            Good good = FindGood(id);
            bool hadQuantity = cart.TryGetValue(id, out int quantity);
            if (good.remain >= 0 && (hadQuantity ? quantity : 0) >= good.remain) return;
            if (!cart.ContainsKey(id))
            {
                cart[id] = 0;
                cartOrder.Add(id);
            }
            cart[id]++;
            qtyEdit[id] = cart[id].ToString();
        }

        // Official 0x06000aa9. Nonzero sources require one native request per unit.
        private void CheckoutCart(byte src)
        {
            if (cart.Count == 0 || NetClient.I == null) return;
            foreach (int id in cartOrder)
            {
                bool found = false;
                foreach (Good good in goods)
                    if (good.id == id) { found = true; break; }
                if (!found) continue;
                int quantity = cart[id];
                if (src == 0)
                    NetClient.I.BuyRackForIndependentRuntime(id, 0, quantity);
                else
                    for (int i = 0; i < quantity; i++) NetClient.I.BuyRackForIndependentRuntime(id, src, 1);
            }
            cart.Clear();
            cartOrder.Clear();
            qtyEdit.Clear();
            NetClient.I.OpenRackForIndependentRuntime();
        }
    }
}
