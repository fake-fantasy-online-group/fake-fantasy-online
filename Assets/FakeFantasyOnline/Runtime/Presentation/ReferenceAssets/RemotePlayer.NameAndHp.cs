using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class RemotePlayer
{
    private GameObject tagGo;
    private SpriteRenderer tagSr;
    private string lastTagName;
    private string lastTagTitle;
    private bool lastTagReady;
    private byte lastTagPk;
    private bool lastTagLeader;
    private static Material tagMat;

    // 06000b33. Cache writes precede baking; a failed bake retains the old sprite.
    private void UpdateNameTag()
    {
        long stamp = LoadTrace.On ? LoadTrace.SegStamp() : 0;
        try
        {
            if (string.IsNullOrEmpty(pname) || !BitmapFont.Ready) return;
            if (_hideEntity)
            {
                if (tagGo) tagGo.SetActive(false);
                return;
            }
            if (CharData.nameMode >= 1 && !_f11Exempt)
            {
                if (tagGo) tagGo.SetActive(false);
                return;
            }
            GameSettings.EnsureLoaded();
            if (!GameSettings.ShowPlayerNames)
            {
                if (tagGo) tagGo.SetActive(false);
                return;
            }
            if (tagGo && !tagGo.activeSelf) tagGo.SetActive(true);
            string effectiveTitle = _isStallOwner ? string.Empty : title ?? string.Empty;
            string team = GameSettings.ShowTeamNames && SocialClient.InParty(id)
                ? SocialClient.TeamName() : string.Empty;
            bool leader = SocialClient.PartyLeaderId != 0 && id == SocialClient.PartyLeaderId;
            if (!tagSr || !tagSr.sprite || pname != lastTagName ||
                effectiveTitle != lastTagTitle || team != lastTagTeam || pkState != lastTagPk ||
                lastTagReady != BitmapFont.Ready || lastTagLeader != leader)
            {
                lastTagName = pname;
                lastTagTitle = effectiveTitle;
                lastTagTeam = team;
                lastTagPk = pkState;
                lastTagReady = BitmapFont.Ready;
                lastTagLeader = leader;
                Sprite old = tagSr ? tagSr.sprite : null;
                string shownName = string.IsNullOrEmpty(team) ? pname : pname + "｜" + team;
                Color32 nameColor = pkState == 1 ? new Color32(170, 170, 170, 255)
                    : pkState == 2 ? new Color32(255, 64, 64, 255) : (Color32)NameCol;
                Color titleColor = TitleCol;
                Texture2D icon = leader ? LeaderFlag.Tex : null;
                Texture2D texture = BitmapFont.BakeNameTag(shownName, effectiveTitle,
                    nameColor, titleColor, 1, null, null, null, null, icon);
                if (!texture) return;
                if (!tagGo)
                {
                    tagGo = new GameObject("NameTag");
                    tagGo.transform.SetParent(transform, false);
                    tagSr = tagGo.AddComponent<SpriteRenderer>();
                    tagSr.sortingOrder = 32000;
                    if (!tagMat)
                    {
                        Shader shader = Shader.Find("Overlay/NameTag");
                        if (shader) tagMat = new Material(shader);
                    }
                    if (tagMat) tagSr.sharedMaterial = tagMat;
                }
                tagSr.sprite = Sprite.Create(texture,
                    new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0f), ppu);
                if (old)
                {
                    Texture2D previousTexture = old.texture;
                    Object.Destroy(old);
                    if (previousTexture) Object.Destroy(previousTexture);
                }
            }
            float y = HeadAnchorY(out float offX);
            if (tagGo) PlayerNameTag.PlaceSharp(tagGo.transform, y, ppu, offX);
        }
        finally
        {
            if (LoadTrace.On) LoadTrace.AddSeg(LoadTrace.Seg.玩_名牌血条, stamp);
        }
    }

    // 06000b35. Height and anchor are sampled even when the bar is hidden.
    private void UpdateHpBar()
    {
        long stamp = LoadTrace.On ? LoadTrace.SegStamp() : 0;
        try
        {
            bool show = maxHp >= 1 && !_isStallOwner &&
                (CharData.nameMode < 2 || _f11Exempt) && !_hideEntity;
            float headY = HeadAnchorY(out float offX);
            float hpY = headY + 29f / ppu;
            if (show && tagGo && tagGo.activeSelf && tagSr && tagSr.sprite)
                hpY = Mathf.Max(hpY, headY + unchecked(tagSr.sprite.texture.height + 2) / ppu);
            hpBarView.Tick(transform, show, hp, maxHp, hpY, offX);
        }
        finally
        {
            if (LoadTrace.On) LoadTrace.AddSeg(LoadTrace.Seg.玩_名牌血条, stamp);
        }
    }
    internal void HideHeadFeedbackForIndependentRuntime()
    {
        if (tagGo) tagGo.SetActive(false);
        hpBarView.Tick(transform, false, hp, maxHp, 0f, 0f);
    }

    internal void UpdateHeadFeedbackForIndependentRuntime()
    {
        bool hidden = _hideEntity;
        UpdateNameTag();
        UpdateHpBar();
        if (hidden)
        {
            if (stallSign) stallSign.Tick(false, 0, null, 0f);
        }
        else UpdateStallSign();
    }

    internal HeadHpBarView HeadHpViewForIndependentRuntime => hpBarView;

}

}
