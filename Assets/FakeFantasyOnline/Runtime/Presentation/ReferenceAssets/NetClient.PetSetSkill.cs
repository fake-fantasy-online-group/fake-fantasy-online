using System;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    // Official 0x06000736, instance System.Void NetClient.PetSetSkill(System.Int32).
    //
    // arm64 rva 0x26e828 (100 bytes) and x86_64 rva 0x27dda0 (80 bytes) are one tail call after
    // the metadata-usage guard:
    //   NetClient.PetCmd(this, 4, skillId, "")
    // The frozen exports render it as `PetCmd(param_1, 4, param_2, metadata_usage_10335_kind5)`.
    // The only official caller, the right-click arm of Win05.DrawPet 0x06001043, passes the
    // literal 0 there (arm64 0x3bb6fc / x86_64 0x3cf1d6, one direct call edge per slice), so the
    // subcommand-4 request is the unselect of the row that is already selected; the Int32 travels
    // unchanged and no value, range or pet identity is decided here.
    //
    // PetCmd itself is a recovered Protocol member since WBD-079
    // (Assets/FakeFantasyOnline/Runtime/Protocol/NetClient.PetCommands.cs): the writer
    // <PetCmd>b__0 0x06000829 runs inside NetClient.Send 0x06000772 after its connected test, so
    // nothing is serialized while the socket is down. This facet writes no wire: the bound writer
    // installed by ReferenceCharacterSkillUsePort over
    // ClientSessionCoordinator.SetPetSkillAsync reaches PrivateServerGateway.SetPetSkillAsync,
    // which invokes the official member inside the single send gate. NetClient.PetUseSkill
    // 0x06000737 shares this port and, since WBD-124, this route as well - until then its gateway
    // arm handed over the codec's pre-encoded bytes.
    private Action<int> writePetSetSkill;

    internal void BindPetSetSkillWriterForIndependentRuntime(Action<int> writer) =>
        writePetSetSkill = writer;

    public void PetSetSkillForIndependentRuntime(int skillId) => writePetSetSkill(skillId);
}
}
