using UnityEngine;

public class BrittleEffect : StatusEffect
{
    public BrittleEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        effectType = Type.negative;
    }

    public override string GetName() => "<color=green>Brittle</color>";
    public override string GetDescription() => $"Inflict <color=green>3</color> additional damage upon being hurt. Reduces Shield Toughness by <color=green>25%</color>.";

    public override void OnApply()
    {
        GameObject indicator = Object.Instantiate(Resources.Load<GameObject>("DamageIndicator"), target.transform.position + new Vector3(0.4f, 0f, 0f), Quaternion.identity);
        indicator.GetComponent<DamageIndicator>().Initialize("Brittle", Color.green);
        target.shieldToughnessAdder -= 0.25f;
    }

    public override void OnTick(float deltaTime) { }

    public override void OnExpire()
    {
        target.shieldToughnessAdder += 0.25f;
    }
}
