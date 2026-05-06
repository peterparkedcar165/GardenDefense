using UnityEngine;

public class SludgeEffect : StatusEffect
{
    public SludgeEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        effectType = Type.negative;
    }

    public override string GetName() => "<color=#9400D3>Sludge</color>";

    public override void OnApply()
    {
        GameObject indicator = Object.Instantiate(Resources.Load<GameObject>("DamageIndicator"), target.transform.position + new Vector3(0.4f, 0f, 0f), Quaternion.identity);
        indicator.GetComponent<DamageIndicator>().Initialize("Sludge", new Color(0.6f, 0.1f, 0.8f));

        bool hasDoT = false;
        foreach (StatusEffect effect in target.activeEffects)
        {
            if (effect is DoTEffect)
            {
                effect.duration += 4f;
                hasDoT = true;
            }
        }

        if (hasDoT)
        {
            target.RemoveEffect<SludgeEffect>();
        }
    }

    public override void OnTick(float deltaTime)
    {
        
    }

    public override void OnExpire()
    {
        
    }
}
