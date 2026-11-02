using UnityEngine;

[CreateAssetMenu(fileName = "SlowBonusMulchData", menuName = "Scriptable Objects/Mulch/Slow Bonus")]
public class SlowBonusMulchData : MulchData
{
    [Tooltip("flat extra Movement Speed reduction added to every slowing effect, e.g. 0.1 = +10%")]
    public float flatBonus = 0.1f;

    public override MulchEffect CreateEffect() => new SlowBonusMulchEffect(this);

    public override string GetDescription() =>
        $"Increase every slowing effect's Movement Speed reduction by a flat <b><color=green>{flatBonus:F2}</color></b>.";
}
