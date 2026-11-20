using UnityEngine;

[CreateAssetMenu(fileName = "EchoSeedMulch", menuName = "Scriptable Objects/Mulch/Echo Seed")]
public class EchoSeedMulchData : MulchData
{
    [Tooltip("every this many attacks, the attack's on hit effects land twice")]
    public TierValue everyAttacks = new TierValue(4f, 3f, 2f);

    public override MulchEffect CreateEffect(MulchItem item) => new EchoSeedMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        int n = Mathf.Max(1, everyAttacks.GetInt(r));
        return $"Every {Green(Ordinal(n))} attack of this plant applies its on hit effects <b>twice</b>: every debuff that attack puts on an insect " +
            $"lands a second time (an extra stack, or a refresh). Primers don't double.";
    }

    private static string Ordinal(int n) => n switch { 1 => "", 2 => "2nd", 3 => "3rd", _ => n + "th" };
}
