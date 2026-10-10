using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class FittingRoomPreview
{
    public void ShowPet(int model)
    {
        EnsureCam();
        mode = 1;
        cam.enabled = true;
        if (actorRoot != null) actorRoot.SetActive(false);
        if (petGo == null)
        {
            petGo = new GameObject("PreviewPet");
            petGo.transform.SetParent(transform, false);
            petGo.transform.position = ORIGIN;
            petSr = petGo.AddComponent<SpriteRenderer>();
            petSr.sharedMaterial = _defMat;
            petSr.sortingOrder = 0;
            var go = new GameObject("PreviewPetGlow");
            go.transform.SetParent(petGo.transform, false);
            petGlowSr = go.AddComponent<SpriteRenderer>();
            petGlowSr.sharedMaterial = EffectPlayer.NpcGlowLayerMat();
            petGlowSr.sortingOrder = 1;
            petGlowSr.enabled = false;
        }
        petGo.SetActive(true);
        if (model == lastPet) return;
        lastPet = model;
        LoadPetGrid(model);
        petDirs = petGrid == null ? 1 : petGrid.GetLength(0);
        petDir = Mathf.Min(2, petDirs - 1);
        ApplyPetFrame();
        FramePet();
    }

    private Sprite CurPetSprite() => petGrid == null || petDirs <= 0
        ? null : petGrid[Mathf.Clamp(petDir, 0, petDirs - 1), 0];
    private Sprite CurPetGlowSprite() => petGlowGrid == null || petDirs <= 0 || petGlowGrid.GetLength(0) != petDirs
        ? null : petGlowGrid[Mathf.Clamp(petDir, 0, petDirs - 1), 0];

    private void ApplyPetFrame()
    {
        if (petSr != null)
        {
            Sprite sprite = CurPetSprite();
            petSr.sprite = sprite;
            petSr.enabled = sprite != null;
        }
        if (petGlowSr != null)
        {
            Sprite sprite = CurPetGlowSprite();
            petGlowSr.sprite = sprite;
            petGlowSr.enabled = sprite != null;
        }
    }

    private void FramePet()
    {
        Bounds bounds = PetBoundsAllDirs();
        if (bounds.size.y <= 0f)
        {
            cam.orthographicSize = 1.2f;
            cam.transform.position = ORIGIN + new Vector3(0f, .9f, -10f);
            return;
        }
        float aspect = rt != null && rt.height > 0 ? (float)rt.width / rt.height : 1f;
        float halfHeight = Mathf.Max(.25f, bounds.extents.y);
        float halfWidth = Mathf.Max(.25f, bounds.extents.x);
        cam.orthographicSize = Mathf.Max(halfHeight * 1.12f,
            aspect > 0f ? halfWidth * 1.12f / aspect : halfWidth * 1.12f);
        cam.transform.position = new Vector3(ORIGIN.x + bounds.center.x, ORIGIN.y + bounds.center.y, ORIGIN.z - 10f);
    }

    private Bounds PetBoundsAllDirs()
    {
        Bounds bounds = default;
        bool any = false;
        if (petGrid != null)
            for (int dir = 0; dir < petGrid.GetLength(0); dir++)
            {
                Sprite sprite = petGrid[dir, 0];
                if (sprite == null) continue;
                if (!any) bounds = sprite.bounds;
                else bounds.Encapsulate(sprite.bounds);
                Sprite glow = petGlowGrid != null && dir < petGlowGrid.GetLength(0) ? petGlowGrid[dir, 0] : null;
                if (glow != null) bounds.Encapsulate(glow.bounds);
                any = true;
            }
        return any ? bounds : default;
    }

    private void LoadPetGrid(int model)
    {
        DestroyPetGrids();
        if (model <= 0) return;
        int action = NpcActions.Find(model, "Stand");
        if (action <= 0) action = NpcActions.Find(model, "Walk");
        if (action > 0) TryLoadPetGrid(string.Format("avatar/npc/npc{0}.gsn", action));
        if (petGrid == null) TryLoadPetGrid(string.Format("avatar/npc/npc{0}01.gsn", model));
    }
}
}
