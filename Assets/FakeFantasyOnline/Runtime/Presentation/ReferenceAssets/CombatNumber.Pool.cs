using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatNumber
{
    private static readonly Stack<CombatNumber> _pool = new Stack<CombatNumber>();
    private static Material overlayMat;
    private int _activeCount;
    private Texture2D _ownedTex;
    private Sprite _ownedSp;
    private Transform anchor;
    private float t;

    private static CombatNumber Rent()
    {
        CombatNumber number = null;
        while (_pool.Count > 0)
        {
            number = _pool.Pop();
            if (number) break;
        }
        if (number) number.gameObject.SetActive(true);
        else number = new GameObject("CombatNum").AddComponent<CombatNumber>();
        number.t = 0f;
        number._activeCount = 0;
        number.anchor = null;
        number.DropOwned();
        number.transform.localScale = Vector3.one;
        return number;
    }

    private SpriteRenderer NextChild()
    {
        SpriteRenderer renderer;
        if (_activeCount < _children.Count)
        {
            renderer = _children[_activeCount];
            renderer.gameObject.SetActive(true);
        }
        else
        {
            GameObject child = new GameObject("g");
            child.transform.SetParent(transform, false);
            renderer = child.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 32100;
            if (overlayMat) renderer.sharedMaterial = overlayMat;
            _children.Add(renderer);
        }
        _activeCount++;
        return renderer;
    }

    private void ReturnToPool()
    {
        ForgetExperienceScene();
        for (int i = 0; i < _children.Count; i++)
            if (_children[i]) _children[i].gameObject.SetActive(false);
        DropOwned();
        gameObject.SetActive(false);
        _pool.Push(this);
    }

    private void DropOwned()
    {
        if (_ownedSp)
        {
            Object.Destroy(_ownedSp);
            _ownedSp = null;
        }
        if (_ownedTex)
        {
            Object.Destroy(_ownedTex);
            _ownedTex = null;
        }
    }

    private void OnDestroy()
    {
        ForgetExperienceScene();
        DropOwned();
    }
}

}
