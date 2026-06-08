using UnityEngine;

public class HelleboreProtectionEffect : ShieldEffect
{
    private readonly float reflectBase;
    private readonly float reflectMP;
    private readonly Hellebore hellebore;

    private static readonly DamageTag[] _reflectTags = { DamageTag.PassiveDamage };

    public HelleboreProtectionEffect(Entity target, float duration, int level, Hellebore source,
                                     float shieldAmount, float reflectBase, float reflectMP)
        : base(target, duration, level, source, shieldAmount)
    {
        this.reflectBase = reflectBase;
        this.reflectMP   = reflectMP;
        this.hellebore   = source;
    }

    public override void OnApply()
    {
        base.OnApply();
        StatusIndicator.Spawn(target.transform.position + Vector3.up * 0.3f, "Thorned Guard", new Color(0.5f, 0.2f, 0.8f));
    }

    // fires for every incoming attack that still has shield remaining
    public override void OnDamageReceived(ElementalType elementalType, Entity damageSource, DamageTag[] damageTags = null)
    {
        if (damageSource == null) return;
        if (damageTags == null || !System.Array.Exists(damageTags, t => t == DamageTag.Attack)) return;
        if (damageSource is Insect ins && ins.team == Team.Friendly) return;
        float dmg = reflectBase + (hellebore != null ? hellebore.magicPower * reflectMP : 0f);
        damageSource.Damage(dmg, DamageType.Magic, ElementalType.Poison, hellebore, false, _reflectTags);
    }

    // intercepts negative effects and mirrors them back as venom
    public override bool TryBlockNegativeEffect(StatusEffect incoming)
    {
        if (amount <= 0f) return false;
        if (incoming.source is Insect attacker && attacker.team != Team.Friendly)
        {
            incoming.target = attacker;
            incoming.source = hellebore;
            attacker.ApplyEffect(incoming);
        }
        StatusIndicator.Spawn(target.transform.position + Vector3.up * 0.5f, "Reflected!", new Color(0.6f, 0.2f, 0.8f));
        return true;
    }

    public override string GetName()        => "<color=#9B30D0>Thorned Guard</color>";
    public override string GetDescription() => $"Shield: <color=grey><b>{amount:F0}/{originalAmount:F0}</b></color>. Attackers receive <color=purple>Poison</color> damage per hit. Negative effects are reflected.";
}
