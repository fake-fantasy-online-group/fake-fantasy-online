using System;
using System.Collections.Generic;
using System.IO;

#if UNITY_EDITOR
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Maps the repository package directory to the package lists recovered from ResLoader.Awake.
/// </summary>
internal static class ReferencePackageMountAdapter
{
    private static readonly string[] RequiredPackageNames =
    {
        "res_q21_000.pkg",
        "res_q21_001.pkg",
        "res_05diff.pkg",
        "res_ff21.pkg",
        "zpatch_petart.pkg",
        "zpatch_tower10.pkg"
    };

    public static void OpenIfComplete(
        string packageDirectory,
        out PkgArchive pkg,
        out PkgArchive pkg24,
        out PkgArchive pkgFf21)
    {
        pkg = null;
        pkg24 = null;
        pkgFf21 = null;

        bool any = false;
        for (int i = 0; i < RequiredPackageNames.Length; i++)
        {
            any |= File.Exists(Path.Combine(packageDirectory, RequiredPackageNames[i]));
        }

        if (!any)
        {
            return;
        }

        for (int i = 0; i < RequiredPackageNames.Length; i++)
        {
            string path = Path.Combine(packageDirectory, RequiredPackageNames[i]);
            if (!File.Exists(path))
            {
                throw new InvalidDataException(
                    "Reference package set is partial: " + RequiredPackageNames[i]);
            }
        }

        string q21000 = Path.Combine(packageDirectory, "res_q21_000.pkg");
        string q21001 = Path.Combine(packageDirectory, "res_q21_001.pkg");
        List<string> patches = new List<string>();
        string[] patchPatterns = { "hd_patch*.pkg", "zpatch*.pkg" };
        for (int index = 0; index < patchPatterns.Length; index++)
        {
            patches.AddRange(Directory.GetFiles(packageDirectory, patchPatterns[index]));
        }

        patches.Sort();
        patches.Reverse();

        pkg = new PkgArchive { Tag = "05" };
        List<string> primaryPackages = new List<string>(patches);
        primaryPackages.Add(Path.Combine(packageDirectory, "res_05diff.pkg"));
        primaryPackages.Add(q21000);
        primaryPackages.Add(q21001);
        pkg.MountPkgs(primaryPackages);

        pkg24 = new PkgArchive { Tag = "q21" };
        List<string> nativePackages = new List<string>(patches);
        nativePackages.Add(q21000);
        nativePackages.Add(q21001);
        pkg24.MountPkgs(nativePackages);

        pkgFf21 = new PkgArchive { Tag = "ff21" };
        List<string> ff21Packages = new List<string>(patches);
        ff21Packages.Add(Path.Combine(packageDirectory, "res_ff21.pkg"));
        pkgFf21.MountPkgs(ff21Packages);
    }
}

}
#endif
