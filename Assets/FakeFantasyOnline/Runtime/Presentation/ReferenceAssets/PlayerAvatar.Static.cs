using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerAvatar
{
    static PlayerAvatar()
    {
        _spriteCache = new Dictionary<string, Sprite>();
        _spriteBudget = new ArtBudget<string>("纸娃娃部件", SpriteBudgetBase, 600f);
        _liveAvatars = new List<WeakReference>();
        _avatarPurgeAt = 64;
        _inUseKeys = new HashSet<string>();
        HairRGB = new Color32[]
        {
            new Color32(215, 144, 59, 255),
            new Color32(132, 96, 75, 255),
            new Color32(238, 78, 55, 255),
            new Color32(181, 174, 163, 255),
            new Color32(59, 103, 203, 255),
            new Color32(80, 166, 26, 255),
            new Color32(178, 74, 193, 255),
            new Color32(85, 84, 84, 255)
        };
        LAYERORDER = new int[][]
        {
            new int[] { 6, 7, 4, 0, 3, 2, 1, 5, 8 },
            new int[] { 6, 7, 0, 4, 3, 2, 1, 5, 8 },
            new int[] { 6, 7, 0, 2, 3, 1, 4, 5, 8 },
            new int[] { 7, 6, 5, 0, 2, 3, 1, 4, 8 },
            new int[] { 6, 7, 0, 2, 3, 1, 4, 5, 8 },
            new int[] { 6, 7, 0, 4, 3, 2, 1, 5, 8 },
            new int[] { 6, 7, 4, 0, 3, 2, 1, 5, 8 },
            new int[] { 7, 4, 0, 3, 2, 1, 6, 5, 8 }
        };
        FRAME_TO_OCT = new int[] { 0, 1, 2, 3, 7 };
        TAG2LAYER = new int[] { 7, 1, 2, 3, 4, 5, 6 };
        _warnedFrames = new Dictionary<string, bool>();
        _partIni = new string[] { "npc/npc", "cap/cap", "face/face", "head/head", "backpack/backpack", "weaponl/weaponl", "weaponr/weaponr" };
        _fileCache = new Dictionary<string, byte[]>();
        _fileBudget = new ArtBudget<string>("纸娃娃源gsn(托管字节)", FileBudgetBase, 600f);
        SitPartArt = 2;
        ArtDetail.OnBudgetScaleChanged += ApplyBudgetScale;
        ApplyBudgetScale();
    }
}

}
