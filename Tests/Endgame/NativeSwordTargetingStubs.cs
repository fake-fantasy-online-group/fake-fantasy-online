using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public struct Vector2
    {
        public float x,y;
        public Vector2(float x,float y) { this.x=x;this.y=y; }
        public static float Distance(Vector2 a,Vector2 b) => (float)Math.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y));
    }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z) { this.x=x;this.y=y;this.z=z; }
    }
    public class Transform { public Vector3 position; }
    public static class Time { public static float time; }
    public class Texture2D { }
    public struct Rect { }
    public struct Color { public Color(float r, float g, float b, float a) { } }
    public static class Mathf { public static float Max(float a, float b) => Math.Max(a,b); }
    public static class JsonUtility
    {
        public static T FromJson<T>(string text) => (T)Read(typeof(T), FakeFantasyOnline.Foundation.BoundedJson.Parse(text));
        private static object Read(Type type, object value)
        {
            if (value == null) return null;
            if (type == typeof(string)) return value;
            if (type == typeof(int)) return Convert.ToInt32(value);
            if (type.IsArray)
            {
                var source = (IReadOnlyList<object>)value;
                var output = Array.CreateInstance(type.GetElementType(), source.Count);
                for (int i=0;i<source.Count;i++) output.SetValue(Read(type.GetElementType(),source[i]),i);
                return output;
            }
            var map = (IReadOnlyDictionary<string,object>)value;
            var result = Activator.CreateInstance(type, true);
            foreach (var field in type.GetFields())
                if (map.TryGetValue(field.Name,out var item)) field.SetValue(result,Read(field.FieldType,item));
            return result;
        }
    }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public static class GameTables
    {
        public static readonly Dictionary<string,string> Texts = new Dictionary<string,string>();
        public static string LoadText(string name) => Texts.TryGetValue(name, out var text) ? text : null;
    }
    public sealed class ReferenceResourceLoader
    {
        public byte[] ReadSourceOwned(string path) => System.Text.Encoding.UTF8.GetBytes(GameTables.LoadText(path.Substring(7, path.Length-12)));
    }
    public partial class Win05
    {
        [Serializable] public sealed class SkInfo
        {
            public int id,icon,sys,type,maxlv,pre; public string name,desc; public object[] levels;
            public int castkind,tgt,shape,radius,rwidth,dist,rectl,rectw,canSelf,canFriendly,canEnemy;
        }
        [Serializable] private class SkDB { public SkInfo[] skills; }
        [Serializable] private class SkLayout { }
    }
    public sealed partial class SkillTargeting
    {
        public enum Aim { Instant, Single, Circle, Direction, Trap, Unavailable }
        private const float DefaultRadius=140f, ServerSingleRangeFallback=600f;
        public static string ValidateTarget(int id,object target) => null;
    }
    public interface ISelectable { }
    public sealed class ReferenceWorldRemoteSelectable : ISelectable { public RemotePlayer SourceIdentityForIndependentRuntime; }
    public sealed class RemotePlayer : ISelectable { public bool IsDead; public int id; public UnityEngine.Transform transform=new UnityEngine.Transform(); }
    public sealed class MonsterEntity : ISelectable { public bool IsDead, NoAttack; public int hp = 100; public UnityEngine.Transform transform=new UnityEngine.Transform(); }
    public static class TargetSelect { public static ISelectable Current; public static object CurrentSourceForIndependentRuntime; }
    public static class EndgamePanel { public static FakeFantasyOnline.Application.EndgameClientState GrowthState; }
    public static class SocialClient { public static int SelectedMemberId; public static bool InParty(int id) => false; }
    public sealed class NetClient { public static int SelfEntityId; public static NetClient I; public void UseSkill(int skill,int target) { } }
    public static class UITip { public static void Show(string text,float scale) { } }
    public static class UIScale { public static float Factor => 1f; }
    public static class CharData { public static float atkDist; public static int race; }
    public static class CombatNumber { public static float Ppu=100f; public static UnityEngine.Transform PlayerTransform=new UnityEngine.Transform(); }
    public partial class CombatWorld
    {
        private static CombatWorld I;
        private bool pluginResumingQueuedSkill,autoAttacking;
        private int pluginSkillIntentRevision,queuedSkillId,pendSkillId,pendGroundSkill;
        private float ppu=100f,castPauseUntil,pendGroundRangeU;
        private RemotePlayer pvpTarget;
        private MonsterEntity target;
        private static void Ensure() { if(I==null) I=new CombatWorld(); }
        private static bool TargetWorldPx(out int x,out int y) { x=y=0; return false; }
        public static void RequestGroundCast(int id,int x,int y,float range) { }
        private bool PlayerWithinGround(float range) => false;
        internal static void SetAuditTargetDistance(float pixels)
        {
            Ensure(); if(I.target==null) I.target=new MonsterEntity();
            I.target.transform.position=new UnityEngine.Vector3(pixels/I.ppu,0,0);
        }
        internal static bool AuditTargetInPendingRange() => I.TargetWithin(I.pendSkillRangeU);
    }
    public static class CustomMaps { public static bool IsCustom(int id) => false; }
    public static class GameBootstrap { public static int CurMapId; }
    public static class ZoneOverlay { public static float faShenPx; }
    public static class SkillElementDB { public static int Element(int id) => 0; }
    public static class GameHud
    {
        public static int SkillCooldownCountForIndependentRuntime => 0;
        public static void StartSkillCooldown(int id,float duration) { }
        public static float SkillCooldownFractionForIndependentRuntime(int id) => 0;
        public static bool DrawSkillCooldownForIndependentRuntime(UnityEngine.Rect area,int id,IReadOnlyList<UnityEngine.Texture2D> frames) => false;
        public static int CooldownFrameIndexForIndependentRuntime(float fraction) => 0;
    }
}
