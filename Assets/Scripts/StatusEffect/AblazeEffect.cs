public class AblazeEffect : StatusEffect
{
    private readonly float flatBonusDamage;
    private readonly float maxHealthPercent;
    private static readonly DamageTag[] bonusDamageTags = { DamageTag.Coordinated };

    public AblazeEffect(Entity target, float duration, int level, Entity source, float flatBonusDamage, float maxHealthPercent)
        : base(target, duration, level, source)
    {
        this.flatBonusDamage  = flatBonusDamage;
        this.maxHealthPercent = maxHealthPercent;
        effectType    = Type.positive;
        elementalType = ElementalType.Fire;
    }

    public override string GetName() => "<color=orange><b>Ablaze</b></color>";
    public override string GetDescription() =>
        $"This plant's next attack deals an additional <color=orange><b>{flatBonusDamage:F0}</b></color> + <color=orange><b>{maxHealthPercent * 100f:F0}%</b></color> of the target's Max Health as <color=orange>Fire</color> <color=#FFB6C1>Magic</color> damage.";

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new UnityEngine.Vector3(0.4f, 0f, 0f), "Ablaze", new UnityEngine.Color(1f, 0.4f, 0f));
    }

    public override void OnExpire() { }

    public void Trigger(Insect insect, float effectiveness)
    {
        if (insect == null) return;
        target.RemoveEffect<AblazeEffect>();
        target.StartCoroutine(DelayedBonusDamage(insect, effectiveness));
    }

    private System.Collections.IEnumerator DelayedBonusDamage(Insect insect, float effectiveness)
    {
        yield return new UnityEngine.WaitForSeconds(0.1f);
        if (insect == null || !insect.IsAlive) yield break;
        float totalBonus = (flatBonusDamage + maxHealthPercent * insect.maxHealth) * effectiveness;
        insect.Damage(totalBonus, DamageType.Magic, ElementalType.Fire, target, false, bonusDamageTags);
    }

    public override void OnTick(float deltaTime) { }
}
