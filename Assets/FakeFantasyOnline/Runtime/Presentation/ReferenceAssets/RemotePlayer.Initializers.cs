using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class RemotePlayer
{
    // 06000b38: keep every instance initializer in the original store/allocation order.
    // Each new object is constructed before its field is published; failures stop the
    // remaining initializers and the implicit MonoBehaviour base-constructor call.
    public string pname = string.Empty;
    public string title = string.Empty;
    public float ppu = 100f;
    public float worldW = 18432f;
    public float worldH = 10752f;
    private int curDir = 4;
    private float moveSpeed = 120f;
    public int hp = 1;
    public int maxHp = 1;
    private float pickRaiseApplied = -999f;
    private int glowMode = 1;
    private float catchUp = 1f;
    private float lastPathTime = -1f;
    private readonly Dictionary<string, int> statusMode = new Dictionary<string, int>();
    private readonly Dictionary<string, float> statusExpire = new Dictionary<string, float>();
    private string avatarAura = string.Empty;
    private string titleAura = string.Empty;
    private string soulFxCsv = string.Empty;
    private string chatBubbleTheme = "huang";
    private int loadedMount = -1;
    private int lastOct = 2;
    private string lastTagTeam = string.Empty;
    private HeadHpBarView hpBarView = new HeadHpBarView();

    // 06000b39: finish and publish each array before proceeding to the next initializer.
    // One partial fixes the order without adding an explicit static constructor, retaining
    // the original BeforeFieldInit type semantics and ordinary exception propagation.
    private static readonly int[] dirByOct = { 1, 0, 4, 0, 1, 2, 3, 2 };
    private static readonly bool[] flipByOct = { true, true, false, false, false, false, false, true };
    private static readonly Vector2 PickSizeFoot = new Vector2(0.6f, 1.4f);
    private static readonly Vector2 PickOffFoot = new Vector2(0f, 0.6f);
    private static float _lastBuildAt = -99f;
    private static readonly Color NameCol = Color.white;
    private static readonly Color TitleCol = Color.white;
}

}
