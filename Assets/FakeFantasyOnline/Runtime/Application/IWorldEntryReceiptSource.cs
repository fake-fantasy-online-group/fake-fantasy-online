using System;

namespace FakeFantasyOnline.Application
{
public interface IWorldEntryReceiptSource
{
    void BindWorldEntryReceipt(Action<WorldEntryRecord> receive);
}

// A real title receipt has a prefix before native state publication and a continuation
// after it. Keep both portions bound to one callback without changing legacy gateways.
public interface IWorldEntryPrefixReceiptSource : IWorldEntryReceiptSource
{
    void BindWorldEntryReceipt(Action<WorldEntryRecord> receive, Action begin);
}
}
