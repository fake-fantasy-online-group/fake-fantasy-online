using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerAvatar
{
    public struct Appear
    {
        public int Body;
        public int Cap;
        public int Backpack;
        public int WeaponR;
        public int WeaponL;
        public int Face;
    }

    private struct Layer
    {
        public int ox;
        public int oy;
        public int w;
        public int h;
        public Color32[] px;
        public byte[] px16;
        public int ax;
        public int ay;
        public int tag;
        public bool additive;
        public string key;
        public int scale;
    }

    public const int DIRS = 5;
    public const int FPD = 8;
    public int headPx;
    public int nameTopPx;
    public int sitTopPx;
    public int anchorX;
    public int anchorY;
    public int bodyBoundW;
    public Pose[] stand;
    public Pose[][] standAnim;
    public Pose[,] walk;
    public Pose[] sit;
    public Pose[] ride;
    public bool ok;
    private readonly Pose[][,] _atk;
    private readonly int[] _atkFpd;
    private readonly bool[] _atkBuilt;
    private Pose[,] _mag;
    private int _magFpd;
    private bool _magBuilt;
    private int _bodyModel;
    private int _fallbackBody;
    private bool _female;
    private DimgDecoder.Raw[] _headFr;
    private Appear _ap;
    private string _headKey;
    private float _ppu;
    private Dictionary<int, Pose[]> _poseCache;
    private Dictionary<int, Pose[,]> _poseFramesCache;
    private Dictionary<int, int> _poseFpdCache;
    private Dictionary<int, Sprite> _stillCache;
    private static readonly Dictionary<string, Sprite> _spriteCache;
    private static readonly ArtBudget<string> _spriteBudget;
    private const long SpriteBudgetBase = 268435456L;
    private const long FileBudgetBase = 201326592L;
    private static readonly List<WeakReference> _liveAvatars;
    private static int _avatarPurgeAt;
    private static readonly HashSet<string> _inUseKeys;
    private static readonly Color32[] HairRGB;
    private static readonly int[][] LAYERORDER;
    private static readonly int[] FRAME_TO_OCT;
    private static readonly int[] TAG2LAYER;
    private static readonly Dictionary<string, bool> _warnedFrames;
    private static int[][][][] _depth;
    private static readonly string[] _partIni;
    public const float StandFrameSec = 0.07f;
    public const float WingStandFrameSec = 0.105000004f;
    public int standBodyFpd;
    public int standWingFpd;
    private static readonly Dictionary<string, byte[]> _fileCache;
    private static readonly ArtBudget<string> _fileBudget;
    private const int KFRAMEMAX = 16;
    private const int KSTRIDE = 128;
    public static int SitPartArt;
    private const int AV_CHANNEL = 1;

    public PlayerAvatar()
    {
        _atk = new Pose[3][,];
        _atkFpd = new[] { 1, 1, 1 };
        _atkBuilt = new bool[3];
        _magFpd = 1;
        standBodyFpd = 1;
        standWingFpd = 1;
    }
}

}
