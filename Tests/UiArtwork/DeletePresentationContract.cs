// Executes the production deletion presentation with deterministic Unity substitutes.
// Every delete action below is a counter-only lambda; no client, server or DB is used.
using System;
using System.Linq;
using UnityEngine;
using FakeFantasyOnline.Application;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class TitleFlow
{
    private static void LoadDeleteArt()
    {
        LoadArt();
        ResLoader.I.textures["res/uires/common/msgboxbk.gsa0"]=new Texture2D(346,128);
        foreach(string name in new[]{"affirmbtn","cancelbtn"})
            for(int frame=0;frame<4;frame++)
                ResLoader.I.textures["res/uires/common/"+name+".gsa"+frame]=new Texture2D(70,22);
    }
    private static TitleFlow DeleteFlow()
    {
        GUI.ResetInput();GUIUtility.hotControl=GUIUtility.keyboardControl=0;
        MobileClientPresentation.Enabled=false;GUI.matrix=new Matrix4x4 {m11=1};GUI.Clear();
        LoadDeleteArt();var f=NewFlow();f.serverChars=new[]{
            new ServerCharacterRecord {CharacterId="one",DisplayName="当前角色",Level=28,Head=1,Hair=1},
            new ServerCharacterRecord {CharacterId="two",DisplayName="另一个角色",Level=39,Head=1,Hair=1}};
        f.selSlot=0;f.MirrorSlotToCharData(0);f.EnsureSelectionPresentation();return f;
    }
    public static void DeleteContract()
    {
        int initial=checks;var f=DeleteFlow();int requests=0;f.action=a=>{if(a==TitleCharacterAction.Delete)requests++;};
        Tap(f.DrawCharSelect,700,465);Check(f.pendingDelete&&requests==0,"delete entry only opens one confirmation");
        GUI.Clear();EventAt(EventType.Repaint);f.DrawCharSelect();
        Check(GUI.drawings.Any(d=>d.rect.x==227&&d.rect.y==236&&d.rect.width==346&&d.rect.height==128&&ReferenceEquals(d.texture,f.deleteWindow)),"approved complete 346x128 original window at native size");
        Check(!GUI.drawings.Any(d=>d.rect.width==800&&d.rect.height==600&&ReferenceEquals(d.texture,Texture2D.whiteTexture)),"no invented black screen or enlarged panel");
        Check(GUI.drawings.Any(d=>d.rect.x==278&&d.rect.y==334&&d.rect.width==70&&d.rect.height==22),"confirm face retains original geometry");
        Check(GUI.drawings.Any(d=>d.rect.x==460&&d.rect.y==334&&d.rect.width==70&&d.rect.height==22),"cancel face retains original geometry");
        Check(GUIUtility.keyboardControl==f.deleteCancelControl,"initial keyboard focus is safe cancel");
        int loads=ResLoader.I.loads;f.DrawCharSelect();Check(ResLoader.I.loads==loads,"modal artwork cached across events");
        Tap(f.DrawCharSelect,480,340);Check(!f.pendingDelete&&requests==0,"native cancel sends zero deletion requests");
        f.OpenDeleteConfirmation();EventAt(EventType.KeyDown);Event.current.keyCode=KeyCode.Escape;f.DrawCharSelect();
        Check(!f.pendingDelete&&requests==0&&Event.current.type==EventType.Used,"Escape only cancels and consumes event");
        f.OpenDeleteConfirmation();EventAt(EventType.Repaint);f.DrawCharSelect();EventAt(EventType.KeyDown);Event.current.keyCode=KeyCode.Return;f.DrawCharSelect();
        Check(!f.pendingDelete&&requests==0,"Return on initial cancel focus never deletes");
        f.OpenDeleteConfirmation();Tap(f.DrawCharSelect,10,10);Check(f.pendingDelete&&requests==0&&f.selSlot==0&&Event.current.type==EventType.Used,"outside modal tap blocked without deleting or selecting");
        foreach(var type in new[]{EventType.MouseDown,EventType.MouseUp,EventType.MouseDrag,EventType.ScrollWheel,EventType.KeyDown,EventType.KeyUp})
        { EventAt(type,900,10);f.DrawCharSelect();Check(Event.current.type==EventType.Used,"whole-viewport modal input consumed including wide margins: "+type); }
        EventAt(EventType.MouseDown,300,340);f.DrawCharSelect();EventAt(EventType.MouseUp,100,100);f.DrawCharSelect();
        Check(requests==0&&f.pendingDelete&&GUIUtility.hotControl==0,"confirm press dragged outside cancels its click");
        EventAt(EventType.MouseDown,300,340);Event.current.button=1;f.DrawCharSelect();EventAt(EventType.MouseUp,300,340);Event.current.button=1;f.DrawCharSelect();
        Check(requests==0&&f.pendingDelete,"right click never confirms");
        EventAt(EventType.MouseDown,300,340);f.DrawCharSelect();GUI.enabled=false;EventAt(EventType.MouseUp,300,340);f.DrawCharSelect();GUI.enabled=true;
        Check(requests==0&&f.pendingDelete&&GUIUtility.hotControl==0,"disable after press cancels confirmation");
        Tap(f.DrawCharSelect,300,340);Check(requests==1&&!f.pendingDelete&&f.netMsg=="删除中...","one release submits exactly one existing delete action");
        f.DrawCharSelect();Check(requests==1,"replaying consumed release cannot submit twice");
        f.OpenDeleteConfirmation();EventAt(EventType.MouseDown,300,340);f.DrawCharSelect();f.selSlot=1;
        EventAt(EventType.MouseUp,300,340);f.DrawCharSelect();Check(requests==1&&!f.pendingDelete&&GUIUtility.hotControl==0,"selection changed during confirmation cancels frozen target");
        f.selSlot=0;f.OpenDeleteConfirmation();var changed=f.serverChars.ToArray();changed[0].CharacterId="replacement";f.serverChars=changed;
        Tap(f.DrawCharSelect,300,340);Check(requests==1&&!f.pendingDelete,"replacement roster cannot confirm stale identity");
        f.OpenDeleteConfirmation();changed[0].DisplayName="同槽换名";EventAt(EventType.Repaint);f.DrawCharSelect();
        Check(!f.pendingDelete&&requests==1,"in-place changed name invalidates displayed identity");
        f.OpenDeleteConfirmation();EventAt(EventType.MouseDown,300,340);f.DrawCharSelect();f.pendingDelete=false;
        EventAt(EventType.MouseUp,300,340);f.DrawCharSelect();Check(requests==1&&GUIUtility.hotControl==0&&Event.current.type==EventType.Used,"existing roster-close path clears interrupted capture before background controls");
        f=DeleteFlow();requests=0;f.action=a=>{if(a==TitleCharacterAction.Delete)requests++;};Time.realtimeSinceStartup=0;f.entering=true;
        Tap(f.DrawCharSelect,700,465);Check(f.pendingDelete&&f.entering,"existing deletion admission independent of entry latch retained");
        Tap(f.DrawCharSelect,300,340);f.OpenDeleteConfirmation();Tap(f.DrawCharSelect,300,340);
        Check(requests==2,"no new cross-request deletion busy state machine introduced");
        f.CloseDeleteConfirmation();f.serverChars=Array.Empty<ServerCharacterRecord>();GUI.Clear();EventAt(EventType.Repaint);f.DrawCharSelect();
        Tap(f.DrawCharSelect,700,465);Check(!f.pendingDelete&&requests==2&&f.selSlot==-1,"empty roster stays disabled with zero additional requests");
        Check(GUI.buttons.Any(b=>b.rect.y==455&&!b.enabled),"empty delete remains visible and disabled");
        f=DeleteFlow();f.OpenDeleteConfirmation();EventAt(EventType.Repaint);f.DrawCharSelect();var borrowed=f.deleteWindow;
        ResLoader.I.Pkg24=new object();f.EnsureSelectionPresentation();Check(f.deleteWindow==null,"selection package invalidation releases modal references");
        f.DrawCharSelect();Check(ReferenceEquals(f.deleteWindow,borrowed)&&!borrowed.destroyed,"borrowed modal asset reloaded without destruction");
        f.ReleaseSelectionPresentation();f.ReleaseSelectionPresentation();Check(!borrowed.destroyed,"repeated release never destroys source-owned image");
        // Actual body geometry and full-name coverage, using both bitmap and whole-text fallback.
        f=DeleteFlow();var longRows=f.serverChars.ToArray();longRows[0].DisplayName=new string('长',24);f.serverChars=longRows;f.OpenDeleteConfirmation();
        BitmapFont.Ready=true;BitmapFont.missing='\0';BitmapFont.textDraws.Clear();GUI.labels.Clear();f.DrawDeleteMessage();
        string all=string.Concat(BitmapFont.textDraws.Select(d=>d.text));
        Check(all.Contains(new string('长',24))&&all.Contains("删除后不可恢复")&&all.Contains("装备")&&all.Contains("道具"),"24-character full CJK identity and irreversible loss warning preserved");
        Check(BitmapFont.textDraws.Count<=4&&BitmapFont.textDraws.All(d=>d.scale==1f),"24-character CJK name fits original body at native text size");
        Check(BitmapFont.textDraws.All(d=>d.y>=0&&d.y+12*d.scale<=72)&&GUI.groupDepth==0,"body lines clipped within original 72px area and groups balanced");
        BitmapFont.missing='长';GUI.labels.Clear();BitmapFont.textDraws.Clear();f.DrawDeleteMessage();
        Check(BitmapFont.textDraws.Count==0&&string.Concat(GUI.labels).Contains(new string('长',24))&&!f.deleteTextStyle.richText,"missing glyph falls back for entire unchanged content without rich text");
        // Touch hit targets are separate from untouched image faces and never overlap body text.
        MobileClientPresentation.Enabled=true;
        foreach(float scale in new[]{1f,.9f,.6f,2f})
        {
            GUI.matrix=new Matrix4x4 {m11=scale};Rect a=DeleteHitRect(DeleteConfirmFace),b=DeleteHitRect(DeleteCancelFace);
            Check(a.xMax<b.x&&a.y>=DeleteBody.yMax&&b.y>=DeleteBody.yMax,"mobile targets remain separate from each other and all text at scale "+scale);
            Check(a.Contains(new Vector2(278,334))&&a.Contains(new Vector2(347,355)),"mobile target contains complete original face at scale "+scale);
            Check(a.height*scale>=44f,"mobile target maintains 44 screen-pixel height at tested scale "+scale);
        }
        GUI.matrix=new Matrix4x4 {m11=1};f.CloseDeleteConfirmation();f.OpenDeleteConfirmation();requests=0;f.action=a=>{if(a==TitleCharacterAction.Delete)requests++;};
        Tap(f.DrawCharSelect,270,365);Check(requests==1&&!f.pendingDelete,"single mobile-style tap in padding activates exactly once");
        MobileClientPresentation.Enabled=false;BitmapFont.Ready=false;BitmapFont.missing='\0';
        f=DeleteFlow();f.OpenDeleteConfirmation();ResLoader.I.textures["res/uires/common/msgboxbk.gsa0"]=new Texture2D(348,248);ResLoader.I.textures["res/uires/common/affirmbtn.gsa0"]=new Texture2D(69,24);
        for(int i=1;i<4;i++)ResLoader.I.textures.Remove("res/uires/common/affirmbtn.gsa"+i);
        ArtworkSkin.buttonLabels.Clear();EventAt(EventType.Repaint);f.DrawCharSelect();
        Check(f.deleteWindow==null&&f.deleteFaces[0][0]==null&&ArtworkSkin.buttonLabels.Contains("确认"),"missing or wrong-size resource has labelled fallback, never stretched rejected artwork");
        f.CloseDeleteConfirmation();Console.WriteLine("PASS "+(checks-initial)+" deletion presentation assertions (production methods; counter-only actions; not Unity/device validation)");
    }
}
}
