#!/usr/bin/env python3
"""Exercise production mobile layout and original desktop Box method extracted unchanged."""
from pathlib import Path
import os,subprocess,tempfile
root=Path(__file__).resolve().parents[2];p=root/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
s=(p/'PlayerActionMenu.cs').read_text();a=s.index('    private Rect Box()');b=s.index('{',a)+1;depth=1
while depth:depth+=(s[b]=='{')-(s[b]=='}');b+=1
constants='\n'.join(line for line in s.splitlines() if 'public const float ' in line)
with tempfile.TemporaryDirectory() as folder:
 folder=Path(folder);source=folder/'DesktopBox.cs';exe=folder/'Contract.exe'
 source.write_text('using System.Collections.Generic;using UnityEngine;using FakeFantasyOnline.Presentation.ReferenceAssets;\nclass DesktopMenu {\n'+constants+'\n bool mobileEntry;List<int> items=new List<int>();Vector2 anchor;public DesktopMenu(int count,Vector2 p){anchor=p;for(int i=0;i<count;i++)items.Add(i);}\n'+s[a:b].replace('private Rect Box()','public Rect Box()')+'\n}')
 refs=['-r:'+str(x) for x in sorted(Path(os.environ['FFO_FRAMEWORK_DIR']).glob('*.dll'))]
 subprocess.run([os.environ['FFO_MONO'],os.environ['FFO_CSC'],'-noconfig','-nostdlib+','-nologo','-langversion:9.0','-out:'+str(exe)]+refs+[str(source),str(p/'MobilePlayerMenuLayout.cs'),str(root/'Tests/Mobile/PlayerMenuLayoutContract.cs')],check=True)
 subprocess.run([os.environ['FFO_MONO'],str(exe)],check=True)
