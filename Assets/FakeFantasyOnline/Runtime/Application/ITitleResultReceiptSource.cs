using System;

namespace FakeFantasyOnline.Application
{
// Composition over the original two mutable slots; binding never replays a response.
public interface ITitleResultReceiptSource
{
    void BindLoginResultReceipt(Action<int, string> receive);
    void BindCreateResultReceipt(Action<int, string> receive);
}
}
