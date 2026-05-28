using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public enum Aggressivity { Low, Medium, High }

public abstract class Insect : Entity, IAttackable
{
    public static List<Insect> allInsects = new List<Insect>();
    public static event System.Action<Vector3> OnInsectKilled;
    public static event System.Action<Insect> OnInsectDied;
    private static int ObstacleLayer => LayerMask.GetMask("Obstacle");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        allInsects = new List<Insect>();
        SceneManager.sceneLoaded += (scene, mode) => allInsects.Clear();
    }
    public int currentWaypointIndex = 0;
    protected Transform[] waypoints;
    public bool isFlying = false;
    public static float gravity = 9.8f;
    public static float fallDamageMultiplier = 8f; // TUNING KNOB — damage = verticalVelocity * this
    public float verticalVelocity = 0f;
    public Entity fallDamageSource;
    public bool affectedByGravity => !isFlying && (!HasEffect<BubblePrisonEffect>() || verticalVelocity < 0f);
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

    private Vector3 _preDisplacePosition;
    private bool _isDisplaced = false;
    private bool _returningToPath = false;
    private bool _offPathSlownessActive = false;

    [SerializeField] protected Sprite spriteRight;
    [SerializeField] protected Sprite spriteLeft;
    [System.NonSerialized] protected SpriteRenderer _spriteRenderer;
    protected bool _facingRight = true;
    private Vector3 _prevPosition;

    public virtual IAttackable target
    {
        get
        {
            IAttackable taunted = GetEffect<TauntEffect>()?.taunter;
            if (taunted != null) return taunted;
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
        movementSpeed = baseMovementSpeed + movementSpeedAdder + (baseMovementSpeed * movementSpeedMultiplier);
    }

    public int sunDrop;
    public int expDrop;
    public DamageType attackDamageType = DamageType.Physical;
    public ElementalType attackElementalType = ElementalType.Neutral;

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
        pathOffset = new Vector2(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f));


        if (visual != null && healthBarInstance != null)
        {
            healthBarInstance.transform.SetParent(visual);
            healthBarInstance.transform.localPosition = new Vector3(-0.475f, 0.6f, 0);
        }

        int waveNumber = GameManager.instance.currentWave;
        baseMaxHealth *= 1f + ((waveNumber-1) * 0.04f);
        UpdateStats();
        health = maxHealth;
    }

    protected override void Update()
    {
        base.Update();
        ApplyGravity();
        Move();
        SyncAimPoint();
        UpdateAttack();
        TrackFacing();
        UpdateFacingSprite();
        UpdateOffPathSlowness();
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
                if (verticalVelocity >= 3f)
                {
                    Entity src = fallDamageSource != null ? fallDamageSource : lastSource;
                    if (src != null)
                    {
                        Damage(verticalVelocity * fallDamageMultiplier, DamageType.Physical, ElementalType.Neutral, src, false, new DamageTag[0]);
                        ApplyEffect(new StunEffect(this, 2f, 1, src));
                    }
                    else
                        Damage(verticalVelocity * fallDamageMultiplier, DamageType.Physical, ElementalType.Neutral, new DamageTag[0]);
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

    private void SyncAimPoint()
    {
        if (visual != null && aimPoint != null && aimPoint.localPosition != visual.localPosition)
            aimPoint.localPosition = visual.localPosition;
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

        // displacement just ended this frame — begin walking back to the pre-displace position
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
                Vector3 approachPoint = target.GetApproachPoint(transform.position);
                float dist = Vector3.Distance(transform.position, approachPoint);
                if (dist > attackRange)
                {
                    Vector3 dir = (approachPoint - transform.position).normalized;
                    transform.position += dir * GetMoveSpeed() * Time.deltaTime;
                }
                return;
            }
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

    private void UpdateOffPathSlowness()
    {
        if (isFlying || isDying)
        {
            if (_offPathSlownessActive)
            {
                _offPathSlownessActive = false;
                RemoveEffect<OffPathSlownessEffect>();
            }
            return;
        }

        Vector3 snapped = new Vector3(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.y), 0f);
        bool onPath = false;
        if (Tile.allTiles.TryGetValue(Tile.TileKey(snapped), out Tile t))
            onPath = t.tileType == TileType.Path;

        bool shouldSlow = isOnGround && !onPath;

        if (shouldSlow && !_offPathSlownessActive)
        {
            _offPathSlownessActive = true;
            ApplyEffect(new OffPathSlownessEffect(this));
        }
        else if (!shouldSlow && _offPathSlownessActive)
        {
            _offPathSlownessActive = false;
            RemoveEffect<OffPathSlownessEffect>();
        }
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
            float dist = Vector3.Distance(transform.position, plant.GetApproachPoint(transform.position));
            if (dist <= targetingRange && dist < nearestDist)
            {
                nearestDist = dist;
                nearest = plant;
            }
        }
        return nearest;
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
        isDying = true;
        foreach (StatusEffect e in activeEffects) e.OnTargetDied();
        OnInsectKilled?.Invoke(transform.position);
        OnInsectDied?.Invoke(this);
        if (PlantUpgradeUI.instance?.GetSelectedInsect() == this) PlantUpgradeUI.instance.HidePanel();
        DistributeExp();
        gameManager.AddSun(sunDrop);
        allInsects.Remove(this);
        StartCoroutine(DeathFade());
    }

    public override void Kill()
    {
        if (isDying) return;
        isDying = true;
        foreach (StatusEffect e in activeEffects) e.OnTargetDied();
        OnInsectKilled?.Invoke(transform.position);
        OnInsectDied?.Invoke(this);
        if (PlantUpgradeUI.instance?.GetSelectedInsect() == this) PlantUpgradeUI.instance.HidePanel();
        DistributeExp();
        gameManager.AddSun(sunDrop);
        allInsects.Remove(this);
        StartCoroutine(DeathFade());
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
    public void ReceiveAttack(float damage, Insect attacker)
    {
        Damage(damage, attacker.attackDamageType, attacker.attackElementalType, attacker, false, new DamageTag[] { DamageTag.Melee, DamageTag.Attack });
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
        baseLifesteal          = data.baseLifesteal;
        basePhysicalDamage     = data.basePhysicalDamage;
        baseMagicDamage        = data.baseMagicDamage;
        baseTenacity           = data.baseTenacity;
        basePhysicalResistance = data.basePhysicalResistance;
        baseMagicResistance    = data.baseMagicResistance;
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
            int mins = Mathf.FloorToInt(effect.duration / 60);
            int secs = Mathf.FloorToInt(effect.duration % 60);
            string entry = $"{effect.GetName()} ({mins}:{secs:D2})\n";

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
