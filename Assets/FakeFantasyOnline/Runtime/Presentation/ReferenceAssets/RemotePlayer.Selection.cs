using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class RemotePlayer : ISelectable
{
    public Transform SelTransform => this != null ? transform : null;
    public string SelName => pname;
    public int SelLevel => level;
    public int SelHp => PartyHp(out int value, out _) ? value : hp;
    public int SelMaxHp => PartyHp(out _, out int value) ? value : Mathf.Max(1, maxHp);
    public bool SelFriendly => !HostileToLocal;
    public int SelOwnerId => -1;
    public Vector3 SelFeetPos => transform.position;

    // Official 1.5.8 06000b32: Unity dispatches this on the live Init-created collider.
    // It is independent of PlayerController.Update and has only the GameHud.OverUI gate.
    // Internal visibility lets the existing explicit diagnostic entry use this same body.
    internal void OnMouseDown()
    {
        if (GameHud.OverUI) return;
        if (HostileToLocal)
        {
            CombatWorld.SetPvpTarget(this);
        }
        else
        {
            TargetSelect.Select(this);
        }
    }

    private bool PartyHp(out int chp, out int cmax)
    {
        chp = 0;
        cmax = 0;
        foreach (var member in SocialClient.Party)
        {
            if (member.Item1 == id)
            {
                chp = member.Item4;
                cmax = member.Item5;
                return cmax > 0;
            }
        }
        return false;
    }
}

}
