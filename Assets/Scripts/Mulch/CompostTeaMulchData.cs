using UnityEngine;

[CreateAssetMenu(fileName = "CompostTeaMulchData", menuName = "Scriptable Objects/Mulch/Compost Tea")]
public class CompostTeaMulchData : MulchData
{
    [Tooltip("share of the killing plant's max health it heals per kill, e.g. 0.02 = 2%")]
    public float healPercentPerKill = 0.02f;

    public override MulchEffect CreateEffect() => new CompostTeaMulchEffect(this);

    public override string GetDescription() =>
        $"Whenever a plant kills an insect, it heals <b><color=green>{healPercentPerKill * 100f:F0}%</color></b> of its <b><color=green>Max Health</color></b>.";
}
