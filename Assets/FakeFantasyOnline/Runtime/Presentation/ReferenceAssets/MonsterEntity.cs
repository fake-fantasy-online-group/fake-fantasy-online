using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official MonsterEntity owner. Members are added only as their complete evidence cone
/// is locked; independent-runtime adapters are explicitly separated from the recovered methods.
/// </summary>
public partial class MonsterEntity : MonoBehaviour
{
    private static byte[] ReadNpcGsn(int actId, string suffix, bool preferNative)
    {
        string path = string.Format(
            "avatar/npc/npc{0}{1}.gsn",
            actId,
            suffix ?? string.Empty);
        if (preferNative)
        {
            byte[] native = ResLoader.Read24Res(path);
            if (native != null)
            {
                return native;
            }
        }

        ResLoader loader = ResLoader.I;
        byte[] packageBytes = loader == null || loader.Pkg == null
            ? null
            : loader.Pkg.Read(path);
        if (packageBytes != null)
        {
            return packageBytes;
        }

        return ResLoader.Read24Res(path);
    }

    internal static byte[] ReadNpcGsnForIndependentRuntime(
        int actId,
        string suffix,
        bool preferNative)
    {
        return ReadNpcGsn(actId, suffix, preferNative);
    }
}

}
