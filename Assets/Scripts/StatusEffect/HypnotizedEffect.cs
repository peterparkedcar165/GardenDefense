using UnityEngine;

// permanent marker shown on a hypnotized insect (PvZ hypno style). the actual team flip and the
// kill rewards happen in Insect.Hypnotize; this effect never expires (Tick does not decrement),
// so it stays for the rest of the insect's life as a friendly
public class HypnotizedEffect : StatusEffect
{
    public HypnotizedEffect(Entity target, Entity source)
        : base(target, 1f, 1, source)   // duration is a placeholder; it is never ticked down
    {
        effectType = Type.neutral;       // neutral so cleanses cannot strip it
    }

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Hypnotized", new Color(0.7f, 0.4f, 1f));
    }

    // permanent: never tick the duration down
    public override void Tick(float deltaTime) { }

    // this marker is the source of truth for the hypnotized state, so removing it (e.g. a
    // "deprogrammer" insect) cleanly reverts the insect back to an advancing enemy
    public override void OnExpire()
    {
        Insect insect = target as Insect;
        if (insect == null) return;
        insect.movingBackward = false;
        insect.SetTeam(Team.Enemy);

        // drop engagements it created while friendly so its former victims stop targeting it
        foreach (Insect e in Insect.allInsects)
        {
            EngagedEffect eng = e != null ? e.GetEffect<EngagedEffect>() : null;
            if (eng != null && ReferenceEquals(eng.taunter, insect))
                e.RemoveEffect<EngagedEffect>();
        }
    }

    public override string GetName() => "<color=#B266FF>Hypnotized</color>";
    public override string GetDescription() => "Permanently turned against its own, fighting other insects until it reaches the spawn.";
}
