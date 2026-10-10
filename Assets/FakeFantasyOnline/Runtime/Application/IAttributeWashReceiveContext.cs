using System.Collections.Generic;
namespace FakeFantasyOnline.Application
{
public interface IAttributeWashReceiveContext
{
    void ReceiveAttributeWashInfo(int kind, int tab, int slot, int itemId, int quality,
        List<string> affixes, List<bool> atMax);
}
}
