using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public enum Aggressivity { Low, Medium, High }

public abstract class Insect : Entity, IAttackable
{
    public static List<Insect> allInsects = new List<Insect>();          // enemies only (the wave)
    public static List<Insect> friendlyInsects = new List<Insect>();     // minions + hypnotized insects
    private static int ObstacleLayer => LayerMask.GetMask("Obstacle");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        allInsects = new List<Insect>();
        friendlyInsects = new List<Insect>();
        SceneManager.sceneLoaded += (scene, mode) => { allInsects.Clear(); friendlyInsects.Clear(); };
    }
    public int currentWaypointIndex = 0;
    protected Transform[] waypoints;
    public bool isFlying = false;
    public static float gravity = 9.8f;
    public static float fallDamageMultiplier = 8f;        // damage = velocity * this + maxHealth * healthPercent
    public static float fallDamageHealthMin = 0.04f;      // 4% maxHealth at minimum velocity
    public static float fallDamageHealthMax = 0.08f;      // 8% maxHealth at peak velocity
    public static float fallDamageHealthVelocityCap = 20f; // velocity at which health % reaches max
    public float verticalVelocity = 0f;
    public Entity fallDamageSource;
    public float fallDamageVulnerabilityMultiplier = 0f;
    public bool affectedByGravity => !isFlying && (!HasEffect<BubblePrisonEffect>() || verticalVelocity < 0f);
    protected virtual bool FallDamageImmune => false;
    public bool isOnGround => visual != null && visual.localPosition.y <= 0.4f;

    [SerializeField] public InsectData data;

    // base and final
    public float movementSpeed, baseMovementSpeed;
    public Vector2 windVelocity;
    public Vector2 windMomentum;
    private Vector2 windBlockMask = Vector2.one;
    public Entity lastSource;
    public Aggressivity aggressivity = Aggressivity.Low;
    public float targetingRange = 0f;
    private float _plantAttackCooldown = 0f;

    // combat side. Friendly = minion / hypnotized: seeks enemy insects, holds or walks back.
    public Team team = Team.Enemy;
    public bool movingBackward = false;                 // friendlies that retreat to find a fight
    protected virtual bool ChasesTarget => true;        // false = hold position, let the target come
    protected virtual bool ScalesWithWave => true;      // false = health is not scaled by wave number
    private const float EngageHold = 0.4f;              // engagement is refreshed each frame while in range
    protected float EngageRange => Mathf.Max(attackRange, targetingRange);

    private Vector3 _preDisplacePosition;
    private bool _isDisplaced = false;
    private bool _returningToPath = false;
    private bool _debugOnPath = false;
    private bool _offPathSlownessActive = false;

    [SerializeField] protected Sprite spriteRight;
    [SerializeField] protected Sprite spriteLeft;
    [System.NonSerialized] protected SpriteRenderer _spriteRenderer;
    protected bool _facingRight = true;
    private Vector3 _prevPosition;
    private Vector3 _spawnPosition;   // where it spawned; friendlies retreat here and despawn

    public virtual IAttackable target
    {
        get
        {
            // friendlies (minions, hypnotized) fight enemy insects and ignore taunts (which aim at plants)
            if (team == Team.Friendly) return FindNearestEnemyInRange();
            IAttackable taunted = GetEffect<TauntEffect>()?.taunter;
            if ((taunted as UnityEngine.Object) != null) return taunted;   // ignore a destroyed taunter
            if (HasEffect<ObliviousEffect>() && aggressivity != Aggressivity.Low) return null;

            // if a flying friendly is attacking this ground enemy, stop in place (can't fight back)
            if (_engagedByFriendly != null)
            {
                if (_engagedByFriendly.IsAlive && _engagedByFriendly.team == Team.Friendly
                    && !CanReach(this, _engagedByFriendly)
                    && Vector3.Distance(transform.position, _engagedByFriendly.transform.position) <= _engagedByFriendly.attackRange + 1f)
                    return _engagedByFriendly;
                _engagedByFriendly = null;
            }

            switch (aggressivity)
            {
                case Aggressivity.High:
                    return FindNearestPlantInRange();
                case Aggressivity.Medium:
                    if (_plantAttackCooldown > 0) return null;
                    return FindNearestPlantInRange();
                default:
                    return null;
            }
        }
    }

    private float attackTimer;

    // bonus
    public float movementSpeedAdder, movementSpeedMultiplier;
    
    public virtual float eatMultiplier => 1f;

    public override void UpdateStats()
    {
        base.UpdateStats();
        movementSpeed = Mathf.Max(0f, baseMovementSpeed + movementSpeedAdder + (baseMovementSpeed * movementSpeedMultiplier));
    }

    public int sunDrop;
    public int expDrop;
    public DamageType attackDamageType = DamageType.Physical;
    public ElementalType attackElementalType = ElementalType.Neutral;

    // Fungal Hypnosis hooks: when a hypnotized insect attacks, credit the damage to this entity
    // (the plant) instead of itself, and slow the victim's attack speed
    public Entity attackSourceOverride;
    public float attackSlowPercent;
    public float attackSlowDuration;

    private GameManager gameManager;
    private Transform aimPoint;
    public Transform visual;
    private Vector2 pathOffset;

    protected override void Awake()
    {
        base.Awake();
        allInsects.Add(this);
        // Debug.Log("SPAWNED: " + gameObject);
    }

    void OnDestroy()
    {
        allInsects.Remove(this);
        friendlyInsects.Remove(this);
        if (PlantUpgradeUI.instance != null && PlantUpgradeUI.instance.GetSelectedInsect() == this)
            PlantUpgradeUI.instance.HidePanel();
    }

    private Transform[]   _pendingPath;
    private Transform[][] _pendingAllPaths;
    private Transform[][] _allPaths;

    /// <summary>Called by SpawnManager immediately after Instantiate, before Start runs.</summary>
    public void SetPath(Transform[] path, Transform[][] allPaths = null)
    {
        _pendingPath     = path;
        _pendingAllPaths = allPaths;
    }

    protected override void Start()
    {
        base.Start();
        gameManager = FindAnyObjectByType<GameManager>();
        waypoints  = _pendingPath ?? PathManager.instance.waypoints;
        _allPaths  = _pendingAllPaths;
        expDrop = sunDrop/2;
        aimPoint = transform.Find("AimPoint");
        visual = transform.Find("Visual");
        _spriteRenderer = visual?.GetComponent<SpriteRenderer>();
        _prevPosition = transform.position;
        _spawnPosition = transform.position;   // captured at spawn (the SpawnPoint it came from)
        pathOffset = new Vector2(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f));


        if (visual != null && healthBarInstance != null)
        {
            healthBarInstance.transform.SetParent(visual);
            healthBarInstance.transform.localPosition = new Vector3(-0.475f, 0.6f, 0);
        }

        if (ScalesWithWave)
        {
            int waveNumber = GameManager.instance.currentWave;
            baseMaxHealth *= 1f + ((waveNumber-1) * 0.04f);
        }
        UpdateStats();
        health = maxHealth;
        RefreshHealthBarVisibility();
    }

    protected override void Update()
    {
        base.Update();
        ApplyGravity();
        Move();
        SyncAimPoint();
        UpdateAttack();
        RefreshEngagement();
        TrackFacing();
        UpdateFacingSprite();
        DebugPathTile();
        UpdateDarknessVisibility();
    }

    // in dark (cave) levels, hide the entire insect (body, health bar, shield, etc.)
    // while it stands outside any emitted light, so the player only sees insects in
    // illuminated areas. uses forceRenderingOff so it never fights enabled/SetActive
    // logic elsewhere. visual only , movement, attacks, and targeting are unaffected
    private float _darknessCheckTimer;
    private const float darknessCheckInterval = 0.15f;
    private Renderer[] _cachedRenderers;
    private bool _renderersHidden;

    private void UpdateDarknessVisibility()
    {
        if (isDying) return;

        _darknessCheckTimer -= Time.deltaTime;
        if (_darknessCheckTimer > 0f) return;
        _darknessCheckTimer = darknessCheckInterval;

        // only cave levels (pitchBlack) hide insects in shadow; other dark levels keep them visible
        bool pitchBlack = DarknessManager.instance != null && DarknessManager.instance.pitchBlack;
        bool shouldHide = pitchBlack && !DarknessManager.instance.IsIlluminated(transform.position);
        if (shouldHide == _renderersHidden && _cachedRenderers != null) return;
        _renderersHidden = shouldHide;

        if (_cachedRenderers == null)
            _cachedRenderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in _cachedRenderers)
            if (r != null) r.forceRenderingOff = shouldHide;
    }

    private void TrackFacing()
    {
        float deltaX = transform.position.x - _prevPosition.x;
        if (Mathf.Abs(deltaX) > 0.001f)
            _facingRight = deltaX > 0;
        _prevPosition = transform.position;
    }

    protected virtual void UpdateFacingSprite()
    {
        if (_spriteRenderer == null) return;
        Sprite s = _facingRight ? spriteRight : spriteLeft;
        if (s != null) _spriteRenderer.sprite = s;
    }

    private void ApplyGravity()
    {
        if (visual == null || !affectedByGravity) return;
        if (!isOnGround || verticalVelocity < 0f)
        {
            verticalVelocity += gravity * Time.deltaTime;
            Vector3 pos = visual.localPosition;
            pos.y -= verticalVelocity * Time.deltaTime;
            if (pos.y <= 0.4f)
            {
                pos.y = 0.4f;
                if (verticalVelocity >= 3f && !FallDamageImmune)
                {
                    float healthPercent = Mathf.Lerp(fallDamageHealthMin, fallDamageHealthMax, Mathf.Clamp01((verticalVelocity - 3f) / (fallDamageHealthVelocityCap - 3f)));
                    float fallDmg = (verticalVelocity * fallDamageMultiplier + maxHealth * healthPercent);
                    Entity src = fallDamageSource != null ? fallDamageSource : lastSource;
                    if (src != null)
                    {
                        fallDmg *= (1f + src.fallDamage) * (1f + fallDamageVulnerabilityMultiplier);
                        Damage(fallDmg, DamageType.Physical, ElementalType.Neutral, src, false, new DamageTag[0]);
                        ApplyEffect(new StunEffect(this, 2f, 1, src));
                    }
                    else
                        Damage(fallDmg, DamageType.Physical, ElementalType.Neutral, new DamageTag[0]);
                }
                verticalVelocity = 0f;
            }
            visual.localPosition = pos;
        }
        else
        {
            verticalVelocity = 0f;
        }
    }

    protected override void OnHoverExit()
    {
        if (health >= maxHealth && !HasShield() && healthBarInstance != null)
            healthBarInstance.SetActive(false);
    }

    protected virtual Vector3 AimPointOffset => Vector3.zero;

    private void SyncAimPoint()
    {
        if (visual != null && aimPoint != null)
        {
            Vector3 desired = visual.localPosition + AimPointOffset;
            if (aimPoint.localPosition != desired)
                aimPoint.localPosition = desired;
        }
    }

    protected virtual void Move()
    {
        if (isDying) return;
        if (waypoints == null) return;

        bool wasDisplaced = windMomentum.sqrMagnitude > 0.001f;

        if (windVelocity.sqrMagnitude > 0.001f)
        {
            if (!_isDisplaced && !_returningToPath)
            {
                _preDisplacePosition = transform.position;
                _isDisplaced = true;
            }
            windMomentum = new Vector2(windVelocity.x * windBlockMask.x, windVelocity.y * windBlockMask.y);
            windVelocity = Vector2.zero;
        }
        else
        {
            windBlockMask = Vector2.one;
            windMomentum = Vector2.Lerp(windMomentum, Vector2.zero, 5f * Time.deltaTime);
            if (windMomentum.sqrMagnitude <= 0.001f) windMomentum = Vector2.zero;
        }

        windBlockMask = Vector2.one;

        if (windMomentum.sqrMagnitude > 0.001f)
        {
            Vector3 delta = (Vector3)windMomentum * Time.deltaTime;
            Vector3 newPos = transform.position + delta;
            if (!Physics2D.OverlapCircle(newPos, 0.3f, ObstacleLayer))
            {
                transform.position = newPos;
            }
            else
            {
                Vector3 newPosX = transform.position + new Vector3(delta.x, 0f, 0f);
                Vector3 newPosY = transform.position + new Vector3(0f, delta.y, 0f);
                if (!Physics2D.OverlapCircle(newPosX, 0.3f, ObstacleLayer))
                {
                    transform.position = newPosX;
                    windMomentum.y = 0f;
                    windBlockMask.y = 0f;
                }
                else if (!Physics2D.OverlapCircle(newPosY, 0.3f, ObstacleLayer))
                {
                    transform.position = newPosY;
                    windMomentum.x = 0f;
                    windBlockMask.x = 0f;
                }
                else
                {
                    windMomentum = Vector2.zero;
                    windBlockMask = Vector2.zero;
                }
            }
        }

        // displacement just ended this frame , begin walking back to the pre-displace position
        if (wasDisplaced && windMomentum.sqrMagnitude <= 0.001f)
        {
            _isDisplaced = false;
            _returningToPath = true;
        }

        if (HasEffect<HardCrowdControl>()) return;
        if (HasEffect<BubblePrisonEffect>()) return;
        if (affectedByGravity && !isOnGround) return;

        if (_returningToPath)
        {
            float dist = Vector3.Distance(transform.position, _preDisplacePosition);
            if (dist < 0.1f)
            {
                _returningToPath = false;
            }
            else
            {
                Vector3 dir = (_preDisplacePosition - transform.position).normalized;
                transform.position += dir * GetMoveSpeed() * Time.deltaTime;
            }
            return;
        }

        if (target != null)
        {
            if (!target.IsAlive) { RemoveEffect<TauntEffect>(); }
            else
            {
                // held by an unreachable flyer (e.g. a flying friendly): stop in place, don't chase up to it
                if (target is Insect ti && !CanReach(this, ti)) return;

                Vector3 approachPoint = target.GetApproachPoint(transform.position);
                float dist = Vector3.Distance(transform.position, approachPoint);
                if (dist > attackRange && ChasesTarget)
                {
                    Vector3 dir = (approachPoint - transform.position).normalized;
                    transform.position += dir * GetMoveSpeed() * Time.deltaTime;
                }
                return;
            }
        }

        // friendlies with no target never walk the path forward; their idle behavior is overridable
        // (minions hold, hypnotized retreat, shroomlets return to their hold point)
        if (team == Team.Friendly)
        {
            FriendlyIdle();
            return;
        }

        if (currentWaypointIndex >= waypoints.Length)
        {
            ReachObjective();
            return;
        }

        Transform waypoint = waypoints[currentWaypointIndex];
        Vector3 targetPos = waypoint.position + new Vector3(pathOffset.x, pathOffset.y, 0);
        Vector3 direction = (targetPos - transform.position).normalized;
        transform.position += direction * GetMoveSpeed() * Time.deltaTime;

        if (Vector3.Distance(transform.position, targetPos) < 0.1f)
        {
            currentWaypointIndex++;
        }
    }

    private void DebugPathTile()
    {
        bool onPath = false;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0f);
        foreach (Collider2D c in hits)
        {
            Tile tile = c.GetComponent<Tile>();
            if (tile != null && tile.tileType == TileType.Path) { onPath = true; break; }
        }

        if (onPath && !_debugOnPath)
        {
            Debug.Log($"[{gameObject.name}] entered path tile at {transform.position}");
            if (_offPathSlownessActive)
            {
                _offPathSlownessActive = false;
                RemoveEffect<OffPathSlownessEffect>();
            }
        }
        else if (!onPath && _debugOnPath)
        {
            Debug.Log($"[{gameObject.name}] left path tile at {transform.position}");
            if (!isFlying && !isDying && !_offPathSlownessActive && team != Team.Friendly)
            {
                _offPathSlownessActive = true;
                ApplyEffect(new OffPathSlownessEffect(this));
            }
        }

        _debugOnPath = onPath;
    }

    private void SnapToNearestWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        float       nearestDist  = float.MaxValue;
        int         nearestIndex = currentWaypointIndex;
        Transform[] nearestPath  = waypoints;

        // Search every known path (own + other spawn entries)
        Transform[][] searchPaths = (_allPaths != null && _allPaths.Length > 0)
            ? _allPaths
            : new Transform[][] { waypoints };

        foreach (Transform[] path in searchPaths)
        {
            if (path == null) continue;
            for (int i = 0; i < path.Length; i++)
            {
                if (path[i] == null) continue;
                float dist = Vector3.Distance(transform.position, path[i].position);
                if (dist < nearestDist)
                {
                    nearestDist  = dist;
                    nearestIndex = i;
                    nearestPath  = path;
                }
            }
        }

        // Switch to whichever path had the closest waypoint, targeting the one after it
        waypoints            = nearestPath;
        currentWaypointIndex = Mathf.Min(nearestIndex + 1, waypoints.Length - 1);
    }

    protected override Vector3 GetIndicatorPosition()
    {
        return visual != null ? visual.position + Vector3.up * 0.25f : base.GetIndicatorPosition();
    }

    public Vector3 GetAimPoint()
    {
        // aim at the visual's world position, which follows vertical displacement (e.g. levitation);
        // falls back to the AimPoint child or the root if there's no visual
        if (visual != null) return visual.position;
        return aimPoint != null ? aimPoint.position : transform.position;
    }

    public Transform GetCurrentWaypoint()
    {
        if (currentWaypointIndex >= waypoints.Length)
        return null;

        return waypoints[currentWaypointIndex];
    }

    private void UpdateAttack()
    {
        if (_plantAttackCooldown > 0)
            _plantAttackCooldown -= Time.deltaTime;

        if (target == null) return;
        if (!target.IsAlive) { RemoveEffect<TauntEffect>(); return; }
        if (target is Insect ti && !CanReach(this, ti)) return;   // cannot hit a flyer it can't reach
        if (HasEffect<HardCrowdControl>()) return;
        if (attackSpeed <= 0) return;

        float dist = Vector3.Distance(transform.position, target.GetApproachPoint(transform.position));
        if (dist > attackRange) return;

        attackTimer += Time.deltaTime;
        if (attackTimer >= 1f / attackSpeed)
        {
            attackTimer = 0f;
            IAttackable currentTarget = target;
            Attack();
            if (aggressivity == Aggressivity.Medium && currentTarget is Plant)
                _plantAttackCooldown = 4f;
        }
    }

    public virtual void Attack()
    {
        if (target == null) return;
        target.ReceiveAttack(attackDamage, this);
    }

    private IAttackable FindNearestPlantInRange()
    {
        Plant nearest = null;
        float nearestDist = Mathf.Infinity;
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive) continue;
            if (isOnGround && plant.occupiedTile != null && plant.occupiedTile.isHighground) continue;
            // non-flying insects cannot reach plants sitting on water
            if (!isFlying && plant.occupiedTile != null && plant.occupiedTile.tileType == TileType.Water) continue;
            float dist = Vector3.Distance(transform.position, plant.GetApproachPoint(transform.position));
            if (dist <= targetingRange && dist < nearestDist)
            {
                nearestDist = dist;
                nearest = plant;
            }
        }
        return nearest;
    }

    // moves this insect between the enemy pool (allInsects) and the friendly pool. friendlies
    // are out of allInsects so every plant/AoE that iterates it ignores them (no friendly fire)
    // friendly units (minions, hypnotized) show a green health bar; enemies stay red
    protected override Color HealthBarColor => team == Team.Friendly ? Color.green : Color.red;

    public void SetTeam(Team newTeam)
    {
        if (team == newTeam) return;
        team = newTeam;
        if (newTeam == Team.Friendly)
        {
            allInsects.Remove(this);
            if (!friendlyInsects.Contains(this)) friendlyInsects.Add(this);
        }
        else
        {
            friendlyInsects.Remove(this);
            if (!allInsects.Contains(this)) allInsects.Add(this);
        }
        RefreshHealthBarColor();   // green when friendly, red when enemy
    }

    // the hypnotize transition (PvZ hypno style), triggered by HypnotizedEffect.OnApply. it counts
    // as a kill (sun/exp/death events) but keeps the body alive as a full-health friendly that
    // retreats. to hypnotize an insect, apply a HypnotizedEffect (or a subclass) , don't call this
    // directly. harmless / no-op on an already friendly insect
    public void ApplyHypnosis(Entity source)
    {
        if (team == Team.Friendly) return;
        if (source is Plant p) RegisterAttacker(p);
        DistributeExp();
        RaiseEntityDied(new EntityEventData { target = this, source = source, position = transform.position });
        GameManager.instance?.AddSun(Mathf.CeilToInt(sunDrop * (1f + (source != null ? source.sunYieldBonus : 0f))));

        RemoveEffect<TauntEffect>();   // drop any taunt/engagement it had as an enemy
        RemoveAllDebuffs();
        SetTeam(Team.Friendly);
        movingBackward = true;
        health = maxHealth;            // turned insects come back at full health
        UpdateHealthBar();
    }

    protected Insect _engagedEnemy;   // the enemy a friendly is locked onto (target stickiness)
    private Insect _engagedByFriendly; // the friendly insect currently attacking this enemy (stops it in place)

    // a unit can hit another unless it is grounded while the target flies. so: ground hits ground,
    // flying hits anyone, ground cannot hit flying
    protected static bool CanReach(Insect attacker, Insect target) => attacker.isFlying || !target.isFlying || attacker.attackRange >= 1f;

    // nearest enemy insect within engage range (used by friendlies). enemies live in allInsects
    protected IAttackable FindNearestEnemyInRange()
    {
        return StickyEnemy(e => Vector3.Distance(transform.position, e.transform.position) <= EngageRange);
    }

    // keeps the friendly locked onto one enemy until it dies or leaves the valid set, then picks the
    // nearest new one. this stops enemies that merely pass by from being briefly targeted/taunted.
    // only enemies this unit can actually hit are considered (a ground friendly ignores flyers)
    protected Insect StickyEnemy(System.Func<Insect, bool> isValid)
    {
        if (_engagedEnemy != null && _engagedEnemy.IsAlive && _engagedEnemy.team != team &&
            CanReach(this, _engagedEnemy) && isValid(_engagedEnemy))
            return _engagedEnemy;

        Insect nearest = null;
        float nearestDist = Mathf.Infinity;
        foreach (Insect enemy in allInsects)
        {
            if (enemy == null || !enemy.IsAlive || !CanReach(this, enemy) || !isValid(enemy)) continue;
            float d = Vector3.Distance(transform.position, enemy.transform.position);
            if (d < nearestDist) { nearestDist = d; nearest = enemy; }
        }
        _engagedEnemy = nearest;
        return nearest;
    }

    // a friendly locks its single current target into combat (stops it advancing and makes it
    // fight back). block capacity is one enemy: other enemies in range are not held and walk on.
    // many friendlies can still gang the same enemy, but only one of them "holds" the engagement
    private void RefreshEngagement()
    {
        if (team != Team.Friendly) return;
        Insect enemy = target as Insect;
        if (enemy == null || !enemy.IsAlive) return;
        if (Vector3.Distance(transform.position, enemy.transform.position) > EngageRange) return;

        EngagedEffect existing = enemy.GetEffect<EngagedEffect>();
        if (existing != null && existing.taunter != null && existing.taunter.IsAlive)
        {
            if (ReferenceEquals(existing.taunter, this)) existing.duration = EngageHold; // refresh mine
            return; // already held by a living friendly; I still attack it via my target
        }
        enemy.ApplyEffect(new EngagedEffect(enemy, EngageHold, 1, this, this));
    }

    // what a friendly does when it has no target. default: retreat if hypnotized, else hold still
    protected virtual void FriendlyIdle()
    {
        if (movingBackward) MoveBackward();
    }

    // friendlies that retreat walk back toward the previous waypoint, looking for a fight. once
    // they pass waypoint 0 they head for the spawn point they came from, and despawn on arrival
    private void MoveBackward()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Vector3 targetPos;
        if (currentWaypointIndex > 0)
        {
            Transform wp = waypoints[currentWaypointIndex - 1];
            if (wp == null) return;
            targetPos = wp.position + new Vector3(pathOffset.x, pathOffset.y, 0);
        }
        else
        {
            targetPos = _spawnPosition;   // before the first waypoint: head back to the spawn
        }

        Vector3 dir = (targetPos - transform.position).normalized;
        transform.position += dir * GetMoveSpeed() * Time.deltaTime;

        if (Vector3.Distance(transform.position, targetPos) < 0.1f)
        {
            if (currentWaypointIndex > 0) currentWaypointIndex--;
            else QuietDeath();            // reached the spawn point: despawn (and clean up its engagements)
        }
    }

    protected virtual void ReachObjective()
    {
        gameManager.Damage((int)baseAttackDamage);
        Destroy(gameObject);
    }
    
    protected virtual float GetMoveSpeed() => movementSpeed;

    public virtual Vector3 GetVelocity()
    {
        if (currentWaypointIndex >= waypoints.Length)
        return Vector3.zero;

        Transform target = waypoints[currentWaypointIndex];
        Vector3 targetPos = target.position + new Vector3(pathOffset.x, pathOffset.y, 0);
        Vector3 direction = (targetPos - transform.position).normalized;
        return direction * movementSpeed;
    }

    protected bool isDying = false;

    public override void Kill(Entity source)
    {
        if (isDying) return;
        if (team == Team.Friendly) { QuietDeath(); return; }   // already counted as killed when turned
        isDying = true;
        foreach (StatusEffect e in activeEffects) e.OnTargetDied();
        RaiseEntityDied(new EntityEventData { target = this, source = source, position = transform.position });
        if (PlantUpgradeUI.instance?.GetSelectedInsect() == this) PlantUpgradeUI.instance.HidePanel();
        DistributeExp();
        // the killer's sunYieldBonus (e.g. Aeonium's Blessing) increases sun dropped, rounded up
        gameManager.AddSun(Mathf.CeilToInt(sunDrop * (1f + (source != null ? source.sunYieldBonus : 0f))));
        allInsects.Remove(this);
        friendlyInsects.Remove(this);
        StartCoroutine(DeathFade());
    }

    public override void Kill()
    {
        if (isDying) return;
        if (team == Team.Friendly) { QuietDeath(); return; }   // already counted as killed when turned
        isDying = true;
        foreach (StatusEffect e in activeEffects) e.OnTargetDied();
        RaiseEntityDied(new EntityEventData { target = this, position = transform.position });
        if (PlantUpgradeUI.instance?.GetSelectedInsect() == this) PlantUpgradeUI.instance.HidePanel();
        DistributeExp();
        gameManager.AddSun(sunDrop);
        allInsects.Remove(this);
        friendlyInsects.Remove(this);
        StartCoroutine(DeathFade());
    }

    // a friendly (minion or hypnotized) dies without any reward, event, or sun (it was already
    // counted as killed when summoned/turned). just fades out and leaves the rosters
    private void QuietDeath()
    {
        isDying = true;
        foreach (StatusEffect e in activeEffects) e.OnTargetDied();
        ClearEngagementsByMe();   // its former victims must stop targeting this now-gone friendly
        if (PlantUpgradeUI.instance?.GetSelectedInsect() == this) PlantUpgradeUI.instance.HidePanel();
        allInsects.Remove(this);
        friendlyInsects.Remove(this);
        StartCoroutine(DeathFade());
    }

    // drops the EngagedEffects this insect created, so its victims release once it is gone
    public void ClearEngagementsByMe()
    {
        foreach (Insect e in allInsects)
        {
            EngagedEffect eng = e != null ? e.GetEffect<EngagedEffect>() : null;
            if (eng != null && ReferenceEquals(eng.taunter, this))
                e.RemoveEffect<EngagedEffect>();
        }
    }

    private IEnumerator DeathFade()
    {
        healthBarInstance?.SetActive(false);

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        Color[] startColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            startColors[i] = renderers[i].color;

        float duration = 0.4f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Color c = startColors[i];
                c.a = alpha;
                renderers[i].color = c;
            }
            yield return null;
        }

        Destroy(gameObject);
    }


    // EXP MANAGING
    
    public HashSet<Plant> attackerSet = new HashSet<Plant>();

    public void RegisterAttacker(Plant source)
    {
        if (source == null) return;
        attackerSet.Add(source); // if source already exists, the hashset automatically ignores it
    }

    private void DistributeExp()
    {
        int attackerCount = attackerSet.Count;
        if (attackerCount == 0) return;

        // calculating the share per plant with a minimum of 25% obtained
        float share = Mathf.Max(0.25f, 1f / attackerCount);
        int expReward = (int)(expDrop*share);

        foreach (Plant plant in attackerSet)
        {
            plant.GainExp(expReward);
        }
    }




    void OnMouseDown()
    {
        if (SkillTargetingManager.instance != null && SkillTargetingManager.instance.IsTargeting) return;
        PlantUpgradeUI.instance?.ShowPanel(this);
    }

    // IAttackable
    public bool ReceiveAttack(float damage, Insect attacker)
    {
        float missChance = Mathf.Clamp01(evasion - attacker.accuracy);
        if (UnityEngine.Random.value < missChance)
        {
            StatusIndicator.Spawn(GetIndicatorPosition(), "Miss", new Color(0.55f, 0.6f, 0.75f));
            return false;
        }
        // Fungal Hypnosis: credit the damage to the controlling plant and slow the victim's attacks
        Entity src = attacker.attackSourceOverride != null ? attacker.attackSourceOverride : attacker;
        // friendlies (minions and hypnotized insects) tag their hits as a minion attack
        DamageTag[] tags = attacker.team == Team.Friendly
            ? new DamageTag[] { DamageTag.Melee, DamageTag.Attack, DamageTag.MinionAttack }
            : new DamageTag[] { DamageTag.Melee, DamageTag.Attack };
        Damage(damage, attacker.attackDamageType, attacker.attackElementalType, src, false, tags);
        // Damage lifesteals to 'src'; when the credit is overridden (fungal hypnosis) the attacking
        // insect still heals itself off its own lifesteal (e.g. a hypnotized mosquito)
        if (src != attacker && attacker.lifesteal > 0f && attacker.IsAlive)
            attacker.Heal(damage * attacker.lifesteal);
        if (attacker.attackSlowPercent > 0f && IsAlive)
            ApplyEffect(new AttackSpeedSlowEffect(this, attacker.attackSlowDuration, 1, src, attacker.attackSlowPercent));
        if (attacker.team == Team.Friendly && team != Team.Friendly)
            _engagedByFriendly = attacker;
        return true;
    }
    public bool IsAlive => health > 0 && !isDying;
    public Vector3 Position => transform.position;
    public Vector3 GetApproachPoint(Vector3 _) => transform.position;

    // DESCRIPTIONS
    protected void LoadData()
    {
        if (data == null) return;
        baseMaxHealth          = data.baseMaxHealth;
        baseAttackDamage       = data.baseAttackDamage;
        baseMagicPower        = data.baseMagicPower;
        baseAttackSpeed        = data.baseAttackSpeed;
        baseAttackRange        = data.baseAttackRange;
        baseMovementSpeed      = data.baseMovementSpeed;
        targetingRange         = data.baseTargetingRange;
        baseLifesteal          = data.baseLifesteal;
        basePhysicalDamage     = data.basePhysicalDamage;
        baseMagicDamage        = data.baseMagicDamage;
        baseArmorPenFlat       = data.baseArmorPenFlat;
        baseArmorPenPercent    = data.baseArmorPenPercent;
        baseMagicPenFlat       = data.baseMagicPenFlat;
        baseMagicPenPercent    = data.baseMagicPenPercent;
        baseTenacity           = data.baseTenacity;
        baseArmor      = data.baseArmor;
        baseMagicArmor = data.baseMagicArmor;
        baseFireResistance     = data.baseFireResistance;
        baseWaterResistance    = data.baseWaterResistance;
        baseNatureResistance   = data.baseNatureResistance;
        baseWindResistance     = data.baseWindResistance;
        basePoisonResistance   = data.basePoisonResistance;
        baseIceResistance      = data.baseIceResistance;
        baseDotResistance      = data.baseDotResistance;
        sunDrop                = data.sunDrop;
        aggressivity           = data.aggressivity;
        attackDamageType       = data.attackDamageType;
        attackElementalType    = data.attackElementalType;
        baseLightEmissionRange = data.baseLightEmissionRange;
        baseHealingBonus       = data.baseHealingBonus;
        baseHealingReceived    = data.baseHealingReceived;
        startingShield         = data.startingShield;
        baseEvasion            = data.baseEvasion;
        baseAccuracy           = data.baseAccuracy;
    }

    public virtual string GetName()
    {
        return data != null ? data.displayName : "";
    }

    public virtual string GetDescription()
    {
        return data != null ? data.description : "";
    }

    public virtual string GetPassiveDescription()
    {
        return data != null ? data.passiveDescription : "";
    }

    public string GetActiveEffectsString()
    {
        string negative = "";
        string positive = "";

        foreach (StatusEffect effect in activeEffects)
        {
            string time;
            if (float.IsInfinity(effect.duration))
                time = "∞";   // permanent effects show the infinity symbol
            else
            {
                int mins = Mathf.FloorToInt(effect.duration / 60);
                int secs = Mathf.FloorToInt(effect.duration % 60);
                time = $"{mins}:{secs:D2}";
            }
            string entry = $"{effect.GetName()} ({time})\n";

            if (effect.effectType == StatusEffect.Type.positive)
                positive += entry;
            else
                negative += entry;
        }

        return $"<b>Active Effects:</b>\n\n" +
               $"<b>Positive:</b>\n{(positive == "" ? "None" : positive)}\n" +
               $"<b>Negative:</b>\n{(negative == "" ? "None" : negative)}";
    }
}
