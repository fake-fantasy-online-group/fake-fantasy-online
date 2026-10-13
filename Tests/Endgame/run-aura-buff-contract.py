#!/usr/bin/env python3
"""Execute the production buff tooltip method with Mono, without Unity/Player."""
from pathlib import Path
import os
import shutil
import subprocess
import tempfile

root = Path(__file__).resolve().parents[2]
source = (root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/ReferenceFirstWorldBuffHud.cs').read_text()
# Exact source slice, including the complete production method, not a test copy.
method = source.split('    public static string Tooltip(', 1)[1].split('    internal Texture2D LoadStatusIcon', 1)[0]
method = '    public static string Tooltip(' + method
actor_source = (root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/ActorEffects.Sync.cs').read_text()
asset_method = '    internal static string EquipmentAuraAsset(' + actor_source.split('    internal static string EquipmentAuraAsset(', 1)[1].split('    // 0x06000027', 1)[0]
mono = os.environ.get('FFO_MONO') or shutil.which('mono')
csc = os.environ.get('FFO_CSC') or shutil.which('csc')
if not mono or not csc:
    raise SystemExit('Set FFO_CSC and put Mono on PATH (cloud managed toolchain supported).')
framework = os.environ.get('FFO_FRAMEWORK_DIR')
refs = ['-noconfig']
if framework:
    refs += ['-nostdlib+'] + ['-r:' + str(Path(framework) / name) for name in ('mscorlib.dll', 'System.dll', 'System.Core.dll')]
harness = '''
using System;
using System.Globalization;
class AuraBuffContract {
%s
%s
    static void Main() {
        string aura = Tooltip("白虎卫身", "物理减伤 +12%%；来源：队友", -2, int.MaxValue);
        if (!aura.Contains("12%%") || !aura.Contains("来源：队友") || !aura.EndsWith("(光环生效中)") || aura.Contains("剩余") || aura.Contains("永久")) throw new Exception("source-bound aura duration/detail");
        if (Tooltip("原技能", "", -1, 0) != "原技能\\n(永久)") throw new Exception("legacy permanent skill changed");
        if (Tooltip("原技能", "原有效果", 90, 75) != "原技能\\n原有效果\\n剩余 1分15秒") throw new Exception("finite skill countdown changed");
        if (EquipmentAuraAsset("白虎守城") != "钢铁" || EquipmentAuraAsset("朱鸟衔烬") != "火之盾" || EquipmentAuraAsset("空瓢济厄") != "TFX074_40" || EquipmentAuraAsset("雨洗空符") != "TFX028_40" || EquipmentAuraAsset("玄武镇门") != "TFX028_40" || EquipmentAuraAsset("倒骑观火") != "火之盾" || EquipmentAuraAsset("纸驴驮霜") != "TFX028_40") throw new Exception("formation aura art aliases missing");
        if (EquipmentAuraAsset("原技能") != "原技能") throw new Exception("original skill asset changed");
        Console.WriteLine("PASS 5 production buff tooltip/art contracts (source-bound / permanent / finite / formation art / original art)");
    }
}
''' % (method, asset_method)
with tempfile.TemporaryDirectory(prefix='aura-buff-contract-') as folder:
    path = Path(folder)
    (path / 'Contract.cs').write_text(harness)
    compiler = [mono, csc] if csc.lower().endswith('.exe') else [csc]
    subprocess.run(compiler + refs + ['-nologo', '-target:exe', '-out:' + str(path / 'Contract.exe'), str(path / 'Contract.cs')], check=True)
    subprocess.run([mono, str(path / 'Contract.exe')], check=True)
# Existing permanent-column and actor membership paths already accept all negative
# sentinel values; require those integration gates to stay intact.
assert 'if (rows[index].remainSec < 0)' in source
assert 'if (row.remainSec >= 0)' in source
actor = (root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/PlayerController.BuffAura.cs').read_text()
assert 'if (buff.remainSec >= 0 &&' in actor
assert 'actorFx.SyncAuras(want)' in actor
print('PASS 4 source-bound permanent-column and actor-membership guards')
