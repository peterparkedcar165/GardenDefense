using UnityEngine;

public class Wasp : FlyingInsect
{
    // how much more likely a Photosynthesis plant is to win the weighted target roll (see
    // Insect.GetTauntAffinity) compared to everything else - strong preference, not an absolute
    // guarantee like before
    private const float PhotosynthesisAffinity = 5f;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    protected override float GetTauntAffinity(Plant plant) =>
        plant.data != null && plant.data.family == PlantFamily.Photosynthesis ? PhotosynthesisAffinity : 1f;

    public override string GetDescription() =>
        "Mildly aggressive insect. Stronger aggressivity towards Photosynthesis plants." + FlyingLine() + AggressivityLine();
}
