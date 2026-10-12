using System;
using System.Collections.Generic;
using System.Globalization;
using FakeFantasyOnline.PluginApi;

namespace FakeFantasyOnline.PluginHost
{
    /// <summary>
    /// Transport-neutral JSON envelope adapter. Handle must run on the game thread.
    /// The server dispatches it through MainThreadGameDispatcher; no API is called by a socket worker.
    /// </summary>
    public sealed class GameAgentProtocol
    {
        public const string ProtocolVersion = "ffo-agent/1";
        public const int MaxEventCount = 512;
        private readonly IGameApi api;

        public GameAgentProtocol(IGameApi api)
        {
            this.api = api ?? throw new ArgumentNullException(nameof(api));
        }

        public object Handle(object value)
        {
            string id = null;
            bool sensitive = IsSensitiveLogin(value);
            try
            {
                var request = Object(value, "request");
                id = Text(request, "id", true);
                Only(request, "id", "method", "params");
                var method = Text(request, "method", true);
                object parameters;
                var args = request.TryGetValue("params", out parameters)
                    ? Object(parameters, "params") : new Dictionary<string, object>();
                object result;
                switch (method)
                {
                    case "describe":
                        Only(args);
                        result = Describe();
                        break;
                    case "observe":
                        Only(args, "domain", "arguments");
                        var domain = Text(args, "domain", true);
                        var queryArguments = Arguments(args);
                        var queryCatalog = api as IGameObservationCatalog;
                        if (queryCatalog != null)
                            foreach (var query in queryCatalog.GetQueries())
                                if (query.Domain == domain) RejectInexactIntegerArguments(queryArguments, query.Arguments, "arguments");
                        var snapshot = api.Observe(new GameQuery(domain, queryArguments));
                        result = Map("domain", snapshot.Domain, "sceneRevision", snapshot.SceneRevision,
                            "revision", snapshot.Revision, "data", snapshot.Data);
                        break;
                    case "sensitive-login":
                        Only(args, "account", "password", "expectedTitleInstanceId", "expectedTitleRevision");
                        var login = api as IGameSensitiveLogin;
                        if (login == null) throw new ProtocolException("capability_missing", "Sensitive login is unavailable.");
                        var loginResult = login.SubmitLogin(Credential(args, "account", 20), Credential(args, "password", 40),
                            Text(args, "expectedTitleInstanceId", true), Text(args, "expectedTitleRevision", true));
                        result = Map("submitted", loginResult != null && loginResult.Submitted,
                            "code", SafeLoginCode(loginResult == null ? null : loginResult.Code));
                        break;
                    case "act":
                        Only(args, "name", "arguments", "requestId", "expectedSceneRevision");
                        var requestId = Text(args, "requestId", true);
                        long? expected = args.ContainsKey("expectedSceneRevision")
                            ? Integer(args, "expectedSceneRevision", 0, long.MaxValue) : (long?)null;
                        var actionName = Text(args, "name", true);
                        var actionArguments = Arguments(args);
                        if (ContainsSensitiveKeys(actionArguments)) throw new ProtocolException("sensitive_arguments", "Credentials require the separate ephemeral login method.");
                        foreach (var definition in api.GetActions())
                            if (definition.Name == actionName) RejectInexactIntegerArguments(actionArguments, definition.Arguments, "arguments");
                        result = Receipt(api.Execute(new GameActionRequest(actionName, actionArguments, requestId, expected)));
                        break;
                    case "status":
                    case "action-status":
                        Only(args, "actionId");
                        result = Receipt(api.GetAction(Text(args, "actionId", true)));
                        break;
                    case "cancel":
                        Only(args, "actionId");
                        var actionId = Text(args, "actionId", true);
                        var cancelled = api.CancelAction(actionId);
                        result = Map("actionId", actionId, "cancelled", cancelled,
                            "receipt", Receipt(api.GetAction(actionId)));
                        break;
                    case "events":
                        Only(args, "afterSequence", "maxCount");
                        var afterSequence = Integer(args, "afterSequence", 0, long.MaxValue);
                        var count = args.ContainsKey("maxCount")
                            ? (int)Integer(args, "maxCount", 1, MaxEventCount) : 128;
                        var batch = api.ReadEvents(afterSequence, count);
                        var events = new List<object>(batch.Events.Count);
                        foreach (var item in batch.Events)
                            events.Add(Map("sequence", item.Sequence, "sceneRevision", item.SceneRevision,
                                "name", item.Name, "data", item.Data));
                        result = Map("events", events, "nextSequence", batch.NextSequence,
                            "resyncRequired", batch.ResyncRequired);
                        break;
                    default:
                        throw new ProtocolException("unknown_method", "Unknown method: " + method);
                }
                return Map("id", id, "ok", true, "result", result);
            }
            catch (ProtocolException exception)
            {
                return Error(id, exception.Code, sensitive ? "Sensitive login request was rejected." : exception.Message);
            }
            catch (ArgumentException exception)
            {
                return Error(id, "invalid_params", sensitive ? "Sensitive login request was rejected." : exception.Message);
            }
            catch (Exception)
            {
                return Error(id, "api_error", "The game API could not complete this request.");
            }
        }

        public static bool IsSensitiveLogin(object value)
        {
            var map = value as IReadOnlyDictionary<string, object>;
            object method;
            return map != null && map.TryGetValue("method", out method) && Equals(method, "sensitive-login");
        }

        public static bool ContainsSensitiveKeys(object value)
        {
            var map = value as IReadOnlyDictionary<string, object>;
            if (map != null)
                foreach (var pair in map)
                {
                    var key = pair.Key.Replace("_", "").Replace("-", "").ToLowerInvariant();
                    if (key.Contains("password") || key.Contains("credential") || key == "passwd" || key == "secret" || key == "token") return true;
                    if (ContainsSensitiveKeys(pair.Value)) return true;
                }
            var list = value as System.Collections.IList;
            if (list != null) foreach (var item in list) if (ContainsSensitiveKeys(item)) return true;
            return false;
        }

        private static string Credential(IReadOnlyDictionary<string, object> args, string key, int max)
        {
            object value;
            var text = args.TryGetValue(key, out value) ? value as string : null;
            if (string.IsNullOrEmpty(text) || text.Length > max || text.IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0)
                throw new ProtocolException("invalid_params", "Invalid login fields.");
            return text;
        }

        private static string SafeLoginCode(string code)
        {
            switch (code)
            {
                case "outcome_unknown": case "submitted": case "sent": case "busy": case "not_ready": case "title_changed":
                case "scene_changed": case "invalid_credentials": case "login_unavailable":
                case "stale_title": case "native_unavailable": case "invalid_state":
                case "title_instance_changed": case "title_revision_changed":
                    return code;
                default: return "login_result";
            }
        }

        public static object Error(string id, string code, string message)
        {
            return Map("id", id, "ok", false, "error", Map("code", code, "message", message));
        }

        private object Describe()
        {
            var actions = new List<object>();
            foreach (var action in api.GetActions())
                actions.Add(Map("name", action.Name, "description", action.Description,
                    "arguments", action.Arguments, "available", action.Available,
                    "unavailableReason", action.UnavailableReason));
            var queries = new List<object>();
            var catalog = api as IGameObservationCatalog;
            if (catalog != null)
                foreach (var query in catalog.GetQueries())
                    queries.Add(Map("domain", query.Domain, "description", query.Description, "arguments", query.Arguments));
            return Map("protocol", ProtocolVersion,
                "queries", queries, "observationDiscoveryAvailable", catalog != null,
                "methods", new object[] { "describe", "observe", "act", "status", "action-status", "cancel", "events", "sensitive-login" },
                "actions", actions,
                "sensitiveLoginAvailable", api is IGameSensitiveLogin,
                "sensitiveLoginSemantics", "Ephemeral trusted-local method; no receipt, retention or retry. Loopback has no token authentication. Observe title after submission.",
                "maxEventCount", MaxEventCount,
                "eventCursor", "Client-owned afterSequence; resyncRequired means snapshots must be refreshed.",
                "actionSemantics", "requestId identifies a retry; actionId identifies its receipt. Sent is not completion.",
                "cancellation", "Cancels queued dispatch or cancellable local work; does not undo requests already sent to the game server.");
        }

        private static object Receipt(GameActionReceipt receipt)
        {
            if (receipt == null) return null;
            return Map("actionId", receipt.ActionId, "requestId", receipt.RequestId, "name", receipt.Name,
                "state", receipt.State.ToString().ToLowerInvariant(), "code", receipt.Code,
                "message", receipt.Message, "data", receipt.Data);
        }

        // The shared API also accepts Lua doubles. At this JSON boundary we still know that
        // a double came from an inexact token: exact signed64 integers fit long/ulong/decimal.
        // Reject such values for advertised integer parameters before losing that provenance.
        // Native owners remain responsible for full schema and current-state validation.
        private static void RejectInexactIntegerArguments(IReadOnlyDictionary<string, object> values,
            IReadOnlyDictionary<string, object> schema, string path)
        {
            if (values == null || schema == null) return;
            object propertyValue;
            if (!schema.TryGetValue("properties", out propertyValue)) return;
            var properties = propertyValue as IReadOnlyDictionary<string, object>;
            if (properties == null) return;
            foreach (var pair in values)
            {
                object definitionValue;
                if (!properties.TryGetValue(pair.Key, out definitionValue)) continue;
                var definition = definitionValue as IReadOnlyDictionary<string, object>;
                if (definition == null) continue;
                object type;
                if (definition.TryGetValue("type", out type) && Equals(type, "integer") &&
                    (pair.Value is double || pair.Value is float))
                    throw new ProtocolException("invalid_params", path + "." + pair.Key + " requires an exactly represented JSON integer.");
                var nested = pair.Value as IReadOnlyDictionary<string, object>;
                if (nested != null) RejectInexactIntegerArguments(nested, definition, path + "." + pair.Key);
            }
        }

        private static IReadOnlyDictionary<string, object> Arguments(IReadOnlyDictionary<string, object> args)
        {
            object value;
            return args.TryGetValue("arguments", out value) ? Object(value, "arguments") : null;
        }

        private static IReadOnlyDictionary<string, object> Object(object value, string name)
        {
            var result = value as IReadOnlyDictionary<string, object>;
            if (result == null) throw new ProtocolException("invalid_params", name + " must be an object.");
            return result;
        }

        private static string Text(IReadOnlyDictionary<string, object> args, string name, bool required)
        {
            object value;
            if (!args.TryGetValue(name, out value))
            {
                if (!required) return null;
                throw new ProtocolException("invalid_params", name + " is required.");
            }
            var text = value as string;
            if (string.IsNullOrWhiteSpace(text) || text.Length > 128)
                throw new ProtocolException("invalid_params", name + " must be a nonempty string of at most 128 characters.");
            return text;
        }

        private static long Integer(IReadOnlyDictionary<string, object> args, string name, long min, long max)
        {
            object value;
            if (!args.TryGetValue(name, out value) || value == null || value is bool || value is string || value is double || value is float)
                throw new ProtocolException("invalid_params", name + " must be an integer.");
            try
            {
                // Integral JSON fits long/ulong/decimal. A double may be an inexact fractional token; never round it into an integer.
                var number = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                if (decimal.Truncate(number) != number || number < min || number > max)
                    throw new OverflowException();
                return decimal.ToInt64(number);
            }
            catch (Exception exception) when (exception is InvalidCastException || exception is FormatException || exception is OverflowException)
            {
                throw new ProtocolException("invalid_params", name + " is outside the permitted integer range.");
            }
        }

        private static void Only(IReadOnlyDictionary<string, object> args, params string[] allowed)
        {
            foreach (var key in args.Keys)
            {
                if (Array.IndexOf(allowed, key) < 0)
                    throw new ProtocolException("invalid_params", "Unknown parameter: " + key);
            }
        }

        private static Dictionary<string, object> Map(params object[] pairs)
        {
            var map = new Dictionary<string, object>(StringComparer.Ordinal);
            for (var index = 0; index < pairs.Length; index += 2)
                map.Add((string)pairs[index], pairs[index + 1]);
            return map;
        }

        private sealed class ProtocolException : Exception
        {
            internal ProtocolException(string code, string message) : base(message) { Code = code; }
            internal string Code { get; }
        }
    }
}
