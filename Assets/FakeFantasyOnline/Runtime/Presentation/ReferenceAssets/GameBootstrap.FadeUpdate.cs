using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{
    private static Material fadeTransMat;

    private static Material FadeTransMat()
    {
        if (fadeTransMat == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) fadeTransMat = new Material(shader) { name = "OccFade(shared)" };
        }
        return fadeTransMat;
    }

    private void UpdateFade()
    {
        if (!EnableOccFade) return;
        PlayerController controller = pcRef?.SourceControllerForIndependentRuntime;
        if (controller == null || fadeRegs.Count == 0) return;
        Vector3 position = controller.transform.position;
        int playerOrder = (int)controller.worldY;
        float bodyY = position.y + occBodyUp;
        float step = Time.deltaTime * 6f;
        Material transparent = FadeTransMat();
        for (int i = 0; i < fadeRegs.Count; i++)
        {
            FadeObj item = fadeRegs[i];
            bool covered = !(item.order <= playerOrder || position.x < item.xMin || item.xMax < position.x ||
                bodyY < item.yBot || item.yTop < bodyY);
            float target = covered ? occFadeAlpha : 1f;
            if (!covered && !(item.a < 1f)) continue;
            item.a = Mathf.MoveTowards(item.a, target, step);
            for (int j = 0; j < item.srs.Length; j++)
            {
                SpriteRenderer renderer = item.srs[j];
                if (renderer == null) continue;
                if (item.a >= 0.999f)
                {
                    if (item.orig[j] != null && renderer.sharedMaterial != item.orig[j])
                        renderer.sharedMaterial = item.orig[j];
                }
                else if (transparent != null && renderer.sharedMaterial != transparent)
                    renderer.sharedMaterial = transparent;
                Color color = renderer.color;
                color.a = item.a;
                renderer.color = color;
            }
        }
    }
}

}
