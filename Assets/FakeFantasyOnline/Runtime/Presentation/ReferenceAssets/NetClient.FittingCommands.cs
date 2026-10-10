using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    internal partial class NetClient
    {
        private Action fittingCatalogWriter;
        private Action<byte, string, string, int, int> fittingPageWriter;
        private Action<IReadOnlyList<int>> fittingDescriptionWriter;

        internal void BindFittingCommandsForIndependentRuntime(Action catalog,
            Action<byte, string, string, int, int> page, Action<IReadOnlyList<int>> descriptions)
        {
            fittingCatalogWriter = catalog;
            fittingPageWriter = page;
            fittingDescriptionWriter = descriptions;
        }

        public void FittingCatalogForIndependentRuntime() => fittingCatalogWriter?.Invoke();
        public void FittingCatalogPageForIndependentRuntime(byte kind, string part, string search, int page, int perPage) =>
            fittingPageWriter?.Invoke(kind, part, search, page, perPage);
        public void FittingDescForIndependentRuntime(List<int> ids) => fittingDescriptionWriter?.Invoke(ids);
    }
}
