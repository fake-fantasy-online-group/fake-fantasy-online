namespace FakeFantasyOnline.Application
{

/// <summary>Reached stores of the native counter-window receipt, in wire order.</summary>
public interface ICounterWindowReceiveOwner
{
    void StoreCounterCharges(int value);
    void StoreCounterTtlMilliseconds(int value);
    void StoreCounterSkillId(int value);
    void StampCounterWindowReceipt();
}

}
