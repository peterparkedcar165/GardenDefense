using UnityEngine;

[CreateAssetMenu(fileName = "PrimerCooldownMulchData", menuName = "Scriptable Objects/Mulch/Primer Cooldown Reduction")]
public class PrimerCooldownMulchData : MulchData
{
    [Tooltip("e.g. 0.3 = every elemental Primer's internal cooldown is 30% shorter")]
    [Range(0f, 0.9f)] public float cooldownReductionPercent = 0.3f;

    public override MulchEffect CreateEffect() => new PrimerCooldownMulchEffect(this);
}
