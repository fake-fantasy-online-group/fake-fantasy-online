using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public class MonoBehaviour { }
    public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z) {this.x=x;this.y=y;this.z=z;} }
    public static class JsonUtility
    {
        public static T FromJson<T>(string text) => (T)Read(typeof(T),FakeFantasyOnline.Foundation.BoundedJson.Parse(text));
        static object Read(Type type,object value)
        {
            if(value==null)return null;if(type==typeof(string))return value;if(type==typeof(int))return Convert.ToInt32(value);
            if(type.IsArray){var input=(IReadOnlyList<object>)value;var output=Array.CreateInstance(type.GetElementType(),input.Count);for(int i=0;i<input.Count;i++)output.SetValue(Read(type.GetElementType(),input[i]),i);return output;}
            var map=(IReadOnlyDictionary<string,object>)value;var result=Activator.CreateInstance(type,true);foreach(var field in type.GetFields())if(map.TryGetValue(field.Name,out var item))field.SetValue(result,Read(field.FieldType,item));return result;
        }
    }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public static class GameTables { public static readonly Dictionary<string,string> Texts=new Dictionary<string,string>(); public static string LoadText(string id)=>Texts.TryGetValue(id,out var s)?s:null; }
    public partial class Win05
    {
        [Serializable] public sealed class SkLevel { public int lv,mp; }
        [Serializable] public sealed class SkInfo { public int id,icon,sys,type,maxlv,pre,castkind,tgt,shape,radius,rwidth,dist,rectl,rectw,canSelf,canFriendly,canEnemy;public string name,desc;public SkLevel[] levels; }
        [Serializable] private sealed class SkDB { public SkInfo[] skills; }
        [Serializable] private sealed class SkLayout { }
    }
    internal static class EndgamePanel { internal static FakeFantasyOnline.Application.EndgameClientState GrowthState; }
    public sealed class SkillTargeting { public enum Aim {Instant,Single,Circle,Direction,Trap,Unavailable} }
    public interface ISelectable { }
    public sealed class RemotePlayer:ISelectable {public int id;public bool IsDead;}
    public sealed class ReferenceWorldRemoteSelectable:ISelectable {public RemotePlayer SourceIdentityForIndependentRuntime;}
    public sealed class MonsterEntity:ISelectable {public bool NoAttack;public int hp=100;}
    public static class TargetSelect {public static ISelectable Current;public static object CurrentSourceForIndependentRuntime;}
    public static class SocialClient {public static int SelectedMemberId;public static HashSet<int> Party=new HashSet<int>();public static bool InParty(int id)=>Party.Contains(id);}
    internal partial class NetClient {public static int SelfEntityId=1;public static int PendingCastSkill;}
    public static class CharData {public static int mp,race;public static Dictionary<int,int> skills=new Dictionary<int,int>();}
    public static class UIScale {public static float Factor=>1f;}
    public static class UITip {public static string Last;public static void Show(string text,float scale){Last=text;}}
    public static class GameBootstrap {public static int Flushes;public static void FlushMoveReport(){Flushes++;}}
    public sealed class PlayerController {public static PlayerController I;public float worldW=1000,worldH=1000,ppu=100;public void BreakSit(){}}
    public static class CombatWorld {public static int Casts;public static void NotifyCast(float duration){Casts++;}}
}
