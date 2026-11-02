using UnityEngine;

[CreateAssetMenu(fileName = "IronbarkCounterMulchData", menuName = "Scriptable Objects/Mulch/Ironbark Counter")]
// Ironclad Riposte: a flat Counter Damage bonus for every plant, plus a small shield whenever
// that Counter Damage actually lands. class name kept from when it was Ironbark only
public class IronbarkCounterMulchData : MulchData
{
    [Tooltip("additive Counter Damage, e.g. 0.25 = +25%")]
    public float counterDamageBonus = 0.25f;
    [Tooltip("shield granted to the countering plant, as a percent of the Counter Damage hit's own damage")]
    public float shieldPercentOfDamage = 0.05f;
    [Tooltip("how long the granted shield lasts before expiring unused")]
    public float shieldDuration = 4f;

    public override MulchEffect CreateEffect() => new IronbarkCounterMulchEffect(this);

    public override string GetDescription() =>
        $"Increase Counter Damage by <b><color=green>{counterDamageBonus * 100f:F0}%</color></b>. " +
        $"Dealing Counter Damage shields the plant for <b><color=green>{shieldPercentOfDamage * 100f:F0}%</color></b> of total damage dealt.";
}
