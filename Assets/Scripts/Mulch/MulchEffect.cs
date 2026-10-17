// runtime behavior granted by a picked mulch - the Plant/PlantData split, but for mulches.
// MulchData carries the authored name/tier/condition/tunables; this is where a specific mulch
// actually does something, almost always by subscribing to an existing global event (Entity.
// OnEntityHit, Insect.Kill, GameManager.AddSun, etc.) for as long as it stays active.
// one subclass per mulch, paired with that mulch's own MulchData subclass
public abstract class MulchEffect
{
    protected readonly MulchData data;

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
}
