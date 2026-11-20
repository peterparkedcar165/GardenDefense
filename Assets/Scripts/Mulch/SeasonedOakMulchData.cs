using UnityEngine;

[CreateAssetMenu(fileName = "SeasonedOakMulch", menuName = "Scriptable Objects/Mulch/Seasoned Oak")]
public class SeasonedOakMulchData : MulchData
{
    [Tooltip("armor this plant starts at, on top of its own")]
    public TierValue startArmor = TierValue.Scaled(-30f);
    [Tooltip("armor gained each step")]
    public TierValue armorPerStep = TierValue.Scaled(1f);
    [Tooltip("seconds alive between steps")]
    public float stepInterval = 4f;
    [Tooltip("the climb stops here")]
    public TierValue maxArmor = TierValue.Scaled(30f);

    public override MulchEffect CreateEffect(MulchItem item) => new SeasonedOakMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        const string armor = "<color=#00CED1><b>Armor</b></color>";
        return $"This plant starts at {Red(startArmor.Get(r).ToString("+0.#;-0.#;0"))} {armor} and gains " +
            $"{Green("+" + armorPerStep.Get(r).ToString("0.#"))} {armor} every {Green(stepInterval.ToString("0.#"))} seconds, " +
            $"up to {Green(maxArmor.Get(r).ToString("+0.#;-0.#;0"))}. It keeps its progress through a merge, and through a revival that brings this mulch back.";
    }

    public int MaxSteps(MulchTier r)
    {
        float step = armorPerStep.Get(r);
        return step > 0f ? Mathf.CeilToInt((maxArmor.Get(r) - startArmor.Get(r)) / step) : 0;
    }

    public float ArmorAtStep(MulchTier r, int steps) => Mathf.Min(maxArmor.Get(r), startArmor.Get(r) + steps * armorPerStep.Get(r));
}
