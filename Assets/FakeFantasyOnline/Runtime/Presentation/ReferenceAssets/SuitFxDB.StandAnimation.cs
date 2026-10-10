using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class SuitFxDB
{
    private static readonly HashSet<int> _standAnimModels = new HashSet<int>
    {
        1271, 1273, 1280, 1281, 1282, 1283, 1284, 1285, 1300, 1302, 1351, 1352, 1362, 1363, 1364, 1368, 1369, 1370, 1371, 1372, 1376, 1377, 1378, 1381, 1388, 1389, 1390, 1392, 1393, 1394, 1395, 1396, 1397, 1399, 1400, 1401, 1402, 1403, 1404, 1405, 1406, 1407, 1422, 1423, 1424, 1425, 1700, 1702, 1704, 1718, 1719, 1720, 1721, 1722, 1723, 2298, 2299
    };
    private static readonly HashSet<int> _standAnimBackpackModels = new HashSet<int>
    {
        884, 885, 886, 942
    };

    public static bool IsStandAnimWeaponModel(int model) => model > 0 && _standAnimModels.Contains(model);
    public static bool IsStandAnimBackpackModel(int model) => model > 0 && _standAnimBackpackModels.Contains(model);
}

}
