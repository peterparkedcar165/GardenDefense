using UnityEngine;

[CreateAssetMenu(fileName = "SeasonedOakMulch", menuName = "Scriptable Objects/Mulch/Seasoned Oak")]
public class SeasonedOakMulchData : MulchData
{
    [Tooltip("armor this plant starts at, on top of its own")]
    public TierValue startArmor = new TierValue(-30f, -30f, -30f);
    [Tooltip("armor gained each step")]
    public TierValue armorPerStep = TierValue.Scaled(1f);
    [Tooltip("seconds alive between steps")]
    public float stepInterval = 8f;
    [Tooltip("the climb stops when the total bonus (the start plus everything gained) reaches this")]
    public TierValue maxArmor = new TierValue(30f, 45f, 60f);

    public override MulchEffect CreateEffect(MulchItem item) => new SeasonedOakMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        string armor = Kw("Armor", ArmorColor);
        float start = startArmor.Get(r);
        string opening = start < 0f ? $"Reduce {armor} by {Red((-start).ToString("0.#"))}, but gain"
            : start > 0f ? $"Increase {armor} by {Green(start.ToString("0.#"))}, and gain"
            : "Gain";
        return $"{opening} {Green(armorPerStep.Get(r).ToString("0.#"))} {armor} every {Green(stepInterval.ToString("0.#"))} seconds, " +
            $"until the total bonus reaches {Green(maxArmor.Get(r).ToString("0.#"))}.";
    }

    // armor it can gain at a rarity before the total bonus hits the cap
    public float MaxGain(MulchTier r) => Mathf.Max(0f, maxArmor.Get(r) - startArmor.Get(r));

    // the total bonus with this much armor gained so far
    public float ArmorWithGain(MulchTier r, float gained) => Mathf.Min(maxArmor.Get(r), startArmor.Get(r) + gained);
}
