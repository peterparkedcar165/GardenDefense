using UnityEngine;

[CreateAssetMenu(fileName = "ThornsMulch", menuName = "Scriptable Objects/Mulch/Thorns")]
public class ThornsMulchData : MulchData
{
    [Tooltip("true neutral damage dealt back to an insect each time its physical melee attack hits this plant")]
    public TierValue damage = TierValue.Scaled(8f);

    public override MulchEffect CreateEffect(MulchItem item) => new ThornsMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        return $"Taking <b><color={PhysicalColor}>Physical</color> <color={MeleeColor}>Melee Damage</color></b> returns " +
            $"{Green(damage.Get(r).ToString("0"))} <b><color={TrueColor}>True</color> <color={PlantData.ElementalColor(ElementalType.Neutral)}>Neutral Damage</color></b>.";
    }
}
