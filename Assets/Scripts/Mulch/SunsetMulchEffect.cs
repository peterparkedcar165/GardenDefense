using System.Collections.Generic;

// a normal sun tick (Sunflower.GenerateSunTick), so her yield, drought and solar flare all apply to
// it the same as to her timed ticks. her passive timer isn't touched
public class SunsetMulchEffect : MulchEffect
{
    private readonly SunsetMulchData typedData;

    public SunsetMulchEffect(SunsetMulchData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate() {}
    public override void Deactivate() {}

    public override void OnWaveEnded(int wave)
    {
        foreach (Plant plant in new List<Plant>(Plant.allPlants))
        {
            if (plant is not Sunflower sunflower || sunflower == null || !sunflower.IsAlive) continue;
            for (int i = 0; i < typedData.extraTicks; i++)
                sunflower.GenerateSunTick();
        }
    }
}
