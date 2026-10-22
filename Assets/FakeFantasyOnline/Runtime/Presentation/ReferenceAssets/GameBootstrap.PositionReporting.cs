using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{
    private float _savePosTimer;
    private float _moveReportTimer;
    private bool _wasMoving;

    // Restores the diagnostic prefix before the existing art-clock, fade and reporting code.
    // Update remains partially recovered; this prefix does not establish a full owner pass.
    private void Update()
    {
        LoadTrace.Tick();
        ArtUploadClock.BeginFrame();
        UpdateTraceDiagnosticsForIndependentRuntime();
        UpdateFade();
        _savePosTimer += Time.deltaTime;
        if (_savePosTimer >= 8f)
        {
            _savePosTimer = 0f;
            ReportPos(false);
        }

        _moveReportTimer += Time.deltaTime;
        if (pcRef == null || pcRef.Root == null || session == null || !session.Connected)
        {
            return;
        }

        int x = (int)pcRef.WorldX;
        int y = (int)pcRef.WorldY;
        if (x == _lastReportX && y == _lastReportY)
        {
            if (_wasMoving)
            {
                _wasMoving = false;
                SendMovementForIndependentRuntime(x, y);
                _lastReportX = x;
                _lastReportY = y;
            }
        }
        else if (_moveReportTimer >= 0.3f)
        {
            _moveReportTimer = 0f;
            SendMovementForIndependentRuntime(x, y);
            _lastReportX = x;
            _lastReportY = y;
            _wasMoving = true;
        }
    }

    public static void FlushMoveReport() => FlushMoveReport(false);

    // A physical portal edge must publish even if Update reported the previous
    // position earlier in this frame. Other native callers keep their throttles.
    internal static void FlushMoveReport(bool force)
    {
        GameBootstrap owner = I;
        if (owner == null || owner.pcRef == null || owner.pcRef.Root == null ||
            owner.session == null || !owner.session.Connected)
        {
            return;
        }
        if (!force && owner._moveReportTimer < 0.06f)
        {
            return;
        }

        int x = (int)owner.pcRef.WorldX;
        int y = (int)owner.pcRef.WorldY;
        if (!force && owner._lastReportX != int.MinValue)
        {
            long dx = (long)x - owner._lastReportX;
            long dy = (long)y - owner._lastReportY;
            if (unchecked(dx * dx + dy * dy) < 576L)
            {
                return;
            }
        }

        owner._moveReportTimer = 0f;
        owner.SendMovementForIndependentRuntime(x, y);
        owner._lastReportX = x;
        owner._lastReportY = y;
        owner._wasMoving = true;
    }

    private async void SendMovementForIndependentRuntime(int x, int y)
    {
        if (positionReportLifetime == null)
        {
            return;
        }

        try
        {
            Task<Result> pending = session.ReportPlayerMoveAsync(x, y, positionReportLifetime.Token);
            window?.TrackPositionReportForIndependentRuntime(pending);
            await pending;
        }
        catch (System.OperationCanceledException)
        {
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    internal void CancelPositionReportsForIndependentRuntime()
    {
        positionReportLifetime?.Cancel();
    }
}

}
