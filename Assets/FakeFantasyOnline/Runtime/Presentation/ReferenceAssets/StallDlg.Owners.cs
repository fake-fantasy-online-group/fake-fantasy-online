using System.Collections.Generic;
using UnityEngine;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class StallDlg : MonoBehaviour
{
    public static readonly Dictionary<int, (byte, string)> Owners = new Dictionary<int, (byte, string)>();
    public static bool IsStallOwner(int id) => Owners.ContainsKey(id);

    internal static void BindOwnersForIndependentRuntime(IReadOnlyDictionary<int, WorldStallOwnerState> values)
    {
        Owners.Clear();
        foreach (var pair in values)
            Owners[pair.Key] = ((byte)pair.Value.StallType, pair.Value.DisplayName);
    }
}

}
