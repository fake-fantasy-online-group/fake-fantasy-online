using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{

    // Official 060003db: arm64 0x423dd8 / x86_64 0x439050.
    public static void OnNetGatherSpots(List<ValueTuple<int, int, int, int, int>> zones)
    {
        PlayerController player = PlayerController.I;
        if (player != null && player.gatherZones != null)
        {
            foreach (MapLink.GatherZone zone in _netGatherZones)
                player.gatherZones.Remove(zone);
        }

        _netGatherZones.Clear();
        // A null input deliberately throws here, after removal and cache clearing.
        foreach (ValueTuple<int, int, int, int, int> row in zones)
        {
            MapLink.GatherZone zone = default;
            zone.procId = -1;
            zone.kind = row.Item1;
            zone.poly = new Vector2[4]
            {
                new Vector2(row.Item2, row.Item3),
                new Vector2(row.Item4, row.Item3),
                new Vector2(row.Item4, row.Item5),
                new Vector2(row.Item2, row.Item5),
            };
            _netGatherZones.Add(zone);
        }

        // The official method rechecks the captured player, not PlayerController.I.
        if (player != null)
        {
            if (player.gatherZones == null)
                player.gatherZones = new List<MapLink.GatherZone>();
            player.gatherZones.AddRange(_netGatherZones);
        }
    }

    // Official 060003dc: arm64 0x423004 / x86_64 0x438230.
    private void ApplyNetGatherZones()
    {
        if (_netGatherZones.Count == 0)
            return;

        // The host pcRef is the doll wrapper. Its property is a direct read of the
        // owned PlayerController; reread it where the official pcRef field is read.
        if (pcRef?.SourceControllerForIndependentRuntime != null)
        {
            if (pcRef.SourceControllerForIndependentRuntime.gatherZones == null)
                pcRef.SourceControllerForIndependentRuntime.gatherZones =
                    new List<MapLink.GatherZone>();

            foreach (MapLink.GatherZone zone in _netGatherZones)
            {
                if (!pcRef.SourceControllerForIndependentRuntime.gatherZones.Contains(zone))
                    pcRef.SourceControllerForIndependentRuntime.gatherZones.Add(zone);
            }
        }
    }
}

}
