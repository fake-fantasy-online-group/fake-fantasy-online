using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Official UIManager.Update (0x06000fa8, arm64 rva=0x3a7774 2968 bytes / x86_64 rva=0x3ba580
// 2768 bytes). Both frozen slices agree on every branch below; the addresses quoted in the
// comments are arm64 unless both are given.
//
// Enablement boundary: this method is the Unity Update message of UIManager, and this build has
// no proven official creation point for a UIManager instance (nothing calls AddComponent<UIManager>
// or new UIManager, and no scene/prefab reference has been evidenced). It is therefore restored
// owner-only and deliberately NOT mounted. Keyboard input that actually runs comes from the one
// ClientRootView driver and bounded UIManager.*ForIndependentRuntime helpers. That carrier now
// applies every implemented slice in frozen side-effect order: TypingNow-only Alt+B first; then N,
// O, M, L, X, D, F12, Insert, V, zoom in, zoom out and the Escape tail under the ordinary guards.
// F and P remain outside that bounded carrier. Mounting this body alongside it would create two
// keyboard drivers, so Update stays unmounted until the partial carrier is removed.
//
// The ChatUI panel slices are split only at their native control-flow boundary. The live driver
// calls UpdateFacePanelShortcutForIndependentRuntime before the ordinary-key guard and invokes
// UpdateChannelListShortcutForIndependentRuntime immediately after the Insert carrier. Thus
// TypingNow true admits Alt+B and blocks all ordinary keys; TypingNow false blocks Alt+B and can
// reach V. This is a local carrier for selected hotkeys, not this Update being connected or passed.
//
// UiEsc.CloseTop is the complete shared native decision body. Both this unmounted Unity message
// and the independent world's live Escape carrier call it directly. Restoring that call does not
// create a UIManager or add a second keyboard driver.
public partial class UIManager
{
    private void Update()
    {
        // Arm64 0x3a7938-0x3a7944: the static ChatUI slot +0x68 (atlas: ChatUI.TypingNow) is read
        // first and gates the whole Alt+B slice, then it is read again at 0x3a79dc / 0x3a7a70 for
        // the early-out. Both reads are cctor-guarded separately, so the source below keeps two
        // reads; the second one cannot observe anything the first did not. Being inside this
        // TypingNow == true block is what makes Alt+B the only hotkey that fires while the chat
        // input owns the keyboard, and it is what the live carrier reproduces.
        // KeyCode pinning: 0x62 = B (98), 0x134 = LeftAlt (308), 0x133 = RightAlt (307), read from
        // the UnityEngine KeyCode enum; the two modifier codes are the Alt pair, so the modifier is
        // required and B alone does nothing.
        if (ChatUI.TypingNow)
        {
            // 0x3a7948 GetKeyDown(0x62) then GetKey(0x134) / GetKey(0x133): KeyCode.LeftAlt is
            // 308 and RightAlt 307, so B alone does nothing - the alt modifier is required.
            if (Input.GetKeyDown(KeyCode.B) &&
                (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) &&
                ChatUI.I != null)
            {
                // 0x3a7a28 -> 0x060000e0.
                ChatUI.I.ToggleFacePanel();
            }
        }
        if (ChatUI.TypingNow)
        {
            return;
        }

        // 0x3a7a78: MailDlg static slot +8 (MailDlg.Typing) - non-zero jumps to the return
        // epilogue at 0x3a7b08.
        if (MailDlg.Typing)
        {
            return;
        }
        // 0x3a7ac8 / 0x3a7adc: GossipDlg.get_Typing (0x060005d4).
        if (GossipDlg.Typing)
        {
            return;
        }
        // 0x3a7b00: GUIUtility.get_keyboardControl - zero falls through to the hotkeys, anything
        // else returns.
        if (GUIUtility.keyboardControl != 0)
        {
            return;
        }

        // The 17 hotkey bindings, in the frozen order. Every one is an independent
        // GetKeyDown test: the body never chains them with else-if, so two keys pressed in one
        // frame fire both actions.
        if (Input.GetKeyDown(KeyCode.Q))
        {
            TeamDlg.Toggle();
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            EquipDlg.Toggle();
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            InventoryDlg.Toggle();
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            Win05.Toggle("skilldlg");
        }
        if (Input.GetKeyDown(KeyCode.N))
        {
            TaskDlg.Toggle();
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            SysConfigDlg.Toggle();
        }
        if (Input.GetKeyDown(KeyCode.M) && GameHud.I != null)
        {
            // 0x3a7d30 -> 0x06000451, guarded by the Object.op_Inequality test at 0x3a7d04 on the
            // GameHud slot read out of the GameHud statics at 0x3a7cf4-0x3a7d00 (+0x18): a null
            // slot skips the call, it is not an error.
            GameHud.I.ToggleMapDiagram();
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            // 0x3a7ef0-0x3a7f18: string.IsNullOrEmpty(SocialClient.FamilyName) decides between
            // the two family windows; a non-empty family name opens the detail window.
            if (!string.IsNullOrEmpty(SocialClient.FamilyName))
            {
                FamilyDetailDlg.Toggle();
            }
            else
            {
                FamilyDlg.Toggle();
            }
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            Win05.Toggle("petdlg");
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            MailDlg.Toggle();
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            Win05.ChangeSetHotkey();
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            DuelDlg.Toggle();
        }
        if (Input.GetKeyDown(KeyCode.F12))
        {
            HelpWinDlg.Toggle();
        }
        if (Input.GetKeyDown(KeyCode.Insert))
        {
            HeadHpBar.Toggle();
        }
        if (Input.GetKeyDown(KeyCode.V) && ChatUI.I != null)
        {
            // 0x3a7fcc -> 0x060000e1. V carries no modifier, and it sits after Insert in the
            // guarded chain, so it needs all four typing/focus guards to pass.
            ChatUI.I.ToggleChannelList();
        }
        // 0x3a7fd8 GetKeyDown(0x3d) || 0x3a7fe8 GetKeyDown(0x10e): Equals (61) or KeypadPlus
        // (270); the zoom-in call is 0x3a8068 with w1 = 1.
        if ((Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus)) &&
            GameHud.I != null)
        {
            GameHud.I.MinimapZoom(true);
        }
        // 0x3a8074 GetKeyDown(0x2d) || 0x3a8084 GetKeyDown(0x10d): Minus (45) or KeypadMinus
        // (269); the zoom-out call is 0x3a8104 with w1 = 0.
        if ((Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus)) &&
            GameHud.I != null)
        {
            GameHud.I.MinimapZoom(false);
        }

        // 0x3a8110 GetKeyDown(0x1b) = Escape.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 0x3a8118 -> 06000fca; a handled reservation/close skips this method's entire tail.
            if (UiEsc.CloseTop())
            {
                return;
            }
            // A null stack is not a skip: 0x3a8120 loads it and 0x3a8124 branches to 0x3a8308,
            // the NullReferenceException stub. 0x3a8128-0x3a8130 is the Count < 1 test, and its
            // cold block starts at 0x3a82cc.
            if (stack.Count < 1)
            {
                // 0x3a82d0 ArenaEscDlg.get_ShouldTakeOver (0x0600005b) decides between
                // ArenaEscDlg.Toggle (tail branch at 0x3a82ec) and SysOptionDlg.Toggle (tail
                // branch at 0x3a8304). Both sit in the cold block after the mouse branch.
                if (ArenaEscDlg.ShouldTakeOver)
                {
                    ArenaEscDlg.Toggle();
                    return;
                }
                SysOptionDlg.Toggle();
                return;
            }
            // 0x3a8148: tail branch to UIManager.CloseTop (0x06000fa4, 0x3a754c).
            CloseTop();
            return;
        }

        // The mouse branch runs only when Escape was not pressed this frame.
        if (cam == null)
        {
            return;
        }
        if (stack.Count == 0)
        {
            return;
        }
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }
        Win win = stack[stack.Count - 1];
        Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector3 local = win.root.InverseTransformPoint(world);
        // 0x3a81f8-0x3a8250: meta.w (int at +0x20) and meta.h (int at +0x24) are widened
        // together, halved, and the local point is scaled by 100 into that unit box; the body
        // then tests 0 <= px <= w and 0 <= py <= h with both ends inclusive (fcmgt + mvn on all
        // four lanes, uminv, tbz bit0). The y half is subtracted, so py grows downward.
        float px = win.meta.w * 0.5f + local.x * 100f;
        float py = win.meta.h * 0.5f - local.y * 100f;
        if (px < 0f || px > win.meta.w || py < 0f || py > win.meta.h)
        {
            return;
        }
        UIWinWidget widget = HitWidget(win, px, py);
        if (widget == null)
        {
            return;
        }
        // 0x3a8274 name.ToLower() then 0x3a828c Contains("close") -> the same CloseTop tail
        // branch as Escape.
        if (widget.name.ToLower().Contains("close"))
        {
            CloseTop();
            return;
        }
        // 0x3a82a0 name.ToLower() then the tail branch at 0x3a82c8 into Contains("help"): the
        // result is evaluated and discarded, so the official help widget has no action and the
        // frame simply ends.
        widget.name.ToLower().Contains("help");
    }
}

}
