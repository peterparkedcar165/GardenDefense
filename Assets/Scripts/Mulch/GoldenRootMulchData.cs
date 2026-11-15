using UnityEngine;

[CreateAssetMenu(fileName = "GoldenRootMulchData", menuName = "Scriptable Objects/Mulch/Golden Root")]
public class GoldenRootMulchData : MulchData
{
    [Tooltip("armor the healed plant gets")]
    public float armorBonus = 5f;
    [Tooltip("seconds the armor lasts. another heal refreshes it rather than stacking")]
    public float duration = 4f;

    public override MulchEffect CreateEffect() => new GoldenRootMulchEffect(this);

    public override string GetDescription() =>
        $"{TargetPlantLabel("Rhodiola")}'s heals also give the healed plant <b><color=green>+{armorBonus:0.#}</color></b> " +
        $"<color=#00CED1><b>Armor</b></color> for <b><color=green>{duration:0.#}</color></b> seconds.";
}
