using UnityEngine;

public class FractureEffect : StatusEffect
{
    public FractureEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        effectType = Type.negative;
    }

    public override string GetName() => "<color=#00FFFF>Fracture</color>";
    public override string GetDescription() => "Upon taking Physical Damage, deal an additional Fire Damage instance equal to 25% of the original damage";

    public override void OnApply()
    {
        GameObject indicator = Object.Instantiate(Resources.Load<GameObject>("DamageIndicator"), target.transform.position + new Vector3(0.4f, 0f, 0f), Quaternion.identity);
        indicator.GetComponent<DamageIndicator>().Initialize("Fracture", new Color(0f, 1f, 1f));
    }

    public override void OnTick(float deltaTime)
    {
        
    }

    public override void OnExpire()
    {
        
    }
}
