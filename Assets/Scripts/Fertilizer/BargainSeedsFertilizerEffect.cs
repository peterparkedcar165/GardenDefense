// the discount belongs to a wave number (GameManager.currentWave), so the rest period after a wave
// still counts as that wave: one cheap plant per wave, and a new one the moment the next wave
// starts. the placement phase before wave 1 counts as wave 0
public class BargainSeedsFertilizerEffect : FertilizerEffect
{
    private readonly BargainSeedsFertilizerData typedData;
    private int usedOnWave = -1;

    public BargainSeedsFertilizerEffect(BargainSeedsFertilizerData data) : base(data)
    {
        typedData = data;
    }

    private static int CurrentWave => GameManager.instance != null ? GameManager.instance.currentWave : 0;
    private bool Available => usedOnWave != CurrentWave;

    public override void Activate() => usedOnWave = -1;
    public override void Deactivate() {}

    public override int PlacementDiscount => Available ? typedData.discount : 0;

    public override string GetStatus() => Available ? "Discount ready this wave" : "Discount used this wave";

    public override void OnPlantBought(Plant plant)
    {
        if (Available) usedOnWave = CurrentWave;
    }
}
