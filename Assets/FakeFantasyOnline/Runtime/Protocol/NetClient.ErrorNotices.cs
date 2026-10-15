namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Local queue metadata only; ordinary transport errors and their ordering are
    // unchanged. A retry's identity must never be inferred from server-supplied text.
    private readonly struct QueuedNetworkError
    {
        internal readonly string Message;
        internal readonly bool IsAddressRetry;

        internal QueuedNetworkError(string message, bool isAddressRetry = false)
        {
            Message = message;
            IsAddressRetry = isAddressRetry;
        }
    }
}
}
