using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class MonsterEntity
{
    public void Init(int id, int model, string name, int level, float wx, float wy,
        int hp, int maxHp, float ppu, float worldW, float worldH, bool friendly = false, int elite = 0)
    {
        this.id = id;
        this.model = model;
        mname = name;
        this.level = level;
        this.hp = hp;
        this.maxHp = maxHp;
        worldX = wx;
        worldY = wy;
        this.ppu = ppu;
        this.worldW = worldW;
        this.worldH = worldH;
        this.friendly = friendly;
        eliteKind = (byte)(elite & 0x7f);
        eliteFresh = ((elite >> 7) & 1) != 0;
        transform.position = new Vector3((wx - this.worldW * 0.5f) / this.ppu,
            (this.worldH * 0.5f - wy) / this.ppu, 0f);
        body = gameObject.AddComponent<SpriteRenderer>();
        LoadAnim(model);
        body.sprite = StandSprite(curDir);
        body.sortingOrder = Mathf.Clamp(unchecked((int)wy), 0, 30000);
        shadow = ReferenceWorldBlobShadow.Attach(transform, body, 0f);
        SyncShadowSize();
        col = gameObject.AddComponent<BoxCollider2D>();
        UpdateHitBox();
        EnsureArtDerived();
        if (hp < this.hp && !dying) flashTimer = 0.12f;
        this.hp = Mathf.Max(0, hp);
        if (friendly)
        {
            if (petOwnerId == NetClient.SelfEntityId || !CombatNumber.PetTransform)
                CombatNumber.RegisterPet(transform, 80f / ppu);
            bool show = CharData.petShow && CharData.petShowKnown;
            if (body) body.enabled = show;
            if (col) col.enabled = this.friendly && petOwnerId == NetClient.SelfEntityId ? false : show;
        }
        else if (eliteFresh && !ally)
            EffectPlayer.PlayNamed(transform.position, "guaiwudansheng", ppu, 1, -1, false, false);
        if (eliteKind != 0) ShowEliteFx();
    }

    internal ReferenceWorldBlobShadow ShadowForIndependentRuntime => shadow;
}

}
