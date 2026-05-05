using UnityEngine;

public class DecayEffect : StatusEffect
{

    public float healingReduction = 0.5f, physShred = 0.24f;
    public DecayEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        effectType = Type.negative;
    }

    public override void OnApply()
    {

        GameObject indicator = Object.Instantiate(Resources.Load<GameObject>("DamageIndicator"), target.transform.position + new Vector3(0.4f, 0f, 0f), Quaternion.identity);
        indicator.GetComponent<DamageIndicator>().Initialize("Decay", new Color(0.6f, 0.1f, 0.8f));

        Insect insect = (Insect)target;
        insect.healingBonusAdder -= healingReduction;
        insect.physicalResistanceAdder -= physShred;
    }

    public override void OnTick(float deltaTime)
    {
        
    }

    public override void OnExpire()
    {
        Insect insect = (Insect)target;
        insect.healingBonusAdder += healingReduction;
        insect.physicalResistanceAdder += physShred;
    }
}
