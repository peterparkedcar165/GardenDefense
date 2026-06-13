using UnityEngine;

public class BlossomingEffect : StatusEffect
{
    private readonly float natureDamageBonus;
    private readonly float attackSpeedBonus;

    private const float AffinityBurst = 0.22f;
    private bool _affinityBurstActive = false;

    public BlossomingEffect(Entity target, float duration, int level, Entity source, float natureDamageBonus, float attackSpeedBonus)
        : base(target, duration, level, source)
    {
        this.natureDamageBonus = natureDamageBonus;
        this.attackSpeedBonus = attackSpeedBonus;
        effectType = Type.positive;
    }

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Blossoming", new Color(0.3f, 1f, 0.2f));
        target.natureDamageAdder += natureDamageBonus;
        target.attackSpeedMultiplier += attackSpeedBonus;
        Entity.OnEffectApplied += HandleEffectApplied;
    }

    public override void OnExpire()
    {
        target.natureDamageAdder -= natureDamageBonus;
        target.attackSpeedMultiplier -= attackSpeedBonus;
        Entity.OnEffectApplied -= HandleEffectApplied;
        if (_affinityBurstActive)
        {
            target.elementalAffinityAdder -= AffinityBurst;
            _affinityBurstActive = false;
        }
    }

    public override void OnTick(float deltaTime) { }

    private void HandleEffectApplied(StatusEffect effect)
    {
        if (effect.source != target) return;
        if (!(effect is GerminateEffect) && !(effect is BrittleEffect)) return;
        if (!(source is Begonia beg) || beg.path3Level < Plant.pathLevelCap) return;
        if (_affinityBurstActive) return;
        target.elementalAffinityAdder += AffinityBurst;
        _affinityBurstActive = true;
    }

    public override string GetName() => "<color=green>Blossoming</color>";
    public override string GetDescription()
    {
        string desc = $"Increase <color=green><b>Nature Power</b></color> by <color=green><b>{natureDamageBonus * 100f:F0}%</b></color>, and <color=green><b>Attack Speed</b></color> by <color=green><b>{attackSpeedBonus * 100f:F0}%</b></color>.";
        if (source is Begonia beg && beg.path3Level >= Plant.pathLevelCap)
            desc += " When a plant triggers <color=green><b>Germinate</b></color> or <color=green><b>Brittle</b></color>, they gain <color=green><b>22% Elemental Affinity</b></color> until the end of the effect.";
        return desc;
    }
}
