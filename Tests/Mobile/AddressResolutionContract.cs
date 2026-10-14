// Exercises the actual compiled Protocol resolver, login gate and error-queue drain.
// Only request/parse/wait/trace capabilities are deterministic substitutes; no network or Unity.
using System;
using System.Collections;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Protocol;

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
            Client.OnError = Errors.Add;
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

    public static void Main()
    {
        foreach (string phase in new[] { "factory", "timeout", "send" }) StartupFailure(phase);
        RepeatedStartupFailure();
        foreach (string failure in new[] { "network", "parse", "invalid" }) CompletedFailure(failure);
        NormalSuccessAndCancellation();
        Console.WriteLine("PASS address resolution: " + checks + " assertions against actual Protocol; deterministic request capabilities, no Unity/device run.");
    }
}
