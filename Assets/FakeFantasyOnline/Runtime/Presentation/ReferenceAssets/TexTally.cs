using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Official 1.5.8 TexTally<TId>, tokens 06000b60-06000b64. It accumulates the
// bytes and the distinct id count of one reporting segment and renders them as
// a short diagnostic line; an id seen again inside the same segment is counted
// as shared instead of being added twice.
public class TexTally<TId>
{
    private readonly HashSet<TId> _seen = new HashSet<TId>();

    public long TotalBytes;

    public int TotalCount;

    private long _cur;

    private int _curN;

    private int _curShared;

    public int SharedInSegment => _curShared;

    // 06000b60
    public void Begin()
    {
        _cur = 0L;
        _curN = 0;
        _curShared = 0;
    }

    // 06000b61
    public void AddRaw(TId id, long bytes)
    {
        if (_seen.Add(id))
        {
            _cur += bytes;
            _curN++;
        }
        else
        {
            _curShared++;
        }
    }

    // 06000b62
    public string End(string owner)
    {
        TotalBytes += _cur;
        TotalCount += _curN;

        string text = string.Format("{0} {1}张/{2:F1}MB", owner, _curN, _cur / 1048576f);
        return text + (_curShared > 0 ? string.Format("(+共享{0})", _curShared) : string.Empty);
    }
}
}
