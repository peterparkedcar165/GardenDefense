using UnityEngine;

// ranged artillery insect: implements IShooterInsect, so Insect.target's base getter already
// returns null for it (base UpdateAttack() no-ops, never melees) without needing its own
// override. instead it runs its own scan/lock/charge/fire cycle every frame via Update(),
// independent of whether it's currently being carried - this is what lets it fire while being
// carried without the base Insect class needing to know anything about it
public class BombardierBeetle : Insect, IShooterInsect
{
    private BombardierBeetleData BData => data as BombardierBeetleData;

    private float fireCooldownTimer;
    private bool isCharging;
    private float chargeTimer;
    private bool isPostFireDelay;
    private float postFireTimer;
    private Plant lockedTarget;
    private Vector3 lockedPosition;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        aggressivity = Aggressivity.High;
    }

    protected override void Update()
    {
        base.Update();
        if (isDying) return;
        TickAttack();
    }

    private void TickAttack()
    {
        // while an active Taunt's taunter is a living Plant, it takes absolute priority over the
        // normal backline scan and forces the beetle to root in place, per IShooterInsect's
        // contract: forced to engage the taunter via its own shoot mechanism, never melee
        Plant tauntTarget = GetTauntPlantTarget();
        bool isTaunted = tauntTarget != null;

        // taunted: stand completely still for as long as the taunt holds, not just during the
        // charge/post-fire windows like the normal cycle allows
        if (isTaunted && carriedBy == null)
            attackMovementPaused = true;

        // stays rooted for a beat after firing before it starts walking again, then the attack
        // speed cooldown below plays out while it's free to move, until it's ready to stop again
        if (isPostFireDelay)
        {
            postFireTimer -= Time.deltaTime;
            if (postFireTimer <= 0f)
            {
                isPostFireDelay = false;
                if (carriedBy == null && !isTaunted)
                    attackMovementPaused = false;
            }
            return;
        }

        if (isCharging)
        {
            chargeTimer -= Time.deltaTime;
            if (chargeTimer <= 0f)
                FireProjectile();
            return;
        }

        if (HasEffect<HardCrowdControl>()) return;
        if (attackSpeed <= 0f) return;

        fireCooldownTimer += Time.deltaTime;
        if (fireCooldownTimer < 1f / attackSpeed) return;

        // a taunt bypasses the backline scan (and its attackRange gate) entirely - being forced
        // to engage something specific shouldn't depend on it happening to be in scan range
        Plant candidate = isTaunted ? tauntTarget : FindBacklineTarget();
        if (candidate == null) return; // stays "ready" and keeps rescanning every frame until one appears

        fireCooldownTimer = 0f;
        lockedTarget = candidate;
        lockedPosition = candidate.transform.position;
        isCharging = true;
        chargeTimer = BData != null ? BData.attackChargeTime : 0.75f;

        // grounded: the charge visibly roots it in place. carried: it keeps moving with its
        // carrier and fires anyway, per spec. uses attackMovementPaused (not movementPaused) so
        // a charging/recovering beetle still reads as pickup-eligible to a Duskdarter
        if (carriedBy == null)
            attackMovementPaused = true;
    }

    // among plants within attack range, picks the one furthest from any Path tile (the deepest
    // "backline" plant) rather than the nearest one
    private Plant FindBacklineTarget()
    {
        Plant best = null;
        float bestPathDist = -1f;

        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive) continue;
            if (!CanReachPlant(plant)) continue;

            float rangeDist = Vector3.Distance(transform.position, plant.GetApproachPoint(transform.position));
            if (rangeDist > attackRange) continue;

            float pathDist = DistanceToNearestPathTile(plant.transform.position);
            if (pathDist > bestPathDist)
            {
                bestPathDist = pathDist;
                best = plant;
            }
        }

        return best;
    }

    private static float DistanceToNearestPathTile(Vector3 position)
    {
        float best = float.MaxValue;
        foreach (Tile tile in Tile.allTiles.Values)
        {
            if (tile == null || tile.tileType != TileType.Path) continue;
            float d = Vector3.Distance(position, tile.transform.position);
            if (d < best) best = d;
        }
        return best == float.MaxValue ? 0f : best;
    }

    // fires at the locked position regardless of whether the target is still alive, still in
    // range, or (if carried) the beetle has since wandered off - the lock is final the instant
    // the charge begins
    private void FireProjectile()
    {
        isCharging = false;
        SpawnProjectile(lockedPosition, lockedTarget);
        lockedTarget = null;

        // attackMovementPaused (if it was ever set) stays true through this delay too - only
        // cleared once the delay itself elapses, in TickAttack
        isPostFireDelay = true;
        postFireTimer = BData != null ? BData.postFireDelay : 0.35f;
    }

    private void SpawnProjectile(Vector3 impactPosition, Plant primaryTarget)
    {
        GameObject prefab = BData != null ? BData.projectilePrefab : null;
        if (prefab == null) return;

        // spawns from the visual (the actual sprite, which can sit above the root due to hover/
        // knock-up/etc.), not the root transform - the root stays the anchor for all targeting
        // and range logic above, this only affects where the thrown projectile visually emerges
        Vector3 launchPosition = visual != null ? visual.position : transform.position;

        GameObject obj = Instantiate(prefab, launchPosition, Quaternion.identity);
        BombardierBeetleProjectile proj = obj.GetComponent<BombardierBeetleProjectile>();
        if (proj == null) return;

        float splashPercent       = BData != null ? BData.splashDamagePercent  : 0.5f;
        float splashRadius        = BData != null ? BData.splashRadius        : 1f;
        float scorchChance        = BData != null ? BData.scorchChance        : 0.5f;
        float scorchDuration      = BData != null ? BData.scorchDuration      : 8f;
        float scorchDPS           = BData != null ? BData.scorchDPS           : 16f;
        float scorchHealthPercent = BData != null ? BData.scorchHealthPercent : 0.04f;
        float projSpeed           = BData != null ? BData.projectileSpeed     : 4f;

        proj.Initialize(launchPosition, impactPosition, primaryTarget,
            attackDamage, attackDamage * splashPercent, splashRadius,
            scorchChance, scorchDuration, scorchDPS, scorchHealthPercent,
            attackDamageType, attackElementalType, this, projSpeed);
    }

    public override string GetDescription() =>
        "Lobs a fire bomb at whichever plant sits deepest in the garden, scorching it and anything nearby.";
}
