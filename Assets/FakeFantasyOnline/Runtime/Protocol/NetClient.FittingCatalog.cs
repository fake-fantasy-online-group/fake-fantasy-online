using System.Collections.Generic;

namespace FakeFantasyOnline.Protocol
{
    public sealed partial class NetClient
    {
        // Official 0x060006ec/0x060007a1: catalog and page share the same opcode.
        public void FittingCatalog() => Send(ProtocolOpcodes.FittingCatalogRequest, _ => { });

        // Official 0x060006ed/0x060007c5: no local page or per-page clamp.
        public void FittingCatalogPage(byte kind, string part, string search, int page, int perPage) =>
            Send(ProtocolOpcodes.FittingCatalogRequest, b =>
            {
                b.U8(kind);
                b.Str(part ?? "");
                b.Str(search ?? "");
                b.I32(page);
                b.I32(perPage);
            });

        // Official 0x060006ee/0x060007c7: the header truncates, but the loop writes every id.
        public void FittingDesc(List<int> ids) => Send(ProtocolOpcodes.FittingDescriptionRequest, b =>
        {
            b.U16(unchecked((ushort)ids.Count));
            for (int i = 0; i < ids.Count; i++) b.I32(ids[i]);
        });
    }
}
