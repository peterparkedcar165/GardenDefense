using UnityEngine;

[CreateAssetMenu(fileName = "DormancyMulchData", menuName = "Scriptable Objects/Mulch/Dormancy")]
public class DormancyMulchData : MulchData
{
    [Tooltip("how many whole waves the penalty lasts, counted from the first wave that starts after the pick")]
    public int dormantWaves = 3;
    [Tooltip("damage every plant loses during those waves, e.g. 0.15 = 15% less")]
    public float damagePenalty = 0.15f;
    [Tooltip("damage every plant gains after that, for the rest of the level, e.g. 0.25 = 25% more")]
    public float damageBonus = 0.25f;

    public override MulchEffect CreateEffect() => new DormancyMulchEffect(this);

    public override string GetDescription() =>
        $"For the next <b><color=green>{dormantWaves}</color></b> waves, all plants deal <b><color=red>{damagePenalty * 100f:F0}%</color></b> less damage. " +
        $"After that, they deal <b><color=green>{damageBonus * 100f:F0}%</color></b> more damage for the rest of the level.";
}
