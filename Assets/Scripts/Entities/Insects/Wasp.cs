using UnityEngine;

public class Wasp : FlyingInsect
{
    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    // always goes for a sun generating plant when one is in range and reachable (Tommy, 2026-11-25),
    // switching to one that comes into range; with none in range it picks a plant like any insect
    // (Insect.FindTauntWeightedPlantInRange). it was a 5x weight on them before
    protected override bool HasPlantPreference => true;
    protected override bool PrefersPlant(Plant plant) => plant != null && plant.data != null && plant.data.generatesSun;

    public override string GetAbilityDescription() =>
        "When able to, it will always target a <color=yellow><b>Sun</b></color> generating plant.";
}
