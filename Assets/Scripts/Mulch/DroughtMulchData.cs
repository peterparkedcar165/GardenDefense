using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DroughtMulchData", menuName = "Scriptable Objects/Mulch/Drought")]
public class DroughtMulchData : MulchData
{
    [Tooltip("how many whole waves the drought lasts, counted from the first wave that starts after the pick")]
    public int droughtWaves = 3;
    [Tooltip("sun production during the drought, as a multiplier. 0 = sun producers make nothing")]
    public float droughtSunMultiplier = 0f;
    [Tooltip("sun production after the drought, for the rest of the level, as a multiplier. 2 = double")]
    public float afterSunMultiplier = 2f;

    public override MulchEffect CreateEffect() => new DroughtMulchEffect(this);

    // only worth offering when something in the loadout makes sun
    public override bool MeetsExtraRequirements(List<PlantData> loadout, int currentWave) => LoadoutHasSunProducer(loadout);

    public override string GetDescription()
    {
        const string sun = "<b><color=#FFD700>Sun</color></b>";
        string during = droughtSunMultiplier <= 0f
            ? $"make <b><color=red>no</color></b> {sun}"
            : $"make only <b><color=red>{droughtSunMultiplier * 100f:F0}%</color></b> of their {sun}";
        return $"{sun} producers {during} for the next <b><color=green>{droughtWaves}</color></b> waves. " +
            $"After that, they make <b><color=green>{afterSunMultiplier:0.##}x</color></b> {sun} for the rest of the level.";
    }
}
