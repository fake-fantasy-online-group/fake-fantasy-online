#!/usr/bin/env python3
from pathlib import Path
import os, subprocess, tempfile
root=Path(__file__).resolve().parents[2];src=root/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
s=(src/'TaskDlg.cs').read_text();start=s.index('    public static TaskDB.Def DisplayDef(');brace=s.index('{',start);depth=1;end=brace+1
while depth:
 depth+=(s[end]=='{')-(s[end]=='}');end+=1
body=s[start:end]
window=(src/'TaskDlg.Window.cs').read_text()
done=window[window.index('            List<TaskDB.Def> right ='):window.index('            float titlePad =')]
body+='\npublic static List<string> DoneTitles(int tab) { var rows=new List<object>(); '+done+' return missingDoneTitles; }'

contract='''using System; using System.Collections.Generic;
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
static class TaskDB { public class Def { public int id,type; public string name,desc,publisher,place,award; } public static Dictionary<int,Def> Rows=new Dictionary<int,Def>(); public static Def Get(int id)=>Rows.TryGetValue(id,out var d)?d:null; }
static class TaskClient { public static HashSet<int> Active=new HashSet<int>(); public static HashSet<int> Done=new HashSet<int>(); public static IEnumerable<TaskDB.Def> Recommended(int n)=>new TaskDB.Def[0]; public static bool IsActive(int id)=>Active.Contains(id); }
class TaskDlg { BODY }
static class Test { static void Main() {
foreach(int id in new[]{1900030001,1900030002,1900030100}) {
 if(TaskDlg.DisplayDef(id)!=null)throw new Exception("inactive definitions synthesized");
 TaskClient.Active.Add(id);var d=TaskDlg.DisplayDef(id);
 if(d.name!=CubeQuestTitles.MissingTitle(id)||d.publisher!=""||d.desc!=""||TaskDB.Get(id)!=null)throw new Exception("title-only fallback violated");
 var original=new TaskDB.Def{id=id,name="Native override"};TaskDB.Rows[id]=original;
 if(!object.ReferenceEquals(TaskDlg.DisplayDef(id),original))throw new Exception("native metadata overridden");
}
TaskClient.Active.Add(90001);if(TaskDlg.DisplayDef(90001).name!="妖魔结界试炼")throw new Exception("trial changed");
for(int i=1;i<1000;i++)if(CubeQuestTitles.MissingTitle(i)!=null)throw new Exception("generic fallback expanded");
TaskClient.Active.Add(999);if(TaskDlg.DisplayDef(999).name!="任务999")throw new Exception("legacy unknown changed");
TaskDB.Rows.Clear();TaskClient.Done.UnionWith(new[]{1900030001,1900030002,999});
var titles=TaskDlg.DoneTitles(3);if(titles.Count!=2||!titles.Contains("问鼎周天")||!titles.Contains("幻翼初展"))throw new Exception("completed native title rows missing");
if(TaskDlg.DoneTitles(0).Count!=0||TaskDB.Rows.Count!=0)throw new Exception("recommendations or definitions changed");
TaskDB.Rows[1900030001]=new TaskDB.Def{id=1900030001,name="Native"};if(TaskDlg.DoneTitles(3).Count!=1)throw new Exception("completed native row duplicated");
Console.WriteLine("PASS cube/wing/spirit-daily title fallback / actual DisplayDef and completed-list assertions including 999 unrelated IDs; no resource or TaskDB mutation.");
} } }
'''.replace('BODY',body)
mono=Path(os.environ['FFO_MONO_ROOT'])
with tempfile.TemporaryDirectory() as d:
 d=Path(d);(d/'Test.cs').write_text(contract)
 subprocess.run([str(mono/'bin/mono'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(d/'Test.exe'),str(d/'Test.cs'),str(src/'CubeQuestTitles.cs')],check=True)
 subprocess.run([str(mono/'bin/mono'),str(d/'Test.exe')],check=True)
window=(src/'TaskDlg.Window.cs').read_text()
assert 'else\n                    {\n                        string title = CubeQuestTitles.MissingTitle(id);' in window
assert 'right.Count + missingDoneTitles.Count' in window and 'foreach (string title in missingDoneTitles)' in window
assert 'new TaskDB.Def' not in window
for name in ['PlayerTaskTrack.cs','ReferenceWorldPlayerTaskTrack.cs']:
 assert ': CubeQuestTitles.MissingTitle(' in (src/name).read_text() and 'TaskDB.Get(' in (src/name).read_text()
print('PASS completed-row/tracker source guards; recommendations still use existing TaskDB only.')
