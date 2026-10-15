using System;
using UnityEngine;
using FakeFantasyOnline.Presentation.ReferenceAssets;
namespace UnityEngine {
 public class Transform { public Vector3 position,localScale;public Quaternion rotation; }
 public struct Quaternion {public static Quaternion identity=>new Quaternion();}
 public class Sprite {}
 public class SpriteRenderer {public Transform transform=new Transform();public Sprite sprite;public bool flipX;}
 public class LineRenderer {public float widthMultiplier;public int positionCount;public Vector3[] Positions;public void SetPositions(Vector3[] p){Positions=p;}}
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 public sealed partial class SkillTargeting {
  private SpriteRenderer aimerSR,arrowSR;private Sprite[] aimerFrames,arrowFrames;
  private float aimerTexW=207,aimerHdScale=1,radiusPx=100,aimerTimer,rectLen=200,rectWid=70,fanDeg=90;
  private int aimerIdx,skillId;private const float DefaultRadius=140,AimerFps=10,ArrowScale=1,OutlineWidthPx=4;private const int FanArcSegs=24;
  private LineRenderer selLine=new LineRenderer();
  public void Render(float x,float y)=>UpdateOutline(PlayerController.I,x,y,100,1000,1000);
  public Vector3[] Points=>selLine.Positions;
 }
}
class MobileTargetingGeometryContract {
 static void Check(bool yes,string name){if(!yes)throw new Exception(name);}
 static bool Near(float x,float y)=>Math.Abs(x-y)<.0001f;
 static int Main(){
  var p=PlayerController.I;p.worldX=500;p.worldY=500;p.FaceOct=0;
  GameHud.hotSkill[0]=1;SkillTargeting.Mode=SkillTargeting.Aim.Direction;
  SkillTargeting.BeginMobileHotSlot(0);
  var aim=SkillTargeting.MobileAimPosition(Vector2.zero);
  Check(Near(aim.x,600)&&Near(aim.y,500),"neutral directional press uses current east facing");
  SkillTargeting.I.Render(600,500);var old=SkillTargeting.I.Points[0];
  p.worldX=700;p.worldY=650;p.FaceOct=2;
  SkillTargeting.I.Render(700,650);var next=SkillTargeting.I.Points[0];
  Check(!Near(old.x,next.x)&&Near(next.y,-1.5f),"zero delta rebuilds old world-space outline at moved feet");
  aim=SkillTargeting.MobileAimPosition(Vector2.zero);
  Check(Near(aim.x,700)&&Near(aim.y,250),"fresh neutral aim follows moved feet and south facing");
  aim=SkillTargeting.MobileAimPosition(new Vector2(120,0));
  Check(Near(aim.x,800)&&Near(aim.y,350),"drag remains relative to moved player");
  MobileClientPresentation.Enabled=false;
  var retained=SkillTargeting.I.Points;SkillTargeting.I.Render(700,650);
  Check(ReferenceEquals(retained,SkillTargeting.I.Points),"desktop zero-delta behavior unchanged");
  Console.WriteLine("PASS live mobile geometry: 5 checks");return 0;
 }
}
