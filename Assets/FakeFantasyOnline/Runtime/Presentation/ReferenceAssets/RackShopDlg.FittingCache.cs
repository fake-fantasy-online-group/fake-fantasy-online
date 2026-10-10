using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public sealed partial class RackShopDlg
    {
        private const int FitDescBatch = 48;
        private const float FitDescFlushSec = 0.25f;

        private static int fitPage;
        private static string _fitSentKey;
        private static bool _fitAcked;
        private static int fitTotal;
        private static bool fitPaged;

        // Official 0x06000a9e.
        public static void AddFittingCatalog(List<FitItem> chunk, bool more)
        {
            if (chunk != null)
            {
                foreach (FitItem item in chunk)
                {
                    if (_fitAccIds.Add(item.id)) _fitAcc.Add(item);
                }
            }

            if (more) return;

            fitCatalog = new List<FitItem>(_fitAcc);
            _fitAcc.Clear();
            _fitAccIds.Clear();
            fitById.Clear();
            foreach (FitItem item in fitCatalog) fitById[item.id] = item;
            _fitDesc.Clear();
            _fitDescWant.Clear();
        }

        // Official 0x06000ac6.
        public static void AddFittingDesc(int id, string name, int price, string desc)
        {
            _fitDesc[id] = (name ?? string.Empty, price, desc ?? string.Empty);
        }

        // Official 0x06000ac7.
        public static void SetFittingPage(List<FitItem> page, int total, int pageNo)
        {
            fitCatalog = page ?? new List<FitItem>();
            fitById.Clear();
            foreach (FitItem item in fitCatalog) fitById[item.id] = item;
            fitTotal = total;
            fitPaged = true;
            _fitAcked = true;
            _fitAcc.Clear();
            _fitAccIds.Clear();

            if (fitPage == pageNo) return;

            fitPage = pageNo;
            if (_fitSentKey == null) return;

            string[] parts = _fitSentKey.Split('|');
            if (parts.Length != 5) return;
            parts[3] = pageNo.ToString();
            _fitSentKey = string.Join("|", parts);
        }

        // Official 0x06000ac8.
        public static void ResetFittingCache()
        {
            fitCatalog = new List<FitItem>();
            fitById.Clear();
            _fitAcc.Clear();
            _fitAccIds.Clear();
            _fitDesc.Clear();
            _fitDescWant.Clear();
            _fitSentKey = null;
            _fitAcked = false;
            _fitSentAt = -99f;
            fitTotal = 0;
            fitPaged = false;
            fitPage = 0;
        }

        // Official 0x06000ac9.
        private static void WantFitDesc(int id)
        {
            if (id == 0 || _fitDesc.ContainsKey(id) || _fitDescWant.Contains(id)) return;
            _fitDescWant.Add(id);
        }

        // Official 0x06000aca.
        private static void FlushFitDescWant()
        {
            if (_fitDescWant.Count == 0 || NetClient.I == null) return;

            float now = Time.unscaledTime;
            if (now - _fitDescFlushAt < FitDescFlushSec) return;

            _fitDescFlushAt = Time.unscaledTime;
            int count = Math.Min(FitDescBatch, _fitDescWant.Count);
            List<int> batch = _fitDescWant.GetRange(0, count);
            _fitDescWant.Clear();
            NetClient.I.FittingDescForIndependentRuntime(batch);
        }

        // Official 0x06000acb.
        private string FitTip(int id)
        {
            string name = null;
            int price = 0;
            string desc = null;
            if (fitById.TryGetValue(id, out FitItem item))
            {
                name = item.name;
                price = item.price;
                desc = item.desc;
            }

            if (_fitDesc.TryGetValue(id, out var cached))
            {
                name = cached.name;
                price = cached.price;
                desc = cached.desc;
            }

            if (name == null)
            {
                WantFitDesc(id);
                return string.Empty;
            }

            if (string.IsNullOrEmpty(desc))
            {
                WantFitDesc(id);
                desc = name;
            }
            return desc + "\n价格: " + price + " 彩玉";
        }
    }
}
