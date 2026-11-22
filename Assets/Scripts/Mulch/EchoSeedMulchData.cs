using UnityEngine;

[CreateAssetMenu(fileName = "EchoSeedMulch", menuName = "Scriptable Objects/Mulch/Echo Seed")]
public class EchoSeedMulchData : MulchData
{
    [Tooltip("every this many attacks, the attack's on hit effects land twice")]
    public TierValue everyAttacks = new TierValue(4f, 3f, 2f);
    [Tooltip("seconds after the attack that its on hit effects that aren't debuffs (waterlily's splash) happen again. debuffs land again at once")]
    public float repeatDelay = 0.2f;

    public override MulchEffect CreateEffect(MulchItem item) => new EchoSeedMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        int n = Mathf.Max(1, everyAttacks.GetInt(r));
        return $"Every {Green(n.ToString())} {Kw("Attacks", "green")}, {Kw("On-Hit Effects", OnHitColor)} apply twice.";
    }
}
