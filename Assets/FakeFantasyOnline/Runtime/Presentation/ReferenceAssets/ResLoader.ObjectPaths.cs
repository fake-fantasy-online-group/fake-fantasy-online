using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Official 1.3.4 ResLoader object-path owner recovered from tokens 06000AAB, 06000AAC and
/// 06000AAE. The package index remains the authority; map-specific path lists are forbidden.
/// </summary>
public partial class ResLoader
{
    private static readonly Dictionary<int, string> MapObjNames =
        new Dictionary<int, string>
        {
            { 1, "CC" }, { 2, "CCP" }, { 3, "GM" }, { 4, "GMP" },
            { 5, "HP" }, { 6, "HPP" }, { 7, "HT" }, { 8, "HTP" },
            { 9, "SM" }, { 10, "SMP" }, { 11, "LD" }, { 12, "LDP" },
            { 13, "ZZ" }, { 14, "ZZP" }, { 15, "M" }, { 16, "MP" },
            { 17, "DT" }, { 18, "DTP" }, { 19, "DX" }, { 20, "DXP" },
            { 21, "TT" }, { 22, "TTP" }, { 23, "SK" }, { 24, "SKP" },
            { 25, "ZW" }, { 26, "ZWP" }, { 27, "DD" }, { 28, "DDP" },
            { 29, "HH" }, { 30, "HHP" }, { 31, "GT" }, { 32, "GTP" },
            { 33, "MK" }, { 34, "MKP" }, { 35, "ZY" }, { 36, "ZYP" },
            { 37, "SC" }, { 38, "SCP" }, { 39, "SW" }, { 40, "SWP" },
            { 41, "LG" }, { 42, "LGP" }, { 43, "XJZ" }, { 44, "XQQ" },
            { 45, "JZ" }, { 46, "YB" }, { 47, "YD" }, { 48, "DH" },
            { 49, "YBCC" }, { 50, "NT" }, { 51, "QQ" }, { 52, "JZP" },
            { 53, "QQP" }, { 54, "XQP" }, { 55, "NTP" }, { 56, "DLP" },
            { 57, "CB" }, { 58, "CBP" }, { 59, "QM" }, { 60, "BZ" },
            { 61, "BJWJ" }, { 62, "BJDH" }
        };

    private static readonly Dictionary<int, string> CityNames =
        new Dictionary<int, string>
        {
            { 1, "FY" }, { 2, "XL" }, { 3, "TD" }, { 4, "TY" },
            { 5, "CL" }, { 6, "DFYW" }, { 7, "SW" }, { 8, "YL" },
            { 9, "PL" }, { 10, "TT" }, { 11, "SJ" }, { 12, "HJ" },
            { 13, "SMYW" }, { 14, "HS" }, { 15, "YM" }, { 16, "GC" },
            { 17, "MY" }, { 18, "TH" }, { 19, "XFYW" }, { 20, "TTD" },
            { 21, "YLD" }, { 22, "PLSJ" }, { 23, "SWS" }, { 24, "DXWG" },
            { 25, "CLMG" }, { 26, "TYMG" }, { 27, "TJJ" }, { 28, "JXMG" },
            { 29, "TJFB" }, { 30, "JZSN" }, { 31, "HYYSC" }, { 32, "HYYHS" },
            { 33, "CLY" }, { 34, "HDMZS" }, { 35, "JLG" }, { 36, "HSTX" },
            { 37, "SHLG" }, { 38, "MC" }, { 39, "ZHG" }, { 40, "LHZC" }
        };

    public void BuildCityTable(IEnumerable<int> imageIds)
    {
        if (Pkg == null)
        {
            return;
        }

        Dictionary<long, List<string>> candidatesByObject =
            new Dictionary<long, List<string>>();
        Dictionary<int, Dictionary<string, int>> cityCandidateCounts =
            new Dictionary<int, Dictionary<string, int>>();

        foreach (int imageId in imageIds)
        {
            int objectIndex = (imageId >> 16) & 0xff;
            string objectName;
            if (!MapObjNames.TryGetValue(objectIndex, out objectName))
            {
                continue;
            }

            long objectKey = ((long)objectIndex << 32) | (uint)(imageId & 0xffff);
            List<string> candidates;
            if (!candidatesByObject.TryGetValue(objectKey, out candidates))
            {
                IReadOnlyList<string> paths = Pkg.FindObjectCandidates(
                    objectName, imageId & 0xffff);
                List<string> cityCodes = new List<string>();
                foreach (string path in paths)
                {
                    int slash = path.LastIndexOf('/');
                    string fileName = slash >= 0 ? path.Substring(slash + 1) : path;
                    string[] parts = fileName.Split('_');
                    if (parts.Length > 2)
                    {
                        cityCodes.Add(parts[1]);
                    }
                }

                candidates = cityCodes;
                candidatesByObject[objectKey] = candidates;
            }

            int cityIndex = (int)((uint)imageId >> 24);
            Dictionary<string, int> counts;
            if (!cityCandidateCounts.TryGetValue(cityIndex, out counts))
            {
                counts = new Dictionary<string, int>();
                cityCandidateCounts[cityIndex] = counts;
            }

            foreach (string candidate in candidates)
            {
                int count;
                counts.TryGetValue(candidate, out count);
                counts[candidate] = count + 1;
            }
        }

        set2city.Clear();
        foreach (KeyValuePair<int, Dictionary<string, int>> city in cityCandidateCounts)
        {
            string best = null;
            int bestCount = 0;
            foreach (KeyValuePair<string, int> candidate in city.Value)
            {
                if (candidate.Value > bestCount)
                {
                    best = candidate.Key;
                    bestCount = candidate.Value;
                }
            }

            if (best != null)
            {
                set2city[city.Key] = best;
            }
        }
    }

    internal string ResolveObjectPath(int imageId)
    {
        return ObjPath(imageId);
    }

    private string ObjPath(int imageId)
    {
        string cached;
        if (objPathCache.TryGetValue(imageId, out cached))
        {
            return cached;
        }

        int objectIndex = (imageId >> 16) & 0xff;
        string objectName;
        if (!MapObjNames.TryGetValue(objectIndex, out objectName))
        {
            objPathCache[imageId] = null;
            return null;
        }

        int cityIndex = (int)((uint)imageId >> 24);
        string cityName;
        if (!CityNames.TryGetValue(cityIndex, out cityName) &&
            !set2city.TryGetValue(cityIndex, out cityName))
        {
            cityName = "____";
        }

        string path = string.Format(
            "object/{0}/{1}_{2}_{3:D4}.gso",
            objectName,
            objectName,
            cityName,
            imageId & 0xffff).Replace('\\', '/');
        if (Pkg == null)
        {
            path = null;
        }
        else if (!Pkg.Has(path))
        {
            path = Pkg.FindUnique(
                "object/" + objectName + "/",
                string.Format("_{0:D4}.gso", imageId & 0xffff));
        }

        objPathCache[imageId] = path;
        return path;
    }
}
}
