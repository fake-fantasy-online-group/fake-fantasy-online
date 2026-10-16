// Production map lifecycle and doll binding run with deterministic rendering substitutes.
using FakeFantasyOnline.Presentation.ReferenceAssets;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public static partial class AppearanceRepro
{
    static void Check(bool value, string label)
    {
        if (!value) throw new System.Exception(label);
        System.Console.WriteLine("PASS " + label);
    }
    static void Equip(int seed)
    {
        CharData.SetAppearance(seed, seed + 1, seed + 2, seed + 3, seed + 4, seed + 5);
        CharData.atkVariant = seed + 6;
        CharData.weaponCType = seed + 7;
        CharData.atkDist = seed + 8;
        CharData.glowMode = seed + 9;
        CharData.glowUnlock = seed + 10;
    }
    static void CheckProjection(GameBootstrap boot, int seed, string label)
    {
        WorldEntryRecord built = boot.Builds[boot.Builds.Count - 1];
        CharacterAppearanceRecord a = built.Appearance;
        Check(a.Body == seed && a.Cap == seed + 1 && a.Backpack == seed + 2 &&
            a.RightWeapon == seed + 3 && a.LeftWeapon == seed + 4 && a.Face == seed + 5 &&
            a.AttackVariant == seed + 6 && a.WeaponCategory == seed + 7 && a.AttackDistance == seed + 8 &&
            built.GlowMode == seed + 9 && built.GlowUnlock == seed + 10 &&
            CharData.apBody == seed && CharData.apWeaponR == seed + 3, label);
    }
    public static void Main()
    {
        NetClient.SelfEntityId = 7;
        Equip(900);
        var boot = new GameBootstrap();
        boot.BuildWorld();
        Check(CharData.apBody == 0 && CharData.apWeaponR == 0,
            "initial title-login retains its own receipt appearance");
        Equip(120);
        boot.SwitchMapAuthoritative(2, 40, 50);
        boot.Drain();
        System.Console.WriteLine("POST_TRANSFER body=" + CharData.apBody + " weapon=" + CharData.apWeaponR);
        CheckProjection(boot, 120, "authoritative transfer retains six slots, attack fields and glow");
        Equip(230);
        boot.SwitchMap(3, 60, 70);
        boot.Drain();
        CheckProjection(boot, 230, "local portal retains later equipment changes");
        boot.SwitchMapAuthoritative(3, 80, 90);
        Equip(340); // A receipt arrives after transition start, before its yielding rebuild.
        boot.Drain();
        CheckProjection(boot, 340, "same-map rebuild captures receipt arriving during fade");
        Equip(450);
        boot.BuildOutcomes.Enqueue("throw");
        boot.SwitchMapAuthoritative(4, 80, 90);
        boot.Drain();
        CheckProjection(boot, 450, "failed destination and rollback retain current appearance");
        NetClient.SelfEntityId = 8;
        Equip(770);
        boot.SwitchMapAuthoritative(5, 80, 90);
        boot.Drain();
        CheckProjection(boot, 450, "mismatched character cannot replace entry appearance");
        NetClient.SelfEntityId = 7;
        CharData.SetAppearance(0, 0, 0, 0, 0, 0);
        boot.SwitchMapAuthoritative(6, 80, 90);
        boot.Drain();
        CharacterAppearanceRecord empty = boot.Builds[boot.Builds.Count - 1].Appearance;
        Check(empty.Body == 0 && empty.Cap == 0 && empty.Backpack == 0 && empty.RightWeapon == 0 &&
            empty.LeftWeapon == 0 && empty.Face == 0 && CharData.apBody == 0 && CharData.apWeaponR == 0,
            "unequipped zero slots stay empty instead of restoring old equipment");
    }
}
}
