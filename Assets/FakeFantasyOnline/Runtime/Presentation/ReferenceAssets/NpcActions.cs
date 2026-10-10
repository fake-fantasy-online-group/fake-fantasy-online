using System;
using System.Collections.Generic;
using System.Text;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official NpcActions owner for the action-table members used by MonsterEntity.
/// </summary>
public static partial class NpcActions
{
    private static readonly Dictionary<int, Dictionary<string, int>> _cache =
        new Dictionary<int, Dictionary<string, int>>();
    private static readonly Dictionary<int, Dictionary<string, int>> _intvl =
        new Dictionary<int, Dictionary<string, int>>();
    private static bool _loaded;
    private static string _text;
    private static string _text24;

    private static string TextFor(int model)
    {
        EnsureText();
        if (!string.IsNullOrEmpty(_text) &&
            (_text.Contains(string.Format("\"M{0}\"", model)) ||
             _text.Contains(string.Format("\"A{0}\"", model))))
        {
            return _text;
        }

        if (!string.IsNullOrEmpty(_text24) &&
            (_text24.Contains(string.Format("\"M{0}\"", model)) ||
             _text24.Contains(string.Format("\"A{0}\"", model))))
        {
            return _text24;
        }

        return _text;
    }

    public static int Interval(int model, string name)
    {
        ActionsOf(model);
        Dictionary<string, int> values;
        int value;
        return _intvl.TryGetValue(model, out values) &&
               values.TryGetValue(name, out value)
            ? value
            : 0;
    }

    private static void EnsureText()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        ResLoader loader = ResLoader.I;
        if (loader != null && loader.Pkg != null)
        {
            byte[] primary = loader.Pkg.Read("avatar/npc/_npc_.txt");
            if (primary != null)
            {
                _text = Encoding.ASCII.GetString(primary);
            }
        }

        byte[] q21 = ResLoader.Read24Res("avatar/npc/_npc_.txt");
        if (q21 != null)
        {
            _text24 = Encoding.ASCII.GetString(q21);
        }
    }

    public static int Find(int model, string name)
    {
        Dictionary<string, int> actions = ActionsOf(model);
        int value;
        return actions != null && actions.TryGetValue(name, out value)
            ? value
            : -1;
    }

    /// <summary>
    /// Resolves the optional model-specific rider-pose key from the official action table.
    /// </summary>
    public static string RideKey(int model)
    {
        EnsureText();
        string value = RideKeyIn(_text24, model);
        return !string.IsNullOrEmpty(value)
            ? value
            : RideKeyIn(_text, model);
    }

    public static string FindNameContaining(int model, string substr)
    {
        Dictionary<string, int> actions = ActionsOf(model);
        if (actions == null)
        {
            return null;
        }

        foreach (string name in actions.Keys)
        {
            if (name.IndexOf(substr, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return name;
            }
        }

        return null;
    }

    private static Dictionary<string, int> ActionsOf(int model)
    {
        Dictionary<string, int> actions;
        if (_cache.TryGetValue(model, out actions))
        {
            return actions;
        }

        EnsureText();
        actions = ParseBlock(model);
        _cache[model] = actions;
        return actions;
    }

    private static string RideKeyIn(string text, int model)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        int modelStart = text.IndexOf(string.Format("\"M{0}\"", model));
        if (modelStart < 0)
        {
            modelStart = text.IndexOf(string.Format("\"A{0}\"", model));
        }
        if (modelStart < 0)
        {
            modelStart = text.IndexOf(string.Format("\"N{0}\"", model));
        }
        if (modelStart < 0)
        {
            return string.Empty;
        }

        int nextModel = text.IndexOf("id = \"", modelStart + 1);
        if (nextModel < 0)
        {
            nextModel = text.Length;
        }

        int ride = text.IndexOf("ride = ", modelStart, nextModel - modelStart);
        if (ride < 0)
        {
            return string.Empty;
        }

        int firstQuote = text.IndexOf('"', ride);
        if (firstQuote < 0 || firstQuote > nextModel)
        {
            return string.Empty;
        }

        int secondQuote = text.IndexOf('"', firstQuote + 1);
        return secondQuote >= 0
            ? text.Substring(firstQuote + 1, secondQuote - firstQuote - 1).Trim()
            : string.Empty;
    }

    private static Dictionary<string, int> ParseBlock(int model)
    {
        Dictionary<string, int> actions =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> intervals =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        _intvl[model] = intervals;

        string text = TextFor(model);
        if (string.IsNullOrEmpty(text))
        {
            return actions;
        }

        int modelStart = text.IndexOf(string.Format("\"M{0}\"", model));
        if (modelStart < 0)
        {
            modelStart = text.IndexOf(string.Format("\"A{0}\"", model));
        }
        if (modelStart < 0)
        {
            modelStart = text.IndexOf(string.Format("\"N{0}\"", model));
        }
        if (modelStart < 0)
        {
            return actions;
        }

        int nextModel = text.IndexOf("id = \"", modelStart + 1);
        if (nextModel < 0)
        {
            nextModel = text.Length;
        }

        string block = text.Substring(modelStart, nextModel - modelStart);
        int open = block.IndexOf('{', 0);
        while (open >= 0)
        {
            int close = block.IndexOf('}', open);
            if (close < 0)
            {
                break;
            }

            string row = block.Substring(open + 1, close - open - 1);
            int firstQuote = row.IndexOf('"');
            int secondQuote = firstQuote < 0 ? -1 : row.IndexOf('"', firstQuote + 1);
            if (firstQuote >= 0 && secondQuote > firstQuote)
            {
                string name = row.Substring(
                    firstQuote + 1,
                    secondQuote - firstQuote - 1).Trim();
                int actionId;
                if (int.TryParse(
                        row.Substring(0, row.IndexOf(','))
                            .Trim().TrimStart('{').Trim(),
                        out actionId) &&
                    name.Length > 0)
                {
                    actions[name] = actionId;
                    int intervalComma = row.IndexOf(',', secondQuote);
                    if (intervalComma >= 0)
                    {
                        string intervalText = row.Substring(intervalComma + 1);
                        int tailComma = intervalText.IndexOf(',');
                        if (tailComma >= 0)
                        {
                            intervalText = intervalText.Substring(0, tailComma);
                        }

                        int interval;
                        if (int.TryParse(intervalText.Trim(), out interval))
                        {
                            intervals[name] = interval;
                        }
                    }
                }
            }

            open = block.IndexOf('{', close + 1);
        }

        // Official ParseBlock (0x060008ff) tail-calls FixCrossSetActionIds on every exit
        // from the brace scan, not only on the malformed-row paths: the cross-set id fix
        // runs after the primary block has been read in full.
        FixCrossSetActionIds(model, actions, intervals);
        return actions;
    }
}

}
