using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public sealed partial class RackShopDlg
    {
        private const float FitSearchDebounceSec = 0.35f;
        private const float FitPageRetrySec = 1.2f;

        // Official 0x06000abf.
        private void SyncFitPage(byte kind, string part, string search, int perPage)
        {
            if (NetClient.I == null) return;

            search ??= string.Empty;
            if (search != _fitSearchLast)
            {
                _fitSearchLast = search;
                _fitSearchChangedAt = Time.unscaledTime;
                fitPage = 0;
            }

            string key = kind + "|" + part + "|" + search + "|" + fitPage + "|" + perPage;
            if (key == _fitSentKey)
            {
                if (_fitAcked) return;
                if (Time.unscaledTime - _fitSentAt <= FitPageRetrySec) return;
                _fitSentAt = Time.unscaledTime;
            }
            else
            {
                if (Time.unscaledTime - _fitSearchChangedAt < FitSearchDebounceSec &&
                    _fitSentKey != null && SameExceptSearch(key, _fitSentKey)) return;

                _fitSentKey = key;
                _fitSentAt = Time.unscaledTime;
                _fitAcked = false;
            }

            NetClient.I.FittingCatalogPageForIndependentRuntime(kind, part, search, fitPage, perPage);
        }

        // Official 0x06000ac0.
        private static bool SameExceptSearch(string left, string right)
        {
            string[] leftParts = left.Split('|');
            string[] rightParts = right.Split('|');
            return leftParts.Length == 5 && rightParts.Length == 5 &&
                   leftParts[0] == rightParts[0] &&
                   leftParts[1] == rightParts[1] &&
                   leftParts[3] == rightParts[3] &&
                   leftParts[4] == rightParts[4];
        }
    }
}
