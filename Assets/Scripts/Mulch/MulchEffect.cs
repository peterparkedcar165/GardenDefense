using UnityEngine;

// what an equipped mulch does on its plant, the MulchData/MulchEffect split being the
// PlantData/Plant one. Activate starts it on Owner and Deactivate must undo all of it. the same
// effect object can run again later: a merge on the plant swaps in the higher rarity copy
// (Deactivate, new item, Activate), and a plant revived by a max level Rhodiola skill gets its old
// effects back on its new body (AttachTo, Activate), so anything an effect counts (waves survived,
// and so on) carries over both times
public abstract class MulchEffect
{
    public MulchItem Item { get; private set; }
    public MulchData Data => Item.data;
    public MulchTier Rarity => Item.rarity;
    public Plant Owner { get; private set; }

    protected MulchEffect(MulchItem item)
    {
        Item = item;
    }

    public void AttachTo(Plant owner) => Owner = owner;

    // starts the effect on Owner
    public abstract void Activate();

    // stops it, undoing everything Activate did. called when the plant dies or is removed, when a
    // merge upgrades it, and when the level ends
    public abstract void Deactivate();

    // a merge on the plant: the same effect continues at the higher rarity
    public void Upgrade(MulchItem upgraded)
    {
        Deactivate();
        Item = upgraded;
        Activate();
    }

    // wave hooks, fired by ProceduralLevel through MulchManager. a wave ends the moment its
    // spawning finishes, so insects can still be on the field when OnWaveEnded runs
    public virtual void OnWaveStarted(int wave) {}
    public virtual void OnWaveEnded(int wave) {}

    // Owner banked sun of its own (Plant.GenerateSun). fromKill marks sun paid for a kill, like
    // glowshroom's, rather than the plant's own production
    public virtual void OnSunGenerated(int amount, bool fromKill) {}

    // a mulch dropped from an insect Owner killed: fromChance for a kill's own drop roll (not bad
    // luck protection), false for the wave's guaranteed one
    public virtual void OnKillDrop(MulchItem item, bool fromChance) {}

    // a short live line for tooltips and the active effects window (G), like waves left. null when
    // the description already says everything. since 2026-11-22 (Tommy) every mulch that does
    // something with numbers also shows its impact here, running totals for the level like the
    // damage its splash dealt, one per line
    public virtual string GetStatus() => null;

    // damage through the normal path, returning what it took off the target's health, for the
    // running totals
    protected static float DealDamage(Entity target, float amount, DamageType type, ElementalType element, Entity source, DamageTag[] tags)
    {
        if (target == null) return 0f;
        float before = target.health;
        target.Damage(amount, type, element, source, false, tags);
        return Mathf.Max(0f, before - Mathf.Max(0f, target.health));
    }

    // a number at this copy's rarity
    protected float V(TierValue value) => value.Get(Rarity);
    protected int VI(TierValue value) => value.GetInt(Rarity);

    protected bool OwnerAlive => Owner != null && Owner.IsAlive;

    // effects that need a coroutine run it on their plant, so it stops by itself if the plant goes
    protected Coroutine StartOnOwner(System.Collections.IEnumerator routine) =>
        Owner != null ? Owner.StartCoroutine(routine) : null;

    protected void StopOnOwner(ref Coroutine routine)
    {
        if (routine != null && Owner != null) Owner.StopCoroutine(routine);
        routine = null;
    }
}
