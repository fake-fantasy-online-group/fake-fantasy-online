using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{
    // Official 1.5.8 token 060003c8. No caller is established for this diagnostic.
    private void DiagLayers()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null) return;

        Vector3 playerPosition = player.transform.position;
        SpriteRenderer playerRenderer = player.GetComponent<SpriteRenderer>();
        int playerOrder = playerRenderer ? playerRenderer.sortingOrder : 0;
        var layers = new List<(int, string, float)>();
        layers.Add((playerOrder, "★玩家", 0f));

        foreach (SpriteRenderer renderer in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(
                     FindObjectsInactive.Exclude))
        {
            if (renderer.gameObject == player) continue;
            if (Vector2.Distance(renderer.transform.position, playerPosition) > 3f) continue;
            layers.Add((renderer.sortingOrder, renderer.gameObject.name,
                renderer.transform.position.y - playerPosition.y));
        }

        // Original generated 060003fd subtracts the two order values, including overflow.
        layers.Sort((a, b) => unchecked(a.Item1 - b.Item1));
        var text = new StringBuilder(string.Format(
            "[诊断/层级] 玩家sortingOrder={0}, 附近精灵(按order升序=后面的在上层):\n", playerOrder));
        foreach (var layer in layers)
        {
            text.Append(string.Format("  order={0,-7} {1}  {2}  Δy={3:F2}\n",
                layer.Item1,
                layer.Item1 > playerOrder ? "↑在玩家上" :
                    layer.Item1 < playerOrder ? "↓在玩家下" : "=玩家",
                layer.Item2, layer.Item3));
        }
        // The shipping body discards this builder; it has no log or other output call.
    }
}

}
