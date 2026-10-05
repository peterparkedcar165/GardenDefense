using UnityEngine;
using System.Collections.Generic;

// the thrown shield, sitting on the path after its throw lands. clone of AcornBomb adapted for
// Acorn Knight: no fall-from-sky animation and no impact burst (the throw's projectile already
// dealt its hit and stun before this spawns), just hp, lifetime, taunt and path blocking, same as
// the old bomb. kept as its own class rather than editing AcornBomb.cs, which is now unused by
// the live AcornKnight but left in place rather than deleted outright
public class AcornKnightShield : Minion
{
    private float radius;
    private Transform shadow;

    private const float stunDuration = 2f;

    private readonly HashSet<Insect> tauntedInsects = new HashSet<Insect>();
    private float tauntTickTimer = 0f;

    // skill tree node unlock ids, kept identical to the old bomb's so existing skillPurchases
    // on the shared tree asset still resolve to a working node
    private const string GrassDotUnlock = "acorn_bomb_grass_dot";
    private const string MagicArmorUnlock = "acorn_bomb_magic_armor";
    private const float GrassDotTickInterval = 1f;
    private const float GrassDotArmorPercent = 0.25f;
    private const int MagicArmorBonus = 100;
    private bool _hasGrassDot;
    private float _grassDotTimer;
    private static readonly DamageTag[] grassDotTags = { DamageTag.SkillDamage, DamageTag.AoE };

    private Collider2D _clickCollider;
    private SpriteRenderer _visualSR;
    private SpriteRenderer[] _outlineRenderers;
    private bool _isHovered;

    private GameObject _lifetimeBarInstance;
    private Transform _lifetimeBarFill;
    private float _lifetimeElapsed;

    private bool _goneNotified;
    // fired once, whether the shield expires or is destroyed, so the Knight can start its skill
    // cooldown and begin the re-equip delay
    public event System.Action OnShieldGone;

    public override string GetName() => "<b><color=green>Shield</color></b>";
    public override string GetDescription()
    {
        float remaining = Mathf.Max(0f, lifetime - _lifetimeElapsed);
        return $"Blocks path while it lasts\nTime remaining: {Mathf.CeilToInt(remaining)}s";
    }

    // called by AcornKnight immediately after Instantiate, once the throw has landed
    public void Initialize(float radius, float maxHp, float lifespan, Plant source)
    {
        this.radius = radius;
        owner = source;
        lifetime = lifespan;
        baseMaxHealth = maxHp;
        baseArmor = source != null ? (int)source.armor : 0;
        baseMovementSpeed = 0f;
        baseFireResistance = -0.5f;
        isFlying = false;

        _hasGrassDot = SkillTreeManager.HasUnlock(source, GrassDotUnlock);
        if (SkillTreeManager.HasUnlock(source, MagicArmorUnlock))
            baseMagicArmor = MagicArmorBonus;

        visual = transform.Find("Visual");
        if (visual != null)
            visual.localScale = new Vector3(radius, radius, 1f);

        shadow = transform.Find("Shadow");
        if (shadow != null)
            shadow.localScale = new Vector3(0.8f * radius, 0.4f * radius, 1f);

        SetRangeCircle(radius);

        _clickCollider = GetComponent<Collider2D>();
        if (_clickCollider == null)
        {
            var col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = radius * 0.45f;
            col.isTrigger = true;
            _clickCollider = col;
        }

        UpdateStats();
        health = maxHealth;

        if (IsOnWaterTile())
        {
            Kill();
            return;
        }
    }

    protected override void Start()
    {
        base.Start();
        OnHeal += HandleHeal;

        if (healthBarInstance != null)
        {
            healthBarInstance.transform.SetParent(transform);
            healthBarInstance.transform.localPosition = new Vector3(-0.475f, 0.4f, 0f);
        }

        SpawnLifetimeBar();

        _visualSR = GetComponentInChildren<SpriteRenderer>();
        if (_visualSR != null)
        {
            Shader silhouette = Shader.Find("Custom/SpriteSilhouette");
            Material mat = silhouette != null ? new Material(silhouette) : null;
            float localOffset = radius > 0f ? 0.05f / radius : 0.05f;
            const int count = 8;
            _outlineRenderers = new SpriteRenderer[count];
            for (int i = 0; i < count; i++)
            {
                float angle = i * (360f / count) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * localOffset;
                GameObject obj = new GameObject("Outline");
                obj.transform.SetParent(_visualSR.transform);
                obj.transform.localPosition = offset;
                obj.transform.localScale = Vector3.one;
                obj.transform.localRotation = Quaternion.identity;
                obj.layer = gameObject.layer;
                SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
                sr.sprite = _visualSR.sprite;
                sr.sortingLayerID = _visualSR.sortingLayerID;
                sr.sortingOrder = _visualSR.sortingOrder - 1;
                sr.color = Color.white;
                if (mat != null) sr.material = mat;
                sr.enabled = false;
                _outlineRenderers[i] = sr;
            }
        }
    }

    private void SpawnLifetimeBar()
    {
        GameObject prefab = Resources.Load<GameObject>("HealthBar");
        if (prefab == null) return;

        _lifetimeBarInstance = Instantiate(prefab, transform);
        _lifetimeBarInstance.transform.localPosition = new Vector3(-0.475f, 0.316f, 0f);

        Vector3 scale = _lifetimeBarInstance.transform.localScale;
        scale.x *= 0.75f;
        scale.y *= 0.35f;
        _lifetimeBarInstance.transform.localScale = scale;

        _lifetimeBarFill = _lifetimeBarInstance.transform.Find("Fill");
        if (_lifetimeBarFill != null)
        {
            SpriteRenderer sr = _lifetimeBarFill.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = Color.grey;
        }

        Transform shieldChild = _lifetimeBarInstance.transform.Find("ShieldFill");
        if (shieldChild != null) Destroy(shieldChild.gameObject);

        _lifetimeBarInstance.SetActive(false);
    }

    protected override void OnHover()     { base.OnHover();     _isHovered = true; }
    protected override void OnHoverExit()
    {
        base.OnHoverExit();
        _isHovered = false;
        if (PlantUpgradeUI.instance?.GetSelectedInsect() == this)
            ShowHealthBar();
    }

    void OnMouseDown()
    {
        if (SkillTargetingManager.instance != null && SkillTargetingManager.instance.IsTargeting) return;
        PlantUpgradeUI.instance?.ShowPanel(this);
    }

    private void HandleHeal(EntityEventData data)
    {
        if (data.target != this) return;
        AcornKnight knight = owner as AcornKnight;
        if (knight == null || !knight.IsPath3Maxed) return;
        lifetime += data.amount * 0.02f;
    }

    private void OnDestroy()
    {
        OnHeal -= HandleHeal;
    }

    public override void Attack() { }

    protected override void Update()
    {
        if (isDying) return;

        base.Update();
        _lifetimeElapsed += Time.deltaTime;
        UpdateTaunt();
        if (_hasGrassDot) UpdateGrassDot();

        if (rangeCircle != null)
            rangeCircle.position = transform.position;

        if (_outlineRenderers != null)
        {
            bool show = _isHovered || (PlantUpgradeUI.instance?.GetSelectedInsect() == this);
            if (_visualSR != null)
                foreach (var r in _outlineRenderers)
                    if (r != null) { r.sprite = _visualSR.sprite; r.enabled = show; }
        }

        if (_lifetimeBarInstance != null)
        {
            bool selected = PlantUpgradeUI.instance?.GetSelectedInsect() == this;
            _lifetimeBarInstance.SetActive(_isHovered || selected);
            if (_lifetimeBarFill != null && lifetime > 0f)
            {
                float remaining = Mathf.Clamp01(1f - (_lifetimeElapsed / lifetime));
                Vector3 s = _lifetimeBarFill.localScale;
                s.x = remaining;
                _lifetimeBarFill.localScale = s;
            }
        }
    }

    private bool IsOnWaterTile()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.1f);
        foreach (Collider2D c in hits)
        {
            Tile tile = c.GetComponent<Tile>();
            if (tile != null && tile.tileType == TileType.Water) return true;
        }
        return false;
    }

    private void UpdateGrassDot()
    {
        _grassDotTimer -= Time.deltaTime;
        if (_grassDotTimer > 0f) return;
        _grassDotTimer = GrassDotTickInterval;

        float tickDamage = armor * GrassDotArmorPercent;
        foreach (Insect insect in new List<Insect>(Insect.allInsects))
        {
            if (insect == null || !insect.IsAlive) continue;
            if (Vector3.Distance(transform.position, insect.transform.position) <= radius)
                insect.Damage(tickDamage, DamageType.Physical, ElementalType.Grass, owner, false, grassDotTags);
        }
    }

    private void UpdateTaunt()
    {
        tauntedInsects.RemoveWhere(i => i == null || i.gameObject == null);
        tauntTickTimer -= Time.deltaTime;
        if (tauntTickTimer > 0f) return;
        tauntTickTimer = 0.25f;

        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive) continue;
            if (insect.isFlying) continue;
            TauntEffect existing = insect.GetEffect<TauntEffect>();
            if (existing != null && existing.taunter != (IAttackable)this) continue;
            if (Vector3.Distance(transform.position, insect.transform.position) <= radius)
            {
                insect.ApplyEffect(new TauntEffect(insect, 0.5f, 1, owner, this) { tauntStrength = 3 });
                tauntedInsects.Add(insect);
            }
        }
    }

    public override void Kill()
    {
        ClearTaunts();
        NotifyGone();
        base.Kill();
    }

    public override void Kill(Entity killSource)
    {
        ClearTaunts();
        NotifyGone();
        base.Kill(killSource);
    }

    private void NotifyGone()
    {
        if (_goneNotified) return;
        _goneNotified = true;
        OnShieldGone?.Invoke();
    }

    private void ClearTaunts()
    {
        foreach (Insect insect in tauntedInsects)
        {
            if (insect != null && insect.GetEffect<TauntEffect>()?.taunter == (IAttackable)this)
                insect.RemoveEffect<TauntEffect>();
        }
        tauntedInsects.Clear();
    }
}
