using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{
/// <summary>Bounded process-lifetime recovery, scoped to endpoint/account/authenticated character.
/// It contains no credentials, restores no inventory and never automatically resends.</summary>
public sealed class EndgameRetryJournal
{
    private readonly Dictionary<string, EndgameRequest> requests = new Dictionary<string, EndgameRequest>(StringComparer.Ordinal);
    private readonly List<string> order = new List<string>();
    public void Remember(string scope, EndgameRequest request)
    {
        if (string.IsNullOrEmpty(scope) || request == null || !request.Mutates) return;
        if (!requests.ContainsKey(scope)) order.Add(scope);
        requests[scope] = request;
        while (order.Count > 32) { requests.Remove(order[0]); order.RemoveAt(0); }
    }
    public EndgameRequest Find(string scope) => scope != null && requests.TryGetValue(scope, out var request) ? request : null;
    public void Complete(string scope, string requestID)
    {
        if (scope != null && requests.TryGetValue(scope, out var request) && request.RequestID == requestID) { requests.Remove(scope); order.Remove(scope); }
    }
    public static string Scope(string endpoint, string account, string character)
    {
        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(account) || string.IsNullOrEmpty(character)) return "";
        return endpoint.Length + ":" + endpoint + account.Length + ":" + account + character.Length + ":" + character;
    }
}
}
