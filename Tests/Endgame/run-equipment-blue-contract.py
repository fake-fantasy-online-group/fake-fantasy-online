#!/usr/bin/env python3
"""Execute native UITip.Layout and full diagnostic layout with shared production palette.
Optional file args are actual server tooltip text payloads to decode for a preview.
"""
from pathlib import Path
import os,sys,subprocess,tempfile
root=Path(__file__).resolve().parents[2];p=root/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
s=(p/'UITip.cs').read_text();methods=s[s.index('    private static (List<(string, Color, bool)>, float, float) Layout'):s.index('    private static void DrawBox')]
stubs=(root/'Tests/Endgame/ReferenceTooltipStubs.cs').read_text().replace('internal static class UITip','internal static partial class UITip')
h=r'''
using System;using System.Text;using System.Collections.Generic;using System.IO;using UnityEngine;using FakeFantasyOnline.Protocol;using FakeFantasyOnline.Application;
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
static class BitmapFont {public static float Measure(string text,float s)=>(text??"").Length*6*s;}
static class GameSettings{public static bool ColorWeak;}
internal static partial class UITip {
__METHODS__
internal static List<(string,Color,bool)> Audit(string s)=>Layout(s,1).Item1;
}
class Test {
static void Check(bool b,string n){if(!b)throw new Exception(n);}
static bool Eq(Color a,Color b)=>Math.Abs(a.r-b.r)<.0001&&Math.Abs(a.g-b.g)<.0001&&Math.Abs(a.b-b.b)<.0001;
static bool Precious(int quality)=>__PRECIOUS__;
static void Main(string[] args){Check(!Precious(6)&&!Precious(2)&&!Precious(0)&&Precious(3)&&Precious(4)&&Precious(5),"two-affix color does not change drop confirmation rarity");ReferenceFirstWorldUiTip tip;ReferenceFirstWorldUiTip.TryCreate(new ReferenceResourceLoader(),out tip);
foreach(int q in new[]{1,2,3,4,5,6}){var title=UITip.Audit(TooltipContent.QualityName("套装属性",q));Check(Eq(title[0].Item2,UITip.QualityColor(q)),"heading follows supplied equipment quality");}Check(Eq(UITip.Audit("\u00030套装属性")[0].Item2,UITip.QualityColor(0)),"explicit white equipment heading is not fixed blue");
string[] names={"勇气之刃","胜利王座","夜色拥抱","哲理之石","信仰之翼"};
foreach(string name in names)foreach(int pieces in new[]{0,2,4,6,8}){
 string text="\u0007\u00036两条属性装备\n套装: "+name+" ("+pieces+"/8)\n\u0005  已装备部件\n\u0002  未装备部件";
 foreach(int gate in new[]{2,4,6,8})text+="\n"+(pieces>=gate?"\u0005":"\u0002")+"效果"+gate;
 var rows=UITip.Audit(text);var diag=tip.BuildLayout(text,1).Rows;Check(rows.Count==8&&rows.Count==diag.Count,"all effects retained");
 for(int i=0;i<rows.Count;i++)Check(rows[i].Item1==diag[i].Text&&Eq(rows[i].Item2,diag[i].Color),"native and diagnostic parity");
 Color blue=EquipmentTooltipColors.ActiveBlue;Check(Eq(blue,new Color32(105,223,223,255)),"reference-image cyan exact RGB");Check(Eq(UITip.QualityColor(6),blue),"two-affix label palette");Check(Eq(rows[0].Item2,blue)&&Eq(rows[2].Item2,blue),"two-affix text and active member blue");
 for(int j=0;j<4;j++){var row=rows[j+4];Check(!row.Item1.Contains("件"),"no piece label");Check(Eq(row.Item2,pieces>=(j+1)*2?blue:(Color)new Color32(128,128,128,255)),"each effect activation color");}
}
Check(UITip.QualityColor(2).r==82&&UITip.QualityColor(2).g==150&&UITip.QualityColor(2).b==252,"one-affix quality2 retains deep blue");Check(UITip.QualityColor(1).g==224&&UITip.QualityColor(3).b==255&&UITip.QualityColor(5).r==183,"other quality palette retained");
foreach(string path in args){string text="";if(path.EndsWith(".bin")){WorldInventoryState state;Check(PrivateServerInventoryCodec.TryDecode(File.ReadAllBytes(path),out state),"actual server inventory packet decode");text="";foreach(var item in state.Items){Check(item.Quality==2||item.Quality==6,"one/two affix wire quality");var itemText=TooltipContent.Fmt(TooltipContent.QualityName(item.DisplayName,item.Quality),item.Description);var rendered=UITip.Audit(itemText);Check(!rendered[0].Item1.Contains("\u0005")&&!rendered[0].Item1.Contains("\u0003"),"actual name has no leftover nested color prefix");Check(Eq(rendered[0].Item2,UITip.QualityColor(item.Quality)),"actual packet name uses quality palette");if(item.Quality==6){Check(Eq(rendered[0].Item2,EquipmentTooltipColors.ActiveBlue),"two-affix name actual blue");foreach(var line in rendered)if(line.Item1.Contains("力量")||line.Item1.Contains("体质"))Check(Eq(line.Item2,EquipmentTooltipColors.ActiveBlue),"two-affix actual affix blue");}text+=itemText+"\n";} }else text=File.ReadAllText(path);var rows=UITip.Audit(text);if(!path.Contains("/Fixtures/"))using(var w=new StreamWriter(path+".rows.tsv"))foreach(var row in rows)w.WriteLine((int)Math.Round(row.Item2.r*255)+","+(int)Math.Round(row.Item2.g*255)+","+(int)Math.Round(row.Item2.b*255)+"\t"+row.Item1);}
Console.WriteLine("PASS actual native and diagnostic tooltip layouts: five sets x 0/2/4/6/8 activation, all lines, no count labels, shared #69DFDF two-affix text/active effects, unchanged other quality colors");}
}
}
'''.replace('__METHODS__',methods).replace('__PRECIOUS__',(p/'InventoryDlg.BagDestinations.cs').read_text().split('bool preciousQuality = ')[1].split(';')[0].replace('item.quality','quality'))
managed=Path(os.environ['FFO_MANAGED_DIR']);
fw=Path(os.environ['FFO_FRAMEWORK_DIR']);mono=os.environ['FFO_MONO'];csc=os.environ['FFO_CSC']
with tempfile.TemporaryDirectory(prefix='equipment-blue-') as d:
 d=Path(d);(d/'Test.cs').write_text(h);(d/'Stubs.cs').write_text(stubs)
 subprocess.run([mono,csc,'-nologo','-noconfig','-nostdlib+']+['-r:'+str(fw/n) for n in ['mscorlib.dll','System.dll','System.Core.dll']]+['-r:'+str(managed/n) for n in ['FakeFantasyOnline.Protocol.dll','FakeFantasyOnline.Application.dll','FakeFantasyOnline.Domain.dll','FakeFantasyOnline.Foundation.dll']]+['-out:'+str(d/'Test.exe'),str(d/'Test.cs'),str(d/'Stubs.cs'),str(p/'EquipmentTooltipColors.cs'),str(p/'ReferenceFirstWorldUiTip.cs'),str(p/'TooltipContent.Fmt.cs'),str(p/'TooltipContent.QualityName.cs')],check=True)
 env=dict(os.environ);env['MONO_PATH']=str(managed)+os.pathsep+env.get('MONO_PATH','');subprocess.run([mono,str(d/'Test.exe'),str(root/'Tests/Endgame/Fixtures/two-affix-quality-inventory.bin')]+sys.argv[1:],check=True,env=env)
# These surfaces all call the shared native quality palette, not a copied blue value.
for f in ['InventoryDlg.OnGUI.cs','EquipDlg.OnGUI.cs','StoreDlg.Drawing.cs','ShopDlg.Rows.cs','GroundItems.Names.cs','AttrWashDlg.Window.cs','ChatUI.ShareRows.cs','StallDetailDlg.Window.cs','StallConfigDlg.Window.cs','ExEquipDlg.cs']:
 assert 'UITip.QualityColor(' in (p/f).read_text(),f
assert 'new Color32(82, 150, 252, 255)' in (p/'GroundItems.Icons.cs').read_text() # unrelated category palette
print('PASS inventory/equipment/store/shop/drop-name/wash/chat shared palette wiring; unrelated category colors untouched')
