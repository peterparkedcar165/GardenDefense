using UnityEngine;

public enum DamageType
{
    Physical,
    Magic,
    True
}

public abstract class Entity : MonoBehaviour
{

    public float maxHealth, health, physicalResistance, magicResistance, attackDamage, magicDamage, attackSpeed, attackCooldown, attackCooldownTimer, attackRange, healingBonus = 1, healingReceived = 1;

    public float timeAlive; // leaving it public jsut so i can debug, but shgould be private

    public virtual void Damage(float damageDealt, DamageType damageType)
    {
        float modifiedDamage;

        switch (damageType)
        {
            case DamageType.Physical:
            modifiedDamage = damageDealt * (1 - physicalResistance);
            break;
            case DamageType.Magic:
            modifiedDamage = damageDealt * (1 - magicResistance);
            break;
            default:
            modifiedDamage = damageDealt;
            break;
        }

        health -= modifiedDamage;
        if (health <= 0)
        {
            Kill();
        }
    }

// method for healing
    public virtual void Heal(float healingAmount)
    {
        health += healingAmount * healingReceived;
        if (health >= maxHealth)
        {
            health = maxHealth;
        }
    }

// method for death
    public virtual void Kill()
    {
        Destroy(gameObject);
    }

// upon spawning, occurs before Start()
    protected virtual void Awake()
    {
        health = maxHealth;
    }

    //every tick
    protected virtual void Update()
    {
        timeAlive += Time.deltaTime;
    }
}
