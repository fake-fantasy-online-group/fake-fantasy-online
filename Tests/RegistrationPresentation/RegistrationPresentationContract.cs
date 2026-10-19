using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using FakeFantasyOnline.Presentation.ReferenceAssets;

internal static class RegistrationPresentationContract
{
    private static int checks;
    internal static void Check(bool value, string label) { checks++; if (!value) throw new Exception(label); }
    private static Texture2D Pattern(int width, int height)
    {
        var t = new Texture2D(width,height,TextureFormat.RGBA32,false);
        var p = new Color32[width*height];
        for(int y=0;y<height;y++) for(int x=0;x<width;x++) p[(height-1-y)*width+x]=new Color32((byte)x,(byte)y,7,255);
        t.SetPixels32(p);return t;
    }
    private static Color32 At(Texture2D texture, int x, int y) => texture.GetPixels32()[(texture.height-1-y)*texture.width+x];
    private static void Main(string[] args)
    {
        string subset = File.ReadAllText(args[0]);
        var glyphs = RegistrationPixelArt.ReadSubset(subset);
        Check(glyphs.Count==7 && "确认安全注册新".All(glyphs.ContainsKey),"Only seven licensed candidate glyphs bundled");
        Check(!glyphs.ContainsKey('帐') && !glyphs.ContainsKey('密'),"No original game glyph data bundled");
        Check(RegistrationPixelArt.ReadSubset("# comment\n786e:bad\nxyz:000\n").Count==0,"Malformed subset safely ignored");
        var panel=RegistrationPixelArt.BuildPanel(Pattern(348,29),Pattern(348,16),Pattern(348,40));
        Check(panel.width==348 && panel.height==242,"Approved compact panel dimensions");
        Check(At(panel,20,28).g==28 && At(panel,20,29).g==0 && At(panel,20,175).g==2,"Top/middle repeat without stretch");
        Check(At(panel,20,176).g==0 && At(panel,20,191).g==15,"Inner bottom remains original pixels");
        Check(At(panel,20,192).g==16 && At(panel,20,217).g==16 && At(panel,20,218).g==16 && At(panel,20,241).g==39,"Action space tiles blank row; outer border intact");
        Check(RegistrationPixelArt.BuildPanel(Pattern(347,29),Pattern(348,16),Pattern(348,40))==null,"Incompatible artwork falls back safely");
        var source=Pattern(317,131);var sourcePixels=new Color32[317*131];
        int[] xs={132,144,156,168},ys={28,28,65,65};
        for(int c=0;c<4;c++)for(int y=1;y<12;y++)sourcePixels[(130-ys[c]-y)*317+xs[c]+1]=new Color32(255,(byte)(230-y*10),(byte)(140-y*8),255);
        source.SetPixels32(sourcePixels);var gradient=RegistrationPixelArt.AddNativeGlyphs(source,glyphs);
        Check(gradient!=null && "帐号密码".All(glyphs.ContainsKey),"Native glyphs sampled only at runtime");
        Check(glyphs['帐'][13] && !glyphs['帐'][0] && gradient[0].g==220 && gradient[10].g==120,"Native mask grid and per-row gold sampled");
        Check(RegistrationPixelArt.AddNativeGlyphs(Pattern(325,178),glyphs)==null,"Different login layouts are never sampled blindly");
        Texture2D label=RegistrationPixelArt.BuildLabel("帐号",0,glyphs,gradient);
        Check(label.width==26 && label.height==14 && label.filterMode==FilterMode.Point,"Native 12px advance and hard raster edge");
        Check(At(label,2,2).g==220 && At(label,1,2).r==0 && At(label,1,2).a==255 && At(label,0,0).a==0,"Exact ink, black one-pixel outline, transparent exterior");
        Check(RegistrationPixelArt.BuildLabel("注册新帐号",4,glyphs,gradient).width==78,"Title tracking matches approved preview");
        Check(RegistrationPixelArt.BuildLabel("未知",0,glyphs,gradient)==null,"Missing characters fall back as whole label");
        Check(source.GetPixels32().SequenceEqual(sourcePixels),"Source artwork is never mutated");
        Resources.subset=new TextAsset { text=subset };
        var flow=new TitleFlow { source=source };
        ResLoader.I=null;GUI.Reset();flow.DrawTest();
        Check(GUI.Fields.Count==4 && GUI.Buttons.Count==2,"Missing packages preserve four usable inputs and two actions");
        ResLoader.I=new ResLoader { Pkg=new object(), Pkg24=new object(), PkgFf21=new object() };
        foreach(string name in new[]{"topbar","middlebar","bottombar","completebtn","returnbtn"})
        {int w=name.EndsWith("btn")?152:348;int h=name=="topbar"?29:name=="middlebar"?16:name=="bottombar"?40:33;
            for(int i=0;i<(name.EndsWith("btn")?3:1);i++)ResLoader.I.assets["res/uires/titleui/"+name+".gsa:"+i]=Pattern(w,h);}
        ResLoader.I.assets["res/uires/sysdlg/editbk.gsa:0"]=Pattern(166,24);
        GUI.Reset();flow.DrawTest();int loads=ResLoader.I.loads, resourceLoads=Resources.loads;
        Check(GUI.Draws.Count==10 && GUI.Fields.Count==4 && GUI.Buttons.Count==2,"Package arrival retries and renders native panel/labels/fields");
        Check(GUI.Buttons.All(b=>b.content==GUIContent.none && b.style.normal.background!=null),"Original baked button labels only; no overlaid new text");
        Check(GUI.Buttons.All(b=>b.style.hover.background!=null && b.style.active.background!=null),"All native button states loaded");
        GUI.Reset();flow.DrawTest();Check(ResLoader.I.loads==loads && Resources.loads==resourceLoads,"Repaints reuse loaded textures and labels");
        Check(GUI.Fields.Select(f=>f.rect.y).SequenceEqual(new float[]{228,259,290,321}),"Four evenly spaced rows");
        Check(GUI.Fields.Select(f=>f.limit).SequenceEqual(new[]{24,40,40,24}),"Existing field length constraints preserved");
        Check(GUI.Fields.Select(f=>f.secure).SequenceEqual(new[]{false,true,true,false}),"Both password fields remain masked");
        Check(GUI.Buttons[0].rect.x==242 && GUI.Buttons[1].rect.x==406 && GUI.Buttons.All(b=>b.rect.y==371),"Symmetric approved button geometry");
        Check(GUI.Buttons[0].rect.y-359==12 && 416-GUI.Buttons[0].rect.yMax==12,"Inner-border and outer-border gaps balanced");
        MobileClientPresentation.Enabled=true;GUI.Reset();flow.mobileTitleInput.calls.Clear();flow.DrawTest();
        var touches=flow.mobileTitleInput.calls;
        Check(touches.Count==4 && touches.All(t=>t.rect.height==30),"Four 30px mobile hit targets");
        for(int i=1;i<4;i++)Check(touches[i-1].rect.yMax < touches[i].rect.y,"Adjacent mobile targets never overlap");
        Check(touches[3].rect.yMax<371,"Last input target does not overlap actions");
        Check(touches.Select(t=>t.secure).SequenceEqual(new[]{false,true,true,false}),"Mobile secure flags unchanged");
        MobileClientPresentation.Enabled=false;GUI.Reset();GUI.activateButton=0;flow.DrawTest();Check(flow.submits==1,"Complete routes once to existing registration action");
        GUI.Reset();GUI.activateButton=1;flow.netMsg="transient";flow.DrawTest();Check(flow.Returned && flow.netMsg=="" && flow.mobileTitleInput.closed,"Return restores login and dismisses keyboard");
        Check(flow.invite=="","Invitation wire field stays empty");
        Texture2D owned=ResLoader.I.assets["res/uires/titleui/topbar.gsa:0"];int before=ObjectDestroyed.count;
        flow.ReleaseTest();flow.ReleaseTest();Check(!owned.destroyed && ObjectDestroyed.count>before,"Release destroys generated textures only and is idempotent");
        if(args.Length>=3)RenderExternal(flow,args[1],args[2]);
        Console.WriteLine("PASS registration presentation: "+checks+" assertions; actual production C# with deterministic Unity substitutes, no Unity/device execution.");
    }
    private static void RenderExternal(TitleFlow flow,string input,string output)
    {
        ResLoader.I=new ResLoader { Pkg=new object() };
        foreach(string name in new[]{"topbar","middlebar","bottombar","completebtn","returnbtn","editbk"})
        {
            string path="res/uires/"+(name=="editbk"?"sysdlg/":"titleui/")+name+".gsa:";
            for(int i=0;i<(name.EndsWith("btn")?3:1);i++)ResLoader.I.assets[path+i]=ReadRaw(Path.Combine(input,name+"-"+i+".raw"));
        }
        flow.source=ReadRaw(Path.Combine(input,"loginbk-0.raw"));GUI.Reset();flow.DrawTest();
        Directory.CreateDirectory(output);var lines=new List<string>();int n=0;
        foreach(var draw in GUI.Draws.Concat(GUI.Buttons.Select(b=>new GUI.Draw {rect=b.rect,texture=b.style.normal.background})))
        {
            string name="draw-"+(n++)+".raw";WriteRaw(Path.Combine(output,name),draw.texture);
            lines.Add(name+","+draw.rect.x+","+draw.rect.y+","+draw.rect.width+","+draw.rect.height);
        }
        File.WriteAllLines(Path.Combine(output,"draws.csv"),lines);
    }
    private static Texture2D ReadRaw(string path)
    {
        using(var r=new BinaryReader(File.OpenRead(path)))
        {int w=r.ReadInt32(),h=r.ReadInt32();var t=new Texture2D(w,h,TextureFormat.RGBA32,false);var p=new Color32[w*h];for(int y=0;y<h;y++)for(int x=0;x<w;x++)p[(h-1-y)*w+x]=new Color32(r.ReadByte(),r.ReadByte(),r.ReadByte(),r.ReadByte());t.SetPixels32(p);return t;}
    }
    private static void WriteRaw(string path,Texture2D texture)
    {
        using(var w=new BinaryWriter(File.Create(path)))
        {var pixels=texture.GetPixels32();w.Write(texture.width);w.Write(texture.height);for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++){var p=pixels[(texture.height-1-y)*texture.width+x];w.Write(p.r);w.Write(p.g);w.Write(p.b);w.Write(p.a);}}
    }
}

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    internal static class GsaTextureScale { internal static int Of(Texture2D t)=>1;internal static int Width(Texture2D t)=>t.width;internal static int Height(Texture2D t)=>t.height; }
    internal sealed class ResLoader { internal static ResLoader I;internal object Pkg,Pkg24,PkgFf21;internal int loads;internal Dictionary<string,Texture2D> assets=new Dictionary<string,Texture2D>();internal Texture2D LoadGsaFrameA(string p,int i){loads++;assets.TryGetValue(p+":"+i,out var t);return t;} }
    internal static class MobileClientPresentation { internal static bool Enabled; }
    internal sealed class MobileTitleTextInput { internal List<GUI.Field> calls=new List<GUI.Field>();internal bool closed;internal string Draw(string id,Rect r,string v,int l,bool s,GUIStyle style){calls.Add(new GUI.Field {rect=r,limit=l,secure=s});return v;}internal void Close(){closed=true;} }
    internal static class RecoveredTitleLoginSurface { internal static void DestroyTexture(Texture2D t){if(t&&!t.destroyed){t.destroyed=true;ObjectDestroyed.count++;}} }
    internal sealed partial class TitleFlow
    {
        internal Texture2D source; private Texture2D _loginPanel=>source;
        internal MobileTitleTextInput mobileTitleInput=new MobileTitleTextInput();
        internal GUIStyle _fieldStyle=new GUIStyle(),_inputStyle=new GUIStyle(),_panelStyle=new GUIStyle(),_regButtonStyle=new GUIStyle();
        private string _registerAccount="",_registerPassword="",_registerPasswordConfirm="",_registerSecurityCode="",_registerInviteCode="";
        internal string netMsg="";internal string invite=>_registerInviteCode;internal int submits;
        private enum TitlePage { Login,Register } private TitlePage _page=TitlePage.Register;internal bool Returned=>_page==TitlePage.Login;
        private void DoRegister(){submits++;}private void DrawHintBox(float x,float y,string t,float w){}
        private string DrawMaskedField(Rect r,string v){GUI.Fields.Add(new GUI.Field{rect=r,limit=40,secure=true});return v;}
        internal void DrawTest()=>DrawRegistrationPresentation();internal void ReleaseTest()=>ReleaseRegistrationPresentation();
    }
}
internal static class ObjectDestroyed { internal static int count; }
namespace UnityEngine
{
    internal class Object {internal bool destroyed;public static implicit operator bool(Object o)=>o!=null&&!o.destroyed;}
    internal class TextAsset:Object {internal string text;}
    internal static class Resources {internal static TextAsset subset;internal static int loads;internal static T Load<T>(string p) where T:Object {loads++;return subset as T;}}
    internal struct Color32 {internal byte r,g,b,a;internal Color32(byte r,byte g,byte b,byte a){this.r=r;this.g=g;this.b=b;this.a=a;}}
    internal struct Color {internal Color(float r,float g,float b,float a){}}
    internal enum TextureFormat {RGBA32} internal enum FilterMode {Point} internal enum TextureWrapMode {Clamp}
    internal class Texture2D:Object {internal int width,height;internal bool isReadable=true;internal string name;internal FilterMode filterMode;internal TextureWrapMode wrapMode;private Color32[] pixels;internal Texture2D(int w,int h,TextureFormat f,bool mip){width=w;height=h;pixels=new Color32[w*h];}internal Color32[] GetPixels32()=>(Color32[])pixels.Clone();internal void SetPixels32(Color32[] p){pixels=(Color32[])p.Clone();}internal void Apply(bool m,bool noRead){}}
    internal struct Rect {internal float x,y,width,height;internal float yMax=>y+height;internal Rect(float x,float y,float w,float h){this.x=x;this.y=y;this.width=w;this.height=h;}}
    internal class RectOffset {} internal enum FontStyle {Normal} internal enum TextAnchor {MiddleCenter} internal enum ImagePosition {ImageOnly}
    internal class GUIStyleState {internal Texture2D background;internal Color textColor;}
    internal class GUIStyle {internal GUIStyle(){}internal GUIStyle(GUIStyle s){}internal int fontSize;internal FontStyle fontStyle;internal TextAnchor alignment;internal ImagePosition imagePosition;internal RectOffset border,margin,padding,overflow;internal GUIStyleState normal=new GUIStyleState(),hover=new GUIStyleState(),active=new GUIStyleState(),focused=new GUIStyleState();}
    internal class GUIContent {internal static GUIContent none=new GUIContent();}
    internal class GUISkin {internal GUIStyle label=new GUIStyle();}
    internal static class Mathf {internal static float Round(float f)=>(float)Math.Round(f,MidpointRounding.ToEven);}
    internal static class GUI
    {
        internal class Draw {internal Rect rect;internal Texture2D texture;}
        internal class Field {internal Rect rect;internal int limit;internal bool secure;}
        internal class ButtonCall {internal Rect rect;internal GUIStyle style;internal GUIContent content;}
        internal static GUISkin skin=new GUISkin();internal static List<Draw> Draws=new List<Draw>();internal static List<Field> Fields=new List<Field>();internal static List<ButtonCall> Buttons=new List<ButtonCall>();internal static int activateButton=-1;
        internal static void Reset(){Draws.Clear();Fields.Clear();Buttons.Clear();activateButton=-1;}
        internal static void DrawTexture(Rect r,Texture2D t){Draws.Add(new Draw{rect=r,texture=t});}
        internal static void Box(Rect r,GUIContent c,GUIStyle s){}internal static void Label(Rect r,string t,GUIStyle s){}
        internal static string TextField(Rect r,string v,int l,GUIStyle s){Fields.Add(new Field{rect=r,limit=l});return v;}
        internal static bool Button(Rect r,GUIContent c,GUIStyle s){Buttons.Add(new ButtonCall{rect=r,content=c,style=s});return Buttons.Count-1==activateButton;}
        internal static bool Button(Rect r,string t,GUIStyle s)=>Button(r,(GUIContent)null,s);
    }
}
