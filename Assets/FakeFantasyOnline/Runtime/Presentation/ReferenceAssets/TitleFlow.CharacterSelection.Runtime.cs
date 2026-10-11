using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// One actual title component owns resources, state, and Unity OnGUI dispatch.
// Its callback drives package replacement before the selected page renders.
internal sealed partial class TitleFlow : MonoBehaviour
{
    // Original +0x20. The host supplies this after Awake/SetupNet completes, and
    // DoEnterWorld retains this exact owner across destruction of the title.
    internal GameBootstrap boot;

    // 06000efe: all fifteen original resource/roster arrays belong to the fresh owner,
    // before any Awake resource read or package replacement can publish into them.
    private readonly Texture2D[] worldBtn = new Texture2D[2];
    private readonly Texture2D[] pveBtn = new Texture2D[2];
    private readonly Texture2D[] pvpBtn = new Texture2D[2];
    private readonly Texture2D[] busyBar = new Texture2D[4];
    private readonly Texture2D[] busyWorld = new Texture2D[3];
    private Texture2D[] typebtn = new Texture2D[6];
    private Texture2D[] typebtnSel = new Texture2D[6];
    private Texture2D[] colorbtn = new Texture2D[9];
    private Texture2D[] colorbtnSel = new Texture2D[9];
    private Texture2D[] classBody = new Texture2D[5];
    private Texture2D[,] classpic = new Texture2D[6, 2];
    private Texture2D[,,] nudeAv = new Texture2D[2, 9, 3];
    private Texture2D[,,] hairHead = new Texture2D[2, 9, 3];
    private IReadOnlyList<ServerCharacterRecord> serverChars = new ServerCharacterRecord[0];
    private readonly PlayerAvatar[] slotDoll = new PlayerAvatar[3];

    // Keep the complete 06000efe instance initializer sequence in this one partial:
    // all fifteen arrays, the sixteen empty strings, then the three -1 sentinels.
    // Cross-part declaration ordering must not move a store across an allocation.
    internal string account = string.Empty;
    internal string password = string.Empty;
    private string charName = string.Empty;
    private string _registerAccount = string.Empty;
    private string _registerPassword = string.Empty;
    private string _registerPasswordConfirm = string.Empty;
    private string _registerSecurityCode = string.Empty;
    private string _registerInviteCode = string.Empty;
    private string _recoverAccount = string.Empty;
    private string _recoverSecurityCode = string.Empty;
    private string _recoverPassword = string.Empty;
    private string _recoverPasswordConfirm = string.Empty;
    internal string netMsg = string.Empty;
    private string _appealAccount = string.Empty;
    private string _appealWhen = string.Empty;
    private string _appealReason = string.Empty;
    private int selSlot = -1;
    private int _resolutionIndex = -1;
    private int srvBuiltFor = -1;
    private Texture2D actorbk, actorcover, selProp, proppanel;
    private Texture2D btnCreate, btnEnter, btnDelete, btnReturn;
    private Action<ServerCharacterRecord?> prepareCreation;
    private Action<TitleCharacterAction> action;
    private Action<string> setMessage;
    private GUIStyle center, numStyle, centerWrap;
    private bool pendingDelete;
    internal bool loggingIn;
    internal float loginAtMs;
    internal bool entering;
    internal float enterAtMs;

    // Official 1.5.8 06000eff: keep these initializers in one partial declaration so their
    // allocation/store order is fixed: CLASS, INIT, DOLL_FLIP, WORLD_TXT, WORLD_BAR_FRAME,
    // BUSY_COL, DESC. The login preference flags stay zero-initialized in the login carrier.
    private static readonly string[] CLASS = { "战士", "剑客", "刺客", "药师", "术士" };
    // Original 1.5.8 eff field-default blobs, independently decoded from global metadata.
    // Each ten-int row is a separate allocation, including the equal final two rows.
    private static readonly int[][] INIT =
    {
        new[] { 12, 6, 15, 7, 0, 0, 4, 210, 36, 620 },
        new[] { 10, 9, 16, 5, 0, 0, 4, 224, 36, 600 },
        new[] { 10, 6, 14, 10, 0, 0, 4, 168, 36, 600 },
        new[] { 4, 3, 12, 0, 12, 0, 13, 120, 117, 540 },
        new[] { 4, 3, 12, 0, 12, 0, 13, 120, 117, 540 }
    };

    // DrawDollInFrame reads this field separately for anchor and UV choice.
    private static readonly bool DOLL_FLIP = true;
    private static readonly Color32 WORLD_TXT = new Color32(165, 160, 128, 255);
    private static readonly int[] WORLD_BAR_FRAME = { 2, 1, 0 };
    private static readonly Color32[] BUSY_COL =
    {
        new Color32(90, 169, 5, 255), new Color32(191, 144, 2, 255),
        new Color32(226, 17, 7, 255), new Color32(226, 7, 198, 255)
    };

    private static readonly string[] DESC =
    {
        "战士具有强大的力量与健硕的体魄，可以承受相当的伤害仍屹立不倒。追求高攻击力的战士堪称敌阵内冲锋陷阵的勇者。",
        "剑客是一个相对均衡的职业，侧重于近身攻击，以招式多变和身法灵活见长。可走攻防兼备的剑盾路线，也可选择高攻高命中。",
        "刺客拥有所有职业中最高的敏捷，既适合近身攻击也适合远程攻击，以其灵活多变在不同环境下赢得生机。",
        "药师专注于治愈系与辅助系的法术，是队伍中不可缺少的治疗者。尽管辅助技能多样，攻击法术也有不可小瞧的造诣。",
        "术士是天地间神秘力量的操纵者，可使用强大的法术摧毁敌人，弥补了先天身体的贫弱，是最有潜力的进攻者。"
    };

    internal void BindHostForIndependentRuntime(Action<ServerCharacterRecord?> prepareCreation,
        Action<TitleCharacterAction> action, Action<string> setMessage)
    {
        this.prepareCreation = prepareCreation ?? throw new ArgumentNullException(nameof(prepareCreation));
        this.action = action ?? throw new ArgumentNullException(nameof(action));
        this.setMessage = setMessage ?? throw new ArgumentNullException(nameof(setMessage));
    }

    internal ServerCharacterRecord? SelectedCharacterForIndependentRuntime =>
        selSlot >= 0 && selSlot < serverChars.Count ? serverChars[selSlot] : null;

    // Repeated draw/diagnostic observations must not replay a receipt's avatar release.
    internal void BindRosterForIndependentRuntime(IReadOnlyList<ServerCharacterRecord> characters)
    {
        characters = characters ?? new ServerCharacterRecord[0];
        if (ReferenceEquals(serverChars, characters)) return;
        ReceiveRosterForIndependentRuntime(characters);
    }

    // Original f03 always publishes its argument and releases the previous avatars,
    // even when a receipt supplies the same array reference. The real callback enters
    // here directly; observation deduplication and Unity liveness are not receipt gates.
    internal void ReceiveRosterForIndependentRuntime(IReadOnlyList<ServerCharacterRecord> characters)
    {
        serverChars = characters ?? new ServerCharacterRecord[0];
        ReleaseSlotDolls();
        pendingDelete = false;
        if (serverChars.Count == 0) selSlot = -1;
        else if (selSlot < 0 || selSlot >= serverChars.Count) selSlot = 0;
        MirrorSlotToCharData(selSlot);
    }

    private void ReleaseSlotDolls()
    {
        for (int i = 0; i < slotDoll.Length; i++)
        {
            if (slotDoll[i] != null)
            {
                slotDoll[i].Dispose();
                slotDoll[i] = null;
            }
        }
    }

    // Unity owns this dispatch. Callback clearing and avatar release run once when the
    // actual title component is destroyed, not when a host requests its destruction.
    private void OnDestroy()
    {
        if (ReferenceEquals(pluginTitleOwner, this)) pluginTitleOwner = null;
        if (NetClient.I != null)
        {
            NetClient receiver = NetClient.I;
            if (ReferenceEquals(receiver, null)) throw new NullReferenceException();
            receiver.clearErrorReceipt?.Invoke();
            receiver.clearLoginResultReceipt?.Invoke();
            receiver.clearRosterReceipt?.Invoke();
            receiver.clearCreateResultReceipt?.Invoke();
            receiver.clearEnterWorldReceipt?.Invoke();
            receiver.clearAccountResultsReceipt?.Invoke();
        }
        ReleaseSlotDolls();
    }

    internal void EnsureStylesForIndependentRuntime()
    {
        if (center == null)
        {
            center = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                clipping = TextClipping.Overflow
            };
            ClearStyleBackgrounds(center);
            numStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft, fontSize = 12, richText = true,
                clipping = TextClipping.Overflow
            };
            ClearStyleBackgrounds(numStyle);
            numStyle.normal.textColor = new Color(0.86f, 0.8f, 0.72f, 1f);
            centerWrap = new GUIStyle(center) { wordWrap = true };
        }

        // Shared OnGUI state is written each event, after the once-only style setup.
        GUI.skin.settings.selectionColor = new Color(0.55f, 0.45f, 0.25f, 0.45f);
        GUI.skin.settings.cursorColor = Color.white;
    }

    // Official 1.5.8 06000ec4 is now consumed by the restored Awake resource prefix.
    private static Texture2D L(string n) => Resources.Load<Texture2D>("Title/" + n);

    // 06000ec5 remains qualified: active Avatar inputs use the existing reviewed-PNG bridge.
    private static Texture2D LA(string n) => Resources.Load<Texture2D>("Avatar/" + n);

    // Existing ede size overload forwards the already recovered ec8 draw/hit helper.
    private static bool ImgBtn(float x, float y, Texture2D texture) =>
        RecoveredTitleLoginSurface.DrawImageButton(new Rect(x, y,
            texture ? GsaDecoder.W(texture) : 120f,
            texture ? GsaDecoder.H(texture) : 28f), texture);

    private void LabelValue(float x, float y, int value) =>
        GUI.Label(new Rect(x, y, 60f, 14f), value.ToString(), numStyle);

    // Existing DrawCharSelect local A helper (06000f0a): absent/short attributes use INIT.
    private static int AttributeAt(IReadOnlyList<int> attributes, int index, int fallback)
    {
        if (attributes == null || index >= attributes.Count) return fallback;
        // The native int[] access throws array bounds after the signed upper-bound check.
        // The existing roster's List-backed view has a different negative-index exception.
        if (index < 0) throw new IndexOutOfRangeException();
        return attributes[index];
    }

    private static void ClearStyleBackgrounds(GUIStyle style)
    {
        style.normal.background = null;
        style.hover.background = null;
        style.active.background = null;
        style.focused.background = null;
        style.onNormal.background = null;
        style.onHover.background = null;
        style.onActive.background = null;
        style.onFocused.background = null;
    }
}
}
