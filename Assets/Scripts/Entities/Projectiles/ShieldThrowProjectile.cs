using UnityEngine;

// the shield in flight during Acorn Knight's skill. straight line to a fixed, already clamped
// landing point, no homing and no piercing. whatever it touches first, or the landing point
// itself if it touches nothing, is where the shield comes to rest and spawns AcornKnightShield
public class ShieldThrowProjectile : Projectile
{
    private Vector3 landPosition;
    private System.Action<Vector3, Insect> onLanded;
    private bool _landed;

    public void Initialize(Vector3 landPosition, float damage, DamageType damageType, ElementalType elementalType, float speed, Plant source, System.Action<Vector3, Insect> onLanded)
    {
        this.landPosition = landPosition;
        this.onLanded = onLanded;
        direction = (landPosition - transform.position).normalized;
        projectileDamage = damage;
        projectileSpeed = speed;
        piercing = 0;
        this.damageType = damageType;
        this.elementalType = elementalType;
        maxRange = Vector3.Distance(transform.position, landPosition);
        this.source = source;
        spawnPosition = transform.position;
    }

    protected override void Move()
    {
        transform.position += direction * projectileSpeed * Time.deltaTime;
        if (!Tile.IsInsideGrid(transform.position) || Vector3.Distance(spawnPosition, transform.position) >= maxRange)
            Land(null, landPosition);
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (_landed) return;
        if (other.CompareTag("Insect"))
        {
            Insect insect = other.GetComponentInParent<Insect>();
            if (insect != null && insect.IsAlive && insect.team != Team.Friendly)
                Land(insect, transform.position);
        }
    }

    private void Land(Insect hitInsect, Vector3 position)
    {
        if (_landed) return;
        _landed = true;

        if (hitInsect != null)
        {
            hitInsect.Damage(projectileDamage, damageType, elementalType, source, true, new DamageTag[] { DamageTag.Attack, DamageTag.Projectile, DamageTag.SingleTarget });
            PlaySound(hit);
        }

        onLanded?.Invoke(position, hitInsect);
        OnBeforeDestroy();
        Destroy(gameObject);
    }
}
