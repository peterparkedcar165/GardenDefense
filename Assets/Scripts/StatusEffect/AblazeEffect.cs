public class AblazeEffect : StatusEffect
{
    private readonly float bonusDamage;
    private static readonly DamageTag[] bonusDamageTags = { DamageTag.Coordinated };

    public AblazeEffect(Entity target, float duration, int level, Entity source, float bonusDamage)
        : base(target, duration, level, source)
    {
        this.bonusDamage = bonusDamage;
        effectType    = Type.positive;
        elementalType = ElementalType.Fire;
    }

    public override string GetName() => "<color=orange><b>Ablaze</b></color>";
    public override string GetDescription() => $"This plant's next attack deals an additional <color=orange><b>{bonusDamage:F0}</b></color> <color=orange>Fire</color> <color=#FFB6C1>Magic</color> damage.";

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new UnityEngine.Vector3(0.4f, 0f, 0f), "Ablaze", new UnityEngine.Color(1f, 0.4f, 0f));
        Entity.OnHit += HandleHit;
    }

    public override void OnExpire()
    {
        Entity.OnHit -= HandleHit;
    }

    private void HandleHit(EntityEventData data)
    {
        if (data.source != target) return;
        if (data.target is not Insect insect) return;
        Entity.OnHit -= HandleHit;
        target.RemoveEffect<AblazeEffect>();
        target.StartCoroutine(DelayedBonusDamage(insect));
    }

    private System.Collections.IEnumerator DelayedBonusDamage(Insect insect)
    {
        yield return new UnityEngine.WaitForSeconds(0.1f);
        if (insect == null || !insect.IsAlive) yield break;
        insect.Damage(bonusDamage, DamageType.Magic, ElementalType.Fire, target, false, bonusDamageTags);
    }

    public override void OnTick(float deltaTime) { }
}
