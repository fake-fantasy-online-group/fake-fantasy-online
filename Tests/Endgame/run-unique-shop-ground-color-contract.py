#!/usr/bin/env python3
"""Resource-free palette/identity execution plus shop wiring guards.
Uses extracted production methods in a minimal C# host; does not emulate Unity rendering.
Usage: python3 script.py [source root] [fallback root]
Requires FFO_DOTNET and FFO_DOTNET_CSC (the standard managed-test toolchain).
"""
import os, pathlib, subprocess, sys, tempfile, json
root = pathlib.Path(sys.argv[1]) if len(sys.argv)>1 else pathlib.Path(__file__).resolve().parents[2]
fallback = pathlib.Path(sys.argv[2]) if len(sys.argv)>2 else root
rel = pathlib.Path('Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets')
def read(name):
    p = root / rel / name
    return (p if p.exists() else fallback / rel / name).read_text()
def method(text, signature):
    start=text.index(signature); brace=text.index('{',start); depth=1; end=brace+1
    while depth:
        depth += (text[end]=='{')-(text[end]=='}'); end+=1
    return text[start:end]
source=read('ShopDlg.Source.cs'); rows=read('ShopDlg.Rows.cs'); ground=read('GroundItems.Names.cs'); tips=read('EndgamePanel.Tooltips.cs')
sale=source.split('else if (!stallMode)',1)[1].split('\n        else\n',1)[0]
assert 'EndgamePanel.BagNameQuality(tab, item)' in sale
assert 'null, false, nameQuality, item.id)' in sale
assert 'EndgamePanel.BagTooltip(tab, item,' in sale
assert sale.count('capturedItem.sell, capturedRemaining, capturedItem.quality, -1)')==2
assert '!stallMode && sellMode && entry.tab < 3' in rows
assert 'nativeSale = native.id == entry.id' in rows
assert 'EndgamePanel.BagNameQuality(entry.tab, native)' in rows
assert 'itemBgR, false, nameQuality, entry.id)' in rows
assert 'EndgamePanel.BagTooltip(entry.tab, native, tipText)' in rows
assert 'LabelColor(item.quality, item.uniqueId, protectedItem)' in ground
palette=method(read('UITip.cs'),'public static Color32 QualityColor(')
bag=method(tips,'internal static int BagNameQuality(')
label=ground[ground.index('internal static Color LabelColor('):ground.index(';',ground.index('internal static Color LabelColor('))+1]
host='''using System;
struct Color { public int r,g,b; public static Color red => new Color { r=255 }; public static implicit operator Color(Color32 c) => new Color {r=c.r,g=c.g,b=c.b}; }
struct Color32 { public int r,g,b; public Color32(int r,int g,int b,int a){this.r=r;this.g=g;this.b=b;} }
static class GameSettings { public static bool ColorWeak; }
static class UITip { PALETTE }
static class InventoryDlg { public struct Item { public int id,bagIdx; public byte quality; } }
class Instance { public int UniqueID,FormationID; public bool Valid=true; }
class State { public Instance Item; public Instance Find(string location,int tab,int slot,int id) => location=="bag" && tab==2 && slot==3 && id==42 ? Item : null; }
class Panel { public State State; }
class EndgamePanel { public static Panel Current; static bool MatchesNative(Instance i)=>i.Valid; static bool IsSpecial(Instance i)=>i.UniqueID>0 || i.FormationID>0; BAG }
class GroundItems { LABEL }
class Test {
static int checks; static void Check(bool b){checks++; if(!b)throw new Exception("Check "+checks);}
static bool Same(Color a,Color b)=>a.r==b.r&&a.g==b.g&&a.b==b.b;
static void Main(){
var item=new InventoryDlg.Item{id=42,bagIdx=3};
var instance=new Instance{UniqueID=81}; var state=new State{Item=instance};
foreach(bool weak in new[]{false,true}){GameSettings.ColorWeak=weak;
for(byte q=0;q<=5;q++){item.quality=q;
EndgamePanel.Current=null; Check(EndgamePanel.BagNameQuality(2,item)==q);
EndgamePanel.Current=new Panel{State=state}; Check(EndgamePanel.BagNameQuality(2,item)==5);
Check(EndgamePanel.BagNameQuality(1,item)==q);
instance.Valid=false; Check(EndgamePanel.BagNameQuality(2,item)==q);instance.Valid=true;
instance.UniqueID=0; Check(EndgamePanel.BagNameQuality(2,item)==q);
instance.FormationID=4; Check(EndgamePanel.BagNameQuality(2,item)==5);instance.FormationID=0;instance.UniqueID=81;
Check(Same(GroundItems.LabelColor(q,81,false),UITip.QualityColor(5)));
Check(Same(GroundItems.LabelColor(q,0,false),UITip.QualityColor(q)));
Check(Same(GroundItems.LabelColor(q,81,true),Color.red));
Check(Same(GroundItems.LabelColor(q,0,true),Color.red));
}}
Color gold=UITip.QualityColor(5);Check(gold.r==183&&gold.g==155&&gold.b==98);
Check(!Same(gold,UITip.QualityColor(4)));
Console.WriteLine("PASS: "+checks+" executed palette/identity cases; 10 source-wiring guards");
}}
'''.replace('PALETTE',palette).replace('BAG',bag).replace('LABEL',label)
dotnet=os.environ['FFO_DOTNET'];csc=os.environ['FFO_DOTNET_CSC']
with tempfile.TemporaryDirectory() as d:
    d=pathlib.Path(d); (d/'test.cs').write_text(host)
    runtime=pathlib.Path(dotnet).parent/'shared'/'Microsoft.NETCore.App'
    version=sorted(runtime.iterdir())[-1]
    refs=[f'-r:{p}' for p in version.glob('*.dll')]
    subprocess.run([dotnet,csc,'-nologo','-noconfig','-nostdlib+',f'-out:{d}/test.dll',*refs,str(d/'test.cs')],check=True)
    (d/'test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net6.0','framework':{'name':'Microsoft.NETCore.App','version':version.name}}}))
    subprocess.run([dotnet,str(d/'test.dll')],check=True)
