using UnityEngine;

public class Snail : Insect
{
    protected override void Awake()
    {
        base.Awake();

        // TUNE: core stats
        baseMaxHealth       = 800f;
        baseAttackDamage    = 40f;
        baseAttackSpeed     = 0.5f;
        baseAttackRange     = 0.5f;
        baseMovementSpeed   = 0.5f;
        sunDrop             = 10;

        // Shell — high physical resistance
        baseTenacity           = 0.5f;
        basePhysicalResistance = 0.6f;   // TUNE

        // Moist body — resists water
        baseWaterResistance    = 0.5f;   // TUNE

        // Dries out in heat — takes extra fire damage
        baseFireResistance     = -0.3f;  // TUNE (negative = vulnerability)

        // Soft body absorbs toxins — takes extra poison damage
        basePoisonResistance   = -0.25f; // TUNE (negative = vulnerability)
    }

    public override string GetName() => "<b><color=#8B8B6B>Snail</color></b>";

    public override string GetDescription()
        => $"The {GetName()} is slow and armoured. Its shell resist physical attacks and shrugs off water, but its soft body burns quickly and absorbs poison with ease.";

    public override string GetPassiveDescription()
        => $"The {GetName()}'s shell grants high <color=white>Physical</color> resistance and <color=#4FC3F7>Water</color> resistance, but its moist body is vulnerable to <color=orange>Fire</color> and <color=purple>Poison</color>.";
}
