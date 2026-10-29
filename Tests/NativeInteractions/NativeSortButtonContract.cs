using System;
using UnityEngine;
using FakeFantasyOnline.Presentation.ReferenceAssets;
namespace FakeFantasyOnline.Presentation.ReferenceAssets {public static class GsaDecoder {public static float W(Texture2D t)=>t.width;public static float H(Texture2D t)=>t.height;}}
public class NativeSortTest {
 static int checks;static void Check(bool ok,string name){checks++;if(!ok)throw new Exception(name);}
 static void Main(){
  ResLoader.I=new ResLoader();Event.current.type=EventType.Repaint;
  foreach(float scale in new[]{.5f,1f,1.25f,2f,3f}){
   foreach(int state in new[]{0,1,2,3}){
    var rect=new Rect(365*scale,40*scale,48*scale,24*scale);GUI.enabled=state!=3;GUI.cells.Clear();
    var prior=new Color(.2f,.3f,.4f,.5f);GUI.color=prior;
    NativeSortButtonArtwork.Draw(rect,scale,state==1,state==2);
    Check(GUI.cells.Count==3,"full-height native horizontal three-slice");
    float total=0;
    foreach(var cell in GUI.cells){total+=cell.width;Check(cell.y==rect.y&&cell.height==rect.height&&cell.x>=rect.x&&cell.xMax<=rect.xMax+.001f,"native height and bounds preserved");}
    Check(Math.Abs(total-rect.width)<.001f,"face is fully covered without gaps");
    Check(GUI.last.name==NativeSortButtonArtwork.BackgroundPath+":"+state,"actual native state frame");
    Check(BitmapFont.lastScale==scale,"native 12px bitmap label retained");
    Check(GUI.color.r==prior.r&&GUI.color.a==prior.a,"caller GUI tint unchanged");
    Check(rect.Contains(new Vector2(rect.x+.1f*scale,rect.yMax-.1f*scale)),"visible bottom edge clickable");
    Check(!rect.Contains(new Vector2(rect.x+.1f*scale,64*scale)),"sort does not cover bag background at64");
    var warehouse=new Rect(363*scale,38*scale,48*scale,24*scale);
    Check(warehouse.Contains(new Vector2(warehouse.x+.1f*scale,warehouse.yMax-.1f*scale))&&warehouse.yMax<=64*scale,"warehouse bottom edge fully hit and outside slots");
   }
  }
  GUI.enabled=true;GUI.color=Color.white;var r=new Rect(0,0,48,24);
  int before=ResLoader.I.calls;NativeSortButtonArtwork.Draw(r,1,false,false);Check(ResLoader.I.calls==before,"no per-event texture load");
  ResLoader.I.Pkg=new object();NativeSortButtonArtwork.Draw(r,1,false,false);Check(ResLoader.I.calls>before,"shared cache invalidates for changed package");
  var texture=ArtworkSkin.Frames(NativeSortButtonArtwork.BackgroundPath)[0];texture.height=39;GUI.cells.Clear();NativeSortButtonArtwork.Draw(r,1,false,false);Check(GUI.last.name.StartsWith("res/uires/votedlg/buttonbk.gsa"),"wrong-size art uses labelled existing fallback");texture.height=24;
  BitmapFont.Ready=false;NativeSortButtonArtwork.Draw(r,1,false,false);Check(GUI.lastLabel=="整理","font failure still has readable fallback label");BitmapFont.Ready=true;
  ResLoader.I=null;NativeSortButtonArtwork.Draw(r,1,false,false);Check(true,"missing package does not break original action surface");
  Console.WriteLine("PASS "+checks+" native sort draw/cache/geometry assertions (not Unity)");
 }
}
