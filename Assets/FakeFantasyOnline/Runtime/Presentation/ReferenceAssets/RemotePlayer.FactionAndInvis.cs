namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class RemotePlayer
{
    // 06000ae6 (arm64 0x2d88e4, emitted extent 8 bytes / x86_64 0x2e9b80, 16 bytes). The official
    // body is one store: `faction = f`, instance field +0x110 (atlas offset 272, System.Int32,
    // field token 0x04000ef0). Nothing else is touched - no name tag, no hostility or pickbox
    // refresh - and the incoming value is stored unchanged, including zero and negative values.
    public void SetFaction(int f) => faction = f;

    // 06000ae7 (arm64 0x2d88ec, emitted extent 8 bytes / x86_64 0x2e9b90, 16 bytes). One byte
    // store: `pkState = st`, instance field +0x11c (atlas offset 284, System.Byte, field token
    // 0x04000ef3), arm64 strb / x86_64 movb. No widening, no clamp and no second field.
    public void SetPkState(byte st) => pkState = st;

    // 06000afd (arm64 0x2da60c, 144 bytes / x86_64 0x2eb760, 80 bytes). The official body reads
    // this actor's id (instance field +0x20, System.Int32, field token 0x04000ec1) and returns
    // SocialClient.InParty(id) (0x06000d28) unchanged after the SocialClient class-init guard. It
    // never tests invis (+0x134) itself, so the caller decides whether the actor is invisible at
    // all: Update (0x06000b21) inlines this call exactly as `invis ? InParty(id) ^ 1 : 0`, and
    // get_HiddenByInvis (0x06000afe) is the same consult behind its own invis short-circuit.
    public bool InvisVisibleToMe() => SocialClient.InParty(id);
}

}
