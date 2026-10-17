using System;
using UnityEngine;
using FakeFantasyOnline.Presentation.ReferenceAssets;
public static class MobileWorldScaleContract {
 static int checks;
 static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
 static void Near(float a,float b,string label){Check(Math.Abs(a-b)<.0001f,label+": "+a+" != "+b);}
 static CameraFollow New(bool mobile,int width,int height){MobileClientPresentation.Enabled=mobile;Screen.width=width;Screen.height=height;var c=new CameraFollow{TestCamera=new Camera{aspect=(float)width/height},target=new Transform{position=new Vector3(1.337f,-2.918f,0)}};c.Awake();return c;}
 static CameraFollowBaseline Baseline(CameraFollow c){var b=new CameraFollowBaseline{TestCamera=new Camera{orthographicSize=c.TestCamera.orthographicSize,aspect=c.TestCamera.aspect},target=c.target,worldHalfW=c.worldHalfW,worldHalfH=c.worldHalfH,isoDiamond=c.isoDiamond,wheelZoom=c.wheelZoom};b.Awake();return b;}
 public static void Main(){
  foreach(int height in new[]{360,540,720,1080,1500,2160,3000}) foreach(int width in new[]{640,1281,1920}) foreach(int k in new[]{1,2}) {
   GameSettings.K=k;var desktop=New(false,width,height);var old=Baseline(desktop);old.LateUpdate();int oldScale=ArtDetail.PixelScale;desktop.LateUpdate();
   Near(desktop.TestCamera.orthographicSize,old.TestCamera.orthographicSize,"desktop size unchanged");Near(desktop.transform.position.x,old.transform.position.x,"desktop x unchanged");Near(desktop.transform.position.y,old.transform.position.y,"desktop y unchanged");Check(ArtDetail.PixelScale==oldScale,"desktop art scale unchanged");
   var mobile=New(true,width,height);mobile.LateUpdate();Near(mobile.TestCamera.orthographicSize*2,old.TestCamera.orthographicSize,"mobile exactly 2x");Check(ArtDetail.PixelScale==2*oldScale,"mobile ArtDetail follows camera");Near(mobile.ppu,100,"world PPU unchanged");
   float fixedSize=mobile.TestCamera.orthographicSize;for(int frame=0;frame<10;frame++)mobile.LateUpdate();Near(mobile.TestCamera.orthographicSize,fixedSize,"no frame compounding");
   // Projection/inverse equations use the updated orthographic size, not a scaled world position.
   float worldX=mobile.target.position.x, worldY=mobile.target.position.y;
   float pixelX=(worldX-mobile.transform.position.x)/(mobile.TestCamera.orthographicSize*mobile.TestCamera.aspect)*width*.5f+width*.5f;
   float pixelY=(worldY-mobile.transform.position.y)/mobile.TestCamera.orthographicSize*height*.5f+height*.5f;
   Near((pixelX/width*2-1)*mobile.TestCamera.orthographicSize*mobile.TestCamera.aspect+mobile.transform.position.x,worldX,"project/unproject x");
   Near((pixelY/height*2-1)*mobile.TestCamera.orthographicSize+mobile.transform.position.y,worldY,"project/unproject y");
  }
  GameSettings.K=1;
  var resized=New(true,1920,1080);resized.LateUpdate();Screen.width=1080;Screen.height=1920;resized.TestCamera.aspect=1080f/1920;resized.LateUpdate();Near(resized.TestCamera.orthographicSize,2.4f,"portrait resize recalculates");Screen.height=0;float before=resized.TestCamera.orthographicSize;resized.LateUpdate();Near(resized.TestCamera.orthographicSize,before,"zero-height safe");
  foreach(bool diamond in new[]{false,true}) foreach(bool mobile in new[]{false,true}) {
   var c=New(mobile,1920,1080);c.isoDiamond=diamond;c.worldHalfW=40;c.worldHalfH=30;c.target.position=new Vector3(1000,1000,0);var old=Baseline(c);old.LateUpdate();c.LateUpdate();
   float hw=c.TestCamera.orthographicSize*c.TestCamera.aspect,hh=c.TestCamera.orthographicSize;
   if(diamond) Check((Math.Abs(c.transform.position.x)+hw)/40+(Math.Abs(c.transform.position.y)+hh)/30<=1.001,"diamond viewport bounds");
   else {Check(Math.Abs(c.transform.position.x)+hw<=40.01,"rectangular x viewport bounds");Check(Math.Abs(c.transform.position.y)+hh<=30.01,"rectangular y viewport bounds");}
   if(!mobile){Near(c.transform.position.x,old.transform.position.x,"desktop bound x unchanged");Near(c.transform.position.y,old.transform.position.y,"desktop bound y unchanged");}
   c.worldHalfW=.1f;c.worldHalfH=.1f;c.LateUpdate();Near(c.transform.position.x,0,"tiny map centers x");Near(c.transform.position.y,0,"tiny map centers y");
  }
  var locked=New(true,1920,1080);DuelHud.Locked=true;DuelHud.Position=new Vector3(2,3,0);locked.LateUpdate();Near(locked.transform.position.x,2,"spectator lock x retained");Near(locked.transform.position.y,3,"spectator lock y retained");DuelHud.Locked=false;
  foreach(bool mobile in new[]{false,true}) {var wheel=New(mobile,1920,1080);wheel.wheelZoom=true;wheel.TestCamera.orthographicSize=5.4f;Input.mouseScrollDelta=new Vector2(0,0);wheel.LateUpdate();float start=wheel.TestCamera.orthographicSize;for(int i=0;i<30;i++)wheel.LateUpdate();Near(wheel.TestCamera.orthographicSize,start,"wheel idle does not compound");}
  foreach(bool wheelMode in new[]{false,true}) foreach(float initial in new[]{2f,5f,10f,40f}) {
   var c=New(false,1920,1080);c.wheelZoom=wheelMode;c.TestCamera.orthographicSize=initial;var b=Baseline(c);
   foreach(bool mobile in new[]{false,true,true,false,true,false}) {
    MobileClientPresentation.Enabled=mobile;Input.mouseScrollDelta=new Vector2(0,0);b.LateUpdate();c.LateUpdate();
    Near(c.TestCamera.orthographicSize*(mobile?2:1),b.TestCamera.orthographicSize,"wheel/nonwheel toggle and initialization");
   }
   foreach(bool mobile in new[]{false,true}) foreach(float scroll in new[]{.5f,-.5f,1f,-1f}) {
    MobileClientPresentation.Enabled=mobile;Input.mouseScrollDelta=new Vector2(0,scroll);b.LateUpdate();c.LateUpdate();Near(c.TestCamera.orthographicSize*(mobile?2:1),b.TestCamera.orthographicSize,"desktop/mobile wheel input parity");
   }
  }
  Input.mouseScrollDelta=new Vector2(0,0);
  var nullTarget=New(true,1920,1080);nullTarget.target=null;nullTarget.LateUpdate();Near(nullTarget.TestCamera.orthographicSize,2.7f,"zoom before target exists");nullTarget.LateUpdate();Near(nullTarget.TestCamera.orthographicSize,2.7f,"null target no compounding");
  Console.WriteLine("PASS mobile world scale: "+checks+" checks; desktop baseline, 2x mobile, ArtDetail, repeat frames, resize, bounds, lock, projection equations");
 }
}
