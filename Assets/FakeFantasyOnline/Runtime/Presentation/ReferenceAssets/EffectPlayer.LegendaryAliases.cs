using System;
using System.Collections.Generic;
using System.Globalization;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class EffectPlayer
{
    private readonly struct LegendaryEffectAlias
    {
        public readonly string OriginalEffect;
        public readonly int SelectedMagicId;

        public LegendaryEffectAlias(string originalEffect, int selectedMagicId = 0)
        {
            OriginalEffect = originalEffect;
            SelectedMagicId = selectedMagicId;
        }
    }

    // Keep every historical server LP name compatible without shipping LP_*.eff files.
    // A zero selector reuses the complete original EFF; otherwise retain all original
    // layers of that GSO in memory. Match the case-insensitive package/overlay lookup.
    // These constants are not new artwork or EFF data.
    private static readonly Dictionary<string, LegendaryEffectAlias> LegendaryEffectAliases =
        new Dictionary<string, LegendaryEffectAlias>(StringComparer.OrdinalIgnoreCase)
        {
            { "LP_ZS_A02_01", new LegendaryEffectAlias("WF0021") },
            { "LP_ZS_A02_02", new LegendaryEffectAlias("TFX081_30", 1068) },
            { "LP_ZS_A04_01", new LegendaryEffectAlias("WF0021") },
            { "LP_ZS_A05_01", new LegendaryEffectAlias("TFX045_30", 1063) },
            { "LP_ZS_A06_01", new LegendaryEffectAlias("TFX081_30", 1068) },
            { "LP_ZS_A10_01", new LegendaryEffectAlias("TFX1214_70") },
            { "LP_ZS_A10_02", new LegendaryEffectAlias("TFX045_30", 1063) },
            { "LP_ZS_A12_01", new LegendaryEffectAlias("TFX1032_40", 322) },
            { "LP_ZS_A12_02", new LegendaryEffectAlias("111", 145) },
            { "LP_ZS_A13_01", new LegendaryEffectAlias("TFX1248_40", 1046) },
            { "LP_ZS_A14_01", new LegendaryEffectAlias("TFX078_30", 163) },
            { "LP_ZS_A14_02", new LegendaryEffectAlias("TFX005_30", 162) },
            { "LP_ZS_A15_01", new LegendaryEffectAlias("TFX098_30", 4126) },
            { "LP_ZS_A16_01", new LegendaryEffectAlias("WF0013") },
            { "LP_ZS_A16_02", new LegendaryEffectAlias("TFX081_30", 1068) },
            { "LP_ZS_A18_01", new LegendaryEffectAlias("WF0021") },
            { "LP_ZS_A22_01", new LegendaryEffectAlias("TFX1248_40", 1046) },
            { "LP_ZS_A23_01", new LegendaryEffectAlias("TFX081_30", 1068) },
            { "LP_ZS_A25_01", new LegendaryEffectAlias("TFX9745") },
            { "LP_ZS_A25_02", new LegendaryEffectAlias("TFX098_30", 4126) },
            { "LP_ZS_A27_01", new LegendaryEffectAlias("TFX081_30", 1068) },
            { "LP_ZS_A28_01", new LegendaryEffectAlias("TFX089_30", 4139) },
            { "LP_ZS_D01_01", new LegendaryEffectAlias("TFX9808", 7677) },
            { "LP_ZS_D02_01", new LegendaryEffectAlias("TFX9808", 7677) },
            { "LP_ZS_D09_01", new LegendaryEffectAlias("TFX9808", 7677) },
            { "LP_ZS_D10_01", new LegendaryEffectAlias("TFX9808", 7677) },
            { "LP_ZS_U04_01", new LegendaryEffectAlias("TFX1248_40", 330) },
            { "LP_ZS_U08_01", new LegendaryEffectAlias("TFX1248_40", 330) },
            { "LP_ZS_U10_01", new LegendaryEffectAlias("TFX1248_40", 330) },
            { "LP_JK_A02_01", new LegendaryEffectAlias("TFX060_20") },
            { "LP_JK_A04_01", new LegendaryEffectAlias("TFX909_40") },
            { "LP_JK_A06_01", new LegendaryEffectAlias("TFX1214_70") },
            { "LP_JK_A06_02", new LegendaryEffectAlias("TFX045_30", 1063) },
            { "LP_JK_A07_01", new LegendaryEffectAlias("TFX022_30", 2014) },
            { "LP_JK_A08_01", new LegendaryEffectAlias("TFX022_30", 2014) },
            { "LP_JK_A12_01", new LegendaryEffectAlias("TFX1214_70") },
            { "LP_JK_A12_02", new LegendaryEffectAlias("TFX045_30", 1063) },
            { "LP_JK_A13_01", new LegendaryEffectAlias("TFX1214_70") },
            { "LP_JK_A13_02", new LegendaryEffectAlias("TFX045_30", 1063) },
            { "LP_JK_A14_01", new LegendaryEffectAlias("TFX1234_30") },
            { "LP_JK_A14_02", new LegendaryEffectAlias("TFX6001", 4027) },
            { "LP_JK_A16_01", new LegendaryEffectAlias("TFX8160", 9238) },
            { "LP_JK_A16_02", new LegendaryEffectAlias("TFX061_40", 196) },
            { "LP_JK_A17_01", new LegendaryEffectAlias("TFX1214_70") },
            { "LP_JK_A17_02", new LegendaryEffectAlias("TFX045_30", 1063) },
            { "LP_JK_A19_01", new LegendaryEffectAlias("WF0041") },
            { "LP_JK_A20_01", new LegendaryEffectAlias("WF0041") },
            { "LP_JK_A21_01", new LegendaryEffectAlias("TFX089_30", 4139) },
            { "LP_JK_A22_01", new LegendaryEffectAlias("TFX7005_30", 314) },
            { "LP_JK_A22_02", new LegendaryEffectAlias("TFX072_40", 4108) },
            { "LP_JK_A23_01", new LegendaryEffectAlias("TFX095_30") },
            { "LP_JK_A25_01", new LegendaryEffectAlias("WF0041") },
            { "LP_JK_A27_01", new LegendaryEffectAlias("药师呤唱(咒语)", 4047) },
            { "LP_JK_A28_01", new LegendaryEffectAlias("WF0041") },
            { "LP_JK_D01_01", new LegendaryEffectAlias("TFX9808", 7677) },
            { "LP_JK_D07_01", new LegendaryEffectAlias("TFX9808", 7677) },
            { "LP_JK_D08_01", new LegendaryEffectAlias("TFX9808", 7677) },
            { "LP_JK_D10_01", new LegendaryEffectAlias("TFX9808", 7677) },
            { "LP_JK_U02_01", new LegendaryEffectAlias("TFX1249_40", 4079) },
            { "LP_JK_U10_01", new LegendaryEffectAlias("TFX1214_70") },
            { "LP_SS_A02_01", new LegendaryEffectAlias("TFX036_40", 289) },
            { "LP_SS_A04_01", new LegendaryEffectAlias("111", 145) },
            { "LP_SS_A06_01", new LegendaryEffectAlias("TFX1251_40", 406) },
            { "LP_SS_A07_01", new LegendaryEffectAlias("TFX036_40", 289) },
            { "LP_SS_A10_01", new LegendaryEffectAlias("TFX108_30", 452) },
            { "LP_SS_A11_01", new LegendaryEffectAlias("TFX116_40", 433) },
            { "LP_SS_A12_01", new LegendaryEffectAlias("TFX117_30") },
            { "LP_SS_A13_01", new LegendaryEffectAlias("TFX067_40", 338) },
            { "LP_SS_A14_01", new LegendaryEffectAlias("TFX109_40", 462) },
            { "LP_SS_A16_01", new LegendaryEffectAlias("TFX1250_50") },
            { "LP_SS_A17_01", new LegendaryEffectAlias("TFX1020_30", 290) },
            { "LP_SS_A19_01", new LegendaryEffectAlias("111", 145) },
            { "LP_SS_A20_01", new LegendaryEffectAlias("TFX039_40", 335) },
            { "LP_SS_A21_01", new LegendaryEffectAlias("TFX039_40", 335) },
            { "LP_SS_A21_02", new LegendaryEffectAlias("TFX8020") },
            { "LP_SS_A22_01", new LegendaryEffectAlias("mbr318", 352) },
            { "LP_SS_A22_02", new LegendaryEffectAlias("TFX113_30", 210) },
            { "LP_SS_A24_01", new LegendaryEffectAlias("TFX116_40", 433) },
            { "LP_SS_A27_01", new LegendaryEffectAlias("111", 145) },
            { "LP_SS_A28_01", new LegendaryEffectAlias("TFX1238_50") },
            { "LP_SS_A30_01", new LegendaryEffectAlias("TFX097_30", 172) },
            { "LP_SS_D02_01", new LegendaryEffectAlias("TFX116_40", 433) },
            { "LP_SS_D04_01", new LegendaryEffectAlias("CP0002", 4092) },
            { "LP_SS_D07_01", new LegendaryEffectAlias("CP0002", 321) },
            { "LP_SS_D08_01", new LegendaryEffectAlias("CP0002", 321) },
            { "LP_SS_D09_01", new LegendaryEffectAlias("TFX1020_30", 290) },
            { "LP_SS_U09_01", new LegendaryEffectAlias("TFX101_40", 408) },
            { "LP_YS_A01_01", new LegendaryEffectAlias("TFX058_30", 225) },
            { "LP_YS_A02_01", new LegendaryEffectAlias("TFX1043(分离)") },
            { "LP_YS_A05_01", new LegendaryEffectAlias("TFX316_40", 201) },
            { "LP_YS_A06_01", new LegendaryEffectAlias("TFX010_30", 1067) },
            { "LP_YS_A07_01", new LegendaryEffectAlias("TFX8123", 7833) },
            { "LP_YS_A09_01", new LegendaryEffectAlias("TFX1238_50") },
            { "LP_YS_A11_01", new LegendaryEffectAlias("TFX1214_70") },
            { "LP_YS_A11_02", new LegendaryEffectAlias("TFX045_30", 1063) },
            { "LP_YS_A12_01", new LegendaryEffectAlias("TFX7004_30", 295) },
            { "LP_YS_A15_01", new LegendaryEffectAlias("TFX7004_30", 295) },
            { "LP_YS_A16_01", new LegendaryEffectAlias("TFX1248_40", 1046) },
            { "LP_YS_A19_01", new LegendaryEffectAlias("TFX1214_70") },
            { "LP_YS_A19_02", new LegendaryEffectAlias("TFX045_30", 1063) },
            { "LP_YS_A20_01", new LegendaryEffectAlias("TFX911_40", 5082) },
            { "LP_YS_A21_01", new LegendaryEffectAlias("TFX064_40", 1069) },
            { "LP_YS_A22_01", new LegendaryEffectAlias("TFX1238_50") },
            { "LP_YS_A24_01", new LegendaryEffectAlias("TFX058_30", 225) },
            { "LP_YS_D01_01", new LegendaryEffectAlias("TFX8032") },
            { "LP_YS_D03_01", new LegendaryEffectAlias("TFX8032") },
            { "LP_YS_D08_01", new LegendaryEffectAlias("tfx8129", 7855) },
            { "LP_YS_U02_01", new LegendaryEffectAlias("TFX7012_40", 4013) },
            { "LP_YS_U03_01", new LegendaryEffectAlias("TFX1220_30") },
            { "LP_YS_U05_01", new LegendaryEffectAlias("TFX1246_40") },
            { "LP_CK_A02_01", new LegendaryEffectAlias("心眼") },
            { "LP_CK_A03_01", new LegendaryEffectAlias("test", 5) },
            { "LP_CK_A05_01", new LegendaryEffectAlias("TFX1214_70") },
            { "LP_CK_A05_02", new LegendaryEffectAlias("TFX045_30", 1063) },
            { "LP_CK_A06_01", new LegendaryEffectAlias("TFX095_30") },
            { "LP_CK_A07_01", new LegendaryEffectAlias("TFX097_30", 172) },
            { "LP_CK_A08_01", new LegendaryEffectAlias("TFX048_30", 4084) },
            { "LP_CK_A09_01", new LegendaryEffectAlias("TFX021_50", 1032) },
            { "LP_CK_A11_01", new LegendaryEffectAlias("111", 145) },
            { "LP_CK_A12_01", new LegendaryEffectAlias("mbr273", 4058) },
            { "LP_CK_A13_01", new LegendaryEffectAlias("TFX348_40") },
            { "LP_CK_A14_01", new LegendaryEffectAlias("TFX065_40", 4147) },
            { "LP_CK_A15_01", new LegendaryEffectAlias("TFX048_30", 4084) },
            { "LP_CK_A15_02", new LegendaryEffectAlias("TFX9051") },
            { "LP_CK_A17_01", new LegendaryEffectAlias("TFX023_40", 1034) },
            { "LP_CK_A17_02", new LegendaryEffectAlias("TFX108_30", 452) },
            { "LP_CK_A18_01", new LegendaryEffectAlias("mbr318", 352) },
            { "LP_CK_A18_02", new LegendaryEffectAlias("TFX108_30", 452) },
            { "LP_CK_A23_01", new LegendaryEffectAlias("TFX081_30", 1068) },
            { "LP_CK_A25_01", new LegendaryEffectAlias("WF0011") },
            { "LP_CK_A26_01", new LegendaryEffectAlias("TFX1248_40", 1046) },
            { "LP_CK_A27_01", new LegendaryEffectAlias("TFX7012_40", 2017) },
            { "LP_CK_A28_01", new LegendaryEffectAlias("mbr211", 349) },
            { "LP_CK_A29_01", new LegendaryEffectAlias("TFX045_30", 1063) },
            { "LP_CK_A30_01", new LegendaryEffectAlias("TFX1083_70", 455) },
            { "LP_CK_A30_02", new LegendaryEffectAlias("TFX095_30") },
            { "LP_CK_D02_01", new LegendaryEffectAlias("TFX9558", 8076) },
            { "LP_CK_D03_01", new LegendaryEffectAlias("CP0002", 321) },
            { "LP_CK_D06_01", new LegendaryEffectAlias("TFX8292", 8354) },
            { "LP_CK_D08_01", new LegendaryEffectAlias("TFX1220_30") },
            { "LP_CK_D09_01", new LegendaryEffectAlias("CP0002", 4092) },
            { "LP_TY_A01_01", new LegendaryEffectAlias("TFX1234_30") },
            { "LP_TY_A02_01", new LegendaryEffectAlias("TFX1214_70") },
            { "LP_TY_A02_02", new LegendaryEffectAlias("TFX045_30", 1063) },
            { "LP_TY_A06_01", new LegendaryEffectAlias("TFX081_30", 1068) },
            { "LP_TY_A07_01", new LegendaryEffectAlias("真名", 4109) },
            { "LP_TY_A07_02", new LegendaryEffectAlias("TFX1235_30") },
            { "LP_TY_A08_01", new LegendaryEffectAlias("TFX9853", 7648) },
            { "LP_TY_A10_01", new LegendaryEffectAlias("TFX057_40", 347) },
            { "LP_TY_A12_01", new LegendaryEffectAlias("TFX098_30", 4126) },
            { "LP_TY_A12_02", new LegendaryEffectAlias("TFX046_30", 4025) },
            { "LP_TY_A13_01", new LegendaryEffectAlias("WF0041") },
            { "LP_TY_A14_01", new LegendaryEffectAlias("TFX122_30", 430) },
            { "LP_TY_A14_02", new LegendaryEffectAlias("TFX325") },
            { "LP_TY_A15_01", new LegendaryEffectAlias("集中") },
            { "LP_TY_A16_01", new LegendaryEffectAlias("TFX095_30") },
            { "LP_TY_A19_01", new LegendaryEffectAlias("獒击", 9017) },
            { "LP_TY_A19_02", new LegendaryEffectAlias("TFX007_40", 2907) },
            { "LP_TY_A20_01", new LegendaryEffectAlias("獒击", 9017) },
            { "LP_TY_A20_02", new LegendaryEffectAlias("TFX9959", 7747) },
            { "LP_TY_A21_01", new LegendaryEffectAlias("mbr273", 4058) },
            { "LP_TY_A26_01", new LegendaryEffectAlias("WF0041") },
            { "LP_TY_A28_01", new LegendaryEffectAlias("集中") },
            { "LP_TY_A29_01", new LegendaryEffectAlias("TFX9853", 7648) },
            { "LP_TY_A29_02", new LegendaryEffectAlias("TFX8154", 9233) },
            { "LP_TY_A31_01", new LegendaryEffectAlias("TFX1214_70") },
            { "LP_TY_A31_02", new LegendaryEffectAlias("TFX045_30", 1063) },
            { "LP_TY_A32_01", new LegendaryEffectAlias("TFX1214_70") },
            { "LP_TY_A32_02", new LegendaryEffectAlias("TFX045_30", 1063) },
            { "LP_TY_A38_01", new LegendaryEffectAlias("TFX1214_70") },
            { "LP_TY_A38_02", new LegendaryEffectAlias("TFX045_30", 1063) },
            { "LP_TY_A39_01", new LegendaryEffectAlias("TFX009_30", 1064) },
            { "LP_TY_D02_01", new LegendaryEffectAlias("TFX8292", 8354) },
            { "LP_TY_D03_01", new LegendaryEffectAlias("TFX905_30", 5088) },
            { "LP_TY_D04_01", new LegendaryEffectAlias("TFX9808", 7677) },
            { "LP_TY_D08_01", new LegendaryEffectAlias("CP0002", 321) },
            { "LP_TY_D10_01", new LegendaryEffectAlias("TFX1081_70", 424) },
            { "LP_TY_D11_01", new LegendaryEffectAlias("TFX9808", 7677) },
            { "LP_TY_D12_01", new LegendaryEffectAlias("TFX1081_70", 424) },
            { "LP_TY_D14_01", new LegendaryEffectAlias("TFX1081_70", 424) },
            { "LP_TY_D15_01", new LegendaryEffectAlias("TFX905_30", 5088) },
            { "LP_TY_D16_01", new LegendaryEffectAlias("TFX8292", 8354) },
            { "LP_TY_D18_01", new LegendaryEffectAlias("CP0002", 4092) },
            { "LP_TY_D20_01", new LegendaryEffectAlias("CP0002", 321) },
            { "LP_TY_U10_01", new LegendaryEffectAlias("TFX8154", 9233) },
            { "LP_TY_U17_01", new LegendaryEffectAlias("集中") },
        };

    private static Template SelectLegendaryLayers(Template original, int magicId)
    {
        if (original == null || magicId == 0)
        {
            return original;
        }

        // Parsed layers/keyframes/splines are read-only during playback. Share them,
        // but never remove layers from, or rewrite timing on, the cached original.
        Template selected = new Template { loopFromTail = original.loopFromTail };
        foreach (Layer layer in original.layers)
        {
            int layerMagicId;
            if (layer.sprite == null ||
                !layer.sprite.StartsWith("magic", StringComparison.Ordinal) ||
                !int.TryParse(layer.sprite.Substring(5), NumberStyles.None,
                    CultureInfo.InvariantCulture, out layerMagicId) ||
                layerMagicId != magicId)
            {
                continue;
            }

            selected.layers.Add(layer);
            foreach (KF keyframe in layer.kfs)
            {
                if (selected.lifeMs < keyframe.t)
                {
                    selected.lifeMs = keyframe.t;
                }
            }
        }

        // Match Parse on the former cropped EFF, including its 100 ms tail.
        // The full composite may have a longer duration than the selected layers.
        selected.lifeMs += 100f;
        return selected.layers.Count == 0 ? null : selected;
    }
}

}
