using UnityEngine;

public abstract class Shooter : Plant
{
    public GameObject projectilePrefab;
    public float projectileSpeed;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Update()
    {
        attackCooldown = 1 / attackSpeed;

        if (attackCooldownTimer < attackCooldown)
        {
            attackCooldownTimer += Time.deltaTime;
        } else
        {
            Shoot();
        }
    }

    public void Shoot()
    {
        attackCooldownTimer = 0;
        Debug.Log("Shot!");
    }

    
}
