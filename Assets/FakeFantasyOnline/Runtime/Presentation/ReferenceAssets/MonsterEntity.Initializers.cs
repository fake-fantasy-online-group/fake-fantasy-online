using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class MonsterEntity
{
    // Official 1.5.8 06000664/06000665 initialize these fields in this order.
    // Keeping initializer-bearing declarations in one partial preserves the order while
    // retaining implicit constructors and the type's existing beforefieldinit behavior.
    // Instance constructor: field initialization precedes MonoBehaviour construction.
    private int _heldGen = -1;
    public float ppu = 100f;
    public float worldW = 18432f;
    public float worldH = 10752f;
    private readonly HeadHpBarView hpBarView = new HeadHpBarView();
    private int curDir = 4;
    private float headTopU = 1f;
    private float walkIntervalMs = 120f;
    private float dieIntervalMs = 140f;
    private float atkIntervalMs = 120f;
    private int atkFrame = -1;
    private float prepIntervalMs = 140f;
    private int prepFrame = -1;
    private float moveSpeed = 120f;
    private bool petShownPrev = true;
    private int fidgetFrame = -1;
    private float handFrameSec = 0.12f;
    private int _anyHintD = -1;
    private int _anyHintF = -1;
    private float catchUp = 1f;
    private float lastPathTime = -1f;
    private int _glowVerSeen = -1;
    private readonly Dictionary<string, int> statusMode = new Dictionary<string, int>();
    private readonly Dictionary<string, float> statusExpire = new Dictionary<string, float>();

    // Static constructor: allocation, table and scalar stores in original order.
    private static readonly Dictionary<int, ArtJob> _artJobs =
        new Dictionary<int, ArtJob>();
    private static readonly List<ArtJob> _artPumpBuf = new List<ArtJob>();
    private static readonly HashSet<int> _texFailWarned = new HashSet<int>();
    private static long GridBudgetBytes = GridBudgetBase;
    private static readonly Dictionary<int, int> _gridRef =
        new Dictionary<int, int>();
    private static readonly Dictionary<int, float> _gridLru =
        new Dictionary<int, float>();
    private static readonly Dictionary<int, int> _actModel =
        new Dictionary<int, int>();
    private static readonly List<int> _evictBuf = new List<int>();
    private static readonly int[] dirByOct =
    {
        1,
        0,
        4,
        0,
        1,
        2,
        3,
        2,
    };
    private static readonly bool[] flipByOct =
    {
        true,
        true,
        false,
        false,
        false,
        false,
        false,
        true,
    };
    private static readonly System.Random fidgetRng = new System.Random();
    private static readonly HashSet<int> _recoloredAct = new HashSet<int>();
    private static readonly Dictionary<int, Sprite[,]> _gridCache =
        new Dictionary<int, Sprite[,]>();
    private static readonly Dictionary<int, int> _boundWOf =
        new Dictionary<int, int>();
    private static readonly Dictionary<int, int> _frameWOf =
        new Dictionary<int, int>();
    private static readonly Dictionary<Sprite, Sprite> _glowOf =
        new Dictionary<Sprite, Sprite>();
    private static readonly Dictionary<int, Sprite[,]> _glowGridCache =
        new Dictionary<int, Sprite[,]>();
    // Official 1.5.8 MonsterEntity._firstCache (field token 040008e9):
    // private static readonly Dictionary<int, Sprite>, static field offset 216.
    private static readonly Dictionary<int, Sprite> _firstCache =
        new Dictionary<int, Sprite>();
    private static readonly Color32 MaskColor = new Color32(90, 255, 120, 255);
    private static readonly Dictionary<int, bool> _colorlessModel =
        new Dictionary<int, bool>();
    private static readonly HashSet<int> _recoloredModel = new HashSet<int>();
    private static readonly Dictionary<int, float> _headTopCache =
        new Dictionary<int, float>();
}

}
