using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal sealed partial class TitleFlow
{
    // Existing private deployment input, supplied by the same view during this
    // component's Awake publication; no new endpoint or request activation.
    internal string _changelogEndpoint;

    // Official 1.5.8 TitleFlow.FetchChangelog (06000ecb): allocation/capture only; no request yet.
    internal IEnumerator FetchChangelog()
    {
        return new FetchChangelogEnumerator(0) { Owner = this };
    }

    // Explicit ordinary C# carrier for the complete generated <FetchChangelog>d__116 family.
    // Keeping its fields and state transitions here preserves retained Current, fresh Dispose,
    // exceptional cleanup and post-finally clearing without depending on compiler lowering.
    private sealed class FetchChangelogEnumerator : IEnumerator<object>
    {
        private int _state;
        private object _current;
        internal TitleFlow Owner;
        private UnityWebRequest _request;

        // 06000f10: Object construction and state assignment; references use managed defaults.
        internal FetchChangelogEnumerator(int state)
        {
            _state = state;
        }

        // 06000f14 / 06000f16: no position check and no clearing on completion/disposal.
        public object Current => _current;
        object IEnumerator.Current => _current;

        // 06000f12, including the generated outer fault cleanup.
        public bool MoveNext()
        {
            try
            {
                switch (_state)
                {
                    case 0:
                        _state = -1;
                        if (string.IsNullOrEmpty(Owner._changelogEndpoint)) return false;
                        _request = UnityWebRequest.Get(Owner._changelogEndpoint);
                        _state = -3;
                        _request.timeout = 6;
                        _current = _request.SendWebRequest();
                        _state = 1;
                        return true;

                    case 1:
                        TitleFlow owner = Owner;
                        _state = -3;
                        if (_request.result == UnityWebRequest.Result.Success)
                        {
                            try
                            {
                                Changelog parsed =
                                    JsonUtility.FromJson<Changelog>(_request.downloadHandler.text);
                                if (parsed != null && parsed.notes != null && parsed.notes.Length != 0)
                                    owner._changelog = parsed;
                            }
                            catch
                            {
                                // The original bare managed catch includes the first access,
                                // getter, parse and conditional title assignment only.
                            }

                            // Deliberately read text again, even after rejection or a swallowed
                            // parse-region error. This getter/publication is outside that catch.
                            NoticeBoard.PublishJson(_request.downloadHandler.text);
                        }

                        DisposeRequestFinally();
                        _request = null;
                        return false;

                    default:
                        return false;
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        // 06000f11: fresh state 0 and completed state -1 are unchanged. A cleanup failure leaves
        // state -1, so outer fault handling and later Dispose cannot attempt disposal a second time.
        public void Dispose()
        {
            if (_state == 1 || _state == -3)
                DisposeRequestFinally();
        }

        // 06000f13: unconditional state write, interface disposal, no retained-reference clear.
        private void DisposeRequestFinally()
        {
            _state = -1;
            if (_request != null)
                ((IDisposable)_request).Dispose();
        }

        // 06000f15.
        public void Reset()
        {
            throw new NotSupportedException();
        }
    }
}
}
