using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.PluginApi;

namespace FakeFantasyOnline.PluginHost
{
    /// <summary>Bounded limits for the explicitly enabled, trusted-local-process bridge.</summary>
    public sealed class GameAgentServerOptions
    {
        public GameAgentServerOptions(int maxClients = 8, int maxPendingRequestsPerClient = 8,
            int maxRequestBytes = 65536, int maxResponseBytes = 1048576,
            int idleTimeoutMilliseconds = 120000, int dispatchTimeoutMilliseconds = 10000,
            int sendTimeoutMilliseconds = 5000)
        {
            if (maxClients < 1 || maxClients > 64) throw new ArgumentOutOfRangeException(nameof(maxClients));
            if (maxPendingRequestsPerClient < 1 || maxPendingRequestsPerClient > 128)
                throw new ArgumentOutOfRangeException(nameof(maxPendingRequestsPerClient));
            if (maxRequestBytes < 256 || maxRequestBytes > 1048576) throw new ArgumentOutOfRangeException(nameof(maxRequestBytes));
            if (maxResponseBytes < 256 || maxResponseBytes > 4194304) throw new ArgumentOutOfRangeException(nameof(maxResponseBytes));
            if (idleTimeoutMilliseconds < 100 || idleTimeoutMilliseconds > 600000)
                throw new ArgumentOutOfRangeException(nameof(idleTimeoutMilliseconds));
            if (dispatchTimeoutMilliseconds < 100 || dispatchTimeoutMilliseconds > 60000)
                throw new ArgumentOutOfRangeException(nameof(dispatchTimeoutMilliseconds));
            if (sendTimeoutMilliseconds < 100 || sendTimeoutMilliseconds > 60000)
                throw new ArgumentOutOfRangeException(nameof(sendTimeoutMilliseconds));
            MaxClients = maxClients;
            MaxPendingRequestsPerClient = maxPendingRequestsPerClient;
            MaxRequestBytes = maxRequestBytes;
            MaxResponseBytes = maxResponseBytes;
            IdleTimeoutMilliseconds = idleTimeoutMilliseconds;
            DispatchTimeoutMilliseconds = dispatchTimeoutMilliseconds;
            SendTimeoutMilliseconds = sendTimeoutMilliseconds;
        }
        public int MaxClients { get; }
        public int MaxPendingRequestsPerClient { get; }
        public int MaxRequestBytes { get; }
        public int MaxResponseBytes { get; }
        public int IdleTimeoutMilliseconds { get; }
        public int DispatchTimeoutMilliseconds { get; }
        public int SendTimeoutMilliseconds { get; }
    }

    /// <summary>
    /// UTF-8 newline-delimited JSON over IPv4 loopback. Construction never opens a listener.
    /// Start is opt-in; Stop cancels unexecuted dispatches and closes every socket.
    /// </summary>
    public sealed class GameAgentServer : IDisposable
    {
        private readonly object gate = new object();
        private readonly GameAgentProtocol protocol;
        private readonly MainThreadGameDispatcher dispatcher;
        private readonly GameAgentServerOptions options;
        private readonly int requestedPort;
        private RunState running;
        private bool disposed;
        private string lastError;

        public GameAgentServer(IGameApi api, MainThreadGameDispatcher dispatcher, int port)
            : this(api, dispatcher, port, new GameAgentServerOptions()) { }

        public GameAgentServer(IGameApi api, MainThreadGameDispatcher dispatcher, int port, GameAgentServerOptions options)
        {
            if (port < 0 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port));
            protocol = new GameAgentProtocol(api ?? throw new ArgumentNullException(nameof(api)));
            this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            requestedPort = port;
        }

        public bool IsRunning { get { lock (gate) return running != null; } }
        /// <summary>The active port, or configured port when stopped. Port zero selects an ephemeral port on Start.</summary>
        public int Port { get { lock (gate) return running == null ? requestedPort : running.Port; } }
        public int ConnectedClients { get { lock (gate) return running == null ? 0 : running.Clients.Count; } }
        public string LastError { get { lock (gate) return lastError; } }

        public void Start()
        {
            lock (gate)
            {
                if (disposed) throw new ObjectDisposedException(nameof(GameAgentServer));
                if (running != null) return;
                var listener = new TcpListener(IPAddress.Loopback, requestedPort);
                try
                {
                    listener.Start(options.MaxClients);
                    var state = new RunState(listener);
                    running = state;
                    lastError = null;
                    state.AcceptTask = Task.Run(() => AcceptLoop(state));
                }
                catch (Exception error) { listener.Stop(); lastError = error.Message; throw; }
            }
        }

        public void Stop()
        {
            RunState state;
            ClientSession[] clients;
            lock (gate)
            {
                state = running;
                if (state == null) return;
                running = null;
                clients = new List<ClientSession>(state.Clients).ToArray();
            }
            state.Cancellation.Cancel();
            state.Listener.Stop();
            foreach (var client in clients) client.Close();
            // No task waits on a game action once its queued dispatcher token is cancelled.
            // Socket disposal interrupts blocking reads/writes, including a peer which stops reading.
            var tasks = new List<Task> { state.AcceptTask };
            foreach (var client in clients) tasks.Add(client.Completion);
            try { Task.WaitAll(tasks.ToArray(), 2000); }
            catch (AggregateException) { }
            // Keep the source alive until all workers have stopped using its token.
            ObserveCompletion(tasks, state);
        }

        private static void ObserveCompletion(List<Task> tasks, RunState state)
        {
            Task.WhenAll(tasks).ContinueWith(completed =>
            {
                // Observe worker faults before releasing the run's shared cancellation source.
                var ignored = completed.Exception;
                state.Cancellation.Dispose();
            }, TaskScheduler.Default);
        }

        public void Dispose()
        {
            lock (gate)
            {
                if (disposed) return;
                disposed = true;
            }
            Stop();
        }

        private async Task AcceptLoop(RunState state)
        {
            try
            {
                while (!state.Cancellation.IsCancellationRequested)
                {
                    var socket = await state.Listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    lock (gate)
                    {
                        if (!ReferenceEquals(running, state) || state.Clients.Count >= options.MaxClients)
                        {
                            // Closing immediately keeps admission bounded even when main-thread pumping stops.
                            socket.Close();
                            continue;
                        }
                        ClientSession session;
                        try { session = new ClientSession(socket, protocol, dispatcher, options, state.Cancellation.Token); }
                        catch (Exception error) when (error is IOException || error is SocketException || error is ObjectDisposedException)
                        { socket.Close(); continue; }
                        state.Clients.Add(session);
                        session.Start();
                        _ = session.Completion.ContinueWith(_ =>
                        {
                            lock (gate) state.Clients.Remove(session);
                        }, TaskScheduler.Default);
                    }
                }
            }
            catch (Exception error)
            {
                ClientSession[] clients;
                lock (gate)
                {
                    if (!ReferenceEquals(running, state)) return; // A normal Stop already owns cleanup.
                    lastError = error.Message;
                    running = null;
                    clients = new List<ClientSession>(state.Clients).ToArray();
                }
                state.Cancellation.Cancel();
                state.Listener.Stop();
                var tasks = new List<Task> { state.AcceptTask };
                foreach (var client in clients) { client.Close(); tasks.Add(client.Completion); }
                ObserveCompletion(tasks, state);
            }
        }

        private sealed class RunState
        {
            internal RunState(TcpListener listener)
            {
                Listener = listener;
                Port = ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            internal readonly TcpListener Listener;
            internal readonly int Port;
            internal readonly CancellationTokenSource Cancellation = new CancellationTokenSource();
            internal readonly HashSet<ClientSession> Clients = new HashSet<ClientSession>();
            internal Task AcceptTask;
        }

        private sealed class ClientSession
        {
            private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
            private readonly object queueGate = new object();
            private readonly object writeGate = new object();
            private readonly Queue<byte[]> queue = new Queue<byte[]>();
            private readonly SemaphoreSlim ready = new SemaphoreSlim(0);
            private readonly TcpClient socket;
            private readonly NetworkStream stream;
            private readonly GameAgentProtocol protocol;
            private readonly MainThreadGameDispatcher dispatcher;
            private readonly GameAgentServerOptions options;
            private readonly CancellationTokenSource cancellation;
            private int closed;

            internal ClientSession(TcpClient socket, GameAgentProtocol protocol, MainThreadGameDispatcher dispatcher,
                GameAgentServerOptions options, CancellationToken stop)
            {
                this.socket = socket;
                this.protocol = protocol;
                this.dispatcher = dispatcher;
                this.options = options;
                socket.NoDelay = true;
                socket.ReceiveTimeout = options.IdleTimeoutMilliseconds;
                socket.SendTimeout = options.SendTimeoutMilliseconds;
                stream = socket.GetStream();
                cancellation = CancellationTokenSource.CreateLinkedTokenSource(stop);
            }

            internal Task Completion { get; private set; }

            internal void Start()
            {
                var reader = Task.Run((Action)ReadLoop);
                var processor = Task.Run((Func<Task>)ProcessLoop);
                Completion = Task.WhenAll(reader, processor).ContinueWith(completed =>
                {
                    var ignored = completed.Exception;
                    Close();
                    ready.Dispose();
                    cancellation.Dispose();
                }, TaskScheduler.Default);
            }

            internal void Close()
            {
                if (Interlocked.Exchange(ref closed, 1) != 0) return;
                cancellation.Cancel();
                socket.Close();
                lock (queueGate) { foreach (var bytes in queue) Array.Clear(bytes, 0, bytes.Length); queue.Clear(); }
            }

            private void ReadLoop()
            {
                var buffer = new byte[4096];
                var frame = new MemoryStream();
                try
                {
                    using (frame)
                    {
                        while (!cancellation.IsCancellationRequested)
                        {
                            var count = stream.Read(buffer, 0, buffer.Length);
                            if (count == 0) break;
                            for (var index = 0; index < count; index++)
                            {
                                if (buffer[index] == 10)
                                {
                                    var bytes = frame.ToArray();
                                    Array.Clear(frame.GetBuffer(), 0, (int)frame.Length);
                                    frame.SetLength(0);
                                    lock (queueGate)
                                    {
                                        if (cancellation.IsCancellationRequested) { Array.Clear(bytes, 0, bytes.Length); return; }
                                        if (queue.Count >= options.MaxPendingRequestsPerClient)
                                        {
                                            Array.Clear(bytes, 0, bytes.Length);
                                            Write(GameAgentProtocol.Error(null, "queue_full", "Too many pending requests; reconnect after reducing request rate."));
                                            return;
                                        }
                                        queue.Enqueue(bytes);
                                    }
                                    ready.Release();
                                }
                                else
                                {
                                    if (frame.Length >= options.MaxRequestBytes)
                                    {
                                        Write(GameAgentProtocol.Error(null, "frame_too_large", "Request exceeds the byte limit."));
                                        return;
                                    }
                                    frame.WriteByte(buffer[index]);
                                }
                            }
                            Array.Clear(buffer, 0, count);
                        }
                        Array.Clear(frame.GetBuffer(), 0, (int)frame.Length);
                    }
                }
                catch (IOException) { }
                catch (SocketException) { }
                catch (ObjectDisposedException) { }
                finally
                {
                    Array.Clear(buffer, 0, buffer.Length);
                    var storage = frame.GetBuffer(); // MemoryStream exposes its buffer even after disposal.
                    Array.Clear(storage, 0, storage.Length);
                    Close();
                }
            }

            private async Task ProcessLoop()
            {
                try
                {
                    while (!cancellation.IsCancellationRequested)
                    {
                        await ready.WaitAsync(cancellation.Token).ConfigureAwait(false);
                        byte[] bytes;
                        lock (queueGate)
                        {
                            if (queue.Count == 0) continue;
                            bytes = queue.Dequeue();
                        }
                        object request = null;
                        string id = null;
                        try
                        {
                            var length = bytes.Length;
                            if (length > 0 && bytes[length - 1] == 13) length--;
                            request = GameJson.Parse(Utf8.GetString(bytes, 0, length));
                            var map = request as IReadOnlyDictionary<string, object>;
                            object requestId;
                            if (map != null && map.TryGetValue("id", out requestId))
                            {
                                var candidate = requestId as string;
                                if (!string.IsNullOrWhiteSpace(candidate) && candidate.Length <= 128) id = candidate;
                            }
                        }
                        catch (Exception exception) when (exception is FormatException || exception is DecoderFallbackException || exception is ArgumentException)
                        {
                            Write(GameAgentProtocol.Error(null, "invalid_json", "Expected one UTF-8 JSON object per line."));
                            continue;
                        }
                        finally { Array.Clear(bytes, 0, bytes.Length); }
                        object response;
                        using (var dispatch = CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token))
                        {
                            dispatch.CancelAfter(options.DispatchTimeoutMilliseconds);
                            try
                            {
                                response = await dispatcher.InvokeAsync(() => protocol.Handle(request), dispatch.Token).ConfigureAwait(false);
                            }
                            catch (OperationCanceledException)
                            {
                                if (cancellation.IsCancellationRequested) break;
                                response = GameAgentProtocol.Error(id, "dispatch_timeout", "The request expired before game-thread execution. Retry act only with the same requestId.");
                            }
                            catch (ObjectDisposedException)
                            {
                                response = GameAgentProtocol.Error(id, "server_stopped", "The game dispatcher has stopped.");
                            }
                            catch (InvalidOperationException)
                            {
                                response = GameAgentProtocol.Error(id, "queue_full", "The game-thread queue is full. Retry act only with the same requestId.");
                            }
                            catch (Exception)
                            {
                                response = GameAgentProtocol.Error(id, "api_error", "The game dispatcher could not complete this request.");
                            }
                            finally
                            {
                                // Drop all credential references after execution, cancellation, or timeout.
                                // Managed JSON strings cannot be guaranteed zeroed in memory.
                                if (GameAgentProtocol.IsSensitiveLogin(request))
                                {
                                    var map = request as IDictionary<string, object>;
                                    object args;
                                    if (map != null && map.TryGetValue("params", out args))
                                    {
                                        var fields = args as IDictionary<string, object>;
                                        if (fields != null) fields.Clear();
                                    }
                                    if (map != null) map.Clear();
                                }
                                request = null;
                            }
                        }
                        if (!cancellation.IsCancellationRequested) Write(response);
                    }
                }
                catch (OperationCanceledException) { }
                catch (IOException) { }
                catch (SocketException) { }
                catch (ObjectDisposedException) { }
                finally { Close(); }
            }

            private void Write(object value)
            {
                byte[] bytes;
                try
                {
                    bytes = Utf8.GetBytes(GameJson.Serialize(value) + "\n");
                    if (bytes.Length - 1 > options.MaxResponseBytes)
                        bytes = Utf8.GetBytes(GameJson.Serialize(GameAgentProtocol.Error(ResponseId(value),
                            "response_too_large", "Response exceeds the byte limit; request a smaller observation or event batch.")) + "\n");
                }
                catch (Exception exception) when (exception is ArgumentException || exception is FormatException || exception is InvalidOperationException)
                {
                    bytes = Utf8.GetBytes(GameJson.Serialize(GameAgentProtocol.Error(ResponseId(value),
                        "response_too_large", "Response could not be encoded within protocol limits; request fewer items.")) + "\n");
                }
                if (bytes.Length - 1 > options.MaxResponseBytes)
                    bytes = Utf8.GetBytes(GameJson.Serialize(GameAgentProtocol.Error(null,
                        "response_too_large", "Response exceeds the byte limit.")) + "\n");
                lock (writeGate)
                {
                    if (cancellation.IsCancellationRequested) return;
                    stream.Write(bytes, 0, bytes.Length);
                }
            }

            private static string ResponseId(object value)
            {
                var map = value as IReadOnlyDictionary<string, object>;
                object id;
                return map != null && map.TryGetValue("id", out id) ? id as string : null;
            }
        }
    }
}
