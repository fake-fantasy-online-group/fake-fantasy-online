#!/usr/bin/env python3
"""Compile the production rack icon resolver and exact catalog buffer with API stubs.
Does not launch Unity, load game assets, or claim end-to-end incident reproduction.
"""
from pathlib import Path
import argparse, os, shutil, subprocess, tempfile
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--mono', default=shutil.which('mono'))
p.add_argument('--csc', default=shutil.which('csc') or shutil.which('mcs'))
a=p.parse_args()
repo=Path(__file__).resolve().parents[2]
src=repo/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
receipt=(src/'ReferenceWorldReceiveOwner.RackShop.cs').read_text()
start=receipt.index('    private sealed class GoodsBuffer')
brace=receipt.index('{',start); depth=1; end=brace+1
while depth:
    depth += (receipt[end]=='{') - (receipt[end]=='}'); end+=1
buffer=receipt[start:end]
window=(src/'RackShopDlg.cs').read_text()
start=window.index('        public static void SetGoods(')
brace=window.index('{',start); depth=1; end=brace+1
while depth:
    depth += (window[end]=='{') - (window[end]=='}'); end+=1
set_goods=window[start:end]
assert 'LoadItemIcon' not in buffer, 'catalog receipt must not decode any icons'
for name,count in [('RackShopDlg.Shop.cs',2),('RackShopDlg.FlagGrid.cs',2),('RackShopDlg.CartDrawing.cs',1)]:
    text=(src/name).read_text()
    assert text.count('DrawGoodIcon(')==count, name+' must lazily resolve each visible renderer'
    assert 'good.icon' not in text, name+' still bypasses lazy renderer'
assert 'private const int PerPage = 12;' in (src/'RackShopDlg.Filtering.cs').read_text()
assert 'for (int i = 0; i < PerPage; i++)' in (src/'RackShopDlg.Shop.cs').read_text()
print('PASS: actual receipt and all five visible render paths are wired lazily',flush=True)
if not a.mono or not a.csc: p.error('Pass --mono and --csc to run the compiled behavioral contract')
with tempfile.TemporaryDirectory(prefix='rack-icons-') as td:
    td=Path(td); generated=td/'Receipt.cs'
    generated.write_text('using System.Collections.Generic;\nnamespace FakeFantasyOnline.Presentation.ReferenceAssets { internal static class ReferenceRackShopReceipt { internal static IRackShopGoodsReceiveBuffer CreateGoodsBuffer(int capacity) => new GoodsBuffer(capacity);\n'+buffer+'\n} public sealed partial class RackShopDlg {\n'+set_goods+'\n}}\n')
    exe=td/'VisibleIconsContract.exe'
    compiler=[a.mono,a.csc] if a.csc.endswith('.exe') else [a.csc]
    subprocess.run(compiler+['-nologo','-target:exe','-out:'+str(exe),str(src/'RackShopDlg.Icons.cs'),str(generated),str(Path(__file__).with_name('VisibleIconsContract.cs'))],check=True)
    subprocess.run([a.mono,str(exe)],check=True)
