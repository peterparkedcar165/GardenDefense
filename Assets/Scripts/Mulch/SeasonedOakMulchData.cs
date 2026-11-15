using UnityEngine;

[CreateAssetMenu(fileName = "SeasonedOakMulchData", menuName = "Scriptable Objects/Mulch/Seasoned Oak")]
public class SeasonedOakMulchData : MulchData
{
    [Tooltip("armor every acorn knight starts at, on top of his own")]
    public float startArmor = -30f;
    [Tooltip("armor gained each step")]
    public float armorPerStep = 1f;
    [Tooltip("seconds alive between steps")]
    public float stepInterval = 4f;
    [Tooltip("the climb stops here")]
    public float maxArmor = 30f;

    public override MulchEffect CreateEffect() => new SeasonedOakMulchEffect(this);

    public int MaxSteps => armorPerStep > 0f ? Mathf.CeilToInt((maxArmor - startArmor) / armorPerStep) : 0;
    public float ArmorAtStep(int steps) => Mathf.Min(maxArmor, startArmor + steps * armorPerStep);

    public override string GetDescription()
    {
        string armor = "<color=#00CED1><b>Armor</b></color>";
        return $"{TargetPlantLabel("Acorn Knight")}s start at <b><color=red>{startArmor:+0.#;-0.#;0}</color></b> {armor} and gain " +
            $"<b><color=green>+{armorPerStep:0.#}</color></b> {armor} every <b><color=green>{stepInterval:0.#}</color></b> seconds, " +
            $"up to <b><color=green>{maxArmor:+0.#;-0.#;0}</color></b>. Each keeps his progress through death and revival, and gains nothing while dead.";
    }
}
