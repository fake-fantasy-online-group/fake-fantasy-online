using System;
using System.Security.Cryptography;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace FakeFantasyOnline.Presentation
{

internal static class ReferenceContentAuthorityCheck
{
    public const string Argument = "--ffo-content-authority-check";
    public const string Marker = "FFO_CONTENT_AUTHORITY_CHECK_V1";

    private const string MapDefPath = "map/map.def";
    private const string MapDefHash =
        "26511ccf4728acb89fedebcc6dfbdadb8841de5dd216b90a3423dc7cdbb01d3d";
    private const string TowerPath = "map/pw30210.gsm";
    private const string TowerHash =
        "83e64857b1c2ffaec2064c62677d9e96410878023fdc2de6e4deb1b370c9a164";
    private const string PetPath = "res/pet/M5338.gsa";
    private const string PetHash =
        "3728c5ac4fa5547bd819a8c0ebcb96f66fdb9816acf67fd72cfa818a0728b53c";

    public static bool IsRequested(string[] arguments)
    {
        return arguments != null && Array.IndexOf(arguments, Argument) >= 0;
    }

    public static bool TryVerify(
        ReferenceResourceLoader resources,
        out string resultLine)
    {
        if (resources == null || !resources.UsesCompletePackageSet ||
            resources.Core.Pkg.PkgCount != 5 ||
            resources.Core.Pkg24.PkgCount != 4 ||
            resources.Core.PkgFf21.PkgCount != 3)
        {
            resultLine = Marker + " FAIL code=mount_counts";
            return false;
        }

        if (!Matches(resources.ReadRes(MapDefPath), 99166, MapDefHash))
        {
            resultLine = Marker + " FAIL code=map_def_winner";
            return false;
        }

        if (!Matches(resources.ReadRes(TowerPath), 32604, TowerHash))
        {
            resultLine = Marker + " FAIL code=tower_patch";
            return false;
        }

        if (!Matches(resources.ReadRes(PetPath), 24660, PetHash))
        {
            resultLine = Marker + " FAIL code=pet_patch";
            return false;
        }

        resultLine = Marker +
            " PASS packages=6 primary=5 native=4 ff21=3" +
            " mapDef=zpatch_tower10.pkg tower=present pet=present";
        return true;
    }

    private static bool Matches(byte[] bytes, int size, string expectedHash)
    {
        if (bytes == null || bytes.Length != size)
        {
            return false;
        }

        using (SHA256 sha = SHA256.Create())
        {
            string actual = BitConverter.ToString(sha.ComputeHash(bytes))
                .Replace("-", string.Empty)
                .ToLowerInvariant();
            return string.Equals(actual, expectedHash, StringComparison.Ordinal);
        }
    }
}

}
