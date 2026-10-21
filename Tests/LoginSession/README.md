# Login session regression

Run with a compatible Mono runtime, Roslyn compiler, and managed .NET 4.7.2 reference assemblies:

```sh
export MONO_CFG_DIR=/tmp/rack-mono/etc
export LD_LIBRARY_PATH=/tmp/rack-mono/usr/lib
export FFO_MONO_ROOT=/tmp/rack-mono/usr
export FFO_CSC=/tmp/ffo-roslyn/tasks/net472/csc.exe
export FFO_FRAMEWORK_DIR=/tmp/rack-mono/usr/lib/mono/4.7.2-api
bash Tests/LoginSession/run-contract.sh
```

The script recompiles the complete production Foundation, Domain, Application, Protocol, and PluginApi assemblies before tests. Output defaults to the ignored `Tests/LoginSession/out` directory. The loopback listener binds an ephemeral port; no external service or credentials are used.

## Coverage and limits

`LoginSocketContract` uses the actual production `PrivateServerGateway`, `ClientSessionCoordinator`, `NetClient`, `SynchronizationContextHostExecutor`, encryption, transmit/receive threads and TCP sockets. The test context supplies a host-thread queue; trace/clock are host capabilities. No gateway, transport, login serializer, coordinator or crypto implementation is replaced.

It checks:

- Account A authenticates via encrypted TCP receipt, production GUI-return coordinator method closes the connection, account B authenticates on new TCP/lifetime/AES/MAC/TX queue.
- Same-account rejection retry and already-successful retry open fresh sockets.
- Duplicate GUI admission/send does not steal an active login.
- A real old success packet already enqueued by Rx before timeout is cleared and never reaches the new login.
- Retired Rx/Tx generation errors enqueued before cancellation or after the replacement login cannot reach its error consumer; current-generation and untagged errors still deliver, and resolved address-retry suppression is unchanged.
- Native worker completion published before cancellation but not yet pumped cannot invoke the retired callback.
- Task connect-to-authenticate ownership excludes GUI, Task authentication excludes a second Task connect, and GUI excludes Task without closing the live socket.
- Cancellation after Task connect completion releases ownership on the next GUI attempt.
- Actual Task authentication succeeds and commits Authenticated.
- Ordinary same-server character selection and world-return writer retain the authenticated socket.

The fixture reads the actual RSA handshake on the server side, but does not decrypt/validate it with a production private key. It observes the generated symmetric keys through reflection to encrypt replies and decrypt actual client Login frames. The retired-error test additionally reflects the private error metadata constructor and enqueues synthetic generation-tagged errors, making the late-worker enqueue schedule deterministic. This validates production filtering/retirement, not a forced real OS Rx/Tx race. Reflection does not inject TCP, keys, stages or authentication success. This is a managed production transport integration test, **not** a real game-server/database/password-validation end-to-end test. World return coverage checks its production writer and socket preservation, not the complete world-to-roster asynchronous handshake.

`TitleFlowContract` compiles the complete production `TitleFlow.AccountConnections.cs` and `TitleFlow.NativeGui.cs` source files. It executes the actual BeginLogin, DrawTitleGui timeout, login receipt, and network-error paths. It checks duplicate clicks, elapsed-time timeout and cancellation, stale callback isolation after retry, captured credentials, successful page transition, ordinary failure, and network-error retirement. Unity rendering, remaining UI partials and presentation NetClient are test doubles. This is explicitly a production state-machine regression, **not** a Unity executable or rendered GUI end-to-end test.

The NativeNetClientHost composition wiring itself is outside these two executables. Review that binding separately or run the existing host contract.
