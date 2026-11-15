using UnityEngine;

[CreateAssetMenu(fileName = "WateringCanMulchData", menuName = "Scriptable Objects/Mulch/Watering Can")]
public class WateringCanMulchData : MulchData
{
    [Tooltip("share of each plant's max health healed at the end of each wave, e.g. 0.15 = 15%")]
    public float healPercent = 0.15f;

    public override MulchEffect CreateEffect() => new WateringCanMulchEffect(this);

    public override string GetDescription() =>
        $"At the end of each wave, every plant heals <b><color=green>{healPercent * 100f:F0}%</color></b> of its <b><color=green>Max Health</color></b>.";
}
