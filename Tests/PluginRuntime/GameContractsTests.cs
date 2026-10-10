using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.PluginApi;
using FakeFantasyOnline.PluginHost;

internal static class GameContractsTests
{
    private static int assertions;
    private static void Check(bool condition, string reason)
    {
        assertions++;
        if (!condition) throw new Exception(reason);
    }
    private static void Throws<T>(Action action, string reason) where T : Exception
    {
        assertions++;
        try { action(); }
        catch (T) { return; }
        throw new Exception(reason);
    }
    private static Exception Failure(Task task)
    {
        try { task.GetAwaiter().GetResult(); }
        catch (Exception e) { return e; }
        throw new Exception("Expected a failed task.");
    }
    private static void JsonRoundTrips()
    {
        var text = "汉字 😀\n\r\t\b\f\0\"\\/";
        var source = new Dictionary<string, object>
        {
            { "unicode", text }, { "null", null }, { "bool", true },
            { "int64", long.MinValue }, { "uint64", ulong.MaxValue },
            { "precise", 1234567890.1234567890123456789m },
            { "list", new object[] { 1, "two", false, new Dictionary<string, object> { { "x", 3 } } } }
        };
        string json = GameJson.Serialize(source);
        var copy = (Dictionary<string, object>)GameJson.Parse(json);
        Check((string)copy["unicode"] == text, "Unicode and escapes must round trip.");
        Check(copy["null"] == null && (bool)copy["bool"], "JSON scalar values.");
        Check((long)copy["int64"] == long.MinValue, "Signed 64-bit precision.");
        Check((ulong)copy["uint64"] == ulong.MaxValue, "Unsigned 64-bit precision.");
        Check((decimal)copy["precise"] == 1234567890.1234567890123456789m, "Decimal precision.");
        Check((long)GameJson.Parse("9007199254740993") == 9007199254740993L, "Integers cannot route through double.");
        Check((decimal)GameJson.Parse("79228162514264337593543950335") == decimal.MaxValue, "Decimal maximum integer precision.");
        Check(GameJson.Parse("1.234567890123456789012345678901") is double, "Decimal parser cannot silently round fractional precision.");
        Check((decimal)GameJson.Parse("1.25e2") == 125m, "Exponent exact decimal.");
        Check(GameJson.Parse("1e100") is double, "Finite double exponent fallback.");
        Check((string)GameJson.Parse("\"\\uD83D\\uDE00\"") == "😀", "Escaped surrogate pair.");
        Check((string)GameJson.Parse("\"\\u0000\"") == "\0", "Escaped null character.");
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            Check(GameJson.Serialize(1.25m) == "1.25", "Serialization is culture invariant.");
            Check((decimal)GameJson.Parse("1.25") == 1.25m, "Parsing is culture invariant.");
        }
        finally { CultureInfo.CurrentCulture = previous; }
        Check(GameJson.Serialize(GameJson.Parse(json)) == json, "Nested object round trip.");
        foreach (double number in new[] { double.MaxValue, double.MinValue, double.Epsilon, -double.Epsilon, Math.PI, 1.0 / 3.0, 1e-50, 1e100 })
            Check(Convert.ToDouble(GameJson.Parse(GameJson.Serialize(number)), CultureInfo.InvariantCulture) == number, "Double round trip: " + number);
        foreach (float number in new[] { float.MaxValue, float.MinValue, float.Epsilon, -float.Epsilon, 1.0f / 3.0f })
            Check(Convert.ToSingle(GameJson.Parse(GameJson.Serialize(number)), CultureInfo.InvariantCulture) == number, "Float round trip: " + number);
    }
    private static void JsonRejectsInvalidInput()
    {
        string[] invalid = { "", " ", "01", "-01", "+1", "1.", ".1", "1e", "1e+", "1e9999", "1e-9999", "NaN", "Infinity", "true false", "nullx", "[1,]", "{\"a\":1,}", "{\"a\":1,\"a\":2}", "{a:1}", "\"\\x00\"", "\"\\uD800\"", "\"\\uDC00\"", "\"\ud800\"", "\"\n\"", "\"unfinished", "79228162514264337593543950336" };
        foreach (var input in invalid) Throws<FormatException>(() => GameJson.Parse(input), "Must reject invalid JSON: " + input);
        Throws<FormatException>(() => GameJson.Parse(new string('[', 34) + "0" + new string(']', 34)), "Nesting bound.");
        Throws<FormatException>(() => GameJson.Parse("\"" + new string('x', GameApiData.MaxStringLength + 1) + "\""), "String bound.");
        Throws<FormatException>(() => GameJson.Parse(new string(' ', GameJson.MaxJsonLength + 1)), "Document bound.");
        Throws<FormatException>(() => GameJson.Parse("[" + string.Join(",", new string[GameJson.MaxNodes]).Replace(",", "0,") + "0]"), "Item bound.");
        Throws<ArgumentException>(() => GameJson.Serialize(double.NaN), "NaN rejected.");
        Throws<ArgumentException>(() => GameJson.Serialize(float.PositiveInfinity), "Infinity rejected.");
        Throws<ArgumentException>(() => GameJson.Serialize(DateTime.UtcNow), "No arbitrary CLR serialization.");
        Throws<ArgumentException>(() => GameJson.Serialize("\udc00"), "Invalid raw Unicode rejected.");
        Throws<ArgumentException>(() => GameJson.Serialize(new Hashtable { { 1, "bad" } }), "Object keys must be strings.");
        var cycle = new List<object>(); cycle.Add(cycle);
        Throws<ArgumentException>(() => GameJson.Serialize(cycle), "Cycles rejected.");
        Throws<ArgumentException>(() => GameJson.Serialize(new string('\0', GameApiData.MaxStringLength)), "Escaping expansion respects output bound.");
    }
    private static void ImmutableContracts()
    {
        var nested = new List<object> { new Dictionary<string, object> { { "value", 3 } } };
        var source = new Dictionary<string, object> { { "nested", nested } };
        var request = new GameActionRequest("test", source, "r1", 42);
        source["new"] = 1;
        ((Dictionary<string, object>)nested[0])["value"] = 9;
        nested.Clear();
        var copy = (IReadOnlyList<object>)request.Arguments["nested"];
        Check(!request.Arguments.ContainsKey("new"), "Outer data copied.");
        Check((int)((IReadOnlyDictionary<string, object>)copy[0])["value"] == 3, "Nested data deeply copied.");
        Throws<NotSupportedException>(() => ((IDictionary<string, object>)request.Arguments).Add("x", 2), "Dictionary immutable.");
        Throws<NotSupportedException>(() => ((IList<object>)copy).Add(2), "Nested arrays immutable.");
        Check(request.ExpectedSceneRevision == 42 && request.RequestId == "r1", "Request identity retained.");
        Check(new GameQuery("player").Arguments.Count == 0, "Null args are empty object.");
        var filters = new Dictionary<string, object> { { "maxCount", 64 } };
        var queryDefinition = new GameQueryDefinition("entities", "Nearby actors", filters);
        filters["maxCount"] = 999;
        Check((int)queryDefinition.Arguments["maxCount"] == 64 && queryDefinition.Domain == "entities", "Query discovery schema is immutable.");
        Check(new GameActionReceipt("missing", null, "", GameActionState.Unknown, "unknown_action", null).Name == "", "Unknown action can lack its name.");
        var events = new List<GameEvent> { new GameEvent(3, 1, "changed", null) };
        var batch = new GameEventBatch(events, 3, true); events.Clear();
        Check(batch.Events.Count == 1 && batch.ResyncRequired, "Event batch copies its list.");
        var widgets = new List<PluginWidgetDefinition> { new PluginWidgetDefinition("x", PluginWidgetKind.Label, "Hi", 0, 0, 20, 20) };
        var panel = new PluginPanelDefinition("p", "P", 100, 100, widgets: widgets); widgets.Clear();
        Check(panel.Widgets.Count == 1, "UI widgets copied.");
        Throws<ArgumentException>(() => new PluginPanelDefinition("p", "P", 100, 100, widgets: new[] { panel.Widgets[0], panel.Widgets[0] }), "Widget IDs unique.");
        Throws<ArgumentOutOfRangeException>(() => new PluginPanelDefinition("p", "P", float.NaN, 100), "UI geometry finite.");
    }
    private static void DispatcherLifecycle()
    {
        using (var dispatcher = new MainThreadGameDispatcher(2))
        {
            int calls = 0;
            var one = dispatcher.InvokeAsync(() => ++calls);
            Check(!one.IsCompleted && calls == 0, "Invoke never executes Unity work inline.");
            var bad = dispatcher.InvokeAsync<int>(() => { throw new FormatException("test"); });
            var full = dispatcher.InvokeAsync(() => 9);
            Check(Failure(full) is InvalidOperationException, "Backlog bounded.");
            Check(dispatcher.Pump(1) == 1 && one.Result == 1 && calls == 1, "Bounded pump executes queued job.");
            Check(dispatcher.Pump() == 1 && Failure(bad) is FormatException, "Action errors are isolated.");
            using (var cancel = new CancellationTokenSource())
            {
                var cancelled = dispatcher.InvokeAsync(() => ++calls, cancel.Token);
                cancel.Cancel();
                Check(cancelled.IsCanceled && dispatcher.PendingCount == 0, "Cancellation releases queue capacity immediately.");
                dispatcher.Pump();
                Check(calls == 1, "Cancelled action never invoked.");
                Check(dispatcher.InvokeAsync(() => ++calls, cancel.Token).IsCanceled, "Pre-cancelled job is never queued.");
            }
            using (var cancel = new CancellationTokenSource())
            {
                var executing = dispatcher.InvokeAsync(() => { cancel.Cancel(); return 71; }, cancel.Token);
                dispatcher.Pump();
                Check(executing.Result == 71, "Cancellation cannot invent failure for already executing work.");
            }
            var wrongThread = Task.Run(() => { try { dispatcher.Pump(); return false; } catch (InvalidOperationException) { return true; } });
            Check(wrongThread.Result, "Pump rejects non-owner thread.");
            var pending = dispatcher.InvokeAsync(() => ++calls);
            dispatcher.Dispose();
            Check(Failure(pending) is ObjectDisposedException, "Dispose completes all pending tasks.");
            Check(dispatcher.PendingCount == 0 && dispatcher.Pump() == 0, "Dispose drains queue.");
            Check(Failure(dispatcher.InvokeAsync(() => 2)) is ObjectDisposedException, "Disposed dispatcher rejects work.");
        }
    }
    private static void DispatcherRace()
    {
        using (var dispatcher = new MainThreadGameDispatcher(32))
        {
            int executions = 0;
            for (int i = 0; i < 200; i++)
            {
                using (var token = new CancellationTokenSource())
                {
                    var queued = dispatcher.InvokeAsync(() => Interlocked.Increment(ref executions), token.Token);
                    Task cancelling = Task.Run(() => token.Cancel());
                    dispatcher.Pump();
                    Check(cancelling.Wait(3000), "Cancellation registration must not deadlock execution.");
                    Check(queued.IsCompleted, "Queue/cancellation race terminates.");
                    Check(queued.IsCanceled || queued.Status == TaskStatus.RanToCompletion, "Race has an honest outcome.");
                }
            }
            Check(dispatcher.PendingCount == 0, "Races leave no queue residue.");
            var completions = new List<Task<int>>();
            for (int i = 0; i < 32; i++) completions.Add(dispatcher.InvokeAsync(() => ++executions));
            var dispose = Task.Run(() => dispatcher.Dispose());
            Check(dispose.Wait(3000), "Disposal does not deadlock.");
            foreach (var task in completions) Check(Failure(task) is ObjectDisposedException, "Disposal terminates every queued task.");
        }
    }
    public static int Main()
    {
        JsonRoundTrips(); JsonRejectsInvalidInput(); ImmutableContracts(); DispatcherLifecycle(); DispatcherRace();
        Console.WriteLine("Game contracts: " + assertions + " assertions passed (standalone, no Unity or live gameplay).");
        return 0;
    }
}
