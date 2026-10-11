using UnityEngine;

// applied to Snowdrop's bonded partner for as long as the Snow Bond lasts. at Path3 max,
// while the Ice Beam skill is active, grants the bonded plant bonus Attack Speed - toggled
// every tick rather than fixed, since both IsPath3Maxed and IceBeamActive can change while
// still bonded. mirrors SoilBondEffect's toggle pattern
public class SnowBondHasteEffect : StatusEffect
{
    private readonly Snowdrop snowdrop;
    private bool hasteActive;

    public SnowBondHasteEffect(Entity target, Entity source, Snowdrop snowdrop)
        : base(target, float.MaxValue, 1, source)
    {
        this.snowdrop = snowdrop;
        effectType = Type.positive;
        elementalType = ElementalType.Ice;
        sourceStackable = true;
    }

    public override void OnApply() { }

    public override void OnTick(float deltaTime)
    {
        if (snowdrop == null || !snowdrop.IsAlive)
        {
            duration = 0f;
            return;
        }

        bool shouldBeActive = snowdrop.IsPath3Maxed && snowdrop.IceBeamActive;
        if (shouldBeActive != hasteActive)
        {
            hasteActive = shouldBeActive;
            target.attackSpeedMultiplier += hasteActive ? snowdrop.PartnerHasteBonus : -snowdrop.PartnerHasteBonus;
        }
    }

    public override void OnExpire()
    {
        if (hasteActive) target.attackSpeedMultiplier -= snowdrop.PartnerHasteBonus;
    }

    public override string GetName() => "<color=#00FFFF><b>Snow Bond</b></color>";
    public override string GetDescription()
    {
        string s = $"Bonded to {(snowdrop != null ? snowdrop.GetName() : "a Snowdrop")}.";
        if (hasteActive) s += $"\nIce Beam active: gains <color=green><b>+{snowdrop.PartnerHasteBonus * 100f:F0}%</b></color> Attack Speed.";
        return s;
    }
}
