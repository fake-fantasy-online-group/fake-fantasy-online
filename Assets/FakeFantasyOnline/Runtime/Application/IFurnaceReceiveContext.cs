using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{
public sealed class FurnaceMaterial
{
    public int Id { get; }
    public int Need { get; }
    public int Have { get; }
    public string Name { get; }
    public FurnaceMaterial(int id, int need, int have, string name)
    { Id = id; Need = need; Have = have; Name = name; }
}

public sealed class FurnaceRecipe
{
    public int Result { get; }
    public string Name { get; }
    public int Cost { get; }
    public bool CanMake { get; }
    public IReadOnlyList<FurnaceMaterial> Materials { get; }
    public FurnaceRecipe(int result, string name, int cost, bool canMake, List<FurnaceMaterial> materials)
    { Result = result; Name = name; Cost = cost; CanMake = canMake; Materials = materials.AsReadOnly(); }
}

public interface IFurnaceReceiveContext
{
    void ReceiveFurnaceRecipes(int modType, IReadOnlyList<FurnaceRecipe> recipes);
    void ReceiveRefineInfo(int mode, int slot, int level, int cost, bool canDo, int successRate,
        int tab, int bagSlot, int itemId, IReadOnlyList<FurnaceMaterial> materials, int? safeLevel,
        bool? failDestroys);
    void ReceiveSocketInfo(int itemId, int level, IReadOnlyList<int> sockets, int cost, int successRate,
        bool canDrill, IReadOnlyList<FurnaceMaterial> materials, IReadOnlyList<(int, string)> names,
        int maxHoles, string reason, bool? failDestroys);
}
}
