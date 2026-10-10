using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView : IFurnaceReceiveContext
{
    void IFurnaceReceiveContext.ReceiveFurnaceRecipes(int modType, IReadOnlyList<FurnaceRecipe> recipes)
    {
        var result = new List<ComposeDlg.Recipe>(recipes.Count);
        foreach (var recipe in recipes)
        {
            var mats = new List<ComposeDlg.Mat>(recipe.Materials.Count);
            foreach (var mat in recipe.Materials)
                mats.Add(new ComposeDlg.Mat { id = mat.Id, need = mat.Need, have = mat.Have,
                    name = mat.Name, icon = NetClient.LoadItemIcon(mat.Id) });
            result.Add(new ComposeDlg.Recipe { result = recipe.Result, name = recipe.Name,
                cost = recipe.Cost, canMake = recipe.CanMake, mats = mats, icon = NetClient.LoadItemIcon(recipe.Result) });
        }
        ComposeDlg.SetRecipes(modType, result);
    }

    void IFurnaceReceiveContext.ReceiveRefineInfo(int mode, int slot, int level, int cost, bool canDo,
        int successRate, int tab, int bagSlot, int itemId, IReadOnlyList<FurnaceMaterial> materials,
        int? safeLevel, bool? failDestroys)
    {
        var mats = ProjectFurnaceMaterials(materials);
        if (safeLevel.HasValue) CraftUpgradeUI.SafeLevel = safeLevel.Value;
        if (failDestroys.HasValue) CraftUpgradeUI.FailDestroys = failDestroys.Value;
        CraftUpgradeUI.SetInfo(mode, slot, level, cost, canDo, mats, successRate, tab, bagSlot, itemId);
    }

    void IFurnaceReceiveContext.ReceiveSocketInfo(int itemId, int level, IReadOnlyList<int> sockets,
        int cost, int successRate, bool canDrill, IReadOnlyList<FurnaceMaterial> materials,
        IReadOnlyList<(int, string)> names, int maxHoles, string reason, bool? failDestroys)
    {
        var named = new List<(int, string, Texture2D)>(names.Count);
        foreach (var name in names) named.Add((name.Item1, name.Item2, NetClient.LoadItemIcon(name.Item1)));
        if (failDestroys.HasValue) SocketUI.FailDestroys = failDestroys.Value;
        SocketUI.SetInfo(itemId, level, new List<int>(sockets), cost, successRate, canDrill,
            ProjectFurnaceMaterials(materials), named, maxHoles, reason);
    }

    private static List<CraftUpgradeUI.Mat> ProjectFurnaceMaterials(IReadOnlyList<FurnaceMaterial> materials)
    {
        var result = new List<CraftUpgradeUI.Mat>(materials.Count);
        foreach (var mat in materials)
            result.Add(new CraftUpgradeUI.Mat { id = mat.Id, need = mat.Need, have = mat.Have,
                name = mat.Name, icon = NetClient.LoadItemIcon(mat.Id) });
        return result;
    }
}
}
