using UnityEngine;

// base hypnotize effect. applying it (or any subclass) turns the target insect: OnApply triggers
// the team flip + kill rewards via Insect.ApplyHypnosis. it never expires, so the insect stays
// friendly for the rest of its life. subclasses (e.g. FungalHypnosis) add their own bonuses
public class HypnotizedEffect : StatusEffect
{
    public HypnotizedEffect(Entity target, Entity source)
        : base(target, float.PositiveInfinity, 1, source)   // infinite: never ticks down to 0
    {
        effectType = Type.neutral;       // neutral so cleanses cannot strip it
    }

    // overridable so variants can label/color their own indicator
    protected virtual string IndicatorLabel => "Hypnotized";
    protected virtual Color  IndicatorColor => new Color(0.7f, 0.4f, 1f);

    public override void OnApply()
    {
        (target as Insect)?.ApplyHypnosis(source);   // the actual turning lives on the Insect
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), IndicatorLabel, IndicatorColor);
    }

    // makes a copy of this hypnosis for a newly spawned unit (e.g. a hypnotized queen's children),
    // preserving the variant and its parameters
    public virtual HypnotizedEffect Clone(Entity newTarget) => new HypnotizedEffect(newTarget, source);

    // this marker is the source of truth for the hypnotized state, so removing it (e.g. a
    // "deprogrammer" insect) cleanly reverts the insect back to an advancing enemy
    public override void OnExpire()
    {
        Insect insect = target as Insect;
        if (insect == null) return;
        insect.movingBackward = false;
        insect.ClearEngagementsByMe();   // its former victims stop targeting it
        insect.SetTeam(Team.Enemy);
    }

    public override string GetName() => "<color=#B266FF>Hypnotized</color>";
    public override string GetDescription() => "Permanently turned against its own, fighting other insects until it reaches the spawn.";
}
