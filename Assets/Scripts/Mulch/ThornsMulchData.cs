using UnityEngine;

[CreateAssetMenu(fileName = "ThornsMulch", menuName = "Scriptable Objects/Mulch/Thorns")]
public class ThornsMulchData : MulchData
{
    [Tooltip("true damage dealt back to an insect each time it hits this plant")]
    public TierValue damage = TierValue.Scaled(8f);

    public override MulchEffect CreateEffect(MulchItem item) => new ThornsMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        return $"Insects that hit this plant take {Green(damage.Get(r).ToString("0"))} <b>True Damage</b>.";
    }
}
