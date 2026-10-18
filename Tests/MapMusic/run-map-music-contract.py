#!/usr/bin/env python3
"""Execute production MusicManager with deterministic Unity audio/network boundaries.
Does not decode OGG or claim Unity runtime/audio-device verification.
"""
from pathlib import Path
import argparse, os, subprocess, tempfile
root=Path(__file__).resolve().parents[2]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--music-source',type=Path,help='Alternate MusicManager source for regression proof')
args=parser.parse_args()
p=root/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
s=(p/'GameBootstrap.cs').read_text()
a=s.index('    internal static string MapSourceName(');b=s.index('    private void OnDestroy()',a)
assert 'if (MusicManager.I != null) MusicManager.I.PlayMap(mapName);' in s
with tempfile.TemporaryDirectory(prefix='map-music-') as temp:
 d=Path(temp);bootstrap=d/'MapSource.cs';exe=d/'MapMusic.exe'
 bootstrap.write_text('namespace FakeFantasyOnline.Presentation.ReferenceAssets { public class GameBootstrap {\n'+s[a:b]+'}}')
 command=[os.environ['FFO_MONO'],os.environ['FFO_CSC'],'-noconfig','-nostdlib+','@'+os.environ['FFO_COMPILE_DIR']+'/framework.rsp','-nologo','-langversion:9.0','-out:'+str(exe),str(Path(__file__).with_name('MapMusicContract.cs')),str(args.music_source or p/'ReferenceWorldMapMusic.cs'),str(p/'CustomMaps.cs'),str(bootstrap)]
 subprocess.run(command,check=True);subprocess.run([os.environ['FFO_MONO'],str(exe)],check=True)
