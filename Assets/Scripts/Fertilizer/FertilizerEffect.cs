// runtime behaviour of the level's picked fertilizer, the FertilizerData/FertilizerEffect split
// being the PlantData/Plant one. it lives from the pick (before wave 1) until the level ends, and
// almost always works by nudging one of FertilizerManager's level wide values or by reacting to
// one of the hooks below
public abstract class FertilizerEffect
{
    protected readonly FertilizerData data;
    public FertilizerData Data => data;

    protected FertilizerEffect(FertilizerData data)
    {
        this.data = data;
    }

    // called once, when the player picks this fertilizer
    public abstract void Activate();

    // called when the level ends or restarts (FertilizerManager.ResetForLevel), and by the dev menu
    // when it swaps fertilizers. must fully undo Activate
    public abstract void Deactivate();

    // a plant was placed (or revived). plants placed later need any per plant bonus too
    public virtual void OnPlantPlaced(Plant plant) {}

    // wave hooks, fired by ProceduralLevel. a wave ends the moment its spawning finishes, so
    // insects can still be on the field when OnWaveEnded runs
    public virtual void OnWaveStarted(int wave) {}
    public virtual void OnWaveEnded(int wave) {}

    // an insect reached the end of the path (every leak goes through Insect.DamagePlayerBase)
    public virtual void OnInsectLeaked(Insect insect) {}

    // a flat discount off the next plant bought, read by FertilizerManager.GetPlacementCost
    public virtual int PlacementDiscount => 0;

    // a plant was just bought and placed (see Tile)
    public virtual void OnPlantBought(Plant plant) {}

    // a short live line for the active effects window (GameInfoPanel, G), like how many waves are
    // left. null when the description already says everything
    public virtual string GetStatus() => null;
}
