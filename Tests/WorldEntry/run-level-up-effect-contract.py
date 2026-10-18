#!/usr/bin/env python3
"""Compile the production attribute receipt methods with a recording effect port."""
from pathlib import Path
import argparse, subprocess, tempfile
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--mono', default='mono')
parser.add_argument('--csc', default='mcs')
parser.add_argument('--source', type=Path)
args = parser.parse_args()
root = Path(__file__).resolve().parents[2]
source = (args.source or root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/ReferenceWorldReceiveOwner.cs').read_text()
def member(signature):
    start = source.index(signature); opening = source.index('{', start); end = opening + 1; depth = 1
    while depth:
        if source[end] == '{': depth += 1
        if source[end] == '}': depth -= 1
        end += 1
    return source[start:end]
methods = '\n'.join(member(s) for s in ['    private static void StoreSevenAttributeFields(', '    public void ReceivePlayerAttributeFields(', '    public void ReceiveEnterAttributeFields('])
program = '''using System; using System.Collections.Generic;
class CharData { public static int level, exp, expToNext, hp, maxHp, mp, maxMp; }
class CombatNumber { public static object PlayerTransform; }
class EffectPlayer { public static int Calls; public static void SpawnOn(int target, string name) {
 if(target != 0 || name != "升级特效") throw new Exception("wrong original template/target"); Calls++; } }
class Owner { %s }
class Test {
 static void Check(bool value, string label) { if(!value) throw new Exception(label); }
 static int[] Attr(int level) { return new[]{level,10,100,20,30,40,50}; }
 static void Main() {
 var owner = new Owner(); CharData.level = 1;
 owner.ReceivePlayerAttributeFields(Attr(10)); Check(EffectPlayer.Calls==0,"startup without player");
 CombatNumber.PlayerTransform=new object(); owner.ReceiveEnterAttributeFields(Attr(20));
 Check(EffectPlayer.Calls==0,"entry must not celebrate");
 owner.ReceivePlayerAttributeFields(Attr(21)); Check(EffectPlayer.Calls==1,"live level increase");
 Check(CharData.level==21 && CharData.exp==10 && CharData.maxMp==50,"all authoritative fields preserved");
 owner.ReceivePlayerAttributeFields(Attr(21)); Check(EffectPlayer.Calls==1,"duplicate snapshot");
 owner.ReceivePlayerAttributeFields(Attr(20)); Check(EffectPlayer.Calls==1,"level reduction");
 owner.ReceivePlayerAttributeFields(null); owner.ReceivePlayerAttributeFields(new[]{99});
 Check(CharData.level==20 && EffectPlayer.Calls==1,"malformed snapshot");
 owner.ReceivePlayerAttributeFields(Attr(25)); Check(EffectPlayer.Calls==2,"multi-level gain is one pulse");
 CombatNumber.PlayerTransform=null; owner.ReceivePlayerAttributeFields(Attr(30));
 Check(EffectPlayer.Calls==2,"detached world");
 CombatNumber.PlayerTransform=new object(); owner.ReceivePlayerAttributeFields(Attr(30));
 Check(EffectPlayer.Calls==2,"no late replay after attachment");
 CharData.level=0; owner.ReceivePlayerAttributeFields(Attr(40));
 Check(EffectPlayer.Calls==2,"uninitialized baseline");
 Console.WriteLine("PASS 11 level-up receipt assertions (production methods, recording effect port)");
 }
}''' % methods
with tempfile.TemporaryDirectory(prefix='level-up-effect-') as tmp:
    code = Path(tmp)/'Contract.cs'; code.write_text(program); exe = Path(tmp)/'Contract.exe'
    compiler = [args.mono,args.csc] if args.csc.endswith('.exe') else [args.csc]
    subprocess.run(compiler+['-out:'+str(exe),str(code)],check=True)
    subprocess.run([args.mono,str(exe)],check=True)
