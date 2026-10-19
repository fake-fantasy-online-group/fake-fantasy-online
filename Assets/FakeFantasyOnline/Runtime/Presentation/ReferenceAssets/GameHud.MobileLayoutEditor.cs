using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameHud
{
    private sealed class MobileLayoutPointer
    {
        internal MobileControlId Control;
        internal Vector2 Offset;
        internal int Kind; // 0 drag, 1 size slider, 2 save, 3 cancel, 4 reset.
        internal Rect Hit;
    }
    private readonly Dictionary<int,MobileLayoutPointer> mobileLayoutPointers = new Dictionary<int,MobileLayoutPointer>();
    private readonly List<int> mobileLayoutReleased = new List<int>();
    private MobileControlId mobileLayoutSelected=MobileControlId.Joystick;
    private bool mobileLayoutAwaitRelease;
    private string mobileLayoutError=string.Empty;
    private const int MobileEditableControlCount=14;
    private Rect MobileLayoutToolbar
    {
        get
        {
            Rect safe=MobileClientPresentation.SafeAreaGui;
            float scale=MobileClientPresentation.Scale,width=550f*scale;
            return new Rect(safe.center.x-width*.5f,safe.y+12f*scale,width,104f*scale);
        }
    }
    private Rect MobileLayoutButtonRect(int index)
    {
        Rect bar=MobileLayoutToolbar; float scale=MobileClientPresentation.Scale;
        return new Rect(bar.x+(14f+index*175f)*scale,bar.y+43f*scale,160f*scale,46f*scale);
    }
    private Rect MobileLayoutSizePanel
    {
        get
        {
            Rect bar=MobileLayoutToolbar;float scale=MobileClientPresentation.Scale;
            return new Rect(bar.center.x-180f*scale,bar.yMax+16f*scale,360f*scale,82f*scale);
        }
    }
    private Rect MobileLayoutSliderRect
    {
        get
        {
            Rect panel=MobileLayoutSizePanel;float scale=MobileClientPresentation.Scale;
            return new Rect(panel.x+30f*scale,panel.y+30f*scale,panel.width-60f*scale,44f*scale);
        }
    }

    private bool EnterMobileLayoutEditor()
    {
        // Do not put a draggable HUD above an interactive inventory or other legacy
        // window. Those windows retain their original native action handlers.
        if (MobileClientPresentation.LayoutPanelsOpen)
        {
            MobileFixedFeedback("请先关闭已打开的窗口，再长按药品或坐骑调整布局。");
            return false;
        }
        CancelMobileGesture(); ResetMobileFixedPointers();
        MobileClientPresentation.BeginLayoutEdit();
        PlayerController.I?.StopForMobileLayoutEdit();
        mobileLayoutPointers.Clear();
        mobileLayoutAwaitRelease=true;
        mobileLayoutError=string.Empty;
        return true;
    }

    private void UpdateMobileLayoutEditor()
    {
        ClientInput.CancelAll();
        // A modal can arrive from the server while editing. Revert this unsaved
        // session before exposing the modal instead of letting the two UI layers act.
        if (MobileClientPresentation.LayoutPanelsOpen)
        { ExitMobileLayoutEditor(false); return; }
        bool anyHeld=false;
        for (int index=0;index<Input.touchCount;index++)
        {
            Touch touch=Input.GetTouch(index);
            mobileShortcutPointers.Claim(touch.fingerId);
            if (touch.phase!=TouchPhase.Ended && touch.phase!=TouchPhase.Canceled) anyHeld=true;
            if (!mobileLayoutAwaitRelease) HandleMobileLayoutPointer(touch.fingerId,touch.position,touch.phase);
        }
        if (!UnityEngine.Application.isMobilePlatform && Input.touchCount==0 &&
            (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0) || Input.GetMouseButtonUp(0)))
        {
            mobileShortcutPointers.Claim(-2);
            anyHeld=Input.GetMouseButton(0);
            if (!mobileLayoutAwaitRelease) HandleMobileLayoutPointer(-2,Input.mousePosition,
                Input.GetMouseButtonDown(0) ? TouchPhase.Began : Input.GetMouseButtonUp(0) ? TouchPhase.Ended : TouchPhase.Moved);
        }
        if (mobileLayoutAwaitRelease && !anyHeld) mobileLayoutAwaitRelease=false;
        mobileLayoutReleased.Clear();
        foreach (var pair in mobileLayoutPointers) if (!MobilePointerActive(pair.Key)) mobileLayoutReleased.Add(pair.Key);
        foreach (int pointer in mobileLayoutReleased) mobileLayoutPointers.Remove(pointer);
    }
    private void HandleMobileLayoutPointer(int finger,Vector2 position,TouchPhase phase)
    {
        if (!MobileClientPresentation.LayoutEditing) return;
        Vector2 point=MobileClientPresentation.GuiPoint(position);
        if (phase==TouchPhase.Began)
        {
            MobileLayoutPointer next=null;
            for (int index=0;index<3;index++)
            {
                Rect hit=MobileLayoutButtonRect(index);
                if (hit.Contains(point)) next=new MobileLayoutPointer { Kind=index+2,Hit=hit };
            }
            if (next==null && MobileLayoutSliderRect.Contains(point))
                next=new MobileLayoutPointer { Kind=1,Control=mobileLayoutSelected };
            if (next==null && !MobileLayoutToolbar.Contains(point) && !MobileLayoutSizePanel.Contains(point))
            {
                // Smaller shortcuts win overlapping hit targets; the selected control
                // wins ties, so it remains draggable after the user overlaps two keys.
                int control=HitMobileLayoutControl(point);
                if (control>=0 && !MobileLayoutControlOwned((MobileControlId)control))
                {
                    mobileLayoutSelected=(MobileControlId)control;
                    next=new MobileLayoutPointer { Kind=0,Control=mobileLayoutSelected,
                        Offset=MobileClientPresentation.ResolveControlRect(mobileLayoutSelected).center-point };
                }
            }
            if (next!=null) mobileLayoutPointers[finger]=next;
        }
        if (!mobileLayoutPointers.TryGetValue(finger,out MobileLayoutPointer gesture)) return;
        if (phase==TouchPhase.Canceled) { mobileLayoutPointers.Remove(finger); return; }
        if (gesture.Kind==0)
            MobileClientPresentation.SetControlCenter(gesture.Control,point+gesture.Offset);
        if (gesture.Kind==1)
        {
            Rect slider=MobileLayoutSliderRect;
            float fraction=Mathf.Clamp01((point.x-slider.x)/slider.width);
            MobileClientPresentation.SetControlScale(gesture.Control,
                Mathf.Lerp(MobileControlLayout.MinScale,MobileControlLayout.MaxScale,fraction));
        }
        if (phase!=TouchPhase.Ended) return;
        mobileLayoutPointers.Remove(finger);
        if (gesture.Kind<2 || !gesture.Hit.Contains(point)) return;
        if (gesture.Kind==2) ExitMobileLayoutEditor(true);
        else if (gesture.Kind==3) ExitMobileLayoutEditor(false);
        else
        {
            MobileClientPresentation.ResetLayoutEdit();
            mobileLayoutPointers.Clear(); mobileLayoutAwaitRelease=true; mobileLayoutError=string.Empty;
        }
    }
    private bool MobileLayoutControlOwned(MobileControlId control)
    {
        foreach (var pair in mobileLayoutPointers)
            if (pair.Value.Kind==0 && pair.Value.Control==control) return true;
        return false;
    }
    private int HitMobileLayoutControl(Vector2 point)
    {
        int result=-1;float area=float.MaxValue;
        for (int index=0;index<MobileEditableControlCount;index++)
        {
            Rect rect=MobileClientPresentation.ResolveControlRect((MobileControlId)index);
            if (!rect.Contains(point)) continue;
            float next=rect.width*rect.height;
            if (next<area || (next==area && (MobileControlId)index==mobileLayoutSelected))
            { result=index;area=next; }
        }
        return result;
    }
    private void ExitMobileLayoutEditor(bool save)
    {
        if (save && !MobileClientPresentation.SaveLayoutEdit(out mobileLayoutError)) return;
        if (!save) MobileClientPresentation.CancelLayoutEdit();
        mobileLayoutPointers.Clear(); mobileLayoutAwaitRelease=false;
        CancelMobileGesture(); ResetMobileFixedPointers();
        ClientInput.CancelAll();
    }
    private void SuspendMobileLayoutEditor()
    {
        // Losing focus discards unsaved edits. Never cancel desktop input when no
        // mobile edit session exists (GameHud.Update also runs on desktop).
        if (MobileClientPresentation.LayoutEditing) ExitMobileLayoutEditor(false);
    }

    private void DrawMobileLayoutEditor(float scale)
    {
        if (!MobileClientPresentation.LayoutEditing) return;
        Rect safe=MobileClientPresentation.SafeAreaGui;
        DrawMobileLayoutOutline(safe,new Color(.35f,.75f,.64f,.8f),2f*scale);
        for (int index=0;index<MobileEditableControlCount;index++)
        {
            var id=(MobileControlId)index;
            Rect rect=MobileClientPresentation.ResolveControlRect(id);
            bool selected=id==mobileLayoutSelected;
            DrawMobileLayoutOutline(new Rect(rect.x-3f*scale,rect.y-3f*scale,rect.width+6f*scale,rect.height+6f*scale),
                selected ? new Color(1f,.83f,.40f,1f) : new Color(.4f,.76f,.64f,.8f),selected ? 3f*scale : scale);
        }
        Rect bar=MobileLayoutToolbar;
        ArtworkSkin.Panel(bar,scale);
        MobileHudArtwork.Label(new Rect(bar.x,bar.y+4f*scale,bar.width,32f*scale),"布局编辑 · 选中后拖动位置",scale);
        string[] labels={"保存并锁定","取消","重置默认"};
        for (int index=0;index<labels.Length;index++)
            ArtworkSkin.ButtonFace(MobileLayoutButtonRect(index),labels[index],scale);
        Rect panel=MobileLayoutSizePanel;
        ArtworkSkin.Panel(panel,scale);
        float size=MobileClientPresentation.ControlScale(mobileLayoutSelected);
        MobileHudArtwork.Label(new Rect(panel.x,panel.y+3f*scale,panel.width,30f*scale),
            MobileLayoutControlLabel(mobileLayoutSelected)+" · 大小 "+Mathf.RoundToInt(size*100f)+"%",scale);
        Rect slider=MobileLayoutSliderRect;
        DrawMobileLayoutFill(new Rect(slider.x,slider.center.y-2f*scale,slider.width,4f*scale),new Color(.53f,.55f,.47f,1f));
        float knob=Mathf.InverseLerp(MobileControlLayout.MinScale,MobileControlLayout.MaxScale,size);
        Rect thumb=new Rect(slider.x+slider.width*knob-11f*scale,slider.center.y-11f*scale,22f*scale,22f*scale);
        DrawMobileRoundFace(thumb,scale,true,false);
        DrawMobileLayoutOutline(thumb,new Color(1f,.84f,.4f,1f),2f*scale);
        string hint=string.IsNullOrEmpty(mobileLayoutError) ? "编辑时停止移动、施法和用药；保存后锁定布局" : mobileLayoutError;
        MobileHudArtwork.Label(new Rect(safe.center.x-290f*scale,panel.yMax+8f*scale,580f*scale,34f*scale),hint,scale);
    }
    private static string MobileLayoutControlLabel(MobileControlId id)
    {
        if (id>=MobileControlId.Skill0 && id<=MobileControlId.Skill5) return "技能 "+((int)id-(int)MobileControlId.Skill0+1);
        if (id==MobileControlId.Joystick) return "移动摇杆";
        if (id==MobileControlId.Attack) return "手动攻击";
        if (id==MobileControlId.Pickup) return "拾取";
        if (id==MobileControlId.GroupWheel) return "技能轮盘";
        if (id==MobileControlId.Mount) return "坐骑";
        if (id==MobileControlId.HealthPotion) return "红药";
        if (id==MobileControlId.ManaPotion) return "蓝药";
        return "坐下 / 起立";
    }
    private static void DrawMobileLayoutOutline(Rect rect,Color color,float width)
    {
        DrawMobileLayoutFill(new Rect(rect.x,rect.y,rect.width,width),color);
        DrawMobileLayoutFill(new Rect(rect.x,rect.yMax-width,rect.width,width),color);
        DrawMobileLayoutFill(new Rect(rect.x,rect.y,width,rect.height),color);
        DrawMobileLayoutFill(new Rect(rect.xMax-width,rect.y,width,rect.height),color);
    }
    private static void DrawMobileLayoutFill(Rect rect,Color color)
    {
        Color previous=GUI.color;
        try { GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture); }
        finally { GUI.color=previous; }
    }
}
}
