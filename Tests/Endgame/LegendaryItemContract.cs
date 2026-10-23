using System;
using FakeFantasyOnline.Application;
static class LegendaryItemContract
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static string Payload(string optional) => "{\"kind\":\"snapshot\",\"ok\":true,\"items\":[{\"itemId\":1" + optional + "}]}";
    static void Main()
    {
        Check(EndgameResponse.Parse(Payload("")).Items[0].Legendary == null, "legacy item unchanged");
        string affix = "{\"attr\":3,\"value\":25,\"mode\":0,\"min\":20,\"max\":30,\"future\":true}";
        string extra = ",\"legendary\":{\"powerId\":\"FUTURE-POWER-999\",\"name\":\"未知新特性\",\"description\":\"再次施法触发，不添加延迟\",\"future\":{\"optional\":true},\"affixes\":[" + string.Join(",", new[] {affix,affix,affix,affix,affix,affix}) + "]}";
        var item = EndgameResponse.Parse(Payload(extra)).Items[0];
        Check(item.Legendary.PowerID == "FUTURE-POWER-999" && item.Legendary.Affixes.Count == 6, "opaque power identity and six independent affixes");
        Check(item.Legendary.Affixes[5].Min == 20 && item.Legendary.Affixes[5].Max == 30 && item.Legendary.Affixes[5].Value == 25, "authoritative roll range");
        Check(item.Legendary.Description == "再次施法触发，不添加延迟", "server wording preserved");
        try { EndgameResponse.Parse(Payload(extra.Replace("\"value\":25", "\"value\":31"))); throw new Exception("bad roll accepted"); } catch (FormatException) { }
        Check(EndgameResponse.Parse(Payload(",\"legendary\":null")).Items[0].Legendary == null, "removed legendary presentation");
        Console.WriteLine("PASS legendary optional item, unknown power/fields, six rolls/ranges, descriptions, malformed roll and removal");
    }
}
