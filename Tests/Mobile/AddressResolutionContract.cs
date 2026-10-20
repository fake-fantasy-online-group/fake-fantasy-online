// Exercises the actual compiled Protocol resolver, login gate, error drain, gateway binding,
// and production title dismissal with a field-only title shell. No network or Unity run.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.Protocol;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using NetClient = FakeFantasyOnline.Protocol.NetClient;

internal static class AddressResolutionContract
{
    private static int checks;
    private static void Check(bool value, string name)
    {
        if (!value) throw new Exception(name);
        checks++;
    }

    private sealed class Request : NetClient.IAddressRequest
    {
        internal readonly object Operation = new object();
        internal string ThrowAt;
        internal int Sends, Disposals, TimeoutValue;
        internal int ResultValue = 1;
        internal string ErrorValue = "Connection timed out";
        internal string TextValue = "dispatch fixture";
        internal void MaybeThrow(string phase)
        {
            if (ThrowAt == phase) throw new InvalidOperationException("request " + phase + " failure");
        }
        public int Timeout { set { TimeoutValue = value; MaybeThrow("timeout"); } }
        public object Send() { Sends++; MaybeThrow("send"); return Operation; }
        public int Result { get { MaybeThrow("result"); return ResultValue; } }
        public string Error { get { MaybeThrow("error"); return ErrorValue; } }
        public string Text { get { MaybeThrow("text"); return TextValue; } }
        public void Dispose() { Disposals++; MaybeThrow("dispose"); }
    }

    private sealed class Trace : IWorldReceiveTracePort
    {
        public bool On => false;
        public long Now => 0;
        public float Ms(long started) => 0;
        public void Net(int opcode, int length, float ms) { }
        public void Backlog(int count) { }
    }

    private sealed class Fixture : IDisposable
    {
        internal readonly NetClient Client;
        internal readonly List<string> Warnings = new List<string>();
        internal readonly List<string> Errors = new List<string>();
        internal readonly List<string> ClearedNotices = new List<string>();
        internal readonly TitleFlow Title = new TitleFlow();
        internal readonly object Wait = new object();
        internal readonly ClientConfig.Srv[] Expected =
        {
            new ClientConfig.Srv { name = "fixture", host = "192.0.2.12", port = 4000 }
        };
        internal ClientConfig.Srv[] Published = new ClientConfig.Srv[0];
        internal int Waits, Publications;

        internal Fixture(Func<NetClient.IAddressRequest> request,
            Func<string, ClientConfig.Dispatch> parse = null)
        {
            Client = new NetClient(PrivateServerOptions.LocalDefault(), () => 0f,
                Warnings.Add, new Trace(), () => 5f);
            Client.OnError = error => { Errors.Add(error); Title.netMsg = error; };
            Client.BindAddressRetryNoticeClearForIndependentRuntime(notice =>
            {
                ClearedNotices.Add(notice);
                Title.ClearResolvedAddressRetryNotice(notice);
            });
            Client.BindAddressResolutionForIndependentRuntime(request,
                parse ?? (_ => new ClientConfig.Dispatch { servers = Expected }),
                seconds => { Check(seconds == 5f, "retry interval remains five scaled seconds"); Waits++; return Wait; },
                () => Published, rows => { Publications++; Published = rows; });
        }

        internal void CheckVisibleFailure(string detail)
        {
            Check(!Client.AddressReady && Client.AddressError.Contains(detail), "failed attempt records the useful cause");
            Check(Publications == 0 && Client.host == string.Empty && Client.port == 0,
                "failed attempt does not publish or select a server");
            int before = Errors.Count;
            Check(!Client.Connect(), "ordinary login remains gated without any TCP attempt");
            Check(Errors.Count == before + 1 && Errors[before] == Client.AddressError,
                "ordinary login immediately displays the failure instead of an endless fetching message");
            Client.Update();
            Check(Errors.Count == before + 2 && Errors[before + 1] == Client.AddressError + " (重试中…)",
                "existing network error queue publishes one retry notice");
            Client.Update();
            Check(Errors.Count == before + 2, "error queue does not duplicate the same notice");
        }

        internal void CheckSuccess(IEnumerator resolve, Request request)
        {
            Check(resolve.MoveNext() && ReferenceEquals(resolve.Current, request.Operation),
                "successful request still yields its original operation");
            Check(!Client.AddressReady && Client.AddressError == null && Publications == 0,
                "retry clears stale error and waits for actual completion");
            Check(request.TimeoutValue == 10 && request.Sends == 1 && request.Disposals == 0,
                "normal request keeps ten-second timeout and live request lifetime");
            Check(!resolve.MoveNext(), "completed valid dispatch ends the coroutine");
            Check(Client.AddressReady && Client.AddressError == null &&
                Client.host == Expected[0].host && Client.port == Expected[0].port,
                "success retains original endpoint selection");
            Check(Publications == 1 && ReferenceEquals(Published, Expected),
                "success publishes the original server array once");
            Check(request.Disposals == 1, "completed request is disposed exactly once");
            Check(Title.netMsg == string.Empty, "success dismisses the obsolete displayed address retry notice");
        }

        public void Dispose() { Client.Dispose(); }
    }

    private static void StartupFailure(string phase)
    {
        var failed = new Request { ThrowAt = phase };
        var good = new Request();
        int attempts = 0;
        using (var fixture = new Fixture(() =>
        {
            attempts++;
            if (attempts != 1) return good;
            if (phase == "factory") throw new InvalidOperationException("request factory failure");
            return failed;
        }))
        {
            IEnumerator resolve = fixture.Client.ResolveServerAddress();
            try
            {
                Check(resolve.MoveNext() && ReferenceEquals(resolve.Current, fixture.Wait),
                    phase + " exception reaches retry wait without escaping");
                Check(attempts == 1 && fixture.Waits == 1, "startup failure does not retry in a tight loop");
                Check(failed.Disposals == (phase == "factory" ? 0 : 1), "partly created request is disposed once");
                Check(failed.Sends == (phase == "send" ? 1 : 0), "startup failure does not send twice");
                Check(fixture.Warnings.Count == 1 && fixture.Warnings[0].Contains("第1次"),
                    "startup failure keeps original attempt logging");
                fixture.CheckVisibleFailure("request " + phase + " failure");
                fixture.CheckSuccess(resolve, good);
                Check(attempts == 2 && fixture.Waits == 1, "one recovery attempt completes without extra wait");
            }
            finally { ((IDisposable)resolve).Dispose(); }
        }
    }

    private static void RepeatedStartupFailure()
    {
        var requests = new List<Request>();
        using (var fixture = new Fixture(() =>
        {
            var request = new Request { ThrowAt = "send" };
            requests.Add(request);
            return request;
        }))
        {
            IEnumerator resolve = fixture.Client.ResolveServerAddress();
            try
            {
                for (int i = 1; i <= 3; i++)
                {
                    Check(resolve.MoveNext() && ReferenceEquals(resolve.Current, fixture.Wait), "each rejected send yields one retry wait");
                    Check(requests.Count == i && requests[i - 1].Disposals == 1, "each retry owns and disposes one request");
                    Check(fixture.Warnings.Count == i && fixture.Warnings[i - 1].Contains("第" + i + "次"), "attempt count survives rejected sends");
                    fixture.CheckVisibleFailure("request send failure");
                }
            }
            finally { ((IDisposable)resolve).Dispose(); }
            Check(requests.Count == 3 && fixture.Waits == 3, "stopping retry coroutine does not create another request");
        }
    }

    private static void CompletedFailure(string failure)
    {
        var failed = new Request { ResultValue = failure == "network" ? 2 : 1 };
        var good = new Request();
        int attempts = 0;
        Fixture fixture = null;
        fixture = new Fixture(() => ++attempts == 1 ? failed : good, _ =>
        {
            if (attempts == 1 && failure == "parse") throw new FormatException("malformed dispatch");
            return new ClientConfig.Dispatch { servers = attempts == 1 && failure == "invalid" ? new ClientConfig.Srv[0] : fixture.Expected };
        });
        using (fixture)
        {
            IEnumerator resolve = fixture.Client.ResolveServerAddress();
            try
            {
                Check(resolve.MoveNext() && ReferenceEquals(resolve.Current, failed.Operation), "completed-error request starts normally");
                Check(resolve.MoveNext() && ReferenceEquals(resolve.Current, fixture.Wait), "completed error retains existing retry route");
                Check(failed.Disposals == 1, "completed failed request is disposed once");
                fixture.CheckVisibleFailure(failure == "network" ? "Connection timed out" :
                    failure == "parse" ? "解析服务器地址失败: malformed dispatch" : "服务器地址无效(dispatch.servers 为空)");
                fixture.CheckSuccess(resolve, good);
            }
            finally { ((IDisposable)resolve).Dispose(); }
        }
    }

    private static void LoginWhileAddressPending(bool asynchronous, bool failFirst, string replacement)
    {
        var first = new Request { ResultValue = failFirst ? 2 : 1 };
        var good = new Request();
        int attempts = 0, callbacks = 0;
        using (var fixture = new Fixture(() => ++attempts == 1 ? first : good))
        {
            IEnumerator resolve = fixture.Client.ResolveServerAddress();
            try
            {
                Check(resolve.MoveNext(), "initial address request is in flight before login");
                if (asynchronous)
                    fixture.Client.ConnectAsync(ok => { callbacks++; Check(!ok, "pending address rejects login without connecting"); });
                else
                    Check(!fixture.Client.Connect(), "synchronous login is gated while address is pending");
                string pending = fixture.Title.netMsg;
                Check(pending == "正在获取服务器地址, 请稍候…", "pending login shows the existing address hint");
                Check(callbacks == (asynchronous ? 1 : 0), "pending login completes its callback exactly once");
                if (failFirst)
                {
                    Check(resolve.MoveNext() && ReferenceEquals(resolve.Current, fixture.Wait), "real failure still schedules retry");
                    fixture.Client.Update();
                    Check(fixture.Title.netMsg.Contains("Connection timed out"), "real failure remains visible");
                    Check(resolve.MoveNext(), "failed address request starts its next attempt");
                }
                string newer = replacement == null ? null : replacement == "identical" ?
                    new string(fixture.Title.netMsg.ToCharArray()) : "new login or server result";
                if (newer != null) fixture.Title.netMsg = newer;
                Check(!resolve.MoveNext() && fixture.Client.AddressReady, "address discovery completes successfully");
                fixture.Client.Update();
                Check(newer == null ? fixture.Title.netMsg == string.Empty : ReferenceEquals(fixture.Title.netMsg, newer),
                    "success clears only the owned address hint, preserving newer even identical messages");
                Check(callbacks == (asynchronous ? 1 : 0), "address recovery never replays the login callback");
                Check(!(bool)typeof(NetClient).GetField("_connecting", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(fixture.Client),
                    "address recovery does not start a TCP login or resubmit credentials");
                Check(attempts == (failFirst ? 2 : 1), "success starts no extra address request");
            }
            finally { ((IDisposable)resolve).Dispose(); }
        }
    }

    private static void DirectFailureNoticeRecoversBeforeQueueDrain()
    {
        var failed = new Request { ResultValue = 2 };
        var good = new Request();
        int attempts = 0, callbacks = 0;
        using (var fixture = new Fixture(() => ++attempts == 1 ? failed : good))
        {
            IEnumerator resolve = fixture.Client.ResolveServerAddress();
            try
            {
                Check(resolve.MoveNext() && resolve.MoveNext(), "failed request queues retry before login");
                Check(!string.IsNullOrEmpty(fixture.Client.AddressError), "login sees the nonempty failure cause");
                fixture.Client.ConnectAsync(ok => { callbacks++; Check(!ok, "failed address gates login"); });
                string direct = fixture.Title.netMsg;
                Check(direct == fixture.Client.AddressError && fixture.Errors.Count == 1,
                    "direct failure is displayed without draining the queued retry");
                Check(resolve.MoveNext() && !resolve.MoveNext(), "discovery recovers before error queue drain");
                Check(fixture.Title.netMsg == string.Empty && fixture.ClearedNotices.Count == 1 &&
                    ReferenceEquals(fixture.ClearedNotices[0], direct), "recovery clears the owned direct failure notice");
                fixture.Client.Update();
                Check(fixture.Errors.Count == 1 && fixture.Title.netMsg == string.Empty,
                    "late queue drain suppresses stale retry rather than restoring it");
                Check(callbacks == 1, "recovery never repeats the rejected login callback");
            }
            finally { ((IDisposable)resolve).Dispose(); }
        }
    }

    private static void RepeatedPendingLoginClearsOnlyLatestNotice()
    {
        var good = new Request();
        int callbacks = 0;
        using (var fixture = new Fixture(() => good))
        {
            IEnumerator resolve = fixture.Client.ResolveServerAddress();
            try
            {
                Check(resolve.MoveNext(), "request is pending during repeated clicks");
                for (int i = 0; i < 3; i++)
                    fixture.Client.ConnectAsync(ok => { callbacks++; Check(!ok, "each early click is rejected once"); });
                Check(fixture.Errors.Count == 3 && callbacks == 3, "each click displays its own notice and finishes");
                Check(!ReferenceEquals(fixture.Errors[0], fixture.Errors[1]) &&
                    !ReferenceEquals(fixture.Errors[1], fixture.Errors[2]), "identical click notices have distinct ownership");
                string latest = fixture.Title.netMsg;
                Check(ReferenceEquals(latest, fixture.Errors[2]), "latest click owns the displayed hint");
                Check(!resolve.MoveNext() && fixture.Client.AddressReady, "address succeeds after repeated clicks");
                fixture.Client.Update();
                Check(fixture.Title.netMsg == string.Empty && fixture.ClearedNotices.Count == 1 &&
                    ReferenceEquals(fixture.ClearedNotices[0], latest), "only the last displayed notice is cleared once");
                Check(callbacks == 3, "success never replays any of the earlier login attempts");
            }
            finally { ((IDisposable)resolve).Dispose(); }
        }
    }

    private static void NormalSuccessAndCancellation()
    {
        var good = new Request();
        using (var fixture = new Fixture(() => good))
        {
            IEnumerator resolve = fixture.Client.ResolveServerAddress();
            try { fixture.CheckSuccess(resolve, good); }
            finally { ((IDisposable)resolve).Dispose(); }
            Check(fixture.Waits == 0 && fixture.Warnings.Count == 0 && fixture.Errors.Count == 0,
                "desktop normal success introduces no error or retry");
            Check(fixture.ClearedNotices.Count == 0, "first-attempt success does not issue an error dismissal");
        }
        var pending = new Request();
        using (var fixture = new Fixture(() => pending))
        {
            IEnumerator resolve = fixture.Client.ResolveServerAddress();
            Check(resolve.MoveNext(), "pending request yields before cancellation");
            ((IDisposable)resolve).Dispose();
            Check(pending.Disposals == 1 && !fixture.Client.AddressReady && fixture.Publications == 0,
                "cancelling suspended coroutine still disposes request without publication");
            Check(fixture.Waits == 0 && fixture.Errors.Count == 0, "cancellation does not schedule a retry");
        }
    }

    private static void QueueOrdinaryError(NetClient client, string message)
    {
        // Exercise the actual FIFO without starting a TCP worker just to fabricate its
        // failure. The message is deliberately ordinary even when its text is identical.
        object queue = typeof(NetClient).GetField("_errs", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(client);
        Type entryType = queue.GetType().GetGenericArguments()[0];
        object entry = Activator.CreateInstance(entryType,
            BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { message, false }, null);
        queue.GetType().GetMethod("Enqueue").Invoke(queue, new[] { entry });
    }

    private static void RecoveryPreservesNewerMessage(string kind, bool queuedRetry)
    {
        var failed = new Request { ThrowAt = "send" };
        var good = new Request();
        int attempts = 0;
        using (var fixture = new Fixture(() => ++attempts == 1 ? failed : good))
        {
            IEnumerator resolve = fixture.Client.ResolveServerAddress();
            try
            {
                Check(resolve.MoveNext(), "failed send starts recovery fixture");
                string retry = fixture.Client.AddressError + " (重试中…)";
                if (!queuedRetry) fixture.Client.Update();
                string newer = kind == "identical" ? new string(retry.ToCharArray()) : kind + " result";
                if (queuedRetry) QueueOrdinaryError(fixture.Client, newer);
                else fixture.Title.netMsg = newer;
                Check(resolve.MoveNext() && !resolve.MoveNext(), "valid recovery finishes after a newer message");
                fixture.Client.Update();
                Check(ReferenceEquals(fixture.Title.netMsg, newer),
                    "recovery preserves " + kind + " message after " + (queuedRetry ? "queued" : "displayed") + " retry");
                Check(fixture.Errors.Count == 1, "only the expected displayed retry or ordinary queued error is delivered");
                fixture.Client.Update();
                Check(ReferenceEquals(fixture.Title.netMsg, newer), "next frame does not perform delayed unconditional clearing");
            }
            finally { ((IDisposable)resolve).Dispose(); }
        }
    }

    private static void MultipleQueuedRetries()
    {
        var good = new Request();
        int attempts = 0;
        using (var fixture = new Fixture(() => ++attempts <= 3 ? new Request { ThrowAt = "send" } : good))
        {
            IEnumerator resolve = fixture.Client.ResolveServerAddress();
            try
            {
                Check(resolve.MoveNext(), "first retry queued");
                QueueOrdinaryError(fixture.Client, "first unrelated transport error");
                Check(resolve.MoveNext(), "second retry queued");
                QueueOrdinaryError(fixture.Client, "second unrelated transport error");
                Check(resolve.MoveNext(), "third retry queued");
                Check(resolve.MoveNext() && !resolve.MoveNext(), "recovery completes before any queue drain");
                fixture.Client.Update();
                Check(fixture.Errors.Count == 2 && fixture.Errors[0] == "first unrelated transport error" &&
                    fixture.Errors[1] == "second unrelated transport error", "all stale retries are suppressed while unrelated FIFO ordering survives");
                Check(fixture.ClearedNotices.Count == 0, "never-displayed retries do not issue a dismissal");
            }
            finally { ((IDisposable)resolve).Dispose(); }
        }
    }

    private static void LatestDisplayedRetryAndBoundedOwnership()
    {
        int attempts = 0;
        var good = new Request();
        using (var fixture = new Fixture(() => ++attempts <= 1000 ? new Request { ThrowAt = "send" } : good))
        {
            IEnumerator resolve = fixture.Client.ResolveServerAddress();
            try
            {
                for (int i = 0; i < 1000; i++)
                {
                    Check(resolve.MoveNext(), "long offline period retains scheduled retry behavior");
                    fixture.Client.Update();
                }
                string latest = fixture.Title.netMsg;
                Check(ReferenceEquals(typeof(NetClient).GetField("displayedAddressRetryNotice",
                    BindingFlags.NonPublic | BindingFlags.Instance).GetValue(fixture.Client), latest),
                    "ownership retains exactly the latest displayed retry");
                object queue = typeof(NetClient).GetField("_errs", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(fixture.Client);
                Check((int)queue.GetType().GetProperty("Count").GetValue(queue) == 0, "each frame releases processed queue entries during long offline periods");
                Check(resolve.MoveNext() && !resolve.MoveNext(), "long offline period can recover");
                Check(fixture.Title.netMsg == string.Empty && fixture.ClearedNotices.Count == 1 &&
                    ReferenceEquals(fixture.ClearedNotices[0], latest), "only the latest displayed retry is dismissed once");
                Check(typeof(NetClient).GetField("displayedAddressRetryNotice", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(fixture.Client) == null, "successful resolution releases retained retry ownership");
            }
            finally { ((IDisposable)resolve).Dispose(); }
        }
    }

    private static void TitleBindingLifetime(bool rebind, bool drainSecondRetry = true)
    {
        using (var host = new SynchronizationContextHostExecutor(null, Thread.CurrentThread.ManagedThreadId))
        using (var gateway = new PrivateServerGateway(PrivateServerOptions.LocalDefault(), _ => { }, host, () => 0f,
            new Trace(), () => 5f))
        {
            var oldTitle = new TitleFlow();
            var newTitle = new TitleFlow { netMsg = "new title login error" };
            INetClientInstanceBinding binding = gateway;
            binding.BindNativeNetworkErrorForIndependentRuntime(error => oldTitle.netMsg = error, oldTitle.ClearResolvedAddressRetryNotice);
            var core = (NetClient)typeof(PrivateServerGateway).GetField("_netClient", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(gateway);
            int attempts = 0;
            gateway.BindAddressResolutionForIndependentRuntime(
                () => ++attempts <= 2 ? new Request { ThrowAt = "send" } : new Request(),
                _ => new ClientConfig.Dispatch { servers = new[] { new ClientConfig.Srv { host = "192.0.2.12", port = 4000 } } },
                _ => new object(), () => new ClientConfig.Srv[0], _ => { });
            IEnumerator resolve = gateway.ResolveServerAddressForIndependentRuntime();
            try
            {
                Check(resolve.MoveNext(), "old title receives first retry");
                core.Update();
                string oldNotice = oldTitle.netMsg;
                binding.BindNativeNetworkErrorForIndependentRuntime(null);
                Check(core.OnError == null && typeof(NetClient).GetField("clearAddressRetryNotice",
                    BindingFlags.NonPublic | BindingFlags.Instance).GetValue(core) == null,
                    "existing title teardown clears both paired callbacks");
                if (rebind) binding.BindNativeNetworkErrorForIndependentRuntime(error => newTitle.netMsg = error, newTitle.ClearResolvedAddressRetryNotice);
                Check(resolve.MoveNext(), "retry can continue with destroyed or replaced title");
                if (drainSecondRetry) core.Update();
                Check(resolve.MoveNext() && !resolve.MoveNext(), "resolution completes after title teardown");
                core.Update();
                Check(ReferenceEquals(oldTitle.netMsg, oldNotice), "resolution never calls the destroyed title callback");
                Check(newTitle.netMsg == (rebind && drainSecondRetry ? string.Empty : "new title login error"),
                    "only the currently bound title's retry can be dismissed");
            }
            finally { ((IDisposable)resolve).Dispose(); }
        }
    }

    private static void DismissalExceptionsAreNotDispatchFailures()
    {
        int attempts = 0;
        var good = new Request();
        using (var fixture = new Fixture(() => ++attempts == 1 ? new Request { ThrowAt = "send" } : good))
        {
            IEnumerator resolve = fixture.Client.ResolveServerAddress();
            try
            {
                Check(resolve.MoveNext(), "dismissal-exception fixture starts with retry");
                fixture.Client.Update();
                fixture.Client.BindAddressRetryNoticeClearForIndependentRuntime(_ => { throw new InvalidOperationException("UI dismissal failure"); });
                Check(resolve.MoveNext(), "dismissal-exception recovery request starts");
                bool threw = false;
                try { resolve.MoveNext(); }
                catch (InvalidOperationException error) { threw = error.Message == "UI dismissal failure"; }
                Check(threw && fixture.Client.AddressReady && fixture.Client.AddressError == null,
                    "UI dismissal exceptions escape without corrupting successful dispatch state");
                Check(good.Disposals == 1 && fixture.Waits == 1 && fixture.Warnings.Count == 1,
                    "UI exception neither leaks the request nor restarts discovery");
            }
            finally { ((IDisposable)resolve).Dispose(); }
        }
    }

    public static void Main()
    {
        foreach (string phase in new[] { "factory", "timeout", "send" }) StartupFailure(phase);
        RepeatedStartupFailure();
        foreach (string failure in new[] { "network", "parse", "invalid" }) CompletedFailure(failure);
        NormalSuccessAndCancellation();
        DirectFailureNoticeRecoversBeforeQueueDrain();
        RepeatedPendingLoginClearsOnlyLatestNotice();
        foreach (bool asynchronous in new[] { false, true })
            foreach (bool failFirst in new[] { false, true })
                foreach (string replacement in new string[] { null, "other", "identical" })
                    LoginWhileAddressPending(asynchronous, failFirst, replacement);
        foreach (string kind in new[] { "login", "server notice", "validation", "identical" })
            foreach (bool queued in new[] { false, true }) RecoveryPreservesNewerMessage(kind, queued);
        MultipleQueuedRetries();
        LatestDisplayedRetryAndBoundedOwnership();
        TitleBindingLifetime(false);
        TitleBindingLifetime(true);
        TitleBindingLifetime(true, false);
        DismissalExceptionsAreNotDispatchFailures();
        Console.WriteLine("PASS address resolution: " + checks + " assertions against actual Protocol, gateway binding and title dismissal; deterministic request capabilities and a title field shell, no Unity/device run.");
    }
}

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Compile the production dismissal partial against only its field dependency.
internal sealed partial class TitleFlow
{
    internal string netMsg = string.Empty;
}
}
