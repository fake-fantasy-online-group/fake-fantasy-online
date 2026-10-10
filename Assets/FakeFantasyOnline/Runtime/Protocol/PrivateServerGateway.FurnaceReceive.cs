using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    // Official 1.5.8 Dispatch 0600077f: cases 8020/8021/8024. Optional destruction
    // metadata updates only when present; material/result identifiers remain server-authored.
    private static void ReceiveFurnaceRecipes(byte[] payload, IFurnaceReceiveContext context)
    {
        var r = new NetClient.Buf(payload);
        int mode = r.U8(), count = r.U16();
        var recipes = new List<FurnaceRecipe>(count);
        for (int i = 0; i < count; i++)
        {
            int result = r.I32(); string name = r.Str(); int cost = r.I32(); bool canMake = r.U8() != 0;
            recipes.Add(new FurnaceRecipe(result, name, cost, canMake, ReadFurnaceMaterials(r)));
        }
        context.ReceiveFurnaceRecipes(mode, recipes.AsReadOnly());
    }

    private static List<FurnaceMaterial> ReadFurnaceMaterials(NetClient.Buf r)
    {
        int count = r.U16();
        var materials = new List<FurnaceMaterial>(count);
        for (int i = 0; i < count; i++)
            materials.Add(new FurnaceMaterial(r.I32(), r.I32(), r.I32(), r.Str()));
        return materials;
    }

    private static void ReceiveRefineInfo(byte[] payload, IFurnaceReceiveContext context)
    {
        var r = new NetClient.Buf(payload);
        int mode = r.U8(), slot = r.U8(), level = r.I32(), cost = r.I32();
        bool canDo = r.U8() != 0;
        int success = r.I32(), tab = r.U8(), bagSlot = r.I32(), itemId = r.I32();
        var materials = ReadFurnaceMaterials(r);
        int? safeLevel = null; bool? destroys = null;
        if (r.Remaining >= 5) { safeLevel = r.I32(); destroys = r.U8() != 0; }
        context.ReceiveRefineInfo(mode, slot, level, cost, canDo, success, tab, bagSlot, itemId,
            materials.AsReadOnly(), safeLevel, destroys);
    }

    private static void ReceiveSocketInfo(byte[] payload, IFurnaceReceiveContext context)
    {
        var r = new NetClient.Buf(payload);
        r.U8();
        int itemId = r.I32(), level = r.I32(), count = r.U8();
        var sockets = new List<int>(count);
        for (int i = 0; i < count; i++) sockets.Add(r.I32());
        int cost = r.I32(), success = r.I32(); bool canDrill = r.U8() != 0;
        var materials = ReadFurnaceMaterials(r);
        int namedCount = r.U8();
        var names = new List<(int, string)>(namedCount);
        for (int i = 0; i < namedCount; i++) names.Add((r.I32(), r.Str()));
        int maxHoles = 5; string reason = ""; bool? destroys = null;
        if (r.Remaining > 0) { maxHoles = r.U8(); reason = r.Str(); }
        if (r.Remaining > 0) destroys = r.U8() != 0;
        context.ReceiveSocketInfo(itemId, level, sockets.AsReadOnly(), cost, success, canDrill,
            materials.AsReadOnly(), names.AsReadOnly(), maxHoles, reason, destroys);
    }
}
}
