namespace FakeFantasyOnline.Application
{
// The existing Buf supplies the native receipt's reads without an intermediate DTO.
public interface IWorldLifeSkillReceiptReader
{
    int U16();
    int I32();
    string Str();
    int U8();
}

public interface IWorldLifeSkillReceiptOwner
{
    void ReceiveLifeSkills(IWorldLifeSkillReceiptReader reader);
}
}
