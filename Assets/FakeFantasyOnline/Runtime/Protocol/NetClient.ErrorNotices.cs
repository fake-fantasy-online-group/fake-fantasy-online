namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Local queue metadata only. Transport worker errors belong to their connection
    // generation; address discovery notices are independent of authentication sessions.
    // Neither identity may be inferred from server-supplied text.
    private readonly struct QueuedNetworkError
    {
        internal readonly string Message;
        internal readonly bool IsAddressRetry;
        internal readonly int? Generation;

        internal QueuedNetworkError(string message, bool isAddressRetry = false, int? generation = null)
        {
            Message = message;
            IsAddressRetry = isAddressRetry;
            Generation = generation;
        }
    }
}
}
