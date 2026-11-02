using UnityEngine;

public class Wasp : FlyingInsect
{
    // how much more likely a sun generating plant is to win the weighted target roll (see
    // Insect.GetTauntAffinity) compared to everything else, a strong preference but not a guarantee
    private const float SunGeneratorAffinity = 5f;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    protected override float GetTauntAffinity(Plant plant) =>
        plant.data != null && plant.data.generatesSun ? SunGeneratorAffinity : 1f;

    public override string GetDescription() =>
        "Mildly aggressive insect. Stronger aggressivity towards plants that generate <color=yellow>Sun</color>." + FlyingLine() + AggressivityLine();
}
