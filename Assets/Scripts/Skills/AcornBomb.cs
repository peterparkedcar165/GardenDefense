using UnityEngine;
using System.Collections.Generic;

public class AcornBomb : Minion
{
    private float aoeRadius;
    private float damage;
    private bool hasLanded = false;
    private float fallVelocity = 0f;
    private Transform shadow;

    private const float startHeight = 20f;
    private const float gravityAccel = 9.8f;
    private const float stunDuration = 2f;

    private readonly HashSet<Insect> tauntedInsects = new HashSet<Insect>();
    private float tauntTickTimer = 0f;
    private static readonly DamageTag[] impactTags = { DamageTag.AoE, DamageTag.SkillDamage };

    private Collider2D _clickCollider;
    private SpriteRenderer _visualSR;
    private SpriteRenderer[] _outlineRenderers;
    private bool _isHovered;

    private GameObject _lifetimeBarInstance;
    private Transform _lifetimeBarFill;
    private float _lifetimeElapsed;

    public override string GetName() => "<b><color=green>Acorn</color></b>";
    public override string GetDescription()
    {
        if (!hasLanded) return "Blocks path while it lasts";
        int remaining = (int)Mathf.Max(0f, lifetime - _lifetimeElapsed);
        return $"Blocks path while it lasts\nDuration: {remaining} seconds";
    }

    // called by AcornSprout immediately after Instantiate
    public void Initialize(float aoeRadius, float damage, float lifespan, float maxHp, Plant source)
    {
        this.aoeRadius = aoeRadius;
        this.damage = damage;
        owner = source;
        lifetime = lifespan;
        baseMaxHealth = maxHp;
        baseArmor = source.armor;
        baseMovementSpeed = 0f;
        isFlying = true;

        visual = transform.Find("Visual");
        if (visual != null)
        {
            visual.localPosition = new Vector3(0f, startHeight, 0f);
            visual.localScale = new Vector3(aoeRadius, aoeRadius, 1f);
        }

        shadow = transform.Find("Shadow");
        if (shadow != null)
            shadow.localScale = new Vector3(0f, shadow.localScale.y, 1f);

        SetRangeCircle(aoeRadius);

        // collider is disabled until landing: no mouse events, no targeting while in air
        _clickCollider = GetComponent<Collider2D>();
        if (_clickCollider == null)
        {
            var col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = aoeRadius * 0.45f;
            col.isTrigger = true;
            _clickCollider = col;
        }
        _clickCollider.enabled = false;

        UpdateStats();
        health = maxHealth;
    }

    protected override void Start()
    {
        base.Start();

        // health bar stays on root, not visual (visual is tilted)
        if (healthBarInstance != null)
        {
            healthBarInstance.transform.SetParent(transform);
            healthBarInstance.transform.localPosition = new Vector3(-0.475f, 0.4f, 0f);
        }

        SpawnLifetimeBar();

        // hover/select outline: 8 silhouette sprites at 0.05 world-unit offset, same as plants
        _visualSR = GetComponentInChildren<SpriteRenderer>();
        if (_visualSR != null)
        {
            Shader silhouette = Shader.Find("Custom/SpriteSilhouette");
            Material mat = silhouette != null ? new Material(silhouette) : null;
            float localOffset = aoeRadius > 0f ? 0.05f / aoeRadius : 0.05f;
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
        // keep world health bar visible while selected in the panel
        if (PlantUpgradeUI.instance?.GetSelectedInsect() == this)
            ShowHealthBar();
    }

    void OnMouseDown()
    {
        if (SkillTargetingManager.instance != null && SkillTargetingManager.instance.IsTargeting) return;
        PlantUpgradeUI.instance?.ShowPanel(this);
    }

    public override void Attack() { }

    protected override void Update()
    {
        if (isDying) return;
        if (!hasLanded)
        {
            Fall();
            return;
        }

        base.Update();
        _lifetimeElapsed += Time.deltaTime;
        UpdateTaunt();

        // keep circle centered at ground position, not tilted visual
        if (rangeCircle != null)
            rangeCircle.position = transform.position;

        // outline: show while hovered or selected
        if (_outlineRenderers != null)
        {
            bool show = _isHovered || (PlantUpgradeUI.instance?.GetSelectedInsect() == this);
            if (_visualSR != null)
                foreach (var r in _outlineRenderers)
                    if (r != null) { r.sprite = _visualSR.sprite; r.enabled = show; }
        }

        // lifetime bar fill (drains left to right as time passes)
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

    private void Fall()
    {
        if (visual == null) return;
        fallVelocity += gravityAccel * Time.deltaTime;
        Vector3 pos = visual.localPosition;
        pos.y -= fallVelocity * Time.deltaTime;
        if (pos.y <= 0f)
        {
            pos.y = 0f;
            visual.localPosition = pos;
            if (shadow != null)
                shadow.localScale = new Vector3(0.8f * aoeRadius, 0.4f * aoeRadius, 1f);
            OnLand();
        }
        else
        {
            visual.localPosition = pos;
            if (shadow != null)
            {
                float t = 1f - (pos.y / startHeight);
                shadow.localScale = new Vector3(t * 0.8f * aoeRadius, t * 0.4f * aoeRadius, 1f);
            }
        }
    }

    private void OnLand()
    {
        hasLanded = true;
        isFlying = false;
        if (_clickCollider != null) _clickCollider.enabled = true;
        if (_lifetimeBarInstance != null) _lifetimeBarInstance.SetActive(true);

        // vanish immediately if the bomb landed on water
        if (IsOnWaterTile())
        {
            Kill();
            return;
        }

        foreach (Insect insect in new List<Insect>(Insect.allInsects))
        {
            if (insect == null || !insect.IsAlive) continue;
            if (Vector3.Distance(transform.position, insect.transform.position) <= aoeRadius)
            {
                insect.Damage(damage, owner.damageType, owner.elementalType, owner, false, impactTags);
                insect.ApplyEffect(new StunEffect(insect, stunDuration, 1, owner));
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
            if (Vector3.Distance(transform.position, insect.transform.position) <= aoeRadius)
            {
                insect.ApplyEffect(new TauntEffect(insect, 0.5f, 1, owner, this));
                tauntedInsects.Add(insect);
            }
        }
    }

    public override void Kill()
    {
        ClearTaunts();
        base.Kill();
    }

    public override void Kill(Entity killSource)
    {
        ClearTaunts();
        base.Kill(killSource);
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
