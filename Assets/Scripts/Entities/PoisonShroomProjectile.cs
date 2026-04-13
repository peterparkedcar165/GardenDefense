using UnityEngine;

public class PoisonShroomProjectile : Projectile
{

    public override void Initialize(Vector3 target, float projectileDamage, float projectileSpeed, float maxRange, int piercing, DamageType damageType)
    {
        base.Initialize(target, projectileDamage, projectileSpeed, maxRange, piercing, damageType);
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void OnHit(Insect insect) // to change for every plant
    {
        
        if (owner != null)
            insect.RegisterAttacker(owner);

        insect.Damage(projectileDamage * (1 - insect.poisonResistance), damageType);

        if (piercing > 0)
        {
            piercing--;
        } else
        {
            Destroy(gameObject);
         }


        PoisonShroom shooter = owner as PoisonShroom;


        /* SPECIAL EFFECT */

        if (shooter != null) {
            // float procChance = 0.8f * (1 + shooter.bonusEffectChance);

            // if (Random.value < procChance)
            // {
                int newPoisonLevel = shooter.passiveLevel <= 0 ? 1 : 1 + shooter.passiveLevel; // get poison that will be inflicted

                int currentPoisonLevel = insect.GetEffectLevel<PoisonEffect>(); // get level of current poison


                if (currentPoisonLevel > 0)
                    {
                        foreach (StatusEffect effect in insect.activeEffects)
                        {
                            if (effect is PoisonEffect poison)
                                {
                                poison.duration = 6f;
                                if (newPoisonLevel > currentPoisonLevel) // only upgrade if higher 
                                {
                                    poison.level = newPoisonLevel;
                                } 
                                break;
                                }
                        }
                    } else
                    {
                        insect.ApplyEffect(new PoisonEffect(
                            insect,
                            6f,
                            newPoisonLevel,
                            shooter
                        ));
                    }

           // }
        } else { return; }
    }
    
}
