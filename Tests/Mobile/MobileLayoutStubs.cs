using System;
namespace UnityEngine {
public struct Vector2 { public float sqrMagnitude=>x*x+y*y; public static Vector2 operator -(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y); public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} public static Vector2 operator +(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y); public static Vector2 operator *(Vector2 a,float b)=>new Vector2(a.x*b,a.y*b); }
public struct Rect {public bool Overlaps(Rect r)=>x<r.xMax&&xMax>r.x&&y<r.yMax&&yMax>r.y;public float x,y,width,height; public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;} public float xMax=>x+width;public float yMax=>y+height;public Vector2 center=>new Vector2(x+width/2,y+height/2);public bool Contains(Vector2 p)=>p.x>=x&&p.x<xMax&&p.y>=y&&p.y<yMax;}
public static class Mathf {public static int Clamp(int v,int min,int max)=>Math.Max(min,Math.Min(max,v));public const float Deg2Rad=(float)(Math.PI/180); public static float Cos(float v)=>(float)Math.Cos(v); public static float Sin(float v)=>(float)Math.Sin(v);public static float Min(float a,float b)=>Math.Min(a,b); public static float Max(float a,float b)=>Math.Max(a,b);}
public static class Application {public static bool isMobilePlatform;public static string persistentDataPath=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"ffo-layout-contract-"+Guid.NewGuid().ToString("N"));}
public static class Time {public static int frameCount;}
public class Texture2D { }
public static class Screen {public static int width,height;public static Rect safeArea;}
public static class TouchScreenKeyboard {public static bool visible;public static Rect area;}
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
public static class ClientInput {public static void CancelAll() { }}
public static class UIScale { public static float User=1f; }
public static class AddonUiHost {public static bool PointerOver(UnityEngine.Vector2 point)=>false;}
public class GameHud {internal static UnityEngine.Rect MobileDiagramRect=>new UnityEngine.Rect(250,100,300,200);internal static bool MobileGroupWheelOpening,MobileGroupWheelClosing;public static GameHud I;public bool showChat=true,showAvatar=true,showMinimap=true,diagramOpen;internal static int ControlButtonCountForIndependentRuntime=>9;}
public class EquipDlg {public bool MobileOverlayContains(UnityEngine.Vector2 p)=>false;public static EquipDlg I;public bool IsOpen;}
public class Win05 {public bool MobileOverlayContains(UnityEngine.Vector2 p)=>false;public static Win05 I;public bool IsOpen;}
public class UIManager {public bool MobileWindowContains(UnityEngine.Vector2 p)=>false;public static UIManager I;public bool MobileWindowOpen;}
public static class AccountSecDlg {public static bool Visible;}
public static class ArenaDeathDlg {public static bool Visible;}
public static class ArenaDraftDlg {public static bool Visible;}
public static class ArenaEscDlg {public static bool Visible;}
public static class AttrWashDlg {public static bool Visible;}
public static class ChatCopyOverlay {public static bool Visible;}
public static class ColorDlg {public static bool Visible;}
public static class ComposeDlg {public static bool Visible;}
public static class DealDlg {public static bool Visible;}
public static class DialogUI {public static bool VisibleS;}
public static class DifficultyBox {public static bool Visible;}
public static class DuelDlg {public static bool Visible;}
public static class EquipFuseDlg {public static bool Visible;}
public static class ExEquipDlg {public static bool IsOpen;}
public static class FaceDlg {public static bool Visible;}
public static class FamilyCreateDlg {public static bool Value;public static bool get_Visible()=>Value;}
public static class FamilyDetailDlg {public static bool Value;public static bool get_Visible()=>Value;}
public static class FamilyDlg {public static UnityEngine.Rect Bounds;public static bool MobileContainsPointer(UnityEngine.Vector2 point)=>Value && Bounds.Contains(point);public static bool Value;public static bool get_Visible()=>Value;}
public static class FamilyMailDlg {public static bool Value;public static bool get_Visible()=>Value;}
public static class FamilyRelationDlg {public static bool Value;public static bool get_Visible()=>Value;}
public static class FamilyStashDlg {public static bool Visible;}
public static class FigureDlg {public static UnityEngine.Rect Bounds;public static bool MobileContainsPointer(UnityEngine.Vector2 point)=>Visible && Bounds.Contains(point);public static bool Visible;}
public static class FindModeDlg {public static bool Visible;}
public static class FriendConfigDlg {public static bool Visible;}
public static class FriendInfoDlg {public static bool Visible;}
public static class GarageDlg {public static bool Visible;}
public static class GossipDlg {public static bool Visible;}
public static class GroceryConfigDlg {public static bool Visible;}
public static class GroceryDetailDlg {public static bool Visible;}
public static class GroceryStatusDlg {public static bool Visible;}
public static class HelpWinDlg {public static bool Visible;}
public static class HornDialog {public static bool Visible;}
public class InventoryDlg {public static bool SplitOpen;public static InventoryDlg I;public bool MobileOverlayContains(UnityEngine.Vector2 p)=>false;public static bool IsOpen;}
public static class JobCustomDlg {public static bool Value;public static bool get_Visible()=>Value;}
public static class JobOfferDlg {public static bool Value;public static bool get_Visible()=>Value;}
public static class JobRightDlg {public static bool Value;public static bool get_Visible()=>Value;}
public static class MailConfigDlg {public static bool Visible;}
public static class MailDlg {public static bool ComposeVisible;public static UnityEngine.Rect Bounds;public static bool MobileContainsPointer(UnityEngine.Vector2 point)=>Visible && Bounds.Contains(point);public static bool Visible;}
public static class MailResistDlg {public static bool Visible;}
public static class MasterInfoDlg {public static bool Visible;}
public static class MasterListDlg {public static bool Visible;}
public static class MoneyInputDlg {public static bool Visible;}
public static class PasswdDlg {public static bool Visible;}
public static class PetArousalDlg {public static bool Visible;}
public static class PetForgeDlg {public static bool Visible;}
public static class PlayerActionMenu {public static bool Visible;}
public static class QueryDlg {public static bool Visible;}
public static class RackShopDlg {public static bool Visible;}
public static class RenameDlg {public static bool Visible;}
public static class RepairDlg {public static bool Visible;}
public static class ReportDlg {public static bool Visible;}
public static class ReviveDlg {public static bool Visible;}
public static class SculpDlg {public static bool Visible;}
public static class SecCodeAskDlg {public static bool Visible;}
public static class SelectPopupDlg {public static bool Visible;}
public static class ShopDlg {public static bool Picking;public static UnityEngine.Rect Bounds;public static bool MobileContainsPointer(UnityEngine.Vector2 point)=>Visible && Bounds.Contains(point);public static bool Visible;}
public static class StallDlg {public static bool Visible;}
public static class StoreDlg {public static UnityEngine.Rect Bounds;public static bool MobileContainsPointer(UnityEngine.Vector2 point)=>Visible && Bounds.Contains(point);public static bool Visible;}
public static class SysConfigDlg {public static UnityEngine.Rect Bounds;public static bool MobileContainsPointer(UnityEngine.Vector2 point)=>Visible && Bounds.Contains(point);public static bool Visible;}
public static class SysMsgBox {public static bool Visible;}
public static class SysOptionDlg {public static UnityEngine.Rect Bounds;public static bool MobileContainsPointer(UnityEngine.Vector2 point)=>Visible && Bounds.Contains(point);public static bool Visible;}
public static class TaskDlg {public static UnityEngine.Rect Bounds;public static bool MobileContainsPointer(UnityEngine.Vector2 point)=>Visible && Bounds.Contains(point);public static bool Visible;}
public static class TeamConfigDlg {public static bool Visible;}
public static class TeamDlg {public static UnityEngine.Rect Bounds;public static bool MobileContainsPointer(UnityEngine.Vector2 point)=>Visible && Bounds.Contains(point);public static bool Visible;}
public static class TradeDlg {public static UnityEngine.Rect Bounds;public static bool MobileContainsPointer(UnityEngine.Vector2 point)=>Visible && Bounds.Contains(point);public static bool Visible;}
public static class TransportUI {public static bool VisibleS;}
public static class VoiceSetDlg {public static bool Visible;}
public static class WorldMapDlg {public static bool Visible;}
}

namespace FakeFantasyOnline.Presentation.ReferenceAssets { public static class WinZ {public static bool AnyWindowAt(UnityEngine.Vector2 p)=>false;}public static class UITip {public static bool MobileContainsPointer(UnityEngine.Vector2 p)=>false;} }
