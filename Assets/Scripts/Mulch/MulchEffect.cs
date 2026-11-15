// runtime behavior granted by a picked mulch - the Plant/PlantData split, but for mulches.
// MulchData carries the authored name/tier/condition/tunables; this is where a specific mulch
// actually does something, almost always by subscribing to an existing global event (Entity.
// OnEntityHit, Insect.Kill, GameManager.AddSun, etc.) for as long as it stays active.
// one subclass per mulch, paired with that mulch's own MulchData subclass
public abstract class MulchEffect
{
    protected readonly MulchData data;
    public MulchData Data => data;

    protected MulchEffect(MulchData data)
    {
        this.data = data;
    }

    // called once when the player picks this mulch. subscribe to whatever event this mulch
    // reacts to here
    public abstract void Activate();

    // called when the level ends/restarts (see MulchManager.ResetMulch). unsubscribe here -
    // mulches are session-only, so this must always fully undo Activate()
    public abstract void Deactivate();

    // called whenever a new plant is placed (see MulchManager.NotifyPlantPlaced, hooked from
    // Plant.cs next to FertilizerManager.ApplyTo/SkillTreeManager.ApplyTo) - override this for a
    // mulch that grants a per-plant bonus, so plants placed after the pick get it too. plants
    // already on the field when Activate() runs still need their own pass there, same as
    // FertilizerManager.CommitGenerated/ApplyTo
    public virtual void OnPlantPlaced(Plant plant) {}

    // wave hooks, fired by ProceduralLevel through MulchManager. a wave ends the moment its
    // spawning finishes, the same moment that wave's mulch grants fire, so insects can still be
    // on the field when OnWaveEnded runs
    public virtual void OnWaveStarted(int wave) {}
    public virtual void OnWaveEnded(int wave) {}

    // an insect reached the end of the path and hurt the garden (every leak goes through
    // Insect.DamagePlayerBase)
    public virtual void OnInsectLeaked(Insect insect) {}

    // a plant banked sun of its own (Plant.GenerateSun). fromKill marks sun paid for a kill, like
    // glowshroom's, rather than the plant's own production
    public virtual void OnSunGenerated(Plant plant, int amount, bool fromKill) {}

    // placement cost hooks, read by MulchManager.GetPlacementCost: a flat discount off the next
    // plant bought, or the next plant bought costing nothing at all
    public virtual int PlacementDiscount => 0;
    public virtual bool GrantsFreePlacement => false;

    // a plant was just bought and placed (see Tile). placedForFree is true when a free placement
    // paid for it, so a discount that wasn't needed doesn't get used up as well
    public virtual void OnPlantBought(Plant plant, bool placedForFree) {}

    // whether seed swap and seed sell may take this mulch away. false once a one shot mulch has
    // already paid out, since removing it couldn't undo what it gave
    public virtual bool CanBeRemoved => true;
}
